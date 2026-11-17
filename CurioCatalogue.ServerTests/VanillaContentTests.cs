using CurioCatalogue.Adapters;
using Vintagestory.API.Common;

namespace CurioCatalogue.ServerTests;

/// <summary>
/// The server twins of assumptions the adapter tests make about vanilla stacks (<c>StackKeysTests</c>, <c>CollectionRegistry</c>):
/// here the game itself makes the stack, so the assumption is checked rather than written into the test.
/// </summary>
[Collection(ServerCollection.Name)]
public class VanillaContentTests(GameServerFixture server) : BookTestBase(server)
{
    [Fact]
    public void A_tapestry_taken_off_a_wall_is_a_north_piece_with_its_picture_and_a_member_of_the_collection()
    {
        // Hung on an east wall; the drop is what the player gets by breaking it (vssurvivalmod BlockTapestry.cs#GetDrops,
        // BehaviorHorizontalAttachable.cs:66#dropBlockFace, "north").
        var seen = game.OnMain(() =>
        {
            var (pos, _) = TestBlocks.Place(game, "tapestry-east", s => s.Attributes.SetString("type", "ambush1"));
            var drops = World.BlockAccessor.GetBlock(pos).GetDrops(World, pos, null);
            return (string.Join(", ", drops.Select(Describe)), Member: drops.Length == 1 && Mod.Collections.Defined("tapestries") is { } c && Mod.Collections.HasMember(c, drops[0]));
        });

        Assert.Equal(("tapestry-north:ambush1 x1", true), seen);
    }

    [Fact]
    public void Every_icon_a_collection_gives_its_values_levels_and_layouts_is_in_the_game()
    {
        // The window draws a value's icon in a table's heading (L2b), a level's and a layout's beside its name in the choice
        // (L2c): an item or block, or an SVG of ours, found by DefinitionIcons.Find; one the game lacks falls back to words,
        // silently, so a misspelt code or file is caught here. Our SVGs are in our mod, which this server loads (textures are
        // universal: vsapi AssetCategory.cs:23#textures).
        var seen = game.OnMain(() => Mod.Collections.All.Values
            .SelectMany(c => c.Definition.Dimensions
                .SelectMany(d => c.Values[d.Code].Select(v => (Name: $"{c.Id} {d.Code} {v}", Icon: d.ValueIcon(v))))
                .Concat(c.Definition.Levels.Select(l => (Name: $"{c.Id} level {l.Id}", l.Icon)))
                .Concat(c.Definition.Levels.SelectMany(l => l.Layouts.Select(y => (Name: $"{c.Id} layout {l.Id} {y.Id}", y.Icon)))))
            .Where(i => i.Icon != null)
            .Select(i => (i.Name, i.Icon, Found: DefinitionIcons.Find(World, i.Icon) != null))
            .ToList());

        Assert.DoesNotContain(seen, i => !i.Found);
        // The crystals' ores, grades and rocks (16 + 4 + 17), the ores in a host by the definition's table; three levels, two
        // layouts. The seashells' six colours, our round swatches, and two levels.
        Assert.Equal(37 + 3 + 2, seen.Count(i => i.Name.StartsWith("crystals ")));
        Assert.Equal(6 + 2, seen.Count(i => i.Name.StartsWith("seashells ")));
        Assert.Contains(("seashells color darkpurple", "curiocatalogue:textures/icons/seashell-darkpurple.svg", true), seen);
        Assert.Contains(("crystals ore galena_nativesilver", "game:nugget-nativesilver", true), seen);
        Assert.Contains(("crystals grade rich", "curiocatalogue:textures/icons/grade-rich.svg", true), seen);
        Assert.Contains(("crystals layout all byrock", "game:stone-granite", true), seen);
        Assert.Contains(("seashells level colors", "game:seashell-scallop-turquoise", true), seen);
    }

    [Fact]
    public void A_variants_pattern_warns_once_of_codes_with_its_prefix_that_it_does_not_fit()
    {
        // R1, G3.8: a value is the text between dashes, so a code with another number of parts is left out. A definition that
        // forgot the rock of "crystalizedore-{grade}-{ore}-{rock}" finds none of the crystals; the log says why, once.
        const string json = """
            { "source": { "kind": "variants", "code": "game:crystalizedore-{grade}-{ore}" },
              "dimensions": [ { "code": "grade", "values": [] }, { "code": "ore", "values": [] } ],
              "levels": [ { "id": "all", "keep": ["grade", "ore"], "drop": {} } ] }
            """;
        int mark = game.LogMark();
        int found = game.OnMain(() =>
        {
            var registry = new CollectionRegistry(World, Mod.Keys, new Dictionary<string, CollectionSettings>(),
                () => [new DefinitionAsset(new AssetLocation("othermod", CollectionRegistry.DefinitionsPath + "pairs.json"), json)]);
            return registry.All.Count;
        });

        var warnings = game.LogSince(mark).Where(l => l.Type == EnumLogType.Warning && l.Text.Contains("collection 'pairs'")).ToList();
        Assert.Equal(0, found);
        var warning = Assert.Single(warnings);
        Assert.Contains("crystalizedore-", warning.Text);
        Assert.Contains("do not fit the pattern", warning.Text);
    }

    [Fact]
    public void Every_lang_key_a_collection_names_is_in_the_lang()
    {
        // A key the lang lacks shows as the key itself in the window, the tooltip and the want list (Lang.Get), silently.
        // The per-collection tests check some keys; this checks every key every definition names: the collection, its levels
        // (name, progress, member), layouts, marks, and each value that occurs (name, short name) with the "any" phrase.
        // Wildcard keys (game:tapestry-{value}) as the window matches them (MemberNames.Translated).
        var missing = game.OnMain(() => Mod.Collections.All.Values.SelectMany(c =>
                new[] { c.Definition.NameKey }
                .Concat(c.Definition.Levels.SelectMany(l => new[] { l.NameKey, l.ProgressKey, l.MemberKey }))
                .Concat(c.Definition.Levels.SelectMany(l => l.Layouts.Select(y => y.NameKey)))
                .Concat((c.Definition.Marks ?? []).Select(m => m.NameKey))
                .Concat(c.Definition.Dimensions.SelectMany(d => c.Values[d.Code]
                    .SelectMany(v => new[] { d.ValueNameKey(v), d.ValueShortNameKey(v) }).Append(d.AnyKey)))
                .OfType<string>().Where(key => MemberNames.Translated(key) == null).Select(key => $"{c.Id}: {key}"))
            .Distinct()
            // A picture of one piece is named by the piece alone: no key for its section, on purpose (album polish plan, L1).
            .Except(["tapestries: curiocatalogue:tapestries-section-1x1"]).ToList());

        Assert.Empty(missing);
    }

    [Fact]
    public void The_tapestries_are_88_pieces_in_31_pictures_as_the_guide_says()
    {
        // docs/guide/README.md, Collections, and A12 ("the bar at 0/88"): the other collections' numbers are pinned by their own tests
        // (CrystalsTests, SeashellsTests, ButterfliesTests); a game update that adds a tapestry is caught here, not by a player.
        var seen = game.OnMain(() =>
        {
            var tapestries = Mod.Collections.Defined("tapestries")!;
            return (tapestries.Levels.Single().Members.Count, tapestries.Values["picture"].Count);
        });
        Assert.Equal((88, 31), seen);
    }

    [Fact]
    public void Every_tapestry_piece_has_the_flat_image_the_album_draws()
    {
        // The album draws a piece's own texture (Member.FlatTexture), the one the game puts on the wall (vssurvivalmod
        // BlockTapestry.cs:40#texturePath). Textures load on the server too (vsapi AssetCategory.cs:23#textures, Universal), but the
        // Linux server archive CI uses ships no block textures (docs/test-server.md, checked 09.11.2026): there only the
        // attribute is checked, the files where the game has them.
        var seen = game.OnMain(() =>
        {
            var members = Mod.Collections.Defined("tapestries")!.Levels.SelectMany(l => l.Members).ToList();
            var paths = members.Select(m => (m.Id, Path: m.Attributes.GetValueOrDefault(CurioCatalogue.Core.Member.FlatTexture))).ToList();
            return (members.Count, paths, Shipped: paths.Any(p => p.Path != null && World.AssetManager.Exists(new AssetLocation(p.Path))),
                Exist: paths.Where(p => p.Path != null && World.AssetManager.Exists(new AssetLocation(p.Path))).Select(p => p.Id).ToHashSet());
        });

        Assert.True(seen.Count > 30, $"only {seen.Count} pieces");
        Assert.Empty(seen.paths.Where(p => p.Path is null || !p.Path.StartsWith("game:textures/block/cloth/tapestry/")).Select(p => p.Id));
        if (seen.Shipped)
            Assert.Empty(seen.paths.Where(p => !seen.Exist.Contains(p.Id)).Select(p => p.Id));
    }

    [Fact]
    public void A_hot_ingot_counts_as_the_same_item_as_a_cold_one()
    {
        // The game keeps the heat where GlobalConstants.IgnoredStackAttributes says, so the key ignores it without our config.
        var seen = game.OnMain(() =>
        {
            var cold = Vanilla("ingot-iron");
            var hot = Vanilla("ingot-iron");
            hot.Collectible.SetTemperature(World, hot, 600);
            return (Heated: hot.Collectible.GetTemperature(World, hot) > 500, Same: Mod.Keys.KeyOf(hot).Equals(Mod.Keys.KeyOf(cold)));
        });

        Assert.Equal((true, true), seen);
    }
}
