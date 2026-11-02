using CurioCatalogue.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// Crafting through a real player's grid on the server (C1–C4 of <c>functional.md</c>). A stack put into a grid slot makes the grid
/// look for a recipe (<c>InventoryCraftingGrid.OnItemSlotModified</c> → <c>FindMatchingRecipe</c>: <c>Matches</c>, so our
/// <c>MatchesForCrafting</c>; <c>GenerateOutputStack</c>, so <c>copyAttributesFrom</c> and our <c>OnCreatedByCrafting</c>). Taking the output is
/// <c>ActivateSlot</c> on the output slot with the mouse slot, which is what the server does with the client's packet 7
/// (<c>ServerSystemInventory.HandleActivateInventorySlot</c>), and it consumes the ingredients (our <c>ConsumeCraftingIngredients</c>,
/// else the recipe's <c>ConsumeInput</c>). Decompiled VintagestoryLib 1.22.7 (docs/modding.md, "Crafting on the server").
/// The players are in creative, where the grid is closed for the client; the server's grid works all the same.
/// </summary>
[Collection(ServerCollection.Name)]
public class CraftingTests(GameServerFixture server) : BookTestBase(server)
{
    const int OutputSlot = 9;

    static InventoryCraftingGrid Grid(IServerPlayer player) =>
        (InventoryCraftingGrid)player.InventoryManager.GetOwnInventory(GlobalConstants.craftingInvClassName);

    /// <summary>Puts the stacks into the grid (slot = row * 3 + column), as a player would, one slot at a time.</summary>
    static void Put(IServerPlayer player, params (int Slot, ItemStack Stack)[] stacks)
    {
        var grid = Grid(player);
        foreach (var (slot, stack) in stacks)
        {
            grid[slot].Itemstack = stack;
            grid[slot].MarkDirty();
        }
    }

    /// <summary>The recipe the grid found and the output it shows, before anything is taken.</summary>
    static (string? Recipe, ItemStack? Output) Shown(IServerPlayer player) =>
        (Grid(player).MatchingRecipe?.Name?.Path, Grid(player)[OutputSlot].Itemstack);

    /// <summary>A left click on the output with an empty mouse: the crafted stack ends in the mouse slot.</summary>
    ItemStack Take(IServerPlayer player)
    {
        var mouse = player.InventoryManager.MouseItemSlot;
        var op = new ItemStackMoveOperation(World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge) { ActingPlayer = player };
        Grid(player).ActivateSlot(OutputSlot, mouse, ref op);
        return mouse.Itemstack ?? throw new InvalidOperationException($"nothing crafted; the grid: {Left(player)}");
    }

    /// <summary>What is left in the grid's nine slots, sorted.</summary>
    static string Left(IServerPlayer player) => string.Join(", ", Enumerable.Range(0, OutputSlot)
        .Select(i => Grid(player)[i].Itemstack).Where(s => s != null).Select(s => Describe(s!)).Order());

    Ledger Read(ItemStack book) => Mod.Store.ReadLedger(book, ((ItemLedger)book.Collectible).LedgerMode);

    string Entries(ItemStack book) => string.Join(", ", Read(book).Entries.Select(e => $"{e.Key.Code} x{e.Value.Count}").Order());

    ItemStack Jug(float litres)
    {
        var jug = Vanilla("jug-blue-fired");
        var lime = Vanilla("limewaterportion", (int)(litres * 100)); // itemsPerLitre: 100 (survival itemtypes/liquid/limewaterportion.json)
        ((BlockLiquidContainerBase)jug.Collectible).SetContent(jug, lime);
        return jug;
    }

    // ---- C1, C2: new ledgers

    [Theory]
    [InlineData("normal-teal")]
    [InlineData("aged-darkgreen")]
    public void C1_a_vanilla_book_nails_and_ink_make_a_ledger_of_the_same_cover_with_8_sheets(string cover)
    {
        // Pattern "N_,BQ,N_": nails above and below the book, ink and quill to its right.
        var client = FakeClient.Join(game, "c1-" + cover[..6]);
        var (recipe, shown, taken, left) = game.OnMain(() =>
        {
            var player = client.Player;
            Put(player, (0, Vanilla("metalnailsandstrips-iron")), (3, Vanilla("book-" + cover)), (4, Vanilla("inkandquill")), (6, Vanilla("metalnailsandstrips-iron")));
            var (recipe, output) = Shown(player);
            string? shown = output == null ? null : Describe(output);
            var taken = Take(player);
            return (recipe, shown, (Describe(taken), Read(taken).Sheets, Read(taken).IsEmpty), Left(player));
        });

        Assert.Equal("ledger-fallback", recipe);
        Assert.Equal($"book-ledger-{cover} x1", shown);
        Assert.Equal(($"book-ledger-{cover} x1", 8, true), taken);
        Assert.Equal("inkandquill x1", left); // a tool stays; the book and both nails are used
    }

    [Theory]
    [InlineData("bookshelves/large-book-closed")]
    [InlineData("bookshelves/large-book-open")]
    public void C2_a_large_book_from_ruins_lime_water_and_ink_make_a_ledger_with_32_sheets(string oldBook)
    {
        var client = FakeClient.Join(game, "c2-" + oldBook[^4..]);
        var (recipe, taken, left) = game.OnMain(() =>
        {
            var player = client.Player;
            var book = Vanilla("clutter");
            book.Attributes.SetString("type", oldBook);
            // Shapeless: any order in the grid.
            Put(player, (0, Jug(1.5f)), (4, book), (8, Vanilla("inkandquill")));
            var recipe = Shown(player).Recipe;
            var taken = Take(player);
            var jug = Grid(player)[0].Itemstack;
            float litres = jug == null ? -1 : ((BlockLiquidContainerBase)jug.Collectible).GetCurrentLitres(jug);
            return (recipe, (Describe(taken), Read(taken).Sheets), (Left(player), litres));
        });

        Assert.Equal("ledger-restore", recipe);
        Assert.Equal(("book-ledger-normal-darkgreen x1", 32), taken);
        // The jug stays with what is left of the lime water (the vanilla liquid container takes requiresLitres), the clutter book is used.
        Assert.Equal(("inkandquill x1, jug-blue-fired x1", 0.5f), left);
    }

    [Fact]
    public void C2_a_jug_with_less_than_a_litre_makes_nothing()
    {
        var client = FakeClient.Join(game, "c2-half-litre");
        var shown = game.OnMain(() =>
        {
            var player = client.Player;
            var book = Vanilla("clutter");
            book.Attributes.SetString("type", "bookshelves/large-book-closed");
            Put(player, (0, Jug(0.5f)), (1, book), (2, Vanilla("inkandquill")));
            return Shown(player);
        });

        Assert.Equal((null, null), shown);
    }

    // ---- C3: the album

    [Fact]
    public void C3_an_empty_ledger_and_a_tapestry_make_an_album_and_the_tapestry_stays()
    {
        var client = FakeClient.Join(game, "c3-album");
        var (recipe, album, left) = game.OnMain(() =>
        {
            var player = client.Player;
            Put(player, (0, Book("book-ledger-normal-brickred", 12)), (1, Tapestry("ambush1")));
            var recipe = Shown(player).Recipe;
            var taken = Take(player);
            var album = Read(taken);
            return (recipe, (Describe(taken), album.Sheets, album.Album?.CollectionId, album.Entries.Count), Left(player));
        });

        int members = game.OnMain(() => Mod.Collections.ForSample(Tapestry("ambush1"))!.Members.Count);
        Assert.Equal("ledger-album", recipe);
        Assert.Equal(("book-album-normal-brickred x1", 12, "tapestries", members), album); // sheets kept, every member missing
        Assert.Equal("tapestry-north:ambush1 x1", left);
    }

    [Fact]
    public void C3_a_ledger_with_entries_makes_no_album()
    {
        var client = FakeClient.Join(game, "c3-used");
        var shown = game.OnMain(() =>
        {
            var player = client.Player;
            Put(player, (0, Book("book-ledger-normal-brickred", 12, Vanilla("ingot-iron"))), (1, Tapestry("ambush1")));
            return Shown(player);
        });

        Assert.Equal((null, null), shown);
    }

    [Fact]
    public void C3_a_tapestry_that_is_not_in_the_collection_makes_no_album()
    {
        // The recipe takes any "game:tapestry-*" (ledger.json), so a piece of another side matches it; such a stack comes only
        // from the creative pick block and is not a member (CollectionRegistry). The album is not made and the grid shows nothing.
        var client = FakeClient.Join(game, "c3-east");
        var shown = game.OnMain(() =>
        {
            var player = client.Player;
            var east = Vanilla("tapestry-east");
            east.Attributes.SetString("type", "ambush1");
            Put(player, (0, Book("book-ledger-normal-brickred", 12)), (1, east));
            var (recipe, output) = Shown(player);
            return (recipe, output == null ? null : Describe(output));
        });

        Assert.Equal(("ledger-album", (string?)null), shown);
    }

    // ---- C4: books made from an old book (one with entries)

    [Fact]
    public void C4_recolouring_an_aged_ledger_with_entries_keeps_its_entries_sheets_and_name()
    {
        var client = FakeClient.Join(game, "c4-recolour");
        int logBefore = game.LogEntries().Count;
        var (recipe, before, after, left) = game.OnMain(() =>
        {
            var player = client.Player;
            var old = Book("book-ledger-aged-darkgreen", 5, Vanilla("ingot-iron", 3), Vanilla("ingot-tin"));
            Mod.Store.WriteLedger(old, Read(old) with { Name = "Old stock" });
            var before = (Entries(old), Read(old).Sheets, Read(old).Name);
            Put(player, (0, old), (1, Vanilla("leather-normal-blue")));
            var recipe = Shown(player).Recipe;
            var taken = Take(player);
            return (recipe, before, (Describe(taken), (Entries(taken), Read(taken).Sheets, Read(taken).Name)), Left(player));
        });

        Assert.Equal("ledger-recolor", recipe);
        Assert.Equal(("book-ledger-normal-teal x1", before), after);
        Assert.Equal("", left); // the old book and the leather are used
        AssertNoProblemsSince(logBefore);
    }

    [Fact]
    public void C4_recharging_an_aged_ledger_with_entries_keeps_its_entries_and_uses_only_the_sheets_added()
    {
        var client = FakeClient.Join(game, "c4-recharge");
        int logBefore = game.LogEntries().Count;
        var (recipe, shownSheets, after, left) = game.OnMain(() =>
        {
            var player = client.Player;
            Put(player, (0, Book("book-materials-aged-orange", 26, Vanilla("ingot-iron", 3))), (1, Vanilla("paper-parchment", 10)));
            var (recipe, output) = Shown(player);
            int shownSheets = output == null ? -1 : Read(output).Sheets;
            var taken = Take(player);
            return (recipe, shownSheets, (Describe(taken), Read(taken).Sheets, Entries(taken)), Left(player));
        });

        Assert.Equal("ledger-recharge", recipe);
        Assert.Equal(32, shownSheets); // MaxSheets 32: 6 of the 10 sheets fit
        Assert.Equal(("book-materials-aged-orange x1", 32, "game:ingot-iron x3"), after);
        Assert.Equal("paper-parchment x4", left);
        AssertNoProblemsSince(logBefore);
    }

    [Fact]
    public void C4_a_ledger_with_entries_does_not_switch_mode_and_an_empty_aged_one_does()
    {
        var client = FakeClient.Join(game, "c4-switch");
        var (used, empty) = game.OnMain(() =>
        {
            var player = client.Player;
            Put(player, (4, Book("book-ledger-aged-darkgreen", 5, Vanilla("ingot-iron"))));
            var used = Shown(player).Recipe;
            Grid(player)[4].Itemstack = null;
            Put(player, (4, Book("book-ledger-aged-darkgreen", 5)));
            var taken = Take(player);
            return (used, (Describe(taken), Read(taken).Sheets, Read(taken).Mode));
        });

        Assert.Null(used);
        Assert.Equal(("book-materials-aged-darkgreen x1", 5, LedgerMode.Counts), empty);
    }

    /// <summary>C4 asks for "no stack or attribute error in the log": no warning or error since the test's start.</summary>
    void AssertNoProblemsSince(int index)
    {
        var problems = game.LogEntries().Skip(index).Where(l => l.Type is EnumLogType.Warning or EnumLogType.Error or EnumLogType.Fatal).ToList();
        Assert.True(problems.Count == 0, "problems in the log:\n" + string.Join("\n", problems));
    }
}
