using CurioCatalogue.Adapters;
using CurioCatalogue.Core;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// The butterflies (V4 of the vanilla collections plan; what they are: C3 of the collections design plan): our definition
/// <c>butterflies.json</c> on the game's real items <c>butterfly-dead-{type}</c>, 169 of them (<c>reedbutterfly</c> has no item).
/// One level, every dead butterfly a member of its own; grouped (L4a of the album polish plan) by family and species, or by the
/// step of spawn chance, from tables <c>tools/butterflies.ps1</c> generated from the game's assets. The tests below check those
/// tables against the game, so an update of the game that moves a butterfly is caught: run the script again.
/// </summary>
[Collection(ServerCollection.Name)]
public class ButterfliesTests(GameServerFixture server) : BookTestBase(server)
{
    const string Id = "butterflies";

    DefinedCollection Butterflies => Mod.Collections.Defined(Id) ?? throw new InvalidOperationException("no butterfly collection");

    LayoutDef Layout(string id) => Butterflies.Levels.Single().Layouts.Single(l => l.Id == id);

    [Fact]
    public void By_family_the_169_butterflies_are_in_109_species_each_in_exactly_one_and_in_one_of_seven_families()
    {
        var seen = game.OnMain(() =>
        {
            var level = Butterflies.Levels.Single();
            var groups = level.Groups(Layout("byfamily"));
            return (level.DefaultLayout.Id, groups.Count, groups.Sum(g => g.Members.Count),
                groups.SelectMany(g => g.Members).Select(m => m.Id).Distinct().Count(),
                groups.Select(g => g.Values[0]).Distinct().ToList(),
                Butterflies.Definition.Dimensions.Single(d => d.Code == "species").Values.Count,
                groups.Select(g => g.Values[1]).ToList());
        });

        Assert.Equal(("byfamily", 109, 169, 169), (seen.Item1, seen.Item2, seen.Item3, seen.Item4));
        // Every value is a listed one: a species or family the tables lack would be its own value, after the listed (L4a).
        Assert.Equal(["papilionidae", "hesperiidae", "pieridae", "riodinidae", "lycaenidae", "nymphalidae", "moths"], seen.Item5);
        Assert.Equal(109, seen.Item6);
        Assert.Equal(109, seen.Item7.Distinct().Count());
    }

    [Fact]
    public void By_rarity_the_169_butterflies_are_in_six_steps_rarest_first()
    {
        var seen = game.OnMain(() => Butterflies.Levels.Single().Groups(Layout("byrarity"))
            .Select(g => (g.Values[0], g.Members.Count)).ToList());

        Assert.Equal([("legendary", 6), ("veryrare", 7), ("rare", 19), ("scarce", 38), ("uncommon", 44), ("common", 55)], seen);
    }

    [Fact]
    public void A_species_is_the_genus_and_species_of_the_Latin_name_the_game_gives_each_of_its_types()
    {
        // The game's Latin: "itemdesc-creature-butterfly-{type}" (game/lang/en.json, 1.22.7), with stray spaces and capitals;
        // subspecies and aberrations go with their species ("Polygonia c-album ab. dilutus").
        var wrong = game.OnMain(() => Butterflies.Levels.Single().Members
            .Select(m => (Type: m.Values["type"], Species: m.Values["species"],
                Latin: Lang.Get("game:itemdesc-creature-butterfly-" + m.Values["type"])))
            .Where(m => string.Join("-", m.Latin.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2)) != m.Species)
            .Select(m => $"{m.Type}: {m.Species}, the game says '{m.Latin}'")
            .ToList());

        Assert.Empty(wrong);
    }

    [Fact]
    public void A_species_is_named_by_its_Latin_binomial_alike_in_both_languages()
    {
        var species = game.OnMain(() => Butterflies.Definition.Dimensions.Single(d => d.Code == "species").Values.ToList());
        foreach (var s in species)
        {
            var words = s.Split('-', 2);
            string binomial = char.ToUpperInvariant(words[0][0]) + words[0][1..] + " " + words[1];
            Assert.Equal(binomial, LangText("en", "butterflies-species-" + s));
            Assert.Equal(binomial, LangText("uk", "butterflies-species-" + s));
        }
    }

    [Fact]
    public void A_step_of_rarity_is_one_spawn_chance_of_the_game_and_the_steps_go_from_the_rarest()
    {
        // The chance of each type: "chanceByType" of entities/animal/invertebrate/butterfly.json, read by the server.
        var steps = game.OnMain(() => Butterflies.Levels.Single().Members
            .GroupBy(m => m.Values["rarity"])
            .Select(g => (Step: g.Key, Chances: g.Select(m => World.GetEntityType(new AssetLocation("game:butterfly-" + m.Values["type"])))
                .Select(e => e.Server.SpawnConditions.Runtime.Chance).Distinct().ToList()))
            .ToList());
        var order = game.OnMain(() => Butterflies.Definition.Dimensions.Single(d => d.Code == "rarity").Values.ToList());

        Assert.All(steps, s => Assert.Single(s.Chances));
        var byOrder = steps.OrderBy(s => order.IndexOf(s.Step)).Select(s => s.Chances[0]).ToList();
        Assert.Equal(byOrder.Order().ToList(), byOrder);
        Assert.Equal(6, byOrder.Distinct().Count());
    }

    [Fact]
    public void The_only_level_has_a_member_for_each_of_the_169_dead_butterflies_filled_by_that_butterfly_alone()
    {
        var seen = game.OnMain(() =>
        {
            var items = World.Items.Where(i => i.Code is { Domain: "game" } c && c.Path.StartsWith("butterfly-dead-")).Select(i => i.Code.Path).Order().ToList();
            var level = Butterflies.Levels.Single();
            return (items.Count, Butterflies.Levels.Count, level.Id, level.Members.Count, string.Join(",", level.Members.Select(m => m.Fillers.Count).Distinct()),
                level.Members.Select(m => "butterfly-dead-" + m.Values["type"]).Order().SequenceEqual(items));
        });

        Assert.Equal((169, 1, "all", 169, "1", true), seen);
    }

    [Fact]
    public void The_members_follow_the_order_of_the_games_list()
    {
        // The order is the definition's own, that of the item's variant group (not alphabetical: "smallcoppermale" before "smallcopperabschmidtiifemale").
        var order = game.OnMain(() => Butterflies.Levels.Single().Members.Select(m => m.Values["type"]).ToList());
        Assert.Equal("acmonbluefemale", order[0]);
        Assert.Equal("yellowpansymale", order[^1]);
    }

    [Fact]
    public void The_definitions_list_is_exactly_the_games_types_so_the_log_has_no_warning_of_an_unlisted_one()
    {
        var seen = game.OnMain(() => (
            Butterflies.Definition.Dimensions.Single(d => d.Code == "type").Values.Order().ToList(),
            World.Items.Where(i => i.Code is { Domain: "game" } c && c.Path.StartsWith("butterfly-dead-")).Select(i => i.Code.Path["butterfly-dead-".Length..]).Order().ToList()));
        Assert.Equal(seen.Item2, seen.Item1);
    }

    [Fact]
    public void An_album_takes_a_butterfly_from_a_chest_once_and_leaves_the_rest()
    {
        var client = FakeClient.Join(game, "bfly-take");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            var slots = ((IBlockEntityContainer)chest).Inventory;
            slots[0].Itemstack = Vanilla("butterfly-dead-monarch", 3);
            slots[1].Itemstack = Vanilla("butterfly-dead-peacock");
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = Ours("book-album-normal-brickred");
            Mod.Store.WriteAlbum(hand.Itemstack, Core.Album.New(Butterflies, 8, ""));
            Click(player, hand, pos);
            var read = ReadAlbum(hand.Itemstack!);
            return (read.Collected.Keys.Order().ToList(), Inventory(player), Contents(chest));
        });

        Assert.Equal(["monarch", "peacock"], seen.Item1);
        Assert.Equal("butterfly-dead-monarch x1, butterfly-dead-peacock x1", string.Join(", ", seen.Item2.Split(", ").Order()));
        Assert.Equal("butterfly-dead-monarch x2", seen.Item3);
    }

    [Fact]
    public void A_want_list_names_each_missing_butterfly_and_is_given_by_that_butterfly()
    {
        var client = FakeClient.Join(game, "bfly-wants");
        var seen = game.OnMain(() =>
        {
            var player = client.Player;
            var album = Core.Album.New(Butterflies, 8, "");
            var list = Assert.IsType<CreateInvoiceResult.Created>(album.MakeWantList(album.LevelIn(Butterflies)!, 0, new BlockPosition(0, 0, 0))).Invoice;
            var hand = player.InventoryManager.ActiveHotbarSlot;
            hand.Itemstack = new ItemStack(World.GetItem(InvoiceMaker.InvoiceCode(InvoiceKind.WantList)));
            Mod.Store.WriteInvoice(hand.Itemstack, list);

            var (pos, chest) = TestBlocks.Place(game, "chest-east");
            ((IBlockEntityContainer)chest).Inventory[0].Itemstack = Vanilla("butterfly-dead-redadmiral");
            Click(player, hand, pos);

            var left = Mod.Store.ReadInvoice(hand.Itemstack!, InvoiceKind.WantList);
            return (list.Entries.Length, left.Entries.Select(e => e.Member).ToList(), Inventory(player));
        });

        Assert.Equal(169, seen.Item1);
        Assert.Equal(168, seen.Item2.Count);
        Assert.DoesNotContain("redadmiral", seen.Item2);
        Assert.Equal("butterfly-dead-redadmiral x1", seen.Item3);
    }

    [Fact]
    public void A_butterfly_has_no_name_by_values_and_is_named_by_its_item()
    {
        var (name, line) = game.OnMain(() =>
        {
            var level = Butterflies.Levels.Single();
            var member = level.Find("redadmiral")!;
            return (MemberNames.Name(Butterflies, level, member), MemberNames.NameOr(Butterflies, level, member, "Red admiral"));
        });
        Assert.Null(name); // the album cell's tooltip then has no name line: the stack's title is the name (L1)
        Assert.Equal("Red admiral", line);
    }

    [Fact]
    public void The_collection_its_progress_layouts_and_every_species_family_and_step_have_names_in_the_lang()
    {
        var missing = game.OnMain(() => new[] { Butterflies.Definition.NameKey, Butterflies.Definition.Levels.Single().ProgressKey }
            .Concat(Butterflies.Definition.Levels.Single().Layouts.Select(l => l.NameKey))
            .Concat(Butterflies.Definition.Dimensions.Where(d => d.Code != "type").SelectMany(d => d.Values.Select(d.ValueNameKey)))
            .Where(key => key == null || Lang.Get(key) == key).ToList());
        Assert.Empty(missing);
    }
}
