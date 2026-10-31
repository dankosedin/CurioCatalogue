using NSubstitute;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;

namespace CurioCatalogue.Tests;

/// <summary>
/// Posting on real game slots and stacks; only the world, claims and the bag are mocked.
/// </summary>
public class InvoicePosterTests
{
    private static readonly LedgerLimits Limits = new(32, 500);

    private readonly GameWorld game = new();
    // IPlayer cannot be mocked or implemented outside the game (internal member, vsapi IPlayer.cs:124#IsInInteractionRangeOf); only passed on.
    private readonly IPlayer player = null!;
    private readonly BlockPos pos = new(10, 20, 30);
    private readonly StackKeys keys;
    private readonly LedgerStore store;
    private readonly Block tapestry;
    private readonly Item ingot;
    private readonly Item invoiceItem;
    private readonly Item ledgerItem;
    private readonly Item parchment;
    private bool consumeSheets;
    private bool locked;
    private Collection? collection; // what albums follow; set by the album tests

    // The player's side: main hand (slot 0) holds the invoice, the rest is the hotbar and the backpack.
    private readonly InventoryGeneric playerInventory;
    private readonly ItemSlot offhand;

    public InvoicePosterTests()
    {
        playerInventory = new InventoryGeneric(6, "testplayer-1", game.Api, (i, inv) => i == 1 ? new RefusingSlot(inv) : new ItemSlotSurvival(inv));
        offhand = new InventoryGeneric(1, "testoffhand-1", game.Api)[0];
        keys = new StackKeys(game.World, ["durability"]);
        store = new LedgerStore(keys, _ => collection);
        tapestry = game.Block("tapestry-north");
        tapestry.MaxStackSize = 64;
        ingot = game.Item("ingot-iron");
        ingot.MaxStackSize = 64;
        invoiceItem = game.Item("curiocatalogue:invoice-outgoing", new ItemInvoice());
        ledgerItem = game.Item("curiocatalogue:book-materials-normal-teal", new ItemLedger());
        parchment = game.Item("paper-parchment");
    }

    private InvoicePoster Poster(IHeldBag? bag = null) => new(
        game.World, store, keys,
        new ContainerReader(game.World, stack => bag != null && stack.Collectible?.Code?.Path == "backpack" ? bag : null, TestInventoryBlock.IsStorage),
        new ContainerAccess(game.World, (_, _) => locked),
        () => Limits, () => consumeSheets);

    private ItemStack Tapestry(string type, int size = 1)
    {
        var stack = new ItemStack(tapestry, size);
        stack.Attributes.SetString("type", type);
        return stack;
    }

    private ItemSlot InvoiceSlot => playerInventory[0];
    private List<ItemSlot> PlayerSlots => playerInventory.ToList();

    /// <summary>Puts an invoice for these stacks in the main hand.</summary>
    private void HoldInvoice(params ItemStack[] lines)
    {
        var stack = new ItemStack(invoiceItem);
        store.WriteInvoice(stack, new Invoice(InvoiceKind.Outgoing, [.. lines.Select(store.ToEntry)], 0, new BlockPosition(0, 0, 0)));
        InvoiceSlot.Itemstack = stack;
    }

    /// <summary>Puts a counts ledger in the off-hand.</summary>
    private void HoldLedger(int sheets, params ItemStack[] entries)
    {
        var stack = new ItemStack(ledgerItem);
        store.WriteLedger(stack, Ledger.FromStored(LedgerMode.Counts, sheets, entries.Select(store.ToEntry)));
        offhand.Itemstack = stack;
    }

    private Ledger OffhandLedger => store.ReadLedger(offhand.Itemstack!, LedgerMode.Counts);

    private PostInvoiceResult Post(InvoicePoster? poster = null) =>
        (poster ?? Poster()).Post(player, InvoiceSlot, InvoiceKind.Outgoing, pos, PlayerSlots, offhand);

    private (string, int)[] PlayerItems() =>
        PlayerSlots.Where(s => s.Itemstack != null && s.Itemstack.Collectible != invoiceItem && s.Itemstack.Collectible != parchment && s.Itemstack.Collectible is not ItemLedger)
            .Select(s => (keys.KeyOf(s.Itemstack!).ToString(), s.StackSize)).ToArray();

    private static string TapestryKey(string type) => $"block:game:tapestry-north{{\"type\":\"{type}\"}}";

    // --- Success ---

    [Fact]
    public void Items_move_ledger_updates_and_the_sheet_returns()
    {
        var chest = new TestChest(game.Api, Tapestry("ambush1"), Tapestry("ambush2", 3));
        game.Place(pos, chest);
        HoldInvoice(Tapestry("ambush1"), Tapestry("ambush2", 2));
        HoldLedger(5, Tapestry("ambush1"), Tapestry("ambush2", 2), Tapestry("ambush3"));

        var posted = Assert.IsType<PostInvoiceResult.Posted>(Post());

        Assert.Equal((2, 3, InvoiceFate.SheetReturned), (posted.Lines, posted.Items, posted.Fate));
        Assert.Equal((3, 1), (posted.LedgerBefore!.Value, posted.LedgerAfter!.Value));
        Assert.Null(InvoiceSlot.Itemstack); // the invoice is gone, and the hand stays free: there is room elsewhere
        Assert.Equal(6, OffhandLedger.Sheets);
        Assert.Equal([TapestryKey("ambush3")], OffhandLedger.Entries.Keys.Select(k => k.ToString()));
        Assert.Equal([(TapestryKey("ambush1"), 1), (TapestryKey("ambush2"), 2)], PlayerItems().OrderBy(i => i.Item1));
        Assert.Equal([1], chest.Inventory.Where(s => s.Itemstack != null).Select(s => s.StackSize)); // 1 × ambush2 stays
    }

    [Fact]
    public void Without_a_ledger_the_invoice_becomes_parchment_in_its_slot()
    {
        game.Place(pos, new TestChest(game.Api, Tapestry("ambush1")));
        HoldInvoice(Tapestry("ambush1"));

        var posted = Assert.IsType<PostInvoiceResult.Posted>(Post());

        Assert.Equal(InvoiceFate.Parchment, posted.Fate);
        Assert.Null(posted.LedgerBefore);
        Assert.Equal(parchment, InvoiceSlot.Itemstack!.Collectible);
    }

    [Fact]
    public void Ledger_full_of_sheets_is_updated_and_the_invoice_becomes_parchment()
    {
        game.Place(pos, new TestChest(game.Api, Tapestry("ambush1")));
        HoldInvoice(Tapestry("ambush1"));
        HoldLedger(32, Tapestry("ambush1"));

        var posted = Assert.IsType<PostInvoiceResult.Posted>(Post());

        Assert.Equal(InvoiceFate.Parchment, posted.Fate);
        Assert.Equal(32, OffhandLedger.Sheets);
        Assert.True(OffhandLedger.IsEmpty);
        Assert.Equal(parchment, InvoiceSlot.Itemstack!.Collectible);
    }

    [Fact]
    public void Consume_sheets_makes_the_invoice_vanish_without_a_sheet()
    {
        consumeSheets = true;
        game.Place(pos, new TestChest(game.Api, Tapestry("ambush1")));
        HoldInvoice(Tapestry("ambush1"));
        HoldLedger(5, Tapestry("ambush1"));

        var posted = Assert.IsType<PostInvoiceResult.Posted>(Post());

        Assert.Equal(InvoiceFate.Consumed, posted.Fate);
        Assert.Null(InvoiceSlot.Itemstack);
        Assert.Equal(5, OffhandLedger.Sheets);
        Assert.True(OffhandLedger.IsEmpty);
    }

    [Fact]
    public void Partial_stacks_are_taken_first()
    {
        var chest = new TestChest(game.Api, new ItemStack(ingot, 64), new ItemStack(ingot, 5));
        game.Place(pos, chest);
        HoldInvoice(new ItemStack(ingot, 5));

        Assert.IsType<PostInvoiceResult.Posted>(Post());
        Assert.Equal([64], chest.Inventory.Where(s => s.Itemstack != null).Select(s => s.StackSize));
    }

    [Fact]
    public void The_real_stacks_move_with_their_own_attributes()
    {
        // Durability is ignored for identity, but the moved stack keeps its own.
        var pickaxe = game.Item("pickaxe-iron");
        var worn = new ItemStack(pickaxe);
        worn.Attributes.SetInt("durability", 7);
        game.Place(pos, new TestChest(game.Api, worn));
        HoldInvoice(new ItemStack(pickaxe));

        Assert.IsType<PostInvoiceResult.Posted>(Post());
        Assert.Equal(7, PlayerSlots.Single(s => s.Itemstack?.Collectible == pickaxe).Itemstack!.Attributes.GetInt("durability"));
    }

    [Fact]
    public void Items_top_up_stacks_the_player_already_has()
    {
        game.Place(pos, new TestChest(game.Api, new ItemStack(ingot, 10)));
        playerInventory[3].Itemstack = new ItemStack(ingot, 60);
        HoldInvoice(new ItemStack(ingot, 10));

        Assert.IsType<PostInvoiceResult.Posted>(Post());
        Assert.Equal(64, playerInventory[3]!.StackSize);
        Assert.Equal(6, PlayerSlots.Where(s => s.Itemstack?.Collectible == ingot).Sum(s => s.StackSize) - 64);
    }

    [Fact]
    public void Stacks_of_one_key_the_game_would_not_merge_need_a_slot_each()
    {
        // Two ingot stacks: one key (durability ignored by the config), but the game does not merge them (code review 28.10.2026:
        // the plan put both into the one free slot, and the second move did nothing).
        var worn = new ItemStack(ingot, 3);
        worn.Attributes.SetInt("durability", 5);
        var chest = new TestChest(game.Api, worn, new ItemStack(ingot, 3));
        game.Place(pos, chest);
        for (int i = 2; i < 6; i++) playerInventory[i].Itemstack = new ItemStack(parchment, 1); // only slot 1 is free
        HoldInvoice(new ItemStack(ingot, 6));

        var refused = Assert.IsType<PostInvoiceResult.Refused>(Post());

        Assert.Equal((PostRefusal.NoRoom, 1), (refused.Reason, refused.SlotsShort));
        Assert.Equal(6, chest.Inventory.Where(s => s.Itemstack != null).Sum(s => s.StackSize));
        Assert.Empty(PlayerItems());
    }

    // --- All or nothing ---

    [Fact]
    public void Missing_items_change_nothing()
    {
        var chest = new TestChest(game.Api, Tapestry("ambush1"));
        game.Place(pos, chest);
        HoldInvoice(Tapestry("ambush1"), Tapestry("ambush2", 2));
        HoldLedger(5, Tapestry("ambush1"));

        var refused = Assert.IsType<PostInvoiceResult.Refused>(Post());

        Assert.Equal(PostRefusal.Missing, refused.Reason);
        Assert.Equal([(TapestryKey("ambush2"), 2)], refused.Lacking!.Select(e => (e.Key.ToString(), e.Count)));
        Assert.Single(chest.Inventory, s => s.Itemstack != null);
        Assert.Equal(invoiceItem, InvoiceSlot.Itemstack!.Collectible);
        Assert.Equal(5, OffhandLedger.Sheets);
        Assert.Single(OffhandLedger.Entries);
        Assert.Empty(PlayerItems());
    }

    [Fact]
    public void No_room_changes_nothing()
    {
        var chest = new TestChest(game.Api, Tapestry("a"), Tapestry("b"), Tapestry("c"));
        game.Place(pos, chest);
        for (int i = 1; i < 5; i++) playerInventory[i].Itemstack = new ItemStack(ingot, 64);
        HoldInvoice(Tapestry("a"), Tapestry("b"), Tapestry("c"));

        // One free slot (5), the invoice slot does not count: without a ledger it turns into parchment.
        var refused = Assert.IsType<PostInvoiceResult.Refused>(Post());

        Assert.Equal((PostRefusal.NoRoom, 2), (refused.Reason, refused.SlotsShort));
        Assert.Equal(3, chest.Inventory.Count(s => s.Itemstack != null));
        Assert.Equal(invoiceItem, InvoiceSlot.Itemstack!.Collectible);
    }

    [Fact]
    public void The_invoice_slot_counts_as_free_when_the_invoice_will_vanish()
    {
        game.Place(pos, new TestChest(game.Api, Tapestry("a")));
        for (int i = 1; i < 6; i++) playerInventory[i].Itemstack = new ItemStack(ingot, 64);
        HoldInvoice(Tapestry("a"));
        HoldLedger(5);

        Assert.IsType<PostInvoiceResult.Posted>(Post());
        Assert.Equal(tapestry, InvoiceSlot.Itemstack!.Collectible);
    }

    [Fact]
    public void Slots_that_may_not_hold_the_item_are_not_used()
    {
        // A seed bag slot: Agriculture only (vsapi BagInventory.cs:87#ItemSlotBagContent ItemSlotBagContent storage flags).
        var bagInventory = new InventoryGeneric(1, "testbag-1", game.Api);
        var seedSlot = new ItemSlotBagContent(bagInventory, 0, 0, EnumItemStorageFlags.Agriculture);
        game.Place(pos, new TestChest(game.Api, new ItemStack(ingot, 1)));
        HoldInvoice(new ItemStack(ingot, 1));

        var refused = Poster().Post(player, InvoiceSlot, InvoiceKind.Outgoing, pos, [InvoiceSlot, seedSlot], offhand);
        Assert.Equal(PostRefusal.NoRoom, Assert.IsType<PostInvoiceResult.Refused>(refused).Reason);
        Assert.Null(seedSlot.Itemstack);
    }

    [Fact]
    public void Without_access_nothing_happens()
    {
        var chest = new TestChest(game.Api, Tapestry("ambush1"));
        game.Place(pos, chest);
        HoldInvoice(Tapestry("ambush1"));
        locked = true;

        Assert.Equal(PostRefusal.NoAccess, Assert.IsType<PostInvoiceResult.Refused>(Post()).Reason);
        Assert.Single(chest.Inventory, s => s.Itemstack != null);
    }

    [Fact]
    public void Unsupported_container_is_refused()
    {
        game.Place(pos, new TestPerPlayerChest());
        HoldInvoice(Tapestry("ambush1"));
        Assert.Equal(PostRefusal.Unsupported, Assert.IsType<PostInvoiceResult.Refused>(Post()).Reason);
    }

    // --- Want lists (domain.md, Albums and collections) ---

    private void HoldWantList(params string[] pieces)
    {
        var stack = new ItemStack(invoiceItem);
        store.WriteInvoice(stack, new Invoice(InvoiceKind.WantList, [.. pieces.Select(p => store.ToEntry(Tapestry(p)))], 0, new BlockPosition(0, 0, 0)));
        InvoiceSlot.Itemstack = stack;
    }

    private PostInvoiceResult PostWantList() =>
        Poster().Post(player, InvoiceSlot, InvoiceKind.WantList, pos, PlayerSlots, offhand);

    [Fact]
    public void Want_list_takes_one_of_each_it_finds_and_keeps_the_rest_of_its_list()
    {
        var chest = new TestChest(game.Api, Tapestry("ambush1", 3), Tapestry("notice"), new ItemStack(ingot, 5));
        game.Place(pos, chest);
        HoldWantList("ambush1", "ambush2", "notice");
        HoldLedger(5, Tapestry("x"));

        var posted = Assert.IsType<PostInvoiceResult.Posted>(PostWantList());

        Assert.Equal((2, 2, InvoiceFate.Kept, 1), (posted.Lines, posted.Items, posted.Fate, posted.ListLeft));
        Assert.Equal([(TapestryKey("ambush1"), 1), (TapestryKey("notice"), 1)], PlayerItems().OrderBy(i => i.Item1));
        Assert.Equal([TapestryKey("ambush2")], store.ReadInvoice(InvoiceSlot.Itemstack!, InvoiceKind.WantList).Entries.Select(e => e.Key.ToString()));
        Assert.Equal((5, 1), (OffhandLedger.Sheets, OffhandLedger.Entries.Count)); // the off-hand ledger is not touched
        Assert.Equal(2, chest.Inventory.Where(s => s.Itemstack?.Collectible == tapestry).Sum(s => s.StackSize)); // 2 × ambush1 stay
    }

    [Fact]
    public void Want_list_becomes_parchment_when_its_list_is_done()
    {
        game.Place(pos, new TestChest(game.Api, Tapestry("ambush1")));
        HoldWantList("ambush1");

        var posted = Assert.IsType<PostInvoiceResult.Posted>(PostWantList());

        Assert.Equal((InvoiceFate.Parchment, 0), (posted.Fate, posted.ListLeft));
        Assert.Equal(parchment, InvoiceSlot.Itemstack!.Collectible);
    }

    [Fact]
    public void Finished_want_list_gives_no_sheet_back_even_with_a_book_in_the_off_hand()
    {
        // A want list touches no book (domain.md, Albums and collections); README says so since the review of 28.10.2026.
        game.Place(pos, new TestChest(game.Api, Tapestry("ambush1")));
        HoldWantList("ambush1");
        HoldLedger(5);

        var posted = Assert.IsType<PostInvoiceResult.Posted>(PostWantList());

        Assert.Equal(InvoiceFate.Parchment, posted.Fate);
        Assert.Equal(5, OffhandLedger.Sheets);
        Assert.Equal(parchment, InvoiceSlot.Itemstack!.Collectible);
    }

    [Fact]
    public void Finished_want_list_under_consume_sheets_disappears_like_any_invoice()
    {
        // domain.md, Posting an invoice: "With ConsumeSheets ... the invoice just disappears" (decided 28.10.2026 for want lists too).
        consumeSheets = true;
        game.Place(pos, new TestChest(game.Api, Tapestry("ambush1")));
        HoldWantList("ambush1");

        var posted = Assert.IsType<PostInvoiceResult.Posted>(PostWantList());

        Assert.Equal(InvoiceFate.Consumed, posted.Fate);
        Assert.Null(InvoiceSlot.Itemstack);
    }

    [Fact]
    public void A_blank_invoice_is_refused_and_nothing_changes()
    {
        // From the creative inventory: no lines, no data. It used to post as "0 lines" and give the book a free sheet (ROADMAP, Books).
        var chest = new TestChest(game.Api, Tapestry("ambush1", 2));
        game.Place(pos, chest);
        HoldInvoice();
        HoldLedger(5, Tapestry("ambush3"));

        Assert.Equal(PostRefusal.Blank, Assert.IsType<PostInvoiceResult.Refused>(Post()).Reason);

        Assert.Equal(invoiceItem, InvoiceSlot.Itemstack!.Collectible);
        Assert.Equal(5, OffhandLedger.Sheets);
        Assert.Equal(2, chest.Inventory.Sum(s => s.StackSize));
        Assert.Empty(PlayerItems());
    }

    [Fact]
    public void A_blank_want_list_is_refused_as_blank()
    {
        game.Place(pos, new TestChest(game.Api, Tapestry("ambush1")));
        HoldWantList();

        Assert.Equal(PostRefusal.Blank, Assert.IsType<PostInvoiceResult.Refused>(PostWantList()).Reason);
        Assert.Equal(invoiceItem, InvoiceSlot.Itemstack!.Collectible);
    }

    [Fact]
    public void Want_list_on_a_container_without_anything_from_the_list_is_refused()
    {
        game.Place(pos, new TestChest(game.Api, Tapestry("other")));
        HoldWantList("ambush1");

        Assert.Equal(PostRefusal.NothingFromList, Assert.IsType<PostInvoiceResult.Refused>(PostWantList()).Reason);
        Assert.Equal(invoiceItem, InvoiceSlot.Itemstack!.Collectible);
    }

    [Fact]
    public void Want_list_without_room_changes_nothing()
    {
        var chest = new TestChest(game.Api, Tapestry("a"), Tapestry("b"));
        game.Place(pos, chest);
        for (int i = 1; i < 6; i++) playerInventory[i].Itemstack = new ItemStack(ingot, 64);
        HoldWantList("a", "b");

        Assert.Equal(PostRefusal.NoRoom, Assert.IsType<PostInvoiceResult.Refused>(PostWantList()).Reason);
        Assert.Equal(2, chest.Inventory.Count(s => s.Itemstack != null));
        Assert.Equal(2, store.ReadInvoice(InvoiceSlot.Itemstack!, InvoiceKind.WantList).Entries.Length);
    }

    [Fact]
    public void Want_list_takes_what_fits_and_keeps_the_rest_on_its_list()
    {
        var chest = new TestChest(game.Api, Tapestry("a"), Tapestry("b"), Tapestry("c"));
        game.Place(pos, chest);
        for (int i = 2; i < 6; i++) playerInventory[i].Itemstack = new ItemStack(ingot, 64);
        HoldWantList("a", "b", "c"); // one free slot: 1

        var posted = Assert.IsType<PostInvoiceResult.Posted>(PostWantList());

        Assert.Equal((1, InvoiceFate.Kept, 2, 2), (posted.Items, posted.Fate, posted.ListLeft, posted.NotFitted));
        Assert.Equal([(TapestryKey("a"), 1)], PlayerItems().Where(i => i.Item1 != keys.KeyOf(new ItemStack(ingot)).ToString()));
        Assert.Equal([TapestryKey("b"), TapestryKey("c")], store.ReadInvoice(InvoiceSlot.Itemstack!, InvoiceKind.WantList).Entries.Select(e => e.Key.ToString()));
        Assert.Equal(2, chest.Inventory.Count(s => s.Itemstack != null));
    }

    // --- An album taking new items straight away (domain.md, Albums and collections) ---

    /// <summary>A tapestry collection of these pieces, in this order, and a fresh album of it in the given slot.</summary>
    private ItemSlot HoldAlbum(ItemSlot slot, params string[] pieces)
    {
        collection = new Collection("tapestries", pieces.Select(p => new CollectionMember(keys.KeyOf(Tapestry(p)), p, Tapestry(p))));
        var stack = new ItemStack(game.Item("curiocatalogue:book-album-aged-orange", new ItemLedger()));
        store.WriteLedger(stack, collection.NewAlbum(4, ""));
        slot.Itemstack = stack;
        return slot;
    }

    private string[] Missing(ItemSlot albumSlot) =>
        store.ReadLedger(albumSlot.Itemstack!, LedgerMode.Album).Entries.Keys.Select(k => k.ToString()).Order().ToArray();

    private CollectResult Collect(ItemSlot albumSlot) => Poster().Collect(player, albumSlot, pos, PlayerSlots, InvoiceSlot);

    [Fact]
    public void Album_takes_one_of_each_new_item_and_crosses_them_off()
    {
        var chest = new TestChest(game.Api, Tapestry("a", 3), Tapestry("b"), new ItemStack(ingot, 5));
        game.Place(pos, chest);
        var albumSlot = HoldAlbum(InvoiceSlot, "a", "b", "c"); // in the main hand

        var collected = Assert.IsType<CollectResult.Collected>(Collect(albumSlot));

        Assert.Equal((2, 0, 1), (collected.Taken, collected.NotFitted, collected.MissingLeft));
        Assert.Equal([(TapestryKey("a"), 1), (TapestryKey("b"), 1)], PlayerItems().OrderBy(i => i.Item1));
        Assert.Equal([TapestryKey("c")], Missing(albumSlot));
        Assert.Equal(4, store.ReadLedger(albumSlot.Itemstack!, LedgerMode.Album).Sheets); // no sheet spent
        Assert.Equal(2 + 5, chest.Inventory.Where(s => s.Itemstack != null).Sum(s => s.StackSize)); // duplicates and the rest stay
    }

    [Fact]
    public void Album_takes_what_fits_in_the_collections_order()
    {
        var chest = new TestChest(game.Api, Tapestry("a"), Tapestry("b"), Tapestry("c"));
        game.Place(pos, chest);
        for (int i = 1; i < 6; i++) playerInventory[i].Itemstack = new ItemStack(ingot, 64);
        var albumSlot = HoldAlbum(offhand, "c", "b", "a"); // in the off-hand; only the empty main hand is free

        var collected = Assert.IsType<CollectResult.Collected>(Collect(albumSlot));

        Assert.Equal((1, 2, 2), (collected.Taken, collected.NotFitted, collected.MissingLeft));
        Assert.Equal(TapestryKey("c"), keys.KeyOf(InvoiceSlot.Itemstack!).ToString());
        Assert.Equal([TapestryKey("a"), TapestryKey("b")], Missing(albumSlot));
    }

    [Fact]
    public void Album_with_nothing_new_in_the_container_is_refused()
    {
        game.Place(pos, new TestChest(game.Api, Tapestry("other"), new ItemStack(ingot, 5)));
        var albumSlot = HoldAlbum(InvoiceSlot, "a");

        Assert.Equal(CollectRefusal.NothingNew, Assert.IsType<CollectResult.Refused>(Collect(albumSlot)).Reason);
    }

    [Fact]
    public void Album_without_any_room_changes_nothing()
    {
        var chest = new TestChest(game.Api, Tapestry("a"), Tapestry("b"));
        game.Place(pos, chest);
        for (int i = 1; i < 6; i++) playerInventory[i].Itemstack = new ItemStack(ingot, 64);
        var albumSlot = HoldAlbum(InvoiceSlot, "a", "b"); // the main hand holds the album: no free slot

        var refused = Assert.IsType<CollectResult.Refused>(Collect(albumSlot));

        Assert.Equal((CollectRefusal.NoRoom, 2), (refused.Reason, refused.SlotsShort));
        Assert.Equal(2, chest.Inventory.Count(s => s.Itemstack != null));
        Assert.Equal(2, Missing(albumSlot).Length);
    }

    [Fact]
    public void Album_without_access_takes_nothing()
    {
        var chest = new TestChest(game.Api, Tapestry("a"));
        game.Place(pos, chest);
        var albumSlot = HoldAlbum(InvoiceSlot, "a");
        locked = true;

        Assert.Equal(CollectRefusal.NoAccess, Assert.IsType<CollectResult.Refused>(Collect(albumSlot)).Reason);
        Assert.Single(chest.Inventory, s => s.Itemstack != null);
    }

    [Fact]
    public void Posting_an_invoice_with_an_album_in_the_off_hand_returns_the_sheet_and_keeps_its_entries()
    {
        // Decided 28.10.2026: the sheet goes back as it does when cancelling (less inventory juggling); the album still
        // changes only by taking new items (domain.md, Posting an invoice).
        game.Place(pos, new TestChest(game.Api, Tapestry("a")));
        HoldInvoice(Tapestry("a"));
        HoldAlbum(offhand, "a", "b");

        var posted = Assert.IsType<PostInvoiceResult.Posted>(Post());

        Assert.Equal((InvoiceFate.SheetReturned, (int?)null), (posted.Fate, posted.LedgerBefore));
        Assert.Equal([(TapestryKey("a"), 1)], PlayerItems()); // the invoice's slot was free for it
        Assert.Equal([TapestryKey("a"), TapestryKey("b")], Missing(offhand)); // "a" is not crossed off
        Assert.Equal(5, store.ReadLedger(offhand.Itemstack!, LedgerMode.Album).Sheets);
    }

    [Fact]
    public void Posting_with_a_full_album_in_the_off_hand_makes_parchment()
    {
        game.Place(pos, new TestChest(game.Api, Tapestry("a")));
        HoldInvoice(Tapestry("a"));
        HoldAlbum(offhand, "a");
        store.WriteLedger(offhand.Itemstack!, store.ReadLedger(offhand.Itemstack!, LedgerMode.Album) with { Sheets = Limits.MaxSheets });

        var posted = Assert.IsType<PostInvoiceResult.Posted>(Post());

        Assert.Equal(InvoiceFate.Parchment, posted.Fate);
        Assert.Equal(Limits.MaxSheets, store.ReadLedger(offhand.Itemstack!, LedgerMode.Album).Sheets);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancelling_any_invoice_with_an_album_in_the_off_hand_returns_the_sheet_to_it(bool wantList)
    {
        if (wantList) HoldWantList("a");
        else HoldInvoice(Tapestry("a"));
        HoldAlbum(offhand, "a", "b");

        Assert.Equal(InvoiceFate.SheetReturned, Poster().Cancel(InvoiceSlot, offhand));

        Assert.Null(InvoiceSlot.Itemstack);
        Assert.Equal(5, store.ReadLedger(offhand.Itemstack!, LedgerMode.Album).Sheets);
        Assert.Equal(2, Missing(offhand).Length); // the entries stay
    }

    // --- Cancelling ---

    [Fact]
    public void Cancel_returns_the_sheet_and_moves_nothing()
    {
        var chest = new TestChest(game.Api, Tapestry("ambush1"));
        game.Place(pos, chest);
        HoldInvoice(Tapestry("ambush1"));
        HoldLedger(5, Tapestry("ambush2"));

        Assert.Equal(InvoiceFate.SheetReturned, Poster().Cancel(InvoiceSlot, offhand));

        Assert.Null(InvoiceSlot.Itemstack);
        Assert.Equal(6, OffhandLedger.Sheets);
        Assert.Single(OffhandLedger.Entries); // entries untouched
        Assert.Single(chest.Inventory, s => s.Itemstack != null);
    }

    [Fact]
    public void Cancel_without_a_ledger_or_with_a_full_one_gives_parchment()
    {
        HoldInvoice(Tapestry("ambush1"));
        Assert.Equal(InvoiceFate.Parchment, Poster().Cancel(InvoiceSlot, offhand));
        Assert.Equal(parchment, InvoiceSlot.Itemstack!.Collectible);

        HoldInvoice(Tapestry("ambush1"));
        HoldLedger(32);
        Assert.Equal(InvoiceFate.Parchment, Poster().Cancel(InvoiceSlot, offhand));
        Assert.Equal(32, OffhandLedger.Sheets);
    }

    [Fact]
    public void Cancel_with_consume_sheets_just_removes_the_invoice()
    {
        consumeSheets = true;
        HoldInvoice(Tapestry("ambush1"));
        HoldLedger(5);

        Assert.Equal(InvoiceFate.Consumed, Poster().Cancel(InvoiceSlot, offhand));
        Assert.Null(InvoiceSlot.Itemstack);
        Assert.Equal(5, OffhandLedger.Sheets);
    }

    // --- Bags on the ground ---

    [Fact]
    public void Bag_on_the_ground_gives_its_items_and_saves_its_slots()
    {
        // Assumption: the bag's slots come from GetOrCreateSlots, made in the inventory passed to it, and a changed
        // slot is written back with Store (vsapi BagInventory.cs:128#SaveSlotIntoBag, 152; vsessentialsmod CollectibleBehaviorHeldBag.cs:142#ItemSlotBagContent).
        var bag = Substitute.For<IHeldBag>();
        var backpack = game.Item("backpack");
        var ground = new BlockEntityGroundStorage();
        ground.Inventory.LateInitialize("groundstorage-test", game.Api); // in game Initialize(api) does it
        var bagStack = new ItemStack(backpack);
        ground.Inventory[0].Itemstack = bagStack;
        ItemSlotBagContent? bagSlot = null;
        bag.GetOrCreateSlots(bagStack, Arg.Any<InventoryBase>(), 0, game.World).Returns(call =>
        {
            bagSlot = new ItemSlotBagContent(call.Arg<InventoryBase>(), 0, 0, EnumItemStorageFlags.General)
            {
                Itemstack = Tapestry("ambush1", 2),
            };
            return [bagSlot];
        });
        game.Place(pos, ground);
        HoldInvoice(Tapestry("ambush1", 2));

        Assert.IsType<PostInvoiceResult.Posted>(Post(Poster(bag)));

        Assert.Null(bagSlot!.Itemstack);
        bag.Received().Store(bagStack, bagSlot);
        Assert.Equal([(TapestryKey("ambush1"), 2)], PlayerItems());
    }

    // --- When the game moves less than planned (code review 28.10.2026) ---

    /// <summary>
    /// Player slot 1, an ordinary slot until <see cref="Refuses"/>: then the plan still sees it as free (CanHold) but it takes
    /// nothing (CanTakeFrom, vsapi ItemSlot.cs:222#TryPutInto). Stands for a bug in the plan: the game moving less than planned.
    /// </summary>
    private sealed class RefusingSlot(InventoryBase inventory) : ItemSlotSurvival(inventory)
    {
        public bool Refuses;

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge) =>
            !Refuses && base.CanTakeFrom(sourceSlot, priority);
    }

    /// <summary>Slot 1 takes nothing, slot 2 is free, the rest of the backpack is full.</summary>
    private void OnlySlot2TakesItems()
    {
        ((RefusingSlot)playerInventory[1]).Refuses = true;
        for (int i = 3; i < 6; i++) playerInventory[i].Itemstack = new ItemStack(parchment, 1);
    }

    [Fact]
    public void Invoice_the_game_does_not_fully_move_changes_nothing()
    {
        var chest = new TestChest(game.Api, new ItemStack(ingot, 3));
        game.Place(pos, chest);
        ((RefusingSlot)playerInventory[1]).Refuses = true;
        for (int i = 2; i < 6; i++) playerInventory[i].Itemstack = new ItemStack(parchment, 1);
        HoldInvoice(new ItemStack(ingot, 3));
        HoldLedger(5, new ItemStack(ingot, 3));
        var invoiceBefore = InvoiceSlot.Itemstack!.ToBytes();

        var refused = Assert.IsType<PostInvoiceResult.Refused>(Post());

        Assert.Equal(PostRefusal.MoveFailed, refused.Reason);
        Assert.Equal(3, chest.Inventory[0]!.StackSize);
        Assert.Equal(invoiceBefore, InvoiceSlot.Itemstack!.ToBytes()); // the invoice is back in the hand, unchanged
        Assert.Equal((5, 1), (OffhandLedger.Sheets, OffhandLedger.Entries.Count));
        Assert.Empty(PlayerItems());
    }

    [Fact]
    public void Want_list_crosses_off_only_what_the_game_moved()
    {
        var chest = new TestChest(game.Api, Tapestry("a"), Tapestry("b"));
        game.Place(pos, chest);
        OnlySlot2TakesItems(); // the plan puts a into slot 1, b into slot 2
        HoldWantList("a", "b");

        var posted = Assert.IsType<PostInvoiceResult.Posted>(PostWantList());

        Assert.Equal((1, InvoiceFate.Kept, 1), (posted.Lines, posted.Fate, posted.NotFitted));
        Assert.Equal([TapestryKey("a")], store.ReadInvoice(InvoiceSlot.Itemstack!, InvoiceKind.WantList).Entries.Select(e => e.Key.ToString()));
        Assert.Equal([(TapestryKey("b"), 1)], PlayerItems());
        Assert.Equal([TapestryKey("a")], chest.Inventory.Where(s => s.Itemstack != null).Select(s => keys.KeyOf(s.Itemstack!).ToString()));
    }

    [Fact]
    public void Album_crosses_off_only_what_the_game_moved()
    {
        var chest = new TestChest(game.Api, Tapestry("a"), Tapestry("b"));
        game.Place(pos, chest);
        OnlySlot2TakesItems();
        var albumSlot = HoldAlbum(offhand, "a", "b");

        var collected = Assert.IsType<CollectResult.Collected>(Collect(albumSlot));

        Assert.Equal((1, 1, 1), (collected.Taken, collected.NotFitted, collected.MissingLeft));
        Assert.Equal([TapestryKey("a")], Missing(albumSlot)); // a stayed in the chest, so it is still missing
        Assert.Equal([(TapestryKey("b"), 1)], PlayerItems());
    }
}
