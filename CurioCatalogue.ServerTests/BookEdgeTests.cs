using System.Collections.Immutable;
using System.Reflection;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// The cases <see cref="BookFlowTests"/> left: a full album, blank lists, the config at work, a long name through the channel, and the
/// clicks that are not ours (they go to the base: chests open, lists lie on the ground). Same means: real players, chests, item classes.
/// The config is changed on the running server and put back in <c>finally</c>; tests of one collection run one after the other.
/// </summary>
[Collection(ServerCollection.Name)]
public class BookEdgeTests(GameServerFixture server) : BookTestBase(server)
{
    // ---- the config on a running server

    // The mod builds these on first use from the config of that moment ("keys ??= ..."), so a change after that needs them dropped.
    static readonly string[] ConfigDependent = ["keys", "store", "invoiceMaker", "invoicePoster", "collections"];

    void ForgetConfigDependents()
    {
        foreach (var name in ConfigDependent)
            typeof(CurioCatalogueModSystem).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(Mod, null);
    }

    /// <summary>Runs <paramref name="check"/> (on the main thread) with the config changed, and puts the config back.</summary>
    T WithConfig<T>(Action<CurioCatalogueConfig> change, Func<T> check)
    {
        var config = Mod.Config;
        var saved = (config.SoftEntryLimit, config.ConsumeSheets, config.MaxSheets, config.ExtraIgnoredAttributes, config.TapestryExclude, config.TapestryAdd);
        change(config);
        ForgetConfigDependents();
        try { return check(); }
        finally
        {
            (config.SoftEntryLimit, config.ConsumeSheets, config.MaxSheets, config.ExtraIgnoredAttributes, config.TapestryExclude, config.TapestryAdd) = saved;
            ForgetConfigDependents();
        }
    }

    [Fact]
    public void L5_a_ledger_at_the_soft_limit_warns_and_then_makes_no_more_intake_lists()
    {
        // L5: SoftEntryLimit 3; a book with 2 entries, an intake list with 1 new item: posted, warning; the next intake list is refused.
        var client = FakeClient.Join(game, "limit-three");
        var seen = game.OnMain(() => WithConfig(c => c.SoftEntryLimit = 3, () =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            player.Entity.LeftHandItemSlot.Itemstack = Book("book-ledger-normal-brickred", 7, Vanilla("ingot-iron"), Vanilla("ingot-tin"));
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5));

            Click(player, hand, pos);

            var entries = BookState(player.Entity.LeftHandItemSlot.Itemstack!).Entries;
            // The full book makes no intake list: a chest with something new to it.
            var (pos2, chest2) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest2).Inventory[0].Itemstack = Vanilla("ingot-lead", 2);
            var ledgerHand = player.InventoryManager.ActiveHotbarSlot;
            ledgerHand.Itemstack = player.Entity.LeftHandItemSlot.Itemstack;
            Click(player, ledgerHand, pos2);
            return (entries, ListMade: FindOurs(player, "invoice-incoming") != null, Inventory(player));
        }));
        Assert.Equal((3, false, "ingot-copper x5"), seen);
        // The post's own line has a 3 too ("Book entries: 2 → 3"): look for the warning's words.
        client.WaitFor(() => Chat(client).Any(l => l.Contains("its limit is 3")), "the warning about the limit of 3");
        client.WaitFor(() => Errors(client).Contains("ledgerfull"), "the refusal 'the book is full'");
    }

    [Fact]
    public void W3_with_ConsumeSheets_a_finished_want_list_is_gone_without_parchment()
    {
        var client = FakeClient.Join(game, "consumer");
        var seen = game.OnMain(() => WithConfig(c => c.ConsumeSheets = true, () =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Tapestry("ambush1");
            player.Entity.LeftHandItemSlot.Itemstack = Album(7);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.WantList, Tapestry("ambush1"));

            Click(player, hand, pos);

            return (Inventory(player), Contents(chest), ListLeft: FindOurs(player, "invoice-") != null || hand.Itemstack?.Collectible is ItemInvoice,
                Sheets: BookState(player.Entity.LeftHandItemSlot.Itemstack!).Sheets);
        }));
        Assert.Equal(("tapestry-north:ambush1 x1", "", false, 7), seen);
    }

    [Fact]
    public void An_intake_list_posted_with_ConsumeSheets_does_not_return_the_sheet()
    {
        // The same option for the other kinds (DESIGN: the hardcore option); the want list above is W3 itself.
        var client = FakeClient.Join(game, "hardcore");
        var seen = game.OnMain(() => WithConfig(c => c.ConsumeSheets = true, () =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            player.Entity.LeftHandItemSlot.Itemstack = Book("book-ledger-normal-brickred", 7);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5));

            Click(player, hand, pos);

            return (Inventory(player), BookState(player.Entity.LeftHandItemSlot.Itemstack!).Sheets);
        }));
        Assert.Equal(("ingot-copper x5", 7), seen);
    }

    [Fact]
    public void G2_ExtraIgnoredAttributes_decides_whether_damaged_and_plain_items_are_the_same()
    {
        var seen = game.OnMain(() =>
        {
            ItemStack Damaged() { var s = Vanilla("ingot-iron"); s.Attributes.SetInt("durability", 5); return s; }
            bool SameKey() => Mod.Keys.KeyOf(Damaged()).Equals(Mod.Keys.KeyOf(Vanilla("ingot-iron")));
            return (Default: SameKey(),
                Empty: WithConfig(c => c.ExtraIgnoredAttributes = [], SameKey),
                Listed: WithConfig(c => c.ExtraIgnoredAttributes = ["durability"], SameKey),
                Back: SameKey());
        });
        Assert.Equal((true, false, true, true), seen);
    }

    [Fact]
    public void G3_the_tapestry_collection_follows_the_config_when_it_is_built()
    {
        // The collection is built on first use ("collections ??= ..."), after the world has loaded, with the config of that moment:
        // a change on a running server counts only after the mod's cache is dropped (WithConfig); in the game that is a restart.
        var seen = game.OnMain(() =>
        {
            int Count() => Mod.Collections.Tapestries!.Members.Count;
            bool Has(string piece) => Mod.Collections.ForSample(Tapestry(piece)) != null;
            int all = Count();
            var excluded = WithConfig(c => c.TapestryExclude = ["ambush*"], () => (Count(), Has("ambush1"), Has("ambush2")));
            var added = WithConfig(c => c.TapestryAdd = ["somemod-piece"], () => (Count(), Has("somemod-piece")));
            return (Excluded: (all - excluded.Item1 >= 2, excluded.Item2, excluded.Item3), Added: (added.Item1 - all, added.Item2), Back: (Count() == all, Has("ambush1")));
        });
        Assert.Equal(((true, false, false), (1, true), (true, true)), seen);
    }

    [Fact]
    public void G4_Diagnostics_decides_whether_debug_lines_reach_the_log()
    {
        // The flag is copied into ModLog at the mod's start, so the test sets the flag itself (ModLog is internal): the wiring
        // "Config.Diagnostics -> ModLog.DebugEnabled" in Start is not run again here.
        var client = FakeClient.Join(game, "diagnoser");
        var flag = typeof(CurioCatalogueModSystem).Assembly.GetType("CurioCatalogue.ModLog")!.GetProperty("DebugEnabled", BindingFlags.Static | BindingFlags.Public)!;
        int DebugLines() => game.LogEntries().Count(l => l.Text.Contains("[CurioCatalogue] Server debug"));
        int Linesmade(bool on) => game.OnMain(() =>
        {
            flag.SetValue(null, on);
            try
            {
                int before = DebugLines();
                var player = client.Player;
                var (pos, chest) = TestBlocks.Place(game, "chest-east");
                ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
                var hand = player.InventoryManager.ActiveHotbarSlot;
                hand.Itemstack = Book("book-ledger-normal-brickred", 8);
                Click(player, hand, pos);
                return DebugLines() - before;
            }
            finally { flag.SetValue(null, false); }
        });
        Assert.Equal((true, false), (Linesmade(on: true) > 0, Linesmade(on: false) > 0));
    }

    // ---- full album, blank lists

    [Fact]
    public void I4_a_full_album_does_not_take_the_sheet_back_and_the_list_becomes_parchment()
    {
        var client = FakeClient.Join(game, "full-album");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            int max = Mod.Config.MaxSheets;
            player.Entity.LeftHandItemSlot.Itemstack = Album(max);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5));

            Click(player, hand, pos);

            return (Inventory(player), Contents(chest), BookState(player.Entity.LeftHandItemSlot.Itemstack!).Sheets == max);
        });
        Assert.Equal(("ingot-copper x5, paper-parchment x1", "", true), seen);
        int sheets = game.OnMain(() => Mod.Config.MaxSheets);
        client.WaitFor(() => Chat(client).Any(l => l.Contains($"The album already has {sheets} sheets")), "the chat line 'the album already has N sheets'");
    }

    [Theory]
    [InlineData("Incoming")]
    [InlineData("Outgoing")]
    [InlineData("WantList")]
    public void I11_a_blank_list_is_refused_and_stays_in_the_hand(string kindName)
    {
        var kind = Enum.Parse<InvoiceKind>(kindName);
        var client = FakeClient.Join(game, "blank-" + kindName[..3].ToLowerInvariant());
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            player.Entity.LeftHandItemSlot.Itemstack = Book("book-ledger-normal-brickred", 5);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = new ItemStack(World.GetItem(InvoiceMaker.InvoiceCode(kind))); // as from the creative inventory: no data

            Click(player, hand, pos);

            return (Inventory(player), Contents(chest), ListKept: hand.Itemstack?.Collectible is ItemInvoice, Sheets: BookState(player.Entity.LeftHandItemSlot.Itemstack!).Sheets);
        });
        Assert.Equal(("", "ingot-copper x5", true, 5), seen);
        client.WaitFor(() => Errors(client).Contains("blank"), "the refusal 'nothing to fill'");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void I11_cancelling_a_blank_list_removes_it_and_the_book_keeps_its_sheets(bool withBook)
    {
        var client = FakeClient.Join(game, "cancel-blank-" + (withBook ? "b" : "n"));
        var fingerprint = game.OnMain(() =>
        {
            var player = client.Player;
            if (withBook) player.Entity.LeftHandItemSlot.Itemstack = Book("book-ledger-normal-brickred", 5);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = new ItemStack(World.GetItem(InvoiceMaker.InvoiceCode(InvoiceKind.Incoming)));
            return LedgerStore.Fingerprint(hand.Itemstack);
        });

        client.SendToMod(new CancelInvoiceRequest { Invoice = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });

        client.WaitFor(() => client.Player.InventoryManager.ActiveHotbarSlot.Empty, "the blank list to be gone");
        var seen = game.OnMain(() => (Inventory(client.Player), withBook ? BookState(client.Player.Entity.LeftHandItemSlot.Itemstack!).Sheets : 5));
        Assert.Equal(("", 5), seen); // no parchment, no extra sheet
    }

    // ---- branches the coverage review found untested (T9 of the server tests plan)

    Ledger ReadBook(ItemStack book) => Mod.Store.ReadLedger(book, ((ItemLedger)book.Collectible).LedgerMode);

    /// <summary>
    /// How many items of ours with this code prefix lie on the ground near the player. Main thread. Every test player stands
    /// at the spawn, so a test compares the count before and after its own click.
    /// </summary>
    int OnGroundNear(IServerPlayer player, string codePrefix) =>
        World.GetEntitiesAround(player.Entity.Pos.XYZ, 4, 4, e => e is EntityItem { Itemstack: { } st }
            && st.Collectible.Code.Domain == CurioCatalogueModSystem.ModId && st.Collectible.Code.Path.StartsWith(codePrefix)).Length;

    /// <summary>
    /// The test players are in creative, where the inventory is never full: <c>TryGiveItemstack</c> puts a stack into any player
    /// inventory the player has opened (decompiled VintagestoryLib 1.22.7, <c>PlayerInventoryManager.GetBestSuitedSlot</c>), and in
    /// creative that includes the creative inventory, which swallows it (closing it does not help: it stays opened in creative).
    /// In survival a full inventory refuses the stack and it is left whole. The player is this test's own.
    /// </summary>
    static void AsInSurvival(IServerPlayer player)
    {
        player.WorldData.CurrentGameMode = EnumGameMode.Survival;
        player.BroadcastPlayerData(true);
    }

    [Fact]
    public void A7_clearing_an_album_starts_it_over_and_keeps_its_sheets_and_name()
    {
        // An album's "Clear" starts it over: every member missing again (domain.md, Albums and collections).
        var client = FakeClient.Join(game, "restarter");
        var (fingerprint, members, collected) = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Tapestry("ambush1");
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Album(8);
            Mod.Store.WriteLedger(hand.Itemstack, ReadBook(hand.Itemstack) with { Name = "Wall pieces" });
            int members = BookState(hand.Itemstack).Entries;
            Click(player, hand, pos); // takes ambush1: one missing fewer
            return (LedgerStore.Fingerprint(hand.Itemstack!), members, BookState(hand.Itemstack!).Entries);
        });
        Assert.Equal(members - 1, collected);

        client.SendToMod(new ClearLedgerRequest { Book = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });

        client.WaitFor(() => BookState(client.Player.InventoryManager.ActiveHotbarSlot.Itemstack!).Entries == members, "the album to start over");
        var seen = game.OnMain(() =>
        {
            var album = ReadBook(client.Player.InventoryManager.ActiveHotbarSlot.Itemstack!);
            return (album.Sheets, album.Name, album.Album?.CollectionId);
        });
        Assert.Equal((8, "Wall pieces", "tapestries"), seen);
        client.WaitFor(() => Chat(client).Any(l => l.Contains(members.ToString())), "the chat line 'starts over, still to collect'");
    }

    [Fact]
    public void A5_a_want_list_from_the_album_in_the_off_hand_goes_into_the_empty_main_hand()
    {
        var client = FakeClient.Join(game, "wanter-offhand");
        var fingerprint = game.OnMain(() =>
        {
            // The main hand is the fourth hotbar slot and the first is free: given away instead, the list would go to the first.
            client.Player.InventoryManager.ActiveHotbarSlotNumber = 3;
            var offhand = client.Player.Entity.LeftHandItemSlot;
            offhand.Itemstack = Album(8);
            return LedgerStore.Fingerprint(offhand.Itemstack);
        });

        client.SendToMod(new MakeWantListRequest { Book = new ShownItem { OffHand = true, Fingerprint = fingerprint } });

        client.WaitFor(() => client.Player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible.Code.Path == "invoice-wantlist", "the want list in the main hand");
        Assert.Equal(3, game.OnMain(() => client.Player.InventoryManager.ActiveHotbarSlotNumber));
        Assert.Equal(7, game.OnMain(() => BookState(client.Player.Entity.LeftHandItemSlot.Itemstack!).Sheets));
    }

    [Fact]
    public void A5_a_want_list_lands_on_the_ground_when_the_inventory_is_full()
    {
        var client = FakeClient.Join(game, "wanter-full");
        var (fingerprint, onGround) = game.OnMain(() =>
        {
            var hand = client.Player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Album(8);
            AsInSurvival(client.Player);
            client.Player.Entity.LeftHandItemSlot.Itemstack = Vanilla("stick"); // the off-hand would take it too
            FillWithSticks(client.Player);
            return (LedgerStore.Fingerprint(hand.Itemstack), OnGroundNear(client.Player, "invoice-wantlist"));
        });

        client.SendToMod(new MakeWantListRequest { Book = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });

        client.WaitFor(() => OnGroundNear(client.Player, "invoice-wantlist") > onGround, "the want list on the ground");
        Assert.Equal(7, game.OnMain(() => BookState(client.Player.InventoryManager.ActiveHotbarSlot.Itemstack!).Sheets));
    }

    [Theory]
    [InlineData("allcollected")]
    [InlineData("nosheets")]
    public void A5_a_want_list_is_refused_when_nothing_is_missing_or_no_sheet_is_left(string refusal)
    {
        var client = FakeClient.Join(game, "wl-" + refusal[..5]);
        var (fingerprint, before) = game.OnMain(() =>
        {
            var hand = client.Player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = refusal == "nosheets" ? Album(0) : Album(8);
            if (refusal == "allcollected") Mod.Store.WriteLedger(hand.Itemstack, ReadBook(hand.Itemstack) with { Entries = ImmutableDictionary<ItemKey, Entry>.Empty });
            return (LedgerStore.Fingerprint(hand.Itemstack), BookState(hand.Itemstack));
        });

        client.SendToMod(new MakeWantListRequest { Book = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });

        client.WaitFor(() => Errors(client).Contains(refusal), $"the refusal '{refusal}'");
        var seen = game.OnMain(() => (FindOurs(client.Player, "invoice-") != null, BookState(client.Player.InventoryManager.ActiveHotbarSlot.Itemstack!)));
        Assert.Equal((false, before), seen);
    }

    [Fact]
    public void L1_an_intake_list_lands_on_the_ground_when_the_inventory_is_full()
    {
        var client = FakeClient.Join(game, "maker-full");
        var (sheets, onGround) = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Book("book-ledger-normal-brickred", 8);
            AsInSurvival(player);
            player.Entity.LeftHandItemSlot.Itemstack = Vanilla("stick"); // the off-hand would take it too
            FillWithSticks(player);
            int before = OnGroundNear(player, "invoice-incoming");

            Click(player, hand, pos);
            return (BookState(hand.Itemstack!).Sheets, before);
        });
        Assert.Equal(7, sheets);
        client.WaitFor(() => OnGroundNear(client.Player, "invoice-incoming") > onGround, "the intake list on the ground");
    }

    [Fact]
    public void I4_a_full_ledger_does_not_take_the_sheet_back_and_says_the_book_is_full()
    {
        // I4 for a ledger: the same parchment as for a full album, with the book's own words.
        var client = FakeClient.Join(game, "full-ledger");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            int max = Mod.Config.MaxSheets;
            player.Entity.LeftHandItemSlot.Itemstack = Book("book-ledger-normal-brickred", max);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5));

            Click(player, hand, pos);

            return (Inventory(player), BookState(player.Entity.LeftHandItemSlot.Itemstack!).Sheets == max, max);
        });
        Assert.Equal(("ingot-copper x5, paper-parchment x1", true), (seen.Item1, seen.Item2));
        client.WaitFor(() => Chat(client).Any(l => l.Contains($"The book already has {seen.max} sheets")), "the chat line 'the book already has N sheets'");
    }

    // ---- a long name through the channel

    [Theory]
    [InlineData(60, 40)]
    [InlineData(40, 40)]
    [InlineData(10, 10)]
    public void A6_a_name_is_cut_to_forty_characters_on_the_server(int sent, int stored)
    {
        var client = FakeClient.Join(game, "namer-" + sent);
        var fingerprint = game.OnMain(() =>
        {
            var hand = client.Player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Book("book-ledger-normal-brickred", 5);
            return LedgerStore.Fingerprint(hand.Itemstack);
        });

        client.SendToMod(new RenameLedgerRequest { Name = new string('x', sent), Book = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });

        client.WaitFor(() => NameOf(client).Length > 0, "the ledger to be renamed");
        Assert.Equal(stored, game.OnMain(() => NameOf(client)).Length);
    }

    [Fact]
    public void A6_a_name_never_ends_in_half_a_surrogate_pair()
    {
        var client = FakeClient.Join(game, "namer-emoji");
        var fingerprint = game.OnMain(() =>
        {
            var hand = client.Player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Book("book-ledger-normal-brickred", 5);
            return LedgerStore.Fingerprint(hand.Itemstack);
        });

        // 39 letters, then a pair that would be cut in the middle at 40.
        client.SendToMod(new RenameLedgerRequest { Name = new string('x', 39) + "\U0001F600", Book = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });

        client.WaitFor(() => NameOf(client).Length > 0, "the ledger to be renamed");
        Assert.Equal(39, game.OnMain(() => NameOf(client)).Length);
    }

    string NameOf(FakeClient client) // runs on the main thread (WaitFor calls it there); outside WaitFor use game.OnMain
    {
        var stack = client.Player.InventoryManager.ActiveHotbarSlot.Itemstack!;
        return Mod.Store.ReadLedger(stack, ((ItemLedger)stack.Collectible).LedgerMode).Name;
    }

    // ---- clicks that are not ours

    /// <summary>The engine's order for a right click on a block: the held item first, then the block unless the item took the click.</summary>
    (EnumHandHandling Handling, bool BlockOpened) RightClick(IServerPlayer player, ItemSlot slot, BlockPos pos, bool shift, bool ctrl)
    {
        var controls = player.Entity.Controls;
        controls.ShiftKey = shift;
        controls.CtrlKey = ctrl;
        try
        {
            var selection = new BlockSelection { Position = pos.Copy(), Face = BlockFacing.UP, HitPosition = new Vec3d(0.5, 1, 0.5) };
            var handling = EnumHandHandling.NotHandled;
            slot.Itemstack!.Collectible.OnHeldInteractStart(slot, player.Entity, selection, null, true, ref handling);
            if (handling != EnumHandHandling.NotHandled) return (handling, false);
            var block = World.BlockAccessor.GetBlock(pos);
            block.OnBlockInteractStart(World, player, selection);
            var chest = (IBlockEntityContainer)World.BlockAccessor.GetBlockEntity(pos)!;
            return (handling, player.InventoryManager.OpenedInventories.Contains(chest.Inventory));
        }
        finally
        {
            controls.ShiftKey = false;
            controls.CtrlKey = false;
            player.InventoryManager.CloseInventory(((IBlockEntityContainer)World.BlockAccessor.GetBlockEntity(pos)!).Inventory);
        }
    }

    [Theory]
    [InlineData("book-ledger-normal-brickred", false)]
    [InlineData("book-ledger-normal-brickred", true)] // Ctrl without Shift is not ours either
    [InlineData("book-album-normal-brickred", false)]
    [InlineData("invoice-incoming", false)]
    [InlineData("invoice-wantlist", true)]
    public void I9_a_click_without_Shift_leaves_the_chest_to_open_as_usual(string item, bool ctrl)
    {
        var client = FakeClient.Join(game, "o-" + Guid.NewGuid().ToString("N")[..8]);
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-copper", 5);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = item.StartsWith("book-album") ? Album(8) : item.StartsWith("book") ? Book(item, 8) : Invoice(Enum.Parse<InvoiceKind>(item[8..] == "wantlist" ? "WantList" : "Incoming"), Vanilla("ingot-copper", 5));

            var (handling, opened) = RightClick(player, hand, pos, shift: false, ctrl);

            return (handling, opened, Inventory(player), Contents(chest));
        });
        Assert.Equal((EnumHandHandling.NotHandled, true, "", "ingot-copper x5"), seen);
    }

    [Theory]
    [InlineData("book-ledger-normal-brickred", false)]
    [InlineData("invoice-incoming", false)]
    [InlineData("book-album-normal-brickred", true)] // I8: Shift + Ctrl is no gesture of the album, so it goes to the game too
    public void I10_Shift_on_the_floor_is_the_games_and_the_book_lies_on_the_ground(string item, bool ctrl)
    {
        var client = FakeClient.Join(game, "ground-" + item[5..9] + (ctrl ? "-c" : ""));
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var pos = TestBlocks.FreePos(game);
            World.BlockAccessor.SetBlock(World.GetBlock(new AssetLocation("game", "rock-granite")).BlockId, pos);
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = item.StartsWith("book-album") ? Album(8) : item.StartsWith("book") ? Book(item, 8) : Invoice(InvoiceKind.Incoming, Vanilla("ingot-copper", 5));
            Stand(player, pos.UpCopy());

            var controls = player.Entity.Controls;
            controls.ShiftKey = true;
            controls.CtrlKey = ctrl; // the game's ground storage needs only Shift (BehaviorGroundStorable.Interact)
            EnumHandHandling handling;
            try
            {
                var selection = new BlockSelection { Position = pos.Copy(), Face = BlockFacing.UP, HitPosition = new Vec3d(0.5, 1, 0.5) };
                handling = EnumHandHandling.NotHandled;
                hand.Itemstack.Collectible.OnHeldInteractStart(hand, player.Entity, selection, null, true, ref handling);
            }
            finally { controls.ShiftKey = false; controls.CtrlKey = false; }

            var ground = World.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(pos.UpCopy());
            return (handling != EnumHandHandling.NotHandled, OnGround: ground?.Inventory.Count(s => !s.Empty));
        });
        Assert.Equal((true, (int?)1), (seen.Item1, seen.OnGround));
    }
}
