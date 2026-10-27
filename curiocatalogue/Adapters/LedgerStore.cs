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
/// ledger:  { sheets: int, name: string, album: { id: string, size: int, members: [string] }, entries: [ { stack, count } ] }
/// invoice: { created: double, pos: { x, y, z }, entries: [ { stack, count } ] }
/// </code>
/// The ledger mode and the invoice kind are not stored: the item's variant (its code) already says them.
/// Numbers are read with GetAsInt: attributes that come from JSON (a recipe, /giveitem) hold integers as longs,
/// and GetInt sees only ints (vsapi JsonObject.cs:376#GetInt).
/// Item keys are not stored either but recomputed from the sample stacks on every read, so a change of the
/// ignored attributes in the config applies to existing ledgers.
/// </summary>
public sealed class LedgerStore
{
    public const string RootKey = "curiocatalogue";

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

    public Ledger ReadLedger(ItemStack book, LedgerMode mode)
    {
        var root = book.Attributes.GetTreeAttribute(RootKey);
        if (root == null) return Ledger.Empty(mode, 0);

        var albumTree = root.GetTreeAttribute("album");
        var album = albumTree == null
            ? null
            : new AlbumInfo(albumTree.GetString("id", ""), albumTree.GetAsInt("size"),
                (albumTree["members"] as StringArrayAttribute)?.value);

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
        root.SetInt("sheets", ledger.Sheets);
        if (ledger.Name.Length > 0) root.SetString("name", ledger.Name);
        if (ledger.Album != null)
        {
            var album = root.GetOrAddTreeAttribute("album");
            album.SetString("id", ledger.Album.CollectionId);
            album.SetInt("size", ledger.Album.InitialSize);
            if (ledger.Album.Members is { } members) album["members"] = new StringArrayAttribute(members.ToArray());
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

    private static void WriteEntries(ITreeAttribute root, IEnumerable<Entry> entries)
    {
        var lines = entries.Select(e =>
        {
            var line = new TreeAttribute();
            // A copy: the attribute keeps a reference, and the sample may be shared with an invoice.
            line.SetItemstack("stack", ((ItemStack)e.Sample).Clone());
            line.SetInt("count", e.Count);
            return line;
        }).ToArray();
        root["entries"] = new TreeArrayAttribute(lines);
    }
}
