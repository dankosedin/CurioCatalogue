using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// The ore crystals (V2 of the vanilla collections plan; what they are: C1 of the collections design plan): our definition
/// <c>crystals.json</c> on the game's real items. Of the 407 variants the item allows, 54 never generate and are the
/// definition's <c>exclude</c>; three levels: the richest of each ore (16), the richest each rock gets of each ore (118), every
/// combination (353).
/// </summary>
[Collection(ServerCollection.Name)]
public class CrystalsTests(GameServerFixture server) : BookTestBase(server)
{
    const string Id = "crystals";

    DefinedCollection Crystals => Mod.Collections.Defined(Id) ?? throw new InvalidOperationException("no crystal collection");

    static string CodeOf(ItemKey key) => key.Code[(key.Code.IndexOf(':') + 1)..];

    static string Grade(ItemKey key) => CodeOf(key).Split('-')[1];

    [Fact]
    public void The_levels_have_16_118_and_353_members_and_none_of_a_crystal_that_never_generates()
    {
        var seen = game.OnMain(() => (
            Crystals.Level("richest")!.Members.Count,
            Crystals.Level("rocks")!.Members.Count,
            Crystals.Level("all")!.Members.Count,
            Crystals.Level("all")!.Members.SelectMany(m => m.Fillers).Select(CodeOf)
                .Where(p => p.Contains("-uranium-") || p.Contains("-stibnite-") || p.EndsWith("-hematite-sandstone")
                    || p is "crystalizedore-rich-magnetite-slate" or "crystalizedore-bountiful-magnetite-slate" or "crystalizedore-rich-ilmenite-peridotite")
                .ToList(),
            World.Items.Count(i => i.Code is { Domain: "game" } c && c.Path.StartsWith("crystalizedore-"))));

        // The item allows 407, but its "ore" group is loaded from worldproperties/block/ore-graded.json, which has no stibnite:
        // its 9 are never registered (checked on this server 09.11.2026). 398 less the other 45 that never generate.
        Assert.Equal(398, seen.Item5);
        Assert.Equal((16, 118, 353), (seen.Item1, seen.Item2, seen.Item3));
        Assert.Empty(seen.Item4);
    }

    [Fact]
    public void The_richest_of_each_ore_is_filled_only_by_its_highest_grade_from_any_rock()
    {
        // C1: bountiful for 14 ores; native silver in galena is medium at most; magnetite rich (its bountiful, only in slate,
        // never generates). Quartz gold: bountiful in 13 rocks, so the member is filled by any of 13 codes.
        var seen = game.OnMain(() => Crystals.Level("richest")!.Members.ToDictionary(
            m => m.Values["ore"],
            m => (Grades: string.Join(",", m.Fillers.Select(Grade).Distinct()), Rocks: m.Fillers.Count)));

        Assert.Equal(16, seen.Count);
        Assert.Equal("medium", seen["galena_nativesilver"].Grades);
        Assert.Equal("rich", seen["magnetite"].Grades);
        Assert.All(seen.Where(s => s.Key is not ("galena_nativesilver" or "magnetite")), s => Assert.Equal("bountiful", s.Value.Grades));
        Assert.Equal(13, seen["quartz_nativegold"].Rocks);
    }

    [Fact]
    public void The_richest_each_rock_gets_is_one_crystal_per_ore_and_rock()
    {
        // C1: cassiterite is bountiful in granite but medium at most in andesite.
        var seen = game.OnMain(() =>
        {
            var members = Crystals.Level("rocks")!.Members;
            string Of(string ore, string rock) => string.Join(",",
                members.Single(m => m.Values["ore"] == ore && m.Values["rock"] == rock).Fillers.Select(CodeOf));
            return (members.Max(m => m.Fillers.Count), Of("cassiterite", "granite"), Of("cassiterite", "andesite"), Of("magnetite", "slate"));
        });

        Assert.Equal((1, "crystalizedore-bountiful-cassiterite-granite", "crystalizedore-medium-cassiterite-andesite",
            "crystalizedore-medium-magnetite-slate"), seen);
    }

    [Fact]
    public void An_album_of_the_richest_takes_a_bountiful_crystal_of_any_rock_and_leaves_a_poorer_one()
    {
        var client = FakeClient.Join(game, "crystals-take");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            var slots = ((IBlockEntityContainer)chest).Inventory;
            slots[0].Itemstack = Vanilla("crystalizedore-rich-quartz_nativegold-granite");
            slots[1].Itemstack = Vanilla("crystalizedore-bountiful-quartz_nativegold-basalt");
            slots[2].Itemstack = Vanilla("crystalizedore-bountiful-quartz_nativegold-granite");
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Ours("book-album-normal-brickred");
            var album = Assert.IsType<LevelChoice.Chosen>(Core.Album.New(Crystals, 8, "").ChooseLevel(Crystals, "richest")).Album;
            Mod.Store.WriteAlbum(hand.Itemstack, album);
            Click(player, hand, pos);
            var read = ReadAlbum(hand.Itemstack!);
            return (read.Collected.Keys.Single(), CodeOf(read.Collected.Values.Single().Key), Inventory(player).Split(", ").Order().ToList(), Contents(chest));
        });

        // One member, one crystal: the first in the member's order (its fillers by the rock's order, granite before basalt) that
        // fits (Transfer.PlanWhatFits); the rich one is no filler and stays.
        Assert.Equal("quartz_nativegold", seen.Item1);
        Assert.Equal("crystalizedore-bountiful-quartz_nativegold-granite", seen.Item2);
        Assert.Contains("crystalizedore-bountiful-quartz_nativegold-granite x1", seen.Item3);
        Assert.Equal("crystalizedore-bountiful-quartz_nativegold-basalt x1, crystalizedore-rich-quartz_nativegold-granite x1", seen.Item4);
    }

    [Fact]
    public void A_want_list_of_the_richest_says_any_rock_and_is_given_by_a_crystal_of_another_rock()
    {
        var client = FakeClient.Join(game, "crystals-wants");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var album = Assert.IsType<LevelChoice.Chosen>(Core.Album.New(Crystals, 8, "").ChooseLevel(Crystals, "richest")).Album;
            var list = Assert.IsType<CreateInvoiceResult.Created>(album.MakeWantList(album.LevelIn(Crystals)!, 0, new BlockPosition(0, 0, 0))).Invoice;
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = new ItemStack(World.GetItem(InvoiceMaker.InvoiceCode(InvoiceKind.WantList)));
            Mod.Store.WriteInvoice(hand.Itemstack, list);

            var text = new System.Text.StringBuilder();
            hand.Itemstack.Collectible.GetHeldItemInfo(hand, text, World, false);

            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("crystalizedore-bountiful-quartz_nativegold-granite");
            Click(player, hand, pos);

            var left = Mod.Store.ReadInvoice(hand.Itemstack!, InvoiceKind.WantList);
            var first = (ItemStack)list.Entries[0].Sample;
            return (text.ToString(), first.GetName(), list.Entries.Length, left.Entries.Select(e => e.Member).ToList(), Inventory(player));
        });
        var (info, firstName, lines, left, inventory) = seen;

        // The first line is native copper (the dimension's order), bountiful in basalt only: one crystal, nothing left free,
        // named by its ore and the rock its one crystal has, in lower case (L1); not by the item, which says the grade.
        var infoLines = info.Split(["\r\n", "\n"], StringSplitOptions.None);
        string basalt = game.OnMain(() => Lang.Get("game:rock-basalt"));
        Assert.Contains(LangText("en", "invoice-info-line", 1, $"{LangText("en", "crystals-ore-nativecopper")}, {basalt.ToLowerInvariant()}"), infoLines);
        Assert.DoesNotContain(infoLines, l => l.Contains(firstName));
        // Quartz gold, the third: any of 13 rocks.
        Assert.Contains(LangText("en", "invoice-info-line", 1,
            LangText("en", "invoice-info-member-free", LangText("en", "crystals-ore-quartz_nativegold"), LangText("en", "crystals-any-rock"))), infoLines);
        Assert.Equal(16, lines);
        Assert.Equal(15, left.Count);
        Assert.DoesNotContain("quartz_nativegold", left);
        Assert.Equal("crystalizedore-bountiful-quartz_nativegold-granite x1", inventory);
    }

    [Fact]
    public void A_crystal_is_named_by_the_values_its_level_keeps_in_order_parts_after_the_first_in_lower_case()
    {
        var names = game.OnMain(() =>
        {
            string? Named(string level, string id) => MemberNames.Name(Crystals, Crystals.Level(level)!, Crystals.Level(level)!.Find(id)!);
            // Member ids follow the definition's order of dimensions (grade, ore, rock), not the level's keep.
            // Bountiful native copper is found in basalt only.
            return (Named("all", "bountiful-nativecopper-basalt"), Named("rocks", "nativecopper-basalt"),
                Named("richest", "quartz_nativegold"));
        });

        string copper = LangText("en", "crystals-ore-nativecopper");
        string basalt = game.OnMain(() => Lang.Get("game:rock-basalt")).ToLowerInvariant();
        Assert.Equal($"{copper}, {LangText("en", "crystals-grade-bountiful").ToLowerInvariant()}, {basalt}", names.Item1);
        Assert.Equal($"{copper}, {basalt}", names.Item2);
        // The richest: the grade is dropped as max, so not said; the rock left free is.
        Assert.Equal(LangText("en", "invoice-info-member-free", LangText("en", "crystals-ore-quartz_nativegold"), LangText("en", "crystals-any-rock")), names.Item3);
    }

    [Fact]
    public void Every_ore_has_a_short_name_for_a_table_heading_and_a_full_one_for_its_tooltip()
    {
        var missing = game.OnMain(() =>
        {
            var ore = Crystals.Definition.Dimensions.Single(d => d.Code == "ore");
            // Lang.Get gives the key back when nothing names it.
            return ore.Values.SelectMany(v => new[] { ore.ValueNameKey(v)!, ore.ValueShortNameKey(v)! })
                .Where(key => Lang.Get(key) == key).ToList();
        });
        Assert.Empty(missing);
        Assert.Equal("curiocatalogue:crystals-oreshort-galena", game.OnMain(() =>
            Crystals.Definition.Dimensions.Single(d => d.Code == "ore").ValueShortNameKey("galena")));
    }
}
