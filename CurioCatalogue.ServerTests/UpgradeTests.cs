using System.Reflection;
using System.Text.Json.Nodes;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// What players have today must still work after an update: the golden stacks of every released version
/// (<c>Golden/v&lt;version&gt;/</c>, made by that release's own code; docs/development.md, Golden stacks), read by the current code.
/// The expected values are the capture's inputs (what went into each item), never what the current code reads from it. A stack's
/// ids are replaced by the codes' ids in this world first (<see cref="GoldenStacks.Read"/>), as a world's own ids would be.
/// "Reads" also has to mean "works": one action each on the real item classes, chests and grid (as in
/// <see cref="BookFlowTests"/> and <see cref="CraftingTests"/>).
/// </summary>
[Collection(ServerCollection.Name)]
public class UpgradeTests(GameServerFixture server) : BookTestBase(server)
{
    // The actions use the stacks of one set by name: the 0.1.1 one (a later set gets its own actions when it is made).
    const string V011 = "0.1.1";

    public static IEnumerable<object[]> AllStacks() =>
        GoldenStacks.Versions().SelectMany(v => GoldenStacks.Load(v).Stacks.Select(s => new object[] { v, s.Name }));

    public static IEnumerable<object[]> AllVersions() => GoldenStacks.Versions().Select(v => new object[] { v });

    // ---- reading: what the current code makes of each stack, against what went into it

    /// <summary>What a book or invoice holds, as one comparable text; the expected one is made from the inputs, the observed one from the item.</summary>
    record Seen(string Code, string Mode, int Sheets, string Name, IReadOnlyList<string> Entries, string Album)
    {
        public override string ToString() => $"{Code} {Mode} sheets={Sheets} name=[{Name}] album=[{Album}]\n  " + string.Join("\n  ", Entries);
    }

    static Seen Expected(GoldenStack golden)
    {
        var inputs = golden.Inputs;
        if (inputs.Book is { } book)
            // The books keep their entries sorted by text, so does the observed side.
            return new Seen(inputs.Code, book.Mode, book.Sheets, book.Name, book.Entries.Select(e => e.ToString()).Order(StringComparer.Ordinal).ToList(),
                book.Album is { } a ? $"{a.Collection}: collected {a.Collected.Count} of {a.Collected.Count + book.Entries.Count}, full {a.Full}" : "");
        var invoice = inputs.Invoice ?? throw new InvalidOperationException($"{golden.Name}: inputs have neither book nor invoice");
        return new Seen(inputs.Code, invoice.Kind, 0, "", invoice.Lines.Select(e => e.ToString()).ToList(), "");
    }

    static string Line(Entry e) => new GoldenEntry(e.Key.Code, ((ItemStack)e.Sample).Attributes.GetString("type"), e.Count).ToString();

    Seen Observed(ItemStack stack)
    {
        string code = stack.Collectible.Code.ToString();
        if (stack.Collectible is ItemLedger book)
        {
            var ledger = Mod.Store.ReadLedger(stack, book.LedgerMode);
            string album = "";
            if (ledger.Album is { } info)
            {
                var progress = Mod.Store.CollectionOf(ledger)?.Progress(ledger.Entries.Keys) ?? throw new InvalidOperationException($"no collection {info.CollectionId}");
                album = $"{info.CollectionId}: collected {progress.Collected} of {progress.Total}, full {ledger.IsEmpty}";
            }
            return new Seen(code, ledger.Mode.ToString(), ledger.Sheets, ledger.Name, ledger.Entries.Values.Select(Line).Order(StringComparer.Ordinal).ToList(), album);
        }
        var invoice = Mod.Store.ReadInvoice(stack, ((ItemInvoice)stack.Collectible).InvoiceKind);
        return new Seen(code, invoice.Kind.ToString(), 0, "", invoice.Entries.Select(Line).ToList(), "");
    }

    void CheckRead(string version, string name)
    {
        var golden = GoldenStacks.Load(version).Stacks.Single(s => s.Name == name);
        var read = GoldenStacks.Read(World, golden);
        // "Not newer": the stored version is one this build writes or an older one, so the item is not refused.
        Assert.False(LedgerStore.IsNewer(read), $"{name}: stored data is newer than LedgerStore.Version");
        Assert.Equal(1, read.Attributes.GetTreeAttribute(LedgerStore.RootKey).GetAsInt("v"));
        Assert.Equal(Expected(golden).ToString(), Observed(read).ToString());
    }

    [Theory]
    [MemberData(nameof(AllStacks))]
    public void Every_golden_stack_is_read_as_what_went_into_it(string version, string name)
    {
        game.OnMain(() => { CheckRead(version, name); return 0; });
    }

    // ---- reading with the config the release wrote

    static string ConfigFile => Path.Combine(GamePaths.ModConfig, CurioCatalogueModSystem.ConfigFileName);

    [Theory]
    [MemberData(nameof(AllVersions))]
    public void The_config_a_release_wrote_loads_cleanly_keeps_its_keys_and_the_books_read_with_it(string version)
    {
        // The file a player's update finds is the one the release wrote (the mod writes it back every time, ConfigFileTests):
        // its values win over the new defaults, and what the update adds is filled in with defaults.
        string golden = GoldenStacks.ConfigText(version);
        string? original = File.Exists(ConfigFile) ? File.ReadAllText(ConfigFile) : null;
        int mark = game.LogMark();
        try
        {
            Directory.CreateDirectory(GamePaths.ModConfig);
            File.WriteAllText(ConfigFile, golden);
            var loaded = game.OnMain(() => (CurioCatalogueConfig)typeof(CurioCatalogueModSystem)
                .GetMethod("LoadConfig", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [game.Api])!);
            Assert.Empty(LogProblems.Since(game, mark));
            var written = JsonNode.Parse(File.ReadAllText(ConfigFile))!.AsObject();

            // Every key the release wrote is still a setting with the same value: a renamed or dropped setting loses its key at the write-back.
            var old = JsonNode.Parse(golden)!.AsObject();
            foreach (var (key, value) in old)
            {
                Assert.True(written.ContainsKey(key), $"the setting {key} of {version} is not a setting any more");
                Assert.True(JsonNode.DeepEquals(value, written[key]), $"{key}: {version} wrote {value?.ToJsonString()}, now {written[key]?.ToJsonString()}");
            }
            // A setting the release did not know has its default.
            var defaults = JsonNode.Parse(JsonConvert.SerializeObject(new CurioCatalogueConfig()))!.AsObject();
            foreach (var (key, value) in defaults.Where(p => !old.ContainsKey(p.Key)))
                Assert.True(JsonNode.DeepEquals(value, written[key]), $"{key}: new since {version}, expected its default {value?.ToJsonString()}, got {written[key]?.ToJsonString()}");

            // The books read the same with that config.
            var names = GoldenStacks.Load(version).Stacks.Select(s => s.Name).ToList();
            game.OnMain(() => WithConfig(c => JsonConvert.PopulateObject(JsonConvert.SerializeObject(loaded), c), () =>
            {
                foreach (var name in names) CheckRead(version, name);
                return 0;
            }));
        }
        finally
        {
            if (original != null) File.WriteAllText(ConfigFile, original);
            else File.Delete(ConfigFile);
        }
    }

    [Fact]
    public void An_album_of_0_1_1_keeps_its_progress_when_the_collection_grows()
    {
        // Albums follow their collection (domain.md, Albums and collections): a mod adds a piece, the album's snapshot of 0.1.1 (the
        // stored "members") tells it from a piece already collected. Without the snapshot the new piece would count as collected.
        var golden = GoldenStacks.Load(V011).Stacks.Single(s => s.Name == "album-partial").Inputs.Book!;
        var seen = game.OnMain(() => WithConfig(c => c.TapestryAdd = [.. c.TapestryAdd, "cc-new-piece"], () =>
        {
            var book = Observed(Golden("album-partial"));
            return (book.Album, book.Sheets, book.Entries.Contains("game:tapestry-north:cc-new-piece x1"), book.Entries.Count);
        }));
        Assert.Equal((
            $"tapestries: collected {golden.Album!.Collected.Count} of {golden.Album.Collected.Count + golden.Entries.Count + 1}, full False",
            golden.Sheets, true, golden.Entries.Count + 1), seen);
    }

    // ---- working: one action on each kind of golden item (the set of 0.1.1)

    ItemStack Golden(string name) => GoldenStacks.Read(World, GoldenStacks.Load(V011).Stacks.Single(s => s.Name == name));

    [Fact]
    public void An_intake_list_of_0_1_1_is_posted_on_a_real_chest_into_a_ledger_of_0_1_1()
    {
        // The list (3 tin ingots, the tapestry ambush2) with the ledger in the off-hand; the chest holds them. Posting moves them
        // to the player, adds the lines to the book and gives the sheet back (domain.md, Invoices).
        var client = FakeClient.Join(game, "upg-post");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("ingot-tin", 3);
            ((IBlockEntityContainer)chest).Inventory[1].Itemstack = Tapestry("ambush2");
            var offhand = player.Entity.LeftHandItemSlot;
            offhand.Itemstack = Golden("ledger");
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Golden("intake-list");

            Click(player, hand, pos);

            var book = Observed(offhand.Itemstack!);
            return (book.Sheets, string.Join(", ", book.Entries), Inventory(player), Contents(chest));
        });
        Assert.Equal((9, "game:ingot-copper x1, game:ingot-iron x1, game:ingot-tin x1, game:tapestry-north:ambush1 x1, game:tapestry-north:ambush2 x1",
            "ingot-tin x3, tapestry-north:ambush2 x1", ""), seen);
    }

    [Fact]
    public void A_want_list_of_0_1_1_is_filled_from_a_real_chest()
    {
        // The list names every piece the partial album lacked; the chest has two of them and a piece the list does not name.
        var client = FakeClient.Join(game, "upg-want");
        int lines = GoldenStacks.Load(V011).Stacks.Single(s => s.Name == "want-list").Inputs.Invoice!.Lines.Count;
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            var slots = ((IBlockEntityContainer)chest).Inventory;
            slots[0].Itemstack = Tapestry("nightfall1");
            slots[1].Itemstack = Tapestry("ambush1"); // the album has it already: not on the list
            slots[2].Itemstack = Tapestry("holy2");
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Golden("want-list");

            Click(player, hand, pos);

            return (Observed(hand.Itemstack!).Entries.Count, Inventory(player), Contents(chest));
        });
        Assert.Equal((lines - 2, "tapestry-north:holy2 x1, tapestry-north:nightfall1 x1", "tapestry-north:ambush1 x1"), seen);
    }

    [Fact]
    public void An_album_of_0_1_1_takes_a_new_tapestry_and_leaves_one_it_has()
    {
        var client = FakeClient.Join(game, "upg-album");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            var slots = ((IBlockEntityContainer)chest).Inventory;
            slots[0].Itemstack = Tapestry("nightfall1");
            slots[1].Itemstack = Tapestry("ambush1"); // collected in the golden album
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Golden("album-partial");
            int missingBefore = Observed(hand.Itemstack).Entries.Count;

            Click(player, hand, pos);

            var after = Observed(hand.Itemstack!);
            return (missingBefore - after.Entries.Count, after.Album, after.Sheets, Inventory(player), Contents(chest));
        });
        Assert.Equal((1, "tapestries: collected 4 of 88, full False", 7, "tapestry-north:nightfall1 x1", "tapestry-north:ambush1 x1"), seen);
    }

    [Fact]
    public void A_ledger_of_0_1_1_with_spent_sheets_is_recharged_in_the_grid()
    {
        // ledger-recharge: the book and parchment; the sheets added are the parchment used (29 + 2), as C4 of CraftingTests.
        var client = FakeClient.Join(game, "upg-recharge");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var grid = (InventoryCraftingGrid)player.InventoryManager.GetOwnInventory(GlobalConstants.craftingInvClassName);
            grid[0].Itemstack = Golden("spent");
            grid[0].MarkDirty();
            grid[1].Itemstack = Vanilla("paper-parchment", 2);
            grid[1].MarkDirty();
            string? recipe = grid.MatchingRecipe?.Name?.Path;

            var mouse = player.InventoryManager.MouseItemSlot;
            var op = new ItemStackMoveOperation(World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge) { ActingPlayer = player };
            grid.ActivateSlot(9, mouse, ref op);
            var taken = mouse.Itemstack ?? throw new InvalidOperationException("nothing crafted");
            return (recipe, Describe(taken), Observed(taken).Sheets, grid[1].Empty);
        });
        Assert.Equal(("ledger-recharge", "book-ledger-normal-darkgreen x1", 31, true), seen);
    }
}
