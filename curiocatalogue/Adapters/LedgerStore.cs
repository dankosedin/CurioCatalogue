using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using CurioCatalogue.Core;

namespace CurioCatalogue.Adapters;

/// <summary>
/// Reads and writes ledgers and invoices in their item's attributes, all under the subtree "curiocatalogue":
/// <code>
/// ledger:  { v: int, sheets: int, name: string, album: { id: string, members: [ { stack } ] }, entries: [ { stack, count } ] }
/// invoice: { v: int, created: double, pos: { x, y, z }, entries: [ { stack, count } ] }
/// </code>
/// <c>v</c> is the format version (<see cref="Version"/>); data without it is version 1. Data of a newer version (a book
/// from a newer build of the mod) is read as well as we can but never written: the actions refuse (<see cref="IsNewer"/>).
/// The ledger mode and the invoice kind are not stored: the item's variant (its code) already says them.
/// Numbers are read with GetAsInt: attributes that come from JSON (a recipe, /giveitem) hold integers as longs,
/// and GetInt sees only ints (vsapi JsonObject.cs:376#GetInt).
/// Item keys are not stored either but recomputed from the sample stacks on every read, so a change of the
/// ignored attributes in the config applies to existing ledgers. The album's snapshot of its collection is kept as samples
/// too, so it is re-keyed the same way and always agrees with the entries.
/// </summary>
public sealed class LedgerStore
{
    public const string RootKey = "curiocatalogue";

    /// <summary>The format this build writes. Raise it when the layout changes, and read the older ones.</summary>
    public const int Version = 1;

    private readonly StackKeys keys;
    private readonly System.Func<string, Collection?> collectionOf;

    /// <param name="collectionOf">The collection by id, so that an album read is always up to date with it
    /// (Collection.RefreshIfStale); none in tests that do not care.</param>
    public LedgerStore(StackKeys keys, System.Func<string, Collection?>? collectionOf = null)
    {
        this.keys = keys;
        this.collectionOf = collectionOf ?? (_ => null);
    }

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

    /// <summary>An album's collection id, cheaply (for its shown name); empty for a ledger.</summary>
    public static string ReadCollectionId(ItemStack book) =>
        book.Attributes.GetTreeAttribute(RootKey)?.GetTreeAttribute("album")?.GetString("id", "") ?? "";

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

    public Ledger ReadLedger(ItemStack book, LedgerMode mode)
    {
        var root = book.Attributes.GetTreeAttribute(RootKey);
        if (root == null) return Ledger.Empty(mode, 0);

        var albumTree = root.GetTreeAttribute("album");
        var album = albumTree == null
            ? null
            : new AlbumInfo(albumTree.GetString("id", ""), ReadSnapshot(albumTree));

        var ledger = Ledger.FromStored(mode, root.GetAsInt("sheets"), ReadEntries(root), root.GetString("name", ""), album);

        // An album follows its collection by itself (domain.md, Albums and collections): read as it should be now. The stored data catches
        // up when the server next writes the album (taking new items, making a want list).
        return CollectionOf(ledger) is { } collection ? collection.RefreshIfStale(ledger) : ledger;
    }

    /// <summary>The collection an album follows; null for a ledger or a collection the game no longer has.</summary>
    public Collection? CollectionOf(Ledger ledger) => ledger.Album is { } info ? collectionOf(info.CollectionId) : null;

    public void WriteLedger(ItemStack book, Ledger ledger)
    {
        var root = new TreeAttribute();
        root.SetInt("v", Version);
        root.SetInt("sheets", ledger.Sheets);
        if (ledger.Name.Length > 0) root.SetString("name", ledger.Name);
        if (ledger.Album != null)
        {
            var album = root.GetOrAddTreeAttribute("album");
            album.SetString("id", ledger.Album.CollectionId);
            if (ledger.Album.Members is { } members) album["members"] = new TreeArrayAttribute(members.Select(m => Line(m.Sample)).ToArray());
        }
        // Sorted so that the same ledger always gives the same bytes.
        WriteEntries(root, ledger.Entries.Values.OrderBy(e => e.Key.ToString(), System.StringComparer.Ordinal));
        book.Attributes[RootKey] = root;
    }

    public Invoice ReadInvoice(ItemStack paper, InvoiceKind kind)
    {
        var root = paper.Attributes.GetTreeAttribute(RootKey);
        if (root == null) return new Invoice(kind, [], 0, new BlockPosition(0, 0, 0));

        var pos = root.GetTreeAttribute("pos");
        var position = pos == null
            ? new BlockPosition(0, 0, 0)
            : new BlockPosition(pos.GetAsInt("x"), pos.GetAsInt("y"), pos.GetAsInt("z"));

        // Invoice lines keep their order; equal keys stay separate lines, they are summed when posted.
        return new Invoice(kind, ReadEntries(root).ToImmutableArray(), root.GetDouble("created"), position);
    }

    public void WriteInvoice(ItemStack paper, Invoice invoice)
    {
        var root = new TreeAttribute();
        root.SetInt("v", Version);
        root.SetDouble("created", invoice.CreatedTotalDays);
        var pos = root.GetOrAddTreeAttribute("pos");
        pos.SetInt("x", invoice.Position.X);
        pos.SetInt("y", invoice.Position.Y);
        pos.SetInt("z", invoice.Position.Z);
        WriteEntries(root, invoice.Entries);
        paper.Attributes[RootKey] = root;
    }

    private IEnumerable<Entry> ReadEntries(ITreeAttribute root)
    {
        if (root["entries"] is not TreeArrayAttribute array || array.value == null) yield break;
        foreach (var line in array.value)
        {
            var stack = line.GetItemstack("stack");
            int count = line.GetAsInt("count");
            if (stack == null || count <= 0) continue; // damaged line: drop it rather than fail the whole ledger
            yield return new Entry(keys.KeyOf(stack), count, stack);
        }
    }

    /// <summary>The album's snapshot, re-keyed; null when it has none or an old one of key strings (then the album counts
    /// as made from the current collection, Collection.Refresh).</summary>
    private IReadOnlyList<SnapshotMember>? ReadSnapshot(ITreeAttribute album)
    {
        if (album["members"] is not TreeArrayAttribute array || array.value == null) return null;
        return array.value
            .Select(line => line.GetItemstack("stack"))
            .Where(stack => stack != null) // damaged line: as a member the collection no longer has
            .Select(stack => new SnapshotMember(keys.KeyOf(stack), stack))
            .ToList();
    }

    private static void WriteEntries(ITreeAttribute root, IEnumerable<Entry> entries)
    {
        var lines = entries.Select(e =>
        {
            var line = Line(e.Sample);
            line.SetInt("count", e.Count);
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
