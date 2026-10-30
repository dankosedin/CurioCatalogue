using System.Collections.Immutable;
using Vintagestory.API.Common;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;

namespace CurioCatalogue.Tests;

public class LedgerStoreTests
{
    private readonly GameWorld game = new();
    private readonly LedgerStore store;
    private readonly Item ledgerItem;
    private readonly Item invoiceItem;
    private readonly Block tapestry;
    private readonly Item ingot;

    public LedgerStoreTests()
    {
        store = new LedgerStore(new StackKeys(game.World, ["durability"]));
        ledgerItem = game.Item("curiocatalogue:book-materials-normal-teal");
        invoiceItem = game.Item("curiocatalogue:invoice-incoming");
        tapestry = game.Block("tapestry-north");
        ingot = game.Item("ingot-iron");
    }

    private ItemStack Tapestry(string type, int size = 1)
    {
        var stack = new ItemStack(tapestry, size);
        stack.Attributes.SetString("type", type);
        return stack;
    }

    // Saving a world (or sending an item to a client) writes the stack as bytes; read it back like the game does.
    private static ItemStack ThroughBytes(ItemStack stack) => new(stack.ToBytes());

    private static (string, int)[] Lines(IEnumerable<Entry> entries) =>
        entries.Select(e => (e.Key.ToString(), e.Count)).OrderBy(l => l.Item1).ToArray();

    [Fact]
    public void Container_slot_becomes_an_entry_with_a_single_item_sample()
    {
        var slot = Tapestry("ambush1", 5);
        var entry = store.ToEntry(slot);

        Assert.Equal(5, entry.Count);
        var sample = Assert.IsType<ItemStack>(entry.Sample);
        Assert.Equal(1, sample.StackSize);
        Assert.Equal(5, slot.StackSize); // the container's stack is untouched
    }

    [Fact]
    public void Ledger_survives_a_round_trip_through_bytes()
    {
        var ledger = Ledger.FromStored(LedgerMode.Counts, 27,
            [store.ToEntry(Tapestry("ambush1", 2)), store.ToEntry(Tapestry("salvation11")), store.ToEntry(new ItemStack(ingot, 40))],
            name: "Tapestries", album: new AlbumInfo("tapestries"));

        var book = new ItemStack(ledgerItem);
        store.WriteLedger(book, ledger);
        var back = store.ReadLedger(ThroughBytes(book), LedgerMode.Counts);

        Assert.Equal(27, back.Sheets);
        Assert.Equal("Tapestries", back.Name);
        Assert.Equal(new AlbumInfo("tapestries"), back.Album);
        Assert.Equal(Lines(ledger.Entries.Values), Lines(back.Entries.Values));
        Assert.All(back.Entries.Values, e => Assert.IsType<ItemStack>(e.Sample));
    }

    [Fact]
    public void Ordinary_ledger_has_no_name_and_no_album()
    {
        var book = new ItemStack(ledgerItem);
        store.WriteLedger(book, Ledger.Empty(LedgerMode.Types, 8));
        var back = store.ReadLedger(ThroughBytes(book), LedgerMode.Types);

        Assert.Equal("", back.Name);
        Assert.Null(back.Album);
        Assert.True(back.IsEmpty);
    }

    [Fact]
    public void Same_ledger_always_gives_the_same_bytes()
    {
        var a = Tapestry("a");
        var b = Tapestry("b");
        var one = Ledger.FromStored(LedgerMode.Types, 1, [store.ToEntry(a), store.ToEntry(b)]);
        var other = Ledger.FromStored(LedgerMode.Types, 1, [store.ToEntry(b), store.ToEntry(a)]);

        var bookOne = new ItemStack(ledgerItem);
        var bookOther = new ItemStack(ledgerItem);
        store.WriteLedger(bookOne, one);
        store.WriteLedger(bookOther, other);

        Assert.Equal(bookOne.ToBytes(), bookOther.ToBytes());
    }

    [Fact]
    public void Album_member_snapshot_survives_a_round_trip()
    {
        var book = new ItemStack(ledgerItem);
        var members = new[] { Tapestry("a"), Tapestry("b") }.Select(s => new SnapshotMember(store.ToEntry(s).Key, s)).ToList();
        store.WriteLedger(book, Ledger.FromStored(LedgerMode.Album, 1, [], album: new AlbumInfo("tapestries", members)));

        var back = store.ReadLedger(ThroughBytes(book), LedgerMode.Album).Album!;
        Assert.Equal("tapestries", back.CollectionId);
        Assert.Equal(members.Select(m => m.Key), back.Members!.Select(m => m.Key));
        Assert.All(back.Members!, m => Assert.IsType<ItemStack>(m.Sample));
    }

    [Fact]
    public void A_album_is_read_up_to_date_with_its_collection()
    {
        // Made when the collection had a1, a2; a1 collected. Now the collection also has b1.
        Collection Of(params string[] types) => new("tapestries",
            types.Select(t => new CollectionMember(store.ToEntry(Tapestry(t)).Key, t, Tapestry(t))));
        var album = Of("a1", "a2").NewAlbum(4, "");
        album = album.CrossOff([store.ToEntry(Tapestry("a1")).Key]);
        var book = new ItemStack(ledgerItem);
        store.WriteLedger(book, album);

        var now = Of("a1", "a2", "b1");
        var reading = new LedgerStore(new StackKeys(game.World, ["durability"]), id => id == "tapestries" ? now : null);
        var back = reading.ReadLedger(ThroughBytes(book), LedgerMode.Album);

        Assert.Equal(2, back.Entries.Count); // a2 still missing, b1 new; a1 stays collected
        Assert.Equal(new CollectionProgress(1, 3, 1, 3), now.Progress(back.Entries.Keys));
    }

    [Fact]
    public void Album_keeps_its_progress_when_the_ignored_attributes_change()
    {
        // The snapshot is re-keyed on read like the entries, with the same config. Kept as key strings, it reset every
        // album to nothing collected when a key's text changed (code review 28.10.2026).
        ItemStack Worn(string type)
        {
            var stack = Tapestry(type);
            stack.Attributes.SetInt("durability", 5);
            return stack;
        }
        Collection Of(LedgerStore by) => new("tapestries",
            new[] { "a1", "a2" }.Select(t => new CollectionMember(by.ToEntry(Worn(t)).Key, t, Worn(t))));

        var strict = new LedgerStore(new StackKeys(game.World, []));
        var album = Of(strict).NewAlbum(4, "").CrossOff([strict.ToEntry(Worn("a1")).Key]);
        var book = new ItemStack(ledgerItem);
        strict.WriteLedger(book, album);

        var now = Of(store);
        var reading = new LedgerStore(new StackKeys(game.World, ["durability"]), id => id == "tapestries" ? now : null);
        var back = reading.ReadLedger(ThroughBytes(book), LedgerMode.Album);

        Assert.Equal([reading.ToEntry(Worn("a2")).Key], back.Entries.Keys); // a1 stays collected
    }

    [Fact]
    public void Album_from_before_the_sample_snapshot_counts_as_made_from_the_current_collection()
    {
        // Albums of the development builds kept the snapshot as key strings.
        var book = new ItemStack(ledgerItem);
        store.WriteLedger(book, Ledger.FromStored(LedgerMode.Album, 1, [store.ToEntry(Tapestry("a2"))], album: new AlbumInfo("tapestries")));
        book.Attributes.GetTreeAttribute(LedgerStore.RootKey).GetTreeAttribute("album")["members"] =
            new Vintagestory.API.Datastructures.StringArrayAttribute(["block:game:tapestry-north{\"type\":\"a1\"}"]);

        Assert.Null(store.ReadLedger(ThroughBytes(book), LedgerMode.Album).Album!.Members);
    }

    // --- Format version ---

    [Fact]
    public void Books_and_invoices_are_written_with_the_format_version()
    {
        var book = new ItemStack(ledgerItem);
        store.WriteLedger(book, Ledger.Empty(LedgerMode.Types, 2));
        var paper = new ItemStack(invoiceItem);
        store.WriteInvoice(paper, new Invoice(InvoiceKind.Incoming, [], 0, new BlockPosition(0, 0, 0)));

        Assert.Equal(LedgerStore.Version, book.Attributes.GetTreeAttribute(LedgerStore.RootKey).GetAsInt("v"));
        Assert.Equal(LedgerStore.Version, paper.Attributes.GetTreeAttribute(LedgerStore.RootKey).GetAsInt("v"));
    }

    [Fact]
    public void Only_data_of_a_newer_version_is_newer()
    {
        var current = new ItemStack(ledgerItem);
        store.WriteLedger(current, Ledger.Empty(LedgerMode.Types, 2));
        var unversioned = new ItemStack(ledgerItem); // before versions, or without our data at all
        unversioned.Attributes.GetOrAddTreeAttribute(LedgerStore.RootKey).SetInt("sheets", 2);
        var newer = current.Clone();
        newer.Attributes.GetTreeAttribute(LedgerStore.RootKey).SetInt("v", LedgerStore.Version + 1);

        Assert.False(LedgerStore.IsNewer(current));
        Assert.False(LedgerStore.IsNewer(unversioned));
        Assert.False(LedgerStore.IsNewer(new ItemStack(ledgerItem)));
        Assert.False(LedgerStore.IsNewer(null));
        Assert.True(LedgerStore.IsNewer(ThroughBytes(newer)));
    }

    [Fact]
    public void Numbers_from_json_read_although_they_are_longs()
    {
        // Assumption: JSON integers become LongAttribute in item attributes (vsapi JsonObject.cs:376#GetInt, JsonObject.cs:426#LongAttribute).
        var book = new ItemStack(ledgerItem);
        book.Attributes.GetOrAddTreeAttribute(LedgerStore.RootKey).SetLong("sheets", 8);
        Assert.Equal(8, store.ReadLedger(book, LedgerMode.Types).Sheets);
    }

    [Fact]
    public void Item_without_our_data_reads_as_an_empty_ledger()
    {
        // E.g. a ledger taken from the creative inventory.
        var back = store.ReadLedger(new ItemStack(ledgerItem), LedgerMode.Types);
        Assert.True(back.IsEmpty);
        Assert.Equal(0, back.Sheets);
    }

    [Fact]
    public void Writing_replaces_the_old_data()
    {
        var book = new ItemStack(ledgerItem);
        store.WriteLedger(book, Ledger.FromStored(LedgerMode.Types, 3, [store.ToEntry(Tapestry("a"))]));
        store.WriteLedger(book, Ledger.Empty(LedgerMode.Types, 2));

        var back = store.ReadLedger(book, LedgerMode.Types);
        Assert.True(back.IsEmpty);
        Assert.Equal(2, back.Sheets);
    }

    [Fact]
    public void Changed_ignored_attributes_merge_entries_on_read()
    {
        // Written while "durability" counted, read with it ignored: two pickaxes become one item.
        var pickaxe = game.Item("pickaxe-iron");
        var fresh = new ItemStack(pickaxe);
        var worn = new ItemStack(pickaxe);
        worn.Attributes.SetInt("durability", 3);

        var strict = new LedgerStore(new StackKeys(game.World, []));
        var book = new ItemStack(ledgerItem);
        strict.WriteLedger(book, Ledger.FromStored(LedgerMode.Counts, 0, [strict.ToEntry(fresh), strict.ToEntry(worn)]));
        Assert.Equal(2, strict.ReadLedger(book, LedgerMode.Counts).Entries.Count);

        var back = store.ReadLedger(ThroughBytes(book), LedgerMode.Counts);
        Assert.Equal([("item:game:pickaxe-iron", 2)], Lines(back.Entries.Values));
    }

    [Fact]
    public void Entry_of_a_removed_mod_is_kept()
    {
        var book = new ItemStack(ledgerItem);
        var gone = new ItemStack { Class = EnumItemClass.Item, Id = 999, StackSize = 1 };
        store.WriteLedger(book, Ledger.FromStored(LedgerMode.Types, 0, [store.ToEntry(gone)]));

        var back = store.ReadLedger(ThroughBytes(book), LedgerMode.Types);
        Assert.Equal([("item:#999", 1)], Lines(back.Entries.Values));
    }

    [Fact]
    public void Invoice_survives_a_round_trip_and_keeps_line_order()
    {
        var invoice = new Invoice(InvoiceKind.Incoming,
            [store.ToEntry(Tapestry("b", 2)), store.ToEntry(new ItemStack(ingot, 7)), store.ToEntry(Tapestry("a"))],
            CreatedTotalDays: 123.25, Position: new BlockPosition(512000, 110, -3));

        var paper = new ItemStack(invoiceItem);
        store.WriteInvoice(paper, invoice);
        var back = store.ReadInvoice(ThroughBytes(paper), InvoiceKind.Incoming);

        Assert.Equal(InvoiceKind.Incoming, back.Kind);
        Assert.Equal(123.25, back.CreatedTotalDays);
        Assert.Equal(new BlockPosition(512000, 110, -3), back.Position);
        Assert.Equal(
            invoice.Entries.Select(e => (e.Key, e.Count)),
            back.Entries.Select(e => (e.Key, e.Count)));
    }

    [Fact]
    public void Ledger_read_back_still_compares_with_container_items()
    {
        // The point of it all: keys from a stored ledger match keys of fresh stacks in a chest.
        var book = new ItemStack(ledgerItem);
        store.WriteLedger(book, Ledger.FromStored(LedgerMode.Types, 1, [store.ToEntry(Tapestry("ambush1"))]));
        var ledger = store.ReadLedger(ThroughBytes(book), LedgerMode.Types);

        var result = ledger.CreateInvoice(InvoiceKind.Incoming,
            [store.ToEntry(Tapestry("ambush1", 2)), store.ToEntry(Tapestry("ambush2"))],
            new LedgerLimits(32, 500), 0, new BlockPosition(0, 0, 0));

        var created = Assert.IsType<CreateInvoiceResult.Created>(result);
        Assert.Equal([("block:game:tapestry-north{\"type\":\"ambush2\"}", 1)], Lines(created.Invoice.Entries));
    }
}
