using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using CurioCatalogue.Core;

namespace CurioCatalogue.Adapters;

/// <summary>
/// Reads and writes books and invoices in their item's attributes, all under the subtree "curiocatalogue":
/// <code>
/// ledger:  { v: int, sheets: int, name: string, entries: [ { stack, count } ] }
/// album:   { v: int, sheets: int, name: string, album: { id: string, level: string, collected: [ { id, stack } ], unplaced: [ { stack } ] } }
/// invoice: { v: int, created: double, pos: { x, y, z }, wants: { id: string, level: string }, entries: [ { stack, count, member: string } ] }
/// </code>
/// <c>wants</c> and <c>member</c> only on a want list of format 2: the album's collection and level, and the member each line
/// names (its stack is the member's first item). A want list of format 1 (0.1) has neither: its lines are exact items.
/// <c>v</c> is the format version (<see cref="Version"/>); data without it is version 1. Data of a newer version (a book
/// from a newer build of the mod) is read as well as we can but never written: the actions refuse (<see cref="IsNewer"/>).
/// The book's kind and the invoice kind are not stored: the item's variant (its code) already says them.
/// Numbers are read with GetAsInt: attributes that come from JSON (a recipe, /giveitem) hold integers as longs,
/// and GetInt sees only ints (vsapi JsonObject.cs:376#GetInt).
/// Item keys are not stored either but recomputed from the stacks on every read, so a change of the ignored attributes in the
/// config applies to existing books. An album of format 1 (0.1: <c>album: { id, members: [ { stack } ] }</c>, a snapshot of its
/// collection, and the missing members as <c>entries</c>) is read as format 2 (<see cref="Album.FromFormat1"/>) and written as 2
/// the next time the server writes it.
/// </summary>
public sealed class LedgerStore
{
    public const string RootKey = "curiocatalogue";

    /// <summary>The newest format this build reads and writes. Raise it when a layout changes, and read the older ones.
    /// 2 (0.2): an album stores what it collected, by member (docs/decisions.md, Collections); a want list names its
    /// album's collection, level and members.</summary>
    public const int Version = 2;

    /// <summary>
    /// The version written with a kind of data: the oldest that has its layout, so an older build still reads what did not change
    /// for it (K7, H6 of the collections core plan). Ledgers, bills of materials, intake and pick lists, and want lists of 0.1 have
    /// the layout of 1, which 0.1.1 reads: a player who goes back to it keeps them.
    /// </summary>
    internal static int VersionOf(Book book) => book is Album ? 2 : 1;

    /// <inheritdoc cref="VersionOf(Book)"/>
    internal static int VersionOf(Invoice invoice) => invoice.From != null ? 2 : 1;

    private readonly StackKeys keys;
    private readonly System.Func<string, CollectionResolution> resolve;

    /// <param name="resolve">What the game says of a collection by id (found, off on this server, unknown): an album of format 1 is
    /// read by it, and its unplaced items are placed with it.</param>
    public LedgerStore(StackKeys keys, System.Func<string, CollectionResolution> resolve)
    {
        this.keys = keys;
        this.resolve = resolve;
    }

    /// <summary>For tests that only have collections: every id not given is unknown. None given: no collection is known.</summary>
    public LedgerStore(StackKeys keys, System.Func<string, DefinedCollection?>? collectionOf = null)
        : this(keys, id => collectionOf?.Invoke(id) is { } found ? new CollectionResolution.Found(found) : new CollectionResolution.Unknown(id))
    {
    }

    private DefinedCollection? Known(string id) => resolve(id).Collection;

    /// <summary>One container slot as a core entry: its key, its size, and a copy of one item as the sample.</summary>
    public Entry ToEntry(ItemStack stack)
    {
        var sample = stack.Clone();
        sample.StackSize = 1;
        return new Entry(keys.KeyOf(stack), stack.StackSize, sample);
    }

    /// <summary>Only the name, without reading the entries: cheap enough for the item name shown every frame.</summary>
    public static string ReadName(ItemStack book) =>
        book.Attributes.GetTreeAttribute(RootKey)?.GetString("name", "") ?? "";

    /// <summary>An album's collection id, cheaply (for its shown name); empty for a ledger. The same key in both formats.</summary>
    public static string ReadCollectionId(ItemStack book) =>
        book.Attributes.GetTreeAttribute(RootKey)?.GetTreeAttribute("album")?.GetString("id", "") ?? "";

    /// <summary>The collection a want list of format 2 was made for, cheaply (for the hints over a container); empty for format 1 or
    /// any other invoice.</summary>
    public static string ReadWantedCollectionId(ItemStack paper) =>
        paper.Attributes.GetTreeAttribute(RootKey)?.GetTreeAttribute("wants")?.GetString("id", "") ?? "";

    /// <summary>
    /// A cheap hash of the book's or invoice's data, the same on client and server: the client gets the attributes as the
    /// server's bytes, and a tree keeps its keys in insertion order (vsapi TreeAttribute.cs:97, ConcurrentSmallDictionary).
    /// Windows use it to notice changes and to name the item a request is about.
    /// </summary>
    public static int Fingerprint(ItemStack stack)
    {
        if (stack.Attributes.GetTreeAttribute(RootKey) is not TreeAttribute tree) return 0;
        unchecked
        {
            int hash = (int)2166136261;
            foreach (byte b in tree.ToBytes()) hash = (hash ^ b) * 16777619;
            return hash;
        }
    }

    /// <summary>Is the stack's data from a newer build of the mod? Then it must not be written (the layout is unknown).</summary>
    public static bool IsNewer(ItemStack? stack) =>
        stack?.Attributes.GetTreeAttribute(RootKey)?.GetAsInt("v", 1) > Version;

    /// <summary>The book in the stack, as its kind says: an album for <see cref="LedgerMode.Album"/>, else a ledger.</summary>
    public Book ReadBook(ItemStack book, LedgerMode mode) =>
        mode == LedgerMode.Album ? ReadAlbum(book) : ReadLedger(book, mode);

    public void WriteBook(ItemStack stack, Book book)
    {
        switch (book)
        {
            case Ledger ledger: WriteLedger(stack, ledger); break;
            case Album album: WriteAlbum(stack, album); break;
            default: throw ExhaustiveMatching.ExhaustiveMatch.Failed(book);
        }
    }

    /// <summary>A ledger or a bill of materials; an album is read by <see cref="ReadAlbum"/>.</summary>
    public Ledger ReadLedger(ItemStack book, LedgerMode mode)
    {
        if (mode == LedgerMode.Album) throw new System.ArgumentException("An album is read by ReadAlbum");
        var root = book.Attributes.GetTreeAttribute(RootKey);
        if (root == null) return Ledger.Empty(mode, 0);
        return Ledger.FromStored(mode, root.GetAsInt("sheets"), ReadEntries(root), root.GetString("name", ""));
    }

    public void WriteLedger(ItemStack book, Ledger ledger)
    {
        var root = NewRoot(ledger);
        // Sorted so that the same ledger always gives the same bytes.
        WriteEntries(root, ledger.Entries.Values.OrderBy(e => e.Key.ToString(), System.StringComparer.Ordinal));
        book.Attributes[RootKey] = root;
    }

    /// <summary>
    /// An album, of either format. Its unplaced items of format 1 are placed when its collection is known (nothing is lost while it
    /// is not). An album item without our data (the creative inventory) reads as an album of no collection.
    /// </summary>
    public Album ReadAlbum(ItemStack book)
    {
        var root = book.Attributes.GetTreeAttribute(RootKey);
        var albumTree = root?.GetTreeAttribute("album");
        int sheets = root?.GetAsInt("sheets") ?? 0;
        string name = root?.GetString("name", "") ?? "";
        string id = albumTree?.GetString("id", "") ?? "";
        var collection = id.Length > 0 ? Known(id) : null;

        if (root == null || root.GetAsInt("v", 1) < 2)
        {
            // Format 1: the snapshot, re-keyed like the entries (so the two agree), and the missing entries.
            var missing = root == null ? [] : ReadEntries(root).Select(e => e.Key).ToList();
            return Album.FromFormat1(id, sheets, name, albumTree == null ? [] : ReadSnapshot(albumTree), missing, collection);
        }

        var collected = Album.NoneCollected.ToBuilder();
        foreach (var line in Lines(albumTree, "collected"))
        {
            var stack = line.GetItemstack("stack");
            var member = line.GetString("id");
            if (stack == null || string.IsNullOrEmpty(member)) continue; // damaged line: drop it rather than fail the whole album
            collected.TryAdd(member, new FilledBy(keys.KeyOf(stack), stack));
        }
        var unplaced = Lines(albumTree, "unplaced").Select(line => line.GetItemstack("stack")).OfType<ItemStack>()
            .Select(stack => new FilledBy(keys.KeyOf(stack), stack)).ToImmutableList();
        string? level = albumTree?.GetString("level") is { Length: > 0 } l ? l : null;

        var album = new Album(id, level, collected.ToImmutable(), unplaced, sheets, name);
        return collection != null ? album.Place(collection) : album;
    }

    public void WriteAlbum(ItemStack book, Album album)
    {
        var root = NewRoot(album);
        var tree = root.GetOrAddTreeAttribute("album");
        tree.SetString("id", album.CollectionId);
        if (album.Level != null) tree.SetString("level", album.Level);
        // By member id (Collected is sorted), so that the same album always gives the same bytes.
        tree["collected"] = new TreeArrayAttribute(album.Collected.Select(c =>
        {
            var line = Line(c.Value.Stack);
            line.SetString("id", c.Key);
            return line;
        }).ToArray());
        if (!album.Unplaced.IsEmpty) tree["unplaced"] = new TreeArrayAttribute(album.Unplaced.Select(u => Line(u.Stack)).ToArray());
        book.Attributes[RootKey] = root;
    }

    /// <summary>The collection an album follows; null for a collection the game does not have.</summary>
    public DefinedCollection? CollectionOf(Album album) => Known(album.CollectionId);

    /// <summary>What the game says of the album's collection: there, off on this server, or unknown.</summary>
    public CollectionResolution Resolve(Album album) => resolve(album.CollectionId);

    /// <summary>The same by the collection's id, for what needs no more than the id (the hints over a container).</summary>
    public CollectionResolution Resolve(string collectionId) => resolve(collectionId);

    public Invoice ReadInvoice(ItemStack paper, InvoiceKind kind)
    {
        var root = paper.Attributes.GetTreeAttribute(RootKey);
        if (root == null) return new Invoice(kind, [], 0, new BlockPosition(0, 0, 0));

        var pos = root.GetTreeAttribute("pos");
        var position = pos == null
            ? new BlockPosition(0, 0, 0)
            : new BlockPosition(pos.GetAsInt("x"), pos.GetAsInt("y"), pos.GetAsInt("z"));

        var wants = root.GetTreeAttribute("wants");
        var from = wants?.GetString("id") is { Length: > 0 } id && wants.GetString("level") is { Length: > 0 } level
            ? new WantedFrom(id, level)
            : null;

        // Invoice lines keep their order; equal keys stay separate lines, they are summed when posted.
        return new Invoice(kind, ReadEntries(root).ToImmutableArray(), root.GetDouble("created"), position, from);
    }

    /// <summary>The level a want list of format 2 names, in its collection as the game has it now; null for format 1, any other
    /// invoice, or a collection or level the game does not have (then its lines want exactly their items).</summary>
    public CollectionLevel? LevelOf(Invoice invoice) => WantedIn(invoice)?.Level;

    /// <summary>As <see cref="LevelOf"/>, with the collection (for what a member leaves free, <see cref="DefinedCollection.Free"/>).</summary>
    public (DefinedCollection Collection, CollectionLevel Level)? WantedIn(Invoice invoice) =>
        invoice.From is { } from && Known(from.CollectionId) is { } collection && from.LevelIn(collection) is { } level
            ? (collection, level)
            : null;

    public void WriteInvoice(ItemStack paper, Invoice invoice)
    {
        var root = new TreeAttribute();
        root.SetInt("v", VersionOf(invoice));
        root.SetDouble("created", invoice.CreatedTotalDays);
        var pos = root.GetOrAddTreeAttribute("pos");
        pos.SetInt("x", invoice.Position.X);
        pos.SetInt("y", invoice.Position.Y);
        pos.SetInt("z", invoice.Position.Z);
        if (invoice.From is { } from)
        {
            var wants = root.GetOrAddTreeAttribute("wants");
            wants.SetString("id", from.CollectionId);
            wants.SetString("level", from.Level);
        }
        WriteEntries(root, invoice.Entries);
        paper.Attributes[RootKey] = root;
    }

    private static TreeAttribute NewRoot(Book book)
    {
        var root = new TreeAttribute();
        root.SetInt("v", VersionOf(book));
        root.SetInt("sheets", book.Sheets);
        if (book.Name.Length > 0) root.SetString("name", book.Name);
        return root;
    }

    private static IEnumerable<ITreeAttribute> Lines(ITreeAttribute? tree, string key) =>
        tree?[key] is TreeArrayAttribute { value: { } array } ? array : [];

    private IEnumerable<Entry> ReadEntries(ITreeAttribute root)
    {
        foreach (var line in Lines(root, "entries"))
        {
            var stack = line.GetItemstack("stack");
            int count = line.GetAsInt("count");
            if (stack == null || count <= 0) continue; // damaged line: drop it rather than fail the whole ledger
            yield return new Entry(keys.KeyOf(stack), count, stack, Member: line.GetString("member") is { Length: > 0 } m ? m : null);
        }
    }

    /// <summary>The snapshot of an album of format 1, re-keyed; null when it has none or an old one of key strings (development
    /// builds before 0.1.0: then the album counts as made from the current collection, <see cref="Album.FromFormat1"/>).</summary>
    private IReadOnlyList<FilledBy>? ReadSnapshot(ITreeAttribute album)
    {
        if (album["members"] is not TreeArrayAttribute { value: { } array }) return null;
        return array
            .Select(line => line.GetItemstack("stack"))
            .OfType<ItemStack>() // damaged line: as a member the collection no longer has
            .Select(stack => new FilledBy(keys.KeyOf(stack), stack))
            .ToList();
    }

    private static void WriteEntries(ITreeAttribute root, IEnumerable<Entry> entries)
    {
        var lines = entries.Select(e =>
        {
            var line = Line(e.Sample);
            line.SetInt("count", e.Count);
            if (e.Member != null) line.SetString("member", e.Member);
            return line;
        }).ToArray();
        root["entries"] = new TreeArrayAttribute(lines);
    }

    private static TreeAttribute Line(object sample)
    {
        var line = new TreeAttribute();
        // A copy: the attribute keeps a reference, and the sample may be shared with an invoice or a collection.
        line.SetItemstack("stack", ((ItemStack)sample).Clone());
        return line;
    }
}
