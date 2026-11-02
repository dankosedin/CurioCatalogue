using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// Books and invoices end to end: real players (<see cref="FakeClient"/>), real chests and the real item classes. A gesture is
/// the held item's <c>OnHeldInteractStart</c> on the server, called as the game calls it for a click with Shift (and Ctrl); a
/// gesture from the off-hand and the window requests go through our network channel into the real handlers. They replace the
/// cases of <c>docs/testing/functional.md</c> that need no client (marked there). The checks are on the world's state and on
/// the codes of the red refusals the player received; the texts are the lang files' business.
/// </summary>
[Collection(ServerCollection.Name)]
public class BookFlowTests(GameServerFixture game)
{
    CurioCatalogueModSystem Mod => game.Api.ModLoader.GetModSystem<CurioCatalogueModSystem>();
    IWorldAccessor World => game.Api.World;

    // ---- stacks

    ItemStack Vanilla(string code, int size = 1) =>
        World.GetItem(new AssetLocation("game", code)) is { } item ? new ItemStack(item, size)
        : new ItemStack(World.GetBlock(new AssetLocation("game", code)) ?? throw new InvalidOperationException($"no game:{code}"), size);

    ItemStack Tapestry(string type, int size = 1)
    {
        var stack = Vanilla("tapestry-north", size);
        stack.Attributes.SetString("type", type);
        return stack;
    }

    ItemStack Ours(string code) => new(World.GetItem(new AssetLocation("curiocatalogue", code)) ?? throw new InvalidOperationException($"no curiocatalogue:{code}"));

    /// <summary>A ledger (or bill of materials) with these entries, one line per stack.</summary>
    ItemStack Book(string code, int sheets, params ItemStack[] entries)
    {
        var stack = Ours(code);
        var mode = ((ItemLedger)stack.Collectible).LedgerMode;
        Mod.Store.WriteLedger(stack, Core.Ledger.FromStored(mode, sheets, entries.Select(Mod.Store.ToEntry)));
        return stack;
    }

    /// <summary>An album with every member of the tapestry collection missing.</summary>
    ItemStack Album(int sheets)
    {
        var collection = Mod.Collections.ForSample(Tapestry("ambush1")) ?? throw new InvalidOperationException("no tapestry collection");
        var stack = Ours("book-album-normal-brickred");
        Mod.Store.WriteLedger(stack, collection.NewAlbum(sheets, ""));
        return stack;
    }

    ItemStack Invoice(InvoiceKind kind, params ItemStack[] lines)
    {
        var stack = new ItemStack(World.GetItem(InvoiceMaker.InvoiceCode(kind)));
        Mod.Store.WriteInvoice(stack, new Invoice(kind, [.. lines.Select(Mod.Store.ToEntry)], 0, new BlockPosition(0, 0, 0)));
        return stack;
    }

    // ---- reading the world

    static string Describe(ItemStack stack)
    {
        string? type = stack.Attributes.GetString("type");
        return $"{stack.Collectible.Code.Path}{(type == null ? "" : ":" + type)} x{stack.StackSize}";
    }

    /// <summary>Where taken items may go: the hotbar without the off-hand, and the backpack.</summary>
    static List<ItemSlot> Slots(IServerPlayer player)
    {
        var inventory = player.InventoryManager;
        return inventory.GetHotbarInventory().Where(s => s != inventory.OffhandHotbarSlot)
            .Concat(inventory.GetOwnInventory(GlobalConstants.backpackInvClassName)).ToList();
    }

    /// <summary>The player's stacks that are not ours, sorted ("ingot-copper x5, tapestry-north:ambush1 x1").</summary>
    static string Inventory(IServerPlayer player) => string.Join(", ", Slots(player)
        .Where(s => s.Itemstack is { } st && st.Collectible.Code.Domain != CurioCatalogueModSystem.ModId)
        .Select(s => Describe(s.Itemstack!)).Order());

    static string Contents(BlockEntity container) => string.Join(", ", ((IBlockEntityContainer)container).Inventory
        .Where(s => !s.Empty).Select(s => Describe(s.Itemstack)).Order());

    /// <summary>The first stack of ours in the main inventory with this code prefix, or null.</summary>
    static ItemStack? FindOurs(IServerPlayer player, string codePrefix) => Slots(player)
        .Select(s => s.Itemstack).FirstOrDefault(st => st is { } x && x.Collectible.Code.Domain == CurioCatalogueModSystem.ModId && x.Collectible.Code.Path.StartsWith(codePrefix));

    (int Sheets, int Entries) BookState(ItemStack book) =>
        ToCounts(Mod.Store.ReadLedger(book, ((ItemLedger)book.Collectible).LedgerMode));

    static (int Sheets, int Entries) ToCounts(Core.Ledger l) => (l.Sheets, l.Entries.Count);

    static string Lines(CurioCatalogueModSystem mod, ItemStack invoice) =>
        string.Join(", ", mod.Store.ReadInvoice(invoice, ((ItemInvoice)invoice.Collectible).InvoiceKind).Entries.Select(e => $"{e.Key.Code} x{e.Count}"));

    static List<string> Errors(FakeClient client) => client.Received.Where(p => p.IngameError != null).Select(p => p.IngameError.Code).ToList();
    static List<string> Chat(FakeClient client) => client.Received.Where(p => p.Chatline != null).Select(p => p.Chatline.Message).ToList();

    // ---- acting

    /// <summary>The click with the item in <paramref name="slot"/> on the block at <paramref name="pos"/>, Shift held (and Ctrl if asked).</summary>
    void Click(IServerPlayer player, ItemSlot slot, BlockPos pos, bool ctrl = false)
    {
        var controls = player.Entity.Controls;
        controls.ShiftKey = true;
        controls.CtrlKey = ctrl;
        try
        {
            var selection = new BlockSelection { Position = pos.Copy(), Face = BlockFacing.UP, HitPosition = new Vec3d(0.5, 1, 0.5) };
            var handling = EnumHandHandling.NotHandled;
            slot.Itemstack!.Collectible.OnHeldInteractStart(slot, player.Entity, selection, null, true, ref handling);
        }
        finally
        {
            controls.ShiftKey = false;
            controls.CtrlKey = false;
        }
    }

    /// <summary>Puts the player next to the block (the off-hand gesture checks the reach on the server).</summary>
    void Stand(IServerPlayer player, BlockPos pos, double dx = 0) => MoveTo(player, pos.X + 0.5 + dx, pos.Y, pos.Z + 1.5);

    // A server entity keeps its authoritative place in ServerPos and Pos follows it each tick: set both.
    static void MoveTo(IServerPlayer player, double x, double y, double z)
    {
        player.Entity.ServerPos.SetPos(x, y, z);
        player.Entity.Pos.SetPos(x, y, z);
    }

    /// <summary>Fills the main inventory with sticks, except the main hand and, if asked, one hotbar slot (the second).</summary>
    void FillWithSticks(IServerPlayer player, bool leaveOneFree = false)
    {
        foreach (var (slot, i) in Slots(player).Select((s, i) => (s, i)).Where(x => x.i >= 1 && !(leaveOneFree && x.i == 1))) slot.Itemstack = Vanilla("stick");
    }

    // ---- making invoices

    [Fact]
    public void A_ledger_in_hand_makes_an_intake_list_from_a_real_chest()
    {
        // L1. The book has iron; the chest has iron and copper: the list is copper only, costs a sheet; the chest keeps everything.
        var client = FakeClient.Join(game, "maker");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-iron", 3);
            ((IBlockEntityContainer)chest).Inventory[1].Itemstack = Vanilla("ingot-copper", 5);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Book("book-ledger-normal-brickred", 8, Vanilla("ingot-iron"));

            Click(player, hand, pos);

            var list = FindOurs(player, "invoice-incoming");
            return (Lines: list == null ? "" : Lines(Mod, list), Sheets: BookState(hand.Itemstack).Sheets, Chest: Contents(chest));
        });
        Assert.Equal(("game:ingot-copper x5", 7, "ingot-copper x5, ingot-iron x3"), seen);
        client.WaitFor(() => Chat(client).Count > 0, "the chat line about the new list");
    }

    [Fact]
    public void A_pick_list_holds_what_the_book_and_the_chest_both_have()
    {
        // L2: Shift + Ctrl.
        var client = FakeClient.Join(game, "picker");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-iron", 3);
            ((IBlockEntityContainer)chest).Inventory[1].Itemstack = Vanilla("ingot-copper", 5);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Book("book-ledger-normal-brickred", 8, Vanilla("ingot-iron"));

            Click(player, hand, pos, ctrl: true);

            var list = FindOurs(player, "invoice-outgoing");
            return (list == null ? "" : Lines(Mod, list).Split(" x")[0], BookState(hand.Itemstack).Sheets);
        });
        Assert.Equal(("game:ingot-iron", 7), seen);
    }

    [Fact]
    public void A_bill_of_materials_makes_a_list_by_count()
    {
        // L3: the surplus. The book wants 2 iron, the chest has 5: the intake list says 3.
        var client = FakeClient.Join(game, "counter");
        var lines = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-iron", 5);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Book("book-materials-normal-brickred", 8, Vanilla("ingot-iron", 2));

            Click(player, hand, pos);
            return Lines(Mod, FindOurs(player, "invoice-incoming")!);
        });
        Assert.Equal("game:ingot-iron x3", lines);
    }

    [Fact]
    public void A_ledger_from_the_off_hand_makes_the_list_in_the_empty_hand_through_the_channel()
    {
        // The off-hand gesture: the client's request, checked again on the server (hands, reach, access).
        var client = FakeClient.Join(game, "offhand-maker");
        var pos = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            player.Entity.LeftHandItemSlot.Itemstack = Book("book-ledger-normal-brickred", 8);
            Stand(player, pos);
            return pos;
        });

        client.SendToMod(new MakeInvoiceRequest { X = pos.X, Y = pos.Y, Z = pos.Z });

        client.WaitFor(() => client.Player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible is ItemInvoice, "the list in the empty hand");
        var sheets = game.OnMain(() => BookState(client.Player.Entity.LeftHandItemSlot.Itemstack!).Sheets);
        Assert.Equal(7, sheets);
    }

    [Fact]
    public void The_off_hand_gesture_is_refused_out_of_reach_and_with_a_busy_hand()
    {
        // X4: a request for a chest far away; and a request with something in the main hand.
        var client = FakeClient.Join(game, "far-away");
        var pos = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, _) = TestBlocks.Place(game, "chest-east");
            player.Entity.LeftHandItemSlot.Itemstack = Book("book-ledger-normal-brickred", 8);
            Stand(player, pos, dx: 150); // a creative player reaches 100 blocks (WorldData.PickingRange)
            return pos;
        });
        client.SendToMod(new MakeInvoiceRequest { X = pos.X, Y = pos.Y, Z = pos.Z });
        client.WaitFor(() => Errors(client).Contains("outofreach"), "the refusal 'out of reach'");

        game.OnMain(() =>
        {
            Stand(client.Player, pos);
            client.Player.InventoryManager.ActiveHotbarSlot.Itemstack = Vanilla("stick");
            return 0;
        });
        client.SendToMod(new MakeInvoiceRequest { X = pos.X, Y = pos.Y, Z = pos.Z });
        client.WaitFor(() => Errors(client).Contains("offhandgesture"), "the refusal 'hands'");
        Assert.Equal(7 + 1, game.OnMain(() => BookState(client.Player.Entity.LeftHandItemSlot.Itemstack!).Sheets));
    }

    // ---- posting

    [Fact]
    public void An_intake_list_is_posted_into_the_inventory_and_the_ledger_learns_it()
    {
        // I1: the items move, the book in the off-hand gets the entries and the sheet back, the list is gone.
        var client = FakeClient.Join(game, "poster");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            ((IBlockEntityContainer)chest).Inventory[1].Itemstack = Vanilla("ingot-iron", 3);
            var offhand = player.Entity.LeftHandItemSlot;
            offhand.Itemstack = Book("book-ledger-normal-brickred", 7, Vanilla("ingot-iron"));
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5));

            Click(player, hand, pos);

            return (Inventory: Inventory(player), Chest: Contents(chest), Book: BookState(offhand.Itemstack), ListLeft: FindOurs(player, "invoice-") != null);
        });
        Assert.Equal(("ingot-copper x5", "ingot-iron x3", (8, 2), false), seen);
        client.WaitFor(() => Chat(client).Count > 0, "the chat line about the post");
    }

    [Fact]
    public void A_missing_item_stops_the_whole_post()
    {
        // I2, all or nothing: the chest has the copper but not the tin; not even the copper moves.
        var client = FakeClient.Join(game, "missing");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5), Vanilla("ingot-tin", 1));

            Click(player, hand, pos);

            return (Inventory: Inventory(player), Chest: Contents(chest), ListKept: hand.Itemstack?.Collectible is ItemInvoice);
        });
        Assert.Equal(("", "ingot-copper x5", true), seen);
        // The refusal names the lacking items, so the client can name them in its language (ModNetwork.MissingItemsRefusal).
        client.WaitFor(() => client.Received.Any(p => p.CustomPacket != null), "the refusal naming the missing items");
    }

    [Fact]
    public void No_room_stops_the_whole_post()
    {
        // I5: every slot is taken, the list is not going away (no book in the off-hand): nothing moves.
        var client = FakeClient.Join(game, "crowded");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5));
            FillWithSticks(player);
            string before = Inventory(player);

            Click(player, hand, pos);

            return (Same: Inventory(player) == before, Chest: Contents(chest), ListKept: hand.Itemstack?.Collectible is ItemInvoice);
        });
        Assert.Equal((true, "ingot-copper x5", true), seen);
        client.WaitFor(() => Errors(client).Contains("noroom"), "the refusal 'no room'");
    }

    [Fact]
    public void Without_a_book_in_the_off_hand_a_posted_list_becomes_parchment()
    {
        // I3
        var client = FakeClient.Join(game, "no-book");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.Outgoing, Vanilla("ingot-copper", 5));

            Click(player, hand, pos);

            return (Inventory(player), Contents(chest));
        });
        Assert.Equal(("ingot-copper x5, paper-parchment x1", ""), seen);
    }

    // ---- albums and want lists

    [Fact]
    public void An_album_takes_one_of_each_missing_piece_and_leaves_the_duplicates()
    {
        // A2: ambush1 twice and ambush2 once in the chest; one of each goes, the second ambush1 stays.
        var client = FakeClient.Join(game, "collector");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            var slots = ((IBlockEntityContainer)chest).Inventory;
            slots[0].Itemstack = Tapestry("ambush1");
            slots[1].Itemstack = Tapestry("ambush2");
            slots[2].Itemstack = Tapestry("ambush1", 2);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Album(8);
            int missingBefore = BookState(hand.Itemstack).Entries;

            Click(player, hand, pos);

            return (Inventory(player), Contents(chest), Missing: missingBefore - BookState(hand.Itemstack).Entries);
        });
        Assert.Equal(("tapestry-north:ambush1 x1, tapestry-north:ambush2 x1", "tapestry-north:ambush1 x2", 2), seen);
        client.WaitFor(() => Chat(client).Count > 0, "the chat line about the taking");
    }

    [Fact]
    public void An_album_in_the_off_hand_takes_through_the_channel()
    {
        // A3
        var client = FakeClient.Join(game, "offhand-taker");
        var pos = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Tapestry("ambush1");
            player.Entity.LeftHandItemSlot.Itemstack = Album(8);
            Stand(player, pos);
            return pos;
        });

        client.SendToMod(new MakeInvoiceRequest { X = pos.X, Y = pos.Y, Z = pos.Z });

        client.WaitFor(() => Inventory(client.Player) == "tapestry-north:ambush1 x1", "the tapestry in the inventory");
    }

    [Fact]
    public void Nothing_new_for_the_album_moves_nothing()
    {
        // A4
        var client = FakeClient.Join(game, "nothing-new");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-iron", 3);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Album(8);

            Click(player, hand, pos);

            return (Inventory(player), Contents(chest));
        });
        Assert.Equal(("", "ingot-iron x3"), seen);
        client.WaitFor(() => Errors(client).Contains("nothingnew"), "the refusal 'nothing new'");
    }

    [Fact]
    public void Shift_and_Ctrl_with_an_album_is_left_to_the_game()
    {
        // I8: the album has no such action; it takes nothing.
        var client = FakeClient.Join(game, "ctrl-album");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Tapestry("ambush1");
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Album(8);

            Click(player, hand, pos, ctrl: true);

            return (Inventory(player), Contents(chest));
        });
        Assert.Equal(("", "tapestry-north:ambush1 x1"), seen);
    }

    [Fact]
    public void An_album_makes_a_want_list_through_the_channel()
    {
        // A5: one sheet for the list of everything missing; it appears in the inventory.
        var client = FakeClient.Join(game, "wanter");
        var (fingerprint, missing) = game.OnMain(() =>
        {
            var hand = client.Player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Album(8);
            return (LedgerStore.Fingerprint(hand.Itemstack), BookState(hand.Itemstack).Entries);
        });

        client.SendToMod(new MakeWantListRequest { Book = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });

        client.WaitFor(() => FindOurs(client.Player, "invoice-wantlist") != null, "the want list");
        var seen = game.OnMain(() => (Lines(Mod, FindOurs(client.Player, "invoice-wantlist")!).Split(", ").Length,
            BookState(client.Player.InventoryManager.ActiveHotbarSlot.Itemstack!).Sheets));
        Assert.Equal((missing, 7), seen);
    }

    [Fact]
    public void A_want_list_is_filled_from_two_chests_and_then_becomes_parchment()
    {
        // W1, W2: what a chest has moves and the list gets shorter; the chest with the rest finishes it.
        var client = FakeClient.Join(game, "friend");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (first, firstChest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)firstChest).Inventory[0].Itemstack = Tapestry("ambush1");
            ((IBlockEntityContainer)firstChest).Inventory[1].Itemstack = Vanilla("ingot-iron");
            var (second, secondChest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)secondChest).Inventory[0].Itemstack = Tapestry("ambush2");
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.WantList, Tapestry("ambush1"), Tapestry("ambush2"));

            Click(player, hand, first);
            var afterFirst = (Inventory(player), Contents(firstChest), Left: Lines(Mod, hand.Itemstack!).Split(", ").Length);
            Click(player, hand, second);
            return (afterFirst, Inventory(player), Contents(secondChest));
        });
        Assert.Equal((("tapestry-north:ambush1 x1", "ingot-iron x1", 1),
            "paper-parchment x1, tapestry-north:ambush1 x1, tapestry-north:ambush2 x1", ""), seen);
    }

    [Fact]
    public void A_want_list_takes_what_fits_and_keeps_the_rest_on_the_list()
    {
        // W4: one free slot, two pieces in the chest: one moves, the other stays on the list.
        var client = FakeClient.Join(game, "short-of-room");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Tapestry("ambush1");
            ((IBlockEntityContainer)chest).Inventory[1].Itemstack = Tapestry("ambush2");
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.WantList, Tapestry("ambush1"), Tapestry("ambush2"));
            FillWithSticks(player, leaveOneFree: true);

            Click(player, hand, pos);

            int taken = Slots(player).Count(s => s.Itemstack?.Collectible is BlockTapestry);
            return (taken, Lines(Mod, hand.Itemstack!).Split(", ").Length, ChestLeft: ((IBlockEntityContainer)chest).Inventory.Count(s => !s.Empty));
        });
        Assert.Equal((1, 1, 1), seen);
    }

    [Fact]
    public void Cancelling_a_want_list_gives_the_sheet_back_to_the_album_in_the_off_hand()
    {
        // I12, through the channel.
        var client = FakeClient.Join(game, "canceller");
        var fingerprint = game.OnMain(() =>
        {
            var player = client.Player;
            player.Entity.LeftHandItemSlot.Itemstack = Album(7);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.WantList, Tapestry("ambush1"));
            return LedgerStore.Fingerprint(hand.Itemstack);
        });

        client.SendToMod(new CancelInvoiceRequest { Invoice = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });

        client.WaitFor(() => client.Player.InventoryManager.ActiveHotbarSlot.Empty, "the list to be gone");
        Assert.Equal(8, game.OnMain(() => BookState(client.Player.Entity.LeftHandItemSlot.Itemstack!).Sheets));
    }

    // ---- windows' requests

    [Fact]
    public void Clearing_a_ledger_through_the_channel_empties_it()
    {
        // A7 / L4
        var client = FakeClient.Join(game, "clearer");
        var fingerprint = game.OnMain(() =>
        {
            var hand = client.Player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Book("book-ledger-normal-brickred", 5, Vanilla("ingot-iron"), Vanilla("ingot-tin"));
            return LedgerStore.Fingerprint(hand.Itemstack);
        });

        client.SendToMod(new ClearLedgerRequest { Book = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });

        client.WaitFor(() => BookState(client.Player.InventoryManager.ActiveHotbarSlot.Itemstack!) == (5, 0), "the ledger to be cleared");
    }

    [Fact]
    public void A_request_about_a_book_that_changed_is_refused_and_changes_nothing()
    {
        // X5 / A10: the window showed one state, the hand holds another.
        var client = FakeClient.Join(game, "stale");
        var fingerprint = game.OnMain(() =>
        {
            var hand = client.Player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Book("book-ledger-normal-brickred", 5, Vanilla("ingot-iron"));
            int shown = LedgerStore.Fingerprint(hand.Itemstack);
            hand.Itemstack = Book("book-ledger-normal-brickred", 5, Vanilla("ingot-iron"), Vanilla("ingot-tin"));
            return shown;
        });

        client.SendToMod(new ClearLedgerRequest { Book = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });

        client.WaitFor(() => Errors(client).Contains("notshown"), "the refusal 'not the one in the window'");
        Assert.Equal((5, 2), game.OnMain(() => BookState(client.Player.InventoryManager.ActiveHotbarSlot.Itemstack!)));
    }

    // ---- access

    /// <summary>Claims the chest's surroundings for <paramref name="owner"/> while <paramref name="check"/> runs.</summary>
    T WithClaim<T>(IServerPlayer owner, BlockPos pos, Action<LandClaim> configure, Func<T> check)
    {
        var claim = LandClaim.CreateClaim(owner, 1);
        claim.AddArea(new Cuboidi(pos.X - 1, pos.Y - 1, pos.Z - 1, pos.X + 1, pos.Y + 1, pos.Z + 1));
        configure(claim);
        World.Claims.Add(claim);
        try { return check(); }
        finally { World.Claims.Remove(claim); }
    }

    [Fact]
    public void A_land_claim_refuses_every_gesture_of_the_visitor_until_it_allows_use()
    {
        // X1, X2: making a list, taking for an album and posting, on a chest in another player's claim.
        var owner = FakeClient.Join(game, "claim-keeper");
        var visitor = FakeClient.Join(game, "claim-guest");
        var seen = game.OnMain(() =>
        {
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            var inventory = ((IBlockEntityContainer)chest).Inventory;
            inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            inventory[1].Itemstack = Tapestry("ambush1");
            var player = visitor.Player;
            var hand = player.InventoryManager.ActiveHotbarSlot;

            (string Inventory, string Chest) Try(ItemStack held)
            {
                hand.Itemstack = held;
                Click(player, hand, pos);
                return (Inventory(player), Contents(chest));
            }

            var closed = WithClaim(owner.Player, pos, _ => { }, () => (
                Make: Try(Book("book-ledger-normal-brickred", 8)),
                Collect: Try(Album(8)),
                Post: Try(Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5)))));
            var shared = WithClaim(owner.Player, pos, c => c.PermittedPlayerUids[player.PlayerUID] = EnumBlockAccessFlags.Use,
                () => Try(Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5))));
            return (closed, shared);
        });

        const string untouched = "ingot-copper x5, tapestry-north:ambush1 x1";
        Assert.Equal((("", untouched), ("", untouched), ("", untouched)), seen.closed);
        Assert.Equal(("ingot-copper x5, paper-parchment x1", "tapestry-north:ambush1 x1"), seen.shared);
        visitor.WaitFor(() => Errors(visitor).Count(c => c == "noaccess") >= 3, "three refusals 'no access'");
    }

    [Fact]
    public void A_padlocked_chest_refuses_the_visitor_but_not_its_owner()
    {
        // X3
        var owner = FakeClient.Join(game, "padlock-keeper");
        var visitor = FakeClient.Join(game, "padlock-guest");
        var seen = game.OnMain(() =>
        {
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            bool locked = game.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>().TryLock(pos, owner.Player, "game:padlock");

            (string Inventory, string Chest) Try(IServerPlayer player)
            {
                var hand = player.InventoryManager.ActiveHotbarSlot;
                hand.Itemstack = Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5));
                Click(player, hand, pos);
                return (Inventory(player), Contents(chest));
            }
            return (locked, Visitor: Try(visitor.Player), Owner: Try(owner.Player));
        });
        Assert.Equal((true, ("", "ingot-copper x5"), ("ingot-copper x5, paper-parchment x1", "")), seen);
        visitor.WaitFor(() => Errors(visitor).Contains("noaccess"), "the refusal 'no access'");
    }

    [Theory]
    [InlineData("barrel", false)]
    [InlineData("firepit-cold", false)]
    [InlineData("chest-east", true)]
    public void Containers_the_mod_does_not_read_are_refused(string block, bool perPlayerLoot)
    {
        // I6, I7: "not supported yet"; and the loot chest of ruins, whose slots show the total, not the player's share.
        var client = FakeClient.Join(game, "u-" + block[..Math.Min(block.Length, 6)] + (perPlayerLoot ? "-loot" : ""));
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, _) = TestBlocks.Place(game, block, perPlayerLoot ? stack => stack.Attributes.SetBool("isPerPlayer", true) : null);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Book("book-ledger-normal-brickred", 8);

            Click(player, hand, pos);

            return BookState(hand.Itemstack).Sheets;
        });
        Assert.Equal(8, seen);
        client.WaitFor(() => Errors(client).Contains("unsupported"), "the refusal 'not supported'");
    }
}
