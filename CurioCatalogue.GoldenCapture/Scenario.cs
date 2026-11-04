using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace CurioCatalogue.GoldenCapture;

/// <summary>
/// The items of the golden set (plan backward-compat, B2), made the way a player makes them: books crafted in a real player's grid,
/// lists made and posted by the gestures (<c>OnHeldInteractStart</c> on the server with Shift and Ctrl, as <c>BookTestBase</c> does),
/// the window's actions sent as the release's own network requests. Each item is written with what went into it (<see cref="Inputs"/>),
/// and the run stops if the release itself does not read that back: then the scenario, not the set, is wrong.
/// Written against the types of the release it captures (0.1.1); at the next release it is rewritten for that one, and this one
/// stays in git history with its set.
/// </summary>
sealed class Scenario(GameServerFixture game)
{
    const int OutputSlot = 9;
    const string Ours = "curiocatalogue:";

    readonly List<GoldenStack> stacks = [];

    /// <summary>The made items by name, as the golden set names them: <see cref="Fixture"/> lays them out in the world.</summary>
    public Dictionary<string, ItemStack> Made { get; } = [];

    CurioCatalogueModSystem Mod => game.Api.ModLoader.GetModSystem<CurioCatalogueModSystem>();
    IWorldAccessor World => game.Api.World;

    public IReadOnlyList<GoldenStack> Run()
    {
        var client = FakeClient.Join(game, "capturer");
        // The pieces of the tapestry collection as the release has them, in its order.
        var pieces = game.OnMain(() => Mod.Collections.Tapestries!.Members.Select(m => ((ItemStack)m.Sample).Attributes.GetString("type")!).ToList());
        if (pieces.Count < 4) throw new InvalidOperationException($"the tapestry collection has {pieces.Count} pieces");

        game.OnMain(() =>
        {
            LedgerAndItsLists(client.Player, pieces);
            MaterialsAndRecoloured(client.Player);
            Aged(client.Player);
            Spent(client.Player);
            return 0;
        });
        Named(client);
        PartlyFullAlbumAndWantList(client, pieces);
        game.OnMain(() => { FullAlbum(client.Player, pieces); return 0; });
        return stacks;
    }

    // ---- the items

    /// <summary>A ledger that learnt a chest (an intake list posted), then an intake list and a pick list made by it and kept.</summary>
    void LedgerAndItsLists(IServerPlayer player, List<string> pieces)
    {
        var book = Craft(player, "ledger-fallback", (0, Vanilla("metalnailsandstrips-iron")), (3, Vanilla("book-normal-teal")), (4, Vanilla("inkandquill")), (6, Vanilla("metalnailsandstrips-iron")));
        var pos = Chest(player, Vanilla("ingot-copper", 5), Tapestry(pieces[0]), Vanilla("ingot-iron", 3));
        var hand = player.InventoryManager.ActiveHotbarSlot;
        hand.Itemstack = book;
        Click(player, hand, pos);
        var list = TakeOurs(player, "invoice-incoming");
        player.Entity.LeftHandItemSlot.Itemstack = hand.Itemstack;
        hand.Itemstack = list;
        Click(player, hand, pos);
        book = player.Entity.LeftHandItemSlot.Itemstack!;
        Clear(player);
        Record("ledger", book, new Inputs(
            "ledger-fallback from game:book-normal-teal (8 sheets); an intake list of a chest with 5 copper ingots, a tapestry and 3 iron ingots, posted with the book in the off-hand (the sheet back)",
            Ours + "book-ledger-normal-teal",
            Book: new BookIn("Types", 8, "", Sorted(E("game:ingot-copper"), E("game:tapestry-north", pieces[0]), E("game:ingot-iron")))));

        pos = Chest(player, Vanilla("ingot-tin", 3), Tapestry(pieces[1]));
        hand.Itemstack = book;
        Click(player, hand, pos);
        Record("intake-list", TakeOurs(player, "invoice-incoming"), new Inputs(
            "Shift + RMB with the ledger above on a chest with 3 tin ingots and a tapestry it lacks; kept, not posted",
            Ours + "invoice-incoming",
            Invoice: new InvoiceIn("Incoming", [E("game:ingot-tin", count: 3), E("game:tapestry-north", pieces[1])])));

        pos = Chest(player, Vanilla("ingot-copper", 2), Vanilla("stick", 4), Vanilla("ingot-iron"));
        Click(player, hand, pos, ctrl: true);
        Record("pick-list", TakeOurs(player, "invoice-outgoing"), new Inputs(
            "Shift + Ctrl + RMB with the same ledger on a chest with 2 copper ingots, 4 sticks and an iron ingot; kept, not posted",
            Ours + "invoice-outgoing",
            Invoice: new InvoiceIn("Outgoing", [E("game:ingot-copper", count: 2), E("game:ingot-iron")])));
        Clear(player);
    }

    /// <summary>A bill of materials that learnt a chest by counts, and the same book recoloured with leather (entries kept).</summary>
    void MaterialsAndRecoloured(IServerPlayer player)
    {
        var ledger = Craft(player, "ledger-fallback", (0, Vanilla("metalnailsandstrips-iron")), (3, Vanilla("book-normal-brickred")), (4, Vanilla("inkandquill")), (6, Vanilla("metalnailsandstrips-iron")));
        var book = Craft(player, "ledger-switchmode", (4, ledger));
        var pos = Chest(player, Vanilla("ingot-iron", 4), Vanilla("ingot-copper", 2));
        var hand = player.InventoryManager.ActiveHotbarSlot;
        hand.Itemstack = book;
        Click(player, hand, pos);
        var list = TakeOurs(player, "invoice-incoming");
        player.Entity.LeftHandItemSlot.Itemstack = hand.Itemstack;
        hand.Itemstack = list;
        Click(player, hand, pos);
        book = player.Entity.LeftHandItemSlot.Itemstack!;
        Clear(player);
        var entries = Sorted(E("game:ingot-iron", count: 4), E("game:ingot-copper", count: 2));
        Record("materials", book, new Inputs(
            "ledger-fallback from game:book-normal-brickred, then ledger-switchmode (8 sheets); an intake list of a chest with 4 iron and 2 copper ingots, posted with the book in the off-hand",
            Ours + "book-materials-normal-brickred",
            Book: new BookIn("Counts", 8, "", entries)));

        var recoloured = Craft(player, "ledger-recolor", (0, book.Clone()), (1, Vanilla("leather-normal-black")));
        Record("recoloured", recoloured, new Inputs(
            "the bill of materials above and game:leather-normal-black in the grid (ledger-recolor)",
            Ours + "book-materials-normal-darkgray",
            Book: new BookIn("Counts", 8, "", entries)));
    }

    /// <summary>An empty ledger aged with sulfuric acid.</summary>
    void Aged(IServerPlayer player)
    {
        var ledger = Craft(player, "ledger-fallback", (0, Vanilla("metalnailsandstrips-iron")), (3, Vanilla("book-normal-orange")), (4, Vanilla("inkandquill")), (6, Vanilla("metalnailsandstrips-iron")));
        var aged = Craft(player, "ledger-age", (0, ledger), (1, Jug("acid-full-sulfuric", 1.5f)));
        Record("aged", aged, new Inputs(
            "ledger-fallback from game:book-normal-orange (8 sheets), then ledger-age with a jug of 1.5 l of sulfuric acid",
            Ours + "book-ledger-aged-orange",
            Book: new BookIn("Types", 8, "", [])));
    }

    /// <summary>A ledger of 32 sheets (restored from a ruins' book) that spent three sheets on lists.</summary>
    void Spent(IServerPlayer player)
    {
        var old = Vanilla("clutter");
        old.Attributes.SetString("type", "bookshelves/large-book-closed");
        var book = Craft(player, "ledger-restore", (0, Jug("limewaterportion", 1.5f)), (4, old), (8, Vanilla("inkandquill")));
        var pos = Chest(player, Vanilla("ingot-copper"));
        var hand = player.InventoryManager.ActiveHotbarSlot;
        hand.Itemstack = book;
        for (int i = 0; i < 3; i++)
        {
            Click(player, hand, pos);
            TakeOurs(player, "invoice-incoming"); // lost, as a player may lose them
        }
        Record("spent", hand.Itemstack!, new Inputs(
            "ledger-restore from game:clutter bookshelves/large-book-closed, a jug of 1.5 l of lime water and ink (32 sheets); three intake lists made of a chest with a copper ingot and thrown away",
            Ours + "book-ledger-normal-darkgreen",
            Book: new BookIn("Types", 29, "", [])));
        Clear(player);
    }

    /// <summary>A ledger renamed through the book's window (the release's rename request): a name with non-ASCII letters.</summary>
    void Named(FakeClient client)
    {
        const string name = "Комора № 1";
        int fingerprint = game.OnMain(() =>
        {
            var hand = client.Player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Craft(client.Player, "ledger-fallback", (0, Vanilla("metalnailsandstrips-iron")), (3, Vanilla("book-normal-purple")), (4, Vanilla("inkandquill")), (6, Vanilla("metalnailsandstrips-iron")));
            return LedgerStore.Fingerprint(hand.Itemstack);
        });
        client.SendToMod(new RenameLedgerRequest { Name = name, Book = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });
        client.WaitFor(() => client.Player.InventoryManager.ActiveHotbarSlot.Itemstack is { } book && LedgerStore.ReadName(book) == name, "the book renamed");
        game.OnMain(() =>
        {
            Record("named", client.Player.InventoryManager.ActiveHotbarSlot.Itemstack!, new Inputs(
                $"ledger-fallback from game:book-normal-purple (8 sheets), renamed \"{name}\" in its window",
                Ours + "book-ledger-normal-purple",
                Book: new BookIn("Types", 8, name, [])));
            Clear(client.Player);
            return 0;
        });
    }

    /// <summary>An album that took three pieces from a chest (a duplicate left), then made a want list in its window.</summary>
    void PartlyFullAlbumAndWantList(FakeClient client, List<string> pieces)
    {
        var collected = pieces.Take(3).ToList();
        int fingerprint = game.OnMain(() =>
        {
            var player = client.Player;
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = NewAlbum(player, "olive", pieces[0]);
            var pos = Chest(player, Tapestry(pieces[0]), Tapestry(pieces[1]), Tapestry(pieces[0], 2), Tapestry(pieces[2]));
            Click(player, hand, pos);
            foreach (var slot in Slots(player).Skip(1)) slot.Itemstack = null; // the pieces taken
            return LedgerStore.Fingerprint(hand.Itemstack!);
        });
        client.SendToMod(new MakeWantListRequest { Book = new ShownItem { HotbarSlot = 0, Fingerprint = fingerprint } });
        client.WaitFor(() => FindOurs(client.Player, "invoice-wantlist") != null, "the want list");

        var missing = pieces.Except(collected).ToList();
        game.OnMain(() =>
        {
            var player = client.Player;
            Record("album-partial", player.InventoryManager.ActiveHotbarSlot.Itemstack!, new Inputs(
                $"ledger-fallback from game:book-normal-olive, then ledger-album with a tapestry (8 sheets); Shift + RMB on a chest with the pieces {string.Join(", ", collected)} (and a second {pieces[0]}, left there); then a want list made in its window (a sheet spent)",
                Ours + "book-album-normal-olive",
                Book: new BookIn("Album", 7, "", Sorted([.. missing.Select(p => E("game:tapestry-north", p))]),
                    new AlbumIn(CollectionRegistry.TapestriesId, Sorted([.. collected.Select(p => E("game:tapestry-north", p))]), Full: false))));
            Record("want-list", TakeOurs(player, "invoice-wantlist"), new Inputs(
                "the want list of the album above, made in its window; kept, not posted",
                Ours + "invoice-wantlist",
                Invoice: new InvoiceIn("WantList", [.. missing.Select(p => E("game:tapestry-north", p))])));
            Clear(player);
            return 0;
        });
    }

    /// <summary>An album that took every piece of the collection, from chests of 16 pieces.</summary>
    void FullAlbum(IServerPlayer player, List<string> pieces)
    {
        var hand = player.InventoryManager.ActiveHotbarSlot;
        hand.Itemstack = NewAlbum(player, "gray", pieces[0]);
        // The album takes what fits into the inventory and leaves the rest in the chest; as a player would, the inventory is
        // emptied and the chest clicked again until it is empty.
        foreach (var chunk in pieces.Chunk(16))
        {
            var pos = Chest(player, [.. chunk.Select(p => Tapestry(p))]);
            var chest = ((IBlockEntityContainer)World.BlockAccessor.GetBlockEntity(pos)).Inventory;
            for (int left = chunk.Length; left > 0;)
            {
                Click(player, hand, pos);
                foreach (var slot in Slots(player).Skip(1)) slot.Itemstack = null;
                int now = chest.Count(s => !s.Empty);
                if (now == left) throw new InvalidOperationException($"album-full: the album took nothing of {now} pieces in the chest");
                left = now;
            }
        }
        Record("album-full", hand.Itemstack!, new Inputs(
            $"ledger-fallback from game:book-normal-gray, then ledger-album with a tapestry (8 sheets); Shift + RMB on chests with all {pieces.Count} pieces, 16 a chest, each clicked until empty (the inventory emptied in between)",
            Ours + "book-album-normal-gray",
            Book: new BookIn("Album", 8, "", [], new AlbumIn(CollectionRegistry.TapestriesId, Sorted([.. pieces.Select(p => E("game:tapestry-north", p))]), Full: true))));
        Clear(player);
    }

    ItemStack NewAlbum(IServerPlayer player, string cover, string piece)
    {
        var ledger = Craft(player, "ledger-fallback", (0, Vanilla("metalnailsandstrips-iron")), (3, Vanilla("book-normal-" + cover)), (4, Vanilla("inkandquill")), (6, Vanilla("metalnailsandstrips-iron")));
        return Craft(player, "ledger-album", (0, ledger), (1, Tapestry(piece)));
    }

    // ---- recording, checked by the release

    void Record(string name, ItemStack? stack, Inputs inputs)
    {
        if (stack == null) throw new InvalidOperationException($"{name}: not made");
        Check(name, stack, inputs);
        stacks.Add(Golden.Of(World, name, stack.Clone(), inputs));
        Made[name] = stack.Clone();
        Console.WriteLine($"  {name}: {stack.Collectible.Code}");
    }

    /// <summary>The release reads the item as the inputs say; otherwise the scenario did not make what it meant to.</summary>
    void Check(string name, ItemStack stack, Inputs inputs)
    {
        var problems = new List<string>();
        void Same<T>(string what, T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) problems.Add($"{what}: expected {expected}, the release reads {actual}");
        }
        static string Of(IEnumerable<EntryIn> entries) => "[" + string.Join(", ", entries) + "]";

        Same("code", inputs.Code, stack.Collectible.Code.ToString());
        if (inputs.Book is { } b)
        {
            var book = Mod.Store.ReadLedger(stack, ((ItemLedger)stack.Collectible).LedgerMode);
            Same("mode", b.Mode, book.Mode.ToString());
            Same("sheets", b.Sheets, book.Sheets);
            Same("name", b.Name, book.Name);
            Same("entries", Of(b.Entries), Of(Sorted([.. book.Entries.Values.Select(e => EntryIn.Of((ItemStack)e.Sample, e.Count))])));
            Same("collection", b.Album?.Collection, book.Album?.CollectionId);
            if (b.Album is { } a)
            {
                var collection = Mod.Store.CollectionOf(book) ?? throw new InvalidOperationException($"{name}: the release has no collection {a.Collection}");
                Same("full", a.Full, book.IsEmpty);
                var missing = book.Entries.Keys.ToHashSet();
                Same("collected", Of(a.Collected), Of(Sorted([.. collection.Members.Where(m => !missing.Contains(m.Key)).Select(m => EntryIn.Of((ItemStack)m.Sample, 1))])));
            }
        }
        if (inputs.Invoice is { } i)
        {
            var invoice = Mod.Store.ReadInvoice(stack, ((ItemInvoice)stack.Collectible).InvoiceKind);
            Same("kind", i.Kind, invoice.Kind.ToString());
            Same("lines", Of(i.Lines), Of(invoice.Entries.Select(e => EntryIn.Of((ItemStack)e.Sample, e.Count))));
        }
        if (problems.Count > 0) throw new InvalidOperationException($"{name}:\n  " + string.Join("\n  ", problems));
    }

    static EntryIn E(string code, string? type = null, int count = 1) => new(code, type, count);

    static List<EntryIn> Sorted(params EntryIn[] entries) => [.. entries.Order()];

    // ---- the game's ways (the release's types only: BookTestBase belongs to the current build)

    ItemStack Vanilla(string code, int size = 1) =>
        World.GetItem(new AssetLocation("game", code)) is { } item ? new ItemStack(item, size)
        : new ItemStack(World.GetBlock(new AssetLocation("game", code)) ?? throw new InvalidOperationException($"no game:{code}"), size);

    ItemStack Tapestry(string type, int size = 1)
    {
        var stack = Vanilla("tapestry-north", size);
        stack.Attributes.SetString("type", type);
        return stack;
    }

    ItemStack Jug(string liquid, float litres)
    {
        var jug = Vanilla("jug-blue-fired");
        // itemsPerLitre 100 for both liquids used here (survival itemtypes/liquid/limewaterportion.json, acid.json).
        ((BlockLiquidContainerBase)jug.Collectible).SetContent(jug, Vanilla(liquid, (int)(litres * 100)));
        return jug;
    }

    static InventoryCraftingGrid Grid(IServerPlayer player) =>
        (InventoryCraftingGrid)player.InventoryManager.GetOwnInventory(GlobalConstants.craftingInvClassName);

    /// <summary>
    /// Crafts in the player's grid as the server does with the client's packets (docs/modding.md, Crafting on the server): the
    /// stacks put in one slot at a time, the output taken by a left click into the mouse slot; what is left in the grid is thrown away.
    /// </summary>
    ItemStack Craft(IServerPlayer player, string recipe, params (int Slot, ItemStack Stack)[] put)
    {
        var grid = Grid(player);
        foreach (var (slot, stack) in put)
        {
            grid[slot].Itemstack = stack;
            grid[slot].MarkDirty();
        }
        if (grid.MatchingRecipe?.Name?.Path != recipe) throw new InvalidOperationException($"the grid found {grid.MatchingRecipe?.Name?.Path ?? "no recipe"}, not {recipe}");
        var mouse = player.InventoryManager.MouseItemSlot;
        var op = new ItemStackMoveOperation(World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge) { ActingPlayer = player };
        grid.ActivateSlot(OutputSlot, mouse, ref op);
        var made = mouse.Itemstack ?? throw new InvalidOperationException($"{recipe}: nothing crafted");
        mouse.Itemstack = null;
        for (int i = 0; i < OutputSlot; i++) grid[i].Itemstack = null;
        return made;
    }

    /// <summary>A chest with these stacks, one per slot, and the player next to it.</summary>
    BlockPos Chest(IServerPlayer player, params ItemStack[] contents)
    {
        var (pos, chest) = TestBlocks.Place(game, "chest-east");
        var inventory = ((IBlockEntityContainer)chest).Inventory;
        for (int i = 0; i < contents.Length; i++) inventory[i].Itemstack = contents[i];
        // A server entity keeps its authoritative place in ServerPos and Pos follows it each tick: set both.
        player.Entity.ServerPos.SetPos(pos.X + 0.5, pos.Y, pos.Z + 1.5);
        player.Entity.Pos.SetPos(pos.X + 0.5, pos.Y, pos.Z + 1.5);
        return pos;
    }

    /// <summary>The click with the item in the slot on the block, Shift held (and Ctrl if asked).</summary>
    static void Click(IServerPlayer player, ItemSlot slot, BlockPos pos, bool ctrl = false)
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

    /// <summary>Where the game puts what a player gets: the hotbar without the off-hand, and the backpack.</summary>
    static List<ItemSlot> Slots(IServerPlayer player)
    {
        var inventory = player.InventoryManager;
        return inventory.GetHotbarInventory().Where(s => s != inventory.OffhandHotbarSlot)
            .Concat(inventory.GetOwnInventory(GlobalConstants.backpackInvClassName)).ToList();
    }

    static ItemStack? FindOurs(IServerPlayer player, string codePrefix) => Slots(player).Select(s => s.Itemstack)
        .FirstOrDefault(st => st is { } x && x.Collectible.Code.Domain == CurioCatalogueModSystem.ModId && x.Collectible.Code.Path.StartsWith(codePrefix));

    static ItemStack? TakeOurs(IServerPlayer player, string codePrefix)
    {
        var stack = FindOurs(player, codePrefix);
        if (stack != null) Slots(player).First(s => s.Itemstack == stack).Itemstack = null;
        return stack;
    }

    static void Clear(IServerPlayer player)
    {
        foreach (var slot in Slots(player)) slot.Itemstack = null;
        player.Entity.LeftHandItemSlot.Itemstack = null;
    }
}
