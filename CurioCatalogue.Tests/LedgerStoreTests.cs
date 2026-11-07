using System.Collections.Immutable;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
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
            name: "Tapestries");

        var book = new ItemStack(ledgerItem);
        store.WriteLedger(book, ledger);
        var back = store.ReadLedger(ThroughBytes(book), LedgerMode.Counts);

        Assert.Equal(27, back.Sheets);
        Assert.Equal("Tapestries", back.Name);
        Assert.Equal(Lines(ledger.Entries.Values), Lines(back.Entries.Values));
        Assert.All(back.Entries.Values, e => Assert.IsType<ItemStack>(e.Sample));
    }

    [Fact]
    public void Ordinary_ledger_has_no_name()
    {
        var book = new ItemStack(ledgerItem);
        store.WriteLedger(book, Ledger.Empty(LedgerMode.Types, 8));
        var back = store.ReadLedger(ThroughBytes(book), LedgerMode.Types);

        Assert.Equal("", back.Name);
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

    // --- Albums: format 2 (what was collected, by member) and format 1 (0.1: a snapshot and the missing entries) ---

    private Item? albumItem;

    private ItemStack AlbumItem() => new(albumItem ??= game.Item("curiocatalogue:book-album-normal-teal", new ItemLedger()));

    private DefinedCollection Pieces(params string[] pieces) => TestCollections.Tapestries(new StackKeys(game.World, ["durability"]), t => Tapestry(t), pieces);

    private LedgerStore Reading(DefinedCollection? collection, string[]? ignored = null) =>
        new(new StackKeys(game.World, ignored ?? ["durability"]), id => id == "tapestries" ? collection : null);

    private FilledBy By(ItemStack stack) => new(store.ToEntry(stack).Key, stack);

    /// <summary>An album as 0.1 wrote it (LedgerStore of 0.1.1): v 1, the snapshot of the members under album.members, the missing
    /// members as entries of count 1.</summary>
    private static ItemStack Format1(ItemStack book, int sheets, string name, IEnumerable<ItemStack> snapshot, IEnumerable<ItemStack> missing)
    {
        static TreeAttribute Line(ItemStack stack, int? count = null)
        {
            var line = new TreeAttribute();
            line.SetItemstack("stack", stack.Clone());
            if (count is { } c) line.SetInt("count", c);
            return line;
        }
        var root = new TreeAttribute();
        root.SetInt("v", 1);
        root.SetInt("sheets", sheets);
        if (name.Length > 0) root.SetString("name", name);
        var album = root.GetOrAddTreeAttribute("album");
        album.SetString("id", "tapestries");
        album["members"] = new TreeArrayAttribute(snapshot.Select(s => Line(s)).ToArray());
        root["entries"] = new TreeArrayAttribute(missing.Select(s => Line(s, 1)).ToArray());
        book.Attributes[LedgerStore.RootKey] = root;
        return book;
    }

    [Fact]
    public void Album_survives_a_round_trip_through_bytes()
    {
        var album = new Album("tapestries", "pieces", Album.NoneCollected, [By(Tapestry("gone1"))], 7, "Мої гобелени")
            .Collect([("b", By(Tapestry("b"))), ("a", By(Tapestry("a")))]);
        var book = AlbumItem();
        store.WriteAlbum(book, album);

        var back = store.ReadAlbum(ThroughBytes(book));

        Assert.Equal(("tapestries", "pieces", 7, "Мої гобелени"), (back.CollectionId, back.Level, back.Sheets, back.Name));
        Assert.Equal(["a", "b"], back.Collected.Keys);
        Assert.Equal(album.Collected.Values.Select(c => c.Key), back.Collected.Values.Select(c => c.Key));
        Assert.All(back.Collected.Values, c => Assert.IsType<ItemStack>(c.Stack));
        Assert.Equal([store.ToEntry(Tapestry("gone1")).Key], back.Unplaced.Select(u => u.Key));
    }

    [Fact]
    public void Album_is_written_as_format_2_and_the_same_album_gives_the_same_bytes()
    {
        var one = AlbumItem();
        var other = AlbumItem();
        var empty = new Album("tapestries", null, Album.NoneCollected, [], 3);
        store.WriteAlbum(one, empty.Collect([("a", By(Tapestry("a"))), ("b", By(Tapestry("b")))]));
        store.WriteAlbum(other, empty.Collect([("b", By(Tapestry("b"))), ("a", By(Tapestry("a")))]));

        Assert.Equal(one.ToBytes(), other.ToBytes());
        var root = one.Attributes.GetTreeAttribute(LedgerStore.RootKey);
        Assert.Equal(2, root.GetAsInt("v"));
        Assert.Null(root["entries"]); // nothing missing is stored
        Assert.Null(root.GetTreeAttribute("album")["members"]); // no snapshot
        Assert.Null(root.GetTreeAttribute("album").GetString("level")); // "none chosen" is no level
    }

    [Fact]
    public void Album_of_format_1_is_read_as_the_members_it_collected()
    {
        // 0.1: made when the collection had a1, a2; a1 collected. Now the collection also has b1.
        var book = Format1(AlbumItem(), 4, "Mine", [Tapestry("a1"), Tapestry("a2")], [Tapestry("a2")]);
        var now = Pieces("a1", "a2", "b1");

        var back = Reading(now).ReadAlbum(ThroughBytes(book));

        Assert.Equal(("pieces", 4, "Mine"), (back.Level, back.Sheets, back.Name));
        Assert.Equal(["a1"], back.Collected.Keys);
        Assert.Equal(new LevelProgress(1, 3, 0, 1), back.Progress(now.Levels[0])); // a2 still missing, b1 new
    }

    [Fact]
    public void Album_of_format_1_is_written_back_as_format_2_without_losing_a_piece()
    {
        // The collection is unknown while the server writes it (a sheet returned, a rename): what it collected is kept unplaced,
        // then placed on its members once the collection is back.
        var book = Format1(AlbumItem(), 4, "", [Tapestry("a1"), Tapestry("a2"), Tapestry("b1")], [Tapestry("a2")]);
        var unknown = Reading(null);
        unknown.WriteAlbum(book, unknown.ReadAlbum(ThroughBytes(book)) with { Name = "renamed" });

        Assert.Equal(2, book.Attributes.GetTreeAttribute(LedgerStore.RootKey).GetAsInt("v"));
        var back = Reading(Pieces("a1", "a2", "b1")).ReadAlbum(ThroughBytes(book));
        Assert.Equal(("pieces", "renamed"), (back.Level, back.Name));
        Assert.Equal(["a1", "b1"], back.Collected.Keys);
        Assert.Empty(back.Unplaced);
    }

    [Fact]
    public void Album_of_format_1_keeps_its_progress_when_the_ignored_attributes_change()
    {
        // The snapshot is re-keyed on read like the entries, with the same config. Kept as key strings, it reset every
        // album to nothing collected when a key's text changed (code review 28.10.2026).
        ItemStack Worn(string type)
        {
            var stack = Tapestry(type);
            stack.Attributes.SetInt("durability", 5);
            return stack;
        }
        var book = Format1(AlbumItem(), 4, "", [Worn("a1"), Worn("a2")], [Worn("a2")]);
        var reading = Reading(TestCollections.Tapestries(new StackKeys(game.World, ["durability"]), t => Worn(t), "a1", "a2"));

        Assert.Equal(["a1"], reading.ReadAlbum(ThroughBytes(book)).Collected.Keys); // a1 stays collected
    }

    [Fact]
    public void Album_of_format_1_from_before_the_sample_snapshot_counts_as_made_from_the_current_collection()
    {
        // Albums of the development builds kept the snapshot as key strings.
        var book = Format1(AlbumItem(), 1, "", [], [Tapestry("a2")]);
        book.Attributes.GetTreeAttribute(LedgerStore.RootKey).GetTreeAttribute("album")["members"] =
            new Vintagestory.API.Datastructures.StringArrayAttribute(["block:game:tapestry-north{\"type\":\"a1\"}"]);

        Assert.Equal(["a1", "a3"], Reading(Pieces("a1", "a2", "a3")).ReadAlbum(ThroughBytes(book)).Collected.Keys);
    }

    [Fact]
    public void Album_item_without_our_data_is_an_empty_album_of_no_collection()
    {
        var back = store.ReadAlbum(AlbumItem());
        Assert.Equal(("", 0, true), (back.CollectionId, back.Sheets, back.IsEmpty));
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
