using System.Collections.Immutable;
using CurioCatalogue.Core;

namespace CurioCatalogue.Core.Tests;

/// <summary>
/// Collections built from definitions (docs/decisions.md, Collections), on the three shapes designed in
/// docs/plans/2026-11-04-collections-design.md: ore crystals (C1: three levels, Cartesian, `max`), seashells (C2: `any`,
/// rarity by a rule) and tapestries (hierarchical, a picture's shape).
/// </summary>
public class DefinedCollectionTests
{
    private static DefinedCollection Built(CollectionBuild build, List<string>? warnings = null) => build switch
    {
        CollectionBuild.Built b => Remember(b, warnings),
        CollectionBuild.Invalid i => throw new Xunit.Sdk.XunitException("Invalid: " + string.Join("; ", i.Errors)),
        _ => throw new InvalidOperationException(),
    };

    private static DefinedCollection Remember(CollectionBuild.Built b, List<string>? warnings)
    {
        warnings?.AddRange(b.Warnings);
        return b.Collection;
    }

    private static IReadOnlyList<string> Errors(CollectionBuild build) =>
        Assert.IsType<CollectionBuild.Invalid>(build).Errors;

    private static Dictionary<string, Drop> D(params (string, Drop)[] drops) => drops.ToDictionary(d => d.Item1, d => d.Item2);

    private static Dictionary<string, string> V(params (string, string)[] values) => values.ToDictionary(v => v.Item1, v => v.Item2);

    // --- Ore crystals: the real list of points, game 1.22.7 ---

    private static readonly string[] Grades = ["poor", "medium", "rich", "bountiful"];

    private static readonly string[] Ores =
    [
        "nativecopper", "malachite", "cassiterite", "sphalerite", "bismuthinite", "galena", "galena_nativesilver",
        "quartz_nativegold", "quartz_nativesilver", "limonite", "hematite", "magnetite", "pentlandite", "chromite", "ilmenite",
        "rhodochrosite", "uranium", "stibnite",
    ];

    private static readonly string[] Rocks =
    [
        "andesite", "granite", "basalt", "peridotite", "kimberlite", "slate", "phyllite", "chert", "claystone", "conglomerate",
        "sandstone", "shale", "limestone", "chalk", "whitemarble", "redmarble", "greenmarble",
    ];

    /// <summary>The 54 combinations that never generate in vanilla (collections design, C1).</summary>
    private static readonly string[] NeverGenerated =
    [
        "crystalizedore-*-uranium-*", "crystalizedore-*-stibnite-*", "crystalizedore-*-hematite-sandstone",
        "crystalizedore-rich-magnetite-slate", "crystalizedore-bountiful-magnetite-slate", "crystalizedore-rich-ilmenite-peridotite",
    ];

    private static CollectionDefinition Crystals(IReadOnlyList<string>? grades = null) => new(
        "crystals",
        [
            new DimensionDef("grade", grades ?? Grades),
            new DimensionDef("ore", Ores, NameKey: "game:ore-{value}"),
            new DimensionDef("rock", Rocks, NameKey: "game:rock-{value}", Icon: "game:rock-{value}"),
        ],
        [
            new LevelDef("richest", ["ore"], D(("grade", Drop.Max), ("rock", Drop.Any)), [new LayoutDef("order", [], new Arrangement.InOrder())]),
            new LevelDef("ore-rock", ["ore", "rock"], D(("grade", Drop.Max)),
                [new LayoutDef("table", [], new Arrangement.Table("ore", "rock"))]),
            new LevelDef("all", ["grade", "ore", "rock"], D(),
            [
                new LayoutDef("by-ore", ["ore"], new Arrangement.Table("grade", "rock")),
                new LayoutDef("by-rock", ["rock"], new Arrangement.Table("grade", "ore")),
            ]),
        ],
        [],
        NeverGenerated);

    private static ItemKey CrystalKey(string code) => new(CollectibleKind.Item, "game:" + code, "");

    /// <summary>One point per allowed variant of the game's crystal item; the sample is the code.</summary>
    private static readonly IReadOnlyList<SourcePoint> CrystalPoints =
        File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Data", "crystalizedore-allowedvariants.txt"))
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .Select(code =>
            {
                var parts = code.Split('-');
                return new SourcePoint(code, V(("grade", parts[1]), ("ore", parts[2]), ("rock", parts[3])), CrystalKey(code), code);
            })
            .ToList();

    [Fact]
    public void The_game_allows_407_crystals()
    {
        Assert.Equal(407, CrystalPoints.Count);
    }

    [Fact]
    public void Crystal_levels_have_16_118_and_353_members()
    {
        var warnings = new List<string>();
        var crystals = Built(DefinedCollection.Build(Crystals(), CrystalPoints), warnings);

        Assert.Equal([16, 118, 353], crystals.Levels.Select(l => l.Members.Count));
        Assert.Empty(warnings);
        Assert.DoesNotContain("uranium", crystals.Values["ore"]);
    }

    [Fact]
    public void The_richest_level_is_filled_by_the_highest_grade_of_each_ore_in_any_rock()
    {
        var richest = Built(DefinedCollection.Build(Crystals(), CrystalPoints)).Level("richest")!;

        // Bountiful quartz gold from each of the 13 rocks it occurs in; galena with silver is medium at most; magnetite's
        // bountiful exists only in slate, which never generates: rich (collections design, C1).
        var gold = richest.Find("quartz_nativegold")!;
        var bountifulGold = CrystalPoints.Where(p => p.Name.StartsWith("crystalizedore-bountiful-quartz_nativegold-")).Select(p => p.Key).ToList();
        Assert.Equal(13, gold.Fillers.Count);
        Assert.Equal(bountifulGold.ToHashSet(), gold.Fillers.ToHashSet());
        Assert.Equal(gold.Fillers.Count, gold.Stacks.Count);
        Assert.Equal("bountiful", gold.Values["grade"]);
        Assert.False(gold.Values.ContainsKey("rock"));
        // The stacks to cycle through follow the rocks' order.
        Assert.Equal(gold.Fillers.OrderBy(k => Array.IndexOf(Rocks, k.Code.Split('-')[3])), gold.Fillers);

        Assert.Equal("medium", richest.Find("galena_nativesilver")!.Values["grade"]);
        Assert.Equal("rich", richest.Find("magnetite")!.Values["grade"]);
        Assert.Equal(14, richest.Members.Count(m => m.Values["grade"] == "bountiful"));

        // A crystal of a lower grade fills nothing at this level.
        Assert.Null(richest.MemberOf(CrystalKey("crystalizedore-poor-quartz_nativegold-granite")));
        Assert.All(bountifulGold, k => Assert.Same(gold, richest.MemberOf(k)));
    }

    private static IEnumerable<(string, string)> NameParts(DefinedCollection c, string level, string member) =>
        c.NameParts(c.Level(level)!, c.Level(level)!.Find(member)!).Select(l => (l.Dimension.Code, l.Value));

    [Fact]
    public void A_member_is_named_by_its_kept_values_in_the_order_of_keep_then_by_the_values_its_items_share()
    {
        var plain = Crystals();
        var definition = plain with
        {
            Dimensions = [.. plain.Dimensions.Select(d => d.Code == "grade" ? d with { NameKey = "k:grade-{value}" } : d)],
            Levels = [.. plain.Levels.Select(l => l.Id == "all" ? l with { Keep = ["ore", "grade", "rock"] } : l)],
        };
        var crystals = Built(DefinedCollection.Build(definition, CrystalPoints));

        // Every combination: the level's own order of the parts, while the member's id keeps the definition's.
        Assert.Equal([("ore", "nativecopper"), ("grade", "poor"), ("rock", "andesite")], NameParts(crystals, "all", "poor-nativecopper-andesite"));
        // The richest level drops the rock as any, but bountiful native copper occurs in basalt only: one item, its rock said.
        // Its grade, dropped as max, is never said, though one value.
        Assert.Equal([("ore", "nativecopper"), ("rock", "basalt")], NameParts(crystals, "richest", "nativecopper"));
        // Quartz gold of any of 13 rocks: the rock is free ("any rock", Free), not a part.
        Assert.Equal([("ore", "quartz_nativegold")], NameParts(crystals, "richest", "quartz_nativegold"));
        Assert.Equal([("ore", "galena"), ("rock", "claystone")], NameParts(crystals, "ore-rock", "galena-claystone"));
    }

    [Fact]
    public void A_dimension_without_names_is_no_part_of_a_members_name()
    {
        var plain = Crystals();
        var unnamed = Built(DefinedCollection.Build(plain with { Dimensions = [.. plain.Dimensions.Select(d => d with { NameKey = null })] }, CrystalPoints));

        // The grade of the plain definition has no names either: kept, still not said.
        Assert.Equal([("ore", "nativecopper"), ("rock", "andesite")], NameParts(Built(DefinedCollection.Build(plain, CrystalPoints)), "all", "poor-nativecopper-andesite"));
        // Nothing named: the adapters name the member by its first item.
        Assert.Empty(NameParts(unnamed, "all", "poor-nativecopper-andesite"));
    }

    [Fact]
    public void The_ore_and_rock_level_is_filled_by_the_richest_grade_each_rock_gets()
    {
        var level = Built(DefinedCollection.Build(Crystals(), CrystalPoints)).Level("ore-rock")!;

        // Collections design, C1: medium 44, rich 31, bountiful 43; cassiterite bountiful in granite, medium in andesite.
        var byGrade = level.Members.GroupBy(m => m.Values["grade"]).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(new Dictionary<string, int> { ["medium"] = 44, ["rich"] = 31, ["bountiful"] = 43 }, byGrade);
        Assert.Equal("bountiful", level.Find("cassiterite-granite")!.Values["grade"]);
        Assert.Equal([CrystalKey("crystalizedore-medium-cassiterite-andesite")], level.Find("cassiterite-andesite")!.Fillers);
        Assert.All(level.Members, m => Assert.Single(m.Fillers));
    }

    [Fact]
    public void Every_combination_is_its_own_member_and_layouts_group_it_by_ore_or_by_rock()
    {
        var all = Built(DefinedCollection.Build(Crystals(), CrystalPoints)).Level("all")!;
        var member = all.Find("poor-nativecopper-andesite")!;
        Assert.Same(member, all.MemberOf(CrystalKey("crystalizedore-poor-nativecopper-andesite")));

        var byOre = all.Groups(all.Layouts[0]);
        Assert.Equal(Ores.Take(16), byOre.Select(g => g.Values.Single()));
        // A table of grade × rock: row by row, each row in the rocks' order.
        var copper = byOre[0].Members;
        Assert.Equal(copper.OrderBy(m => Array.IndexOf(Grades, m.Values["grade"])).ThenBy(m => Array.IndexOf(Rocks, m.Values["rock"])), copper);

        var byRock = all.Groups(all.Layouts[1]);
        Assert.Equal(Rocks.Length, byRock.Count);
        Assert.Equal(353, byRock.Sum(g => g.Members.Count));

        // The default layout gives the order of taking new: whole ores first.
        Assert.Equal(byOre.SelectMany(g => g.Members), all.Order);
    }

    [Fact]
    public void A_grade_missing_from_the_list_comes_last_and_ranks_lowest_with_a_warning()
    {
        var warnings = new List<string>();
        var crystals = Built(DefinedCollection.Build(Crystals(grades: ["poor", "medium", "rich"]), CrystalPoints), warnings);

        Assert.Single(warnings);
        Assert.Contains("bountiful", warnings[0]);
        Assert.Equal(["poor", "medium", "rich", "bountiful"], crystals.Values["grade"]);
        // Max: rich beats the unlisted bountiful.
        Assert.Equal("rich", crystals.Level("richest")!.Find("nativecopper")!.Values["grade"]);
    }

    [Fact]
    public void Progress_counts_members_and_complete_groups_of_the_default_layout()
    {
        var all = Built(DefinedCollection.Build(Crystals(), CrystalPoints)).Level("all")!;
        var rhodochrosite = all.Groups(all.DefaultLayout).Single(g => g.Values[0] == "rhodochrosite").Members.Select(m => m.Id).ToList();

        var progress = all.Progress(rhodochrosite.Append("poor-nativecopper-andesite").Append("no-such-member"));

        Assert.Equal(new LevelProgress(rhodochrosite.Count + 1, 353, 1, 16), progress);
    }

    // --- Seashells: 7 types × 6 colours, made-up keys ---

    private static readonly string[] ShellTypes = ["scallop", "sundial", "turritella", "clam", "conch", "seastar", "volute"];
    private static readonly string[] ShellColours = ["latte", "plain", "seafoam", "darkpurple", "cinnamon", "turquoise"];

    private static ItemKey ShellKey(string type, string colour) => new(CollectibleKind.Block, $"game:seashell-{type}-{colour}", "");

    private static readonly CollectionDefinition Seashells = new(
        "seashells",
        [new DimensionDef("type", ShellTypes), new DimensionDef("color", ShellColours, Icon: "game:seashell-scallop-{value}")],
        [
            new LevelDef("type", ["type"], D(("color", Drop.Any)), []),
            new LevelDef("all", ["type", "color"], D(), [new LayoutDef("table", [], new Arrangement.Table("type", "color"))]),
        ],
        [new AttributeRule(new Dictionary<string, IReadOnlyList<string>> { ["color"] = ["seafoam", "turquoise"] }, V(("rarity", "rare")))],
        []);

    // In the source's order colour by colour, to see that the dimensions' order wins.
    private static readonly IReadOnlyList<SourcePoint> ShellPoints =
        ShellColours.Reverse().SelectMany(c => ShellTypes.Select(t =>
            new SourcePoint($"seashell-{t}-{c}", V(("type", t), ("color", c)), ShellKey(t, c), $"{t}-{c}"))).ToList();

    [Fact]
    public void A_seashell_type_is_filled_by_a_shell_of_any_colour()
    {
        var shells = Built(DefinedCollection.Build(Seashells, ShellPoints));
        var byType = shells.Level("type")!;

        Assert.Equal(ShellTypes, byType.Members.Select(m => m.Id));
        var conch = byType.Find("conch")!;
        Assert.Equal(ShellColours.Select(c => ShellKey("conch", c)), conch.Fillers);
        Assert.Equal(ShellColours.Select(c => (object)$"conch-{c}"), conch.Stacks);
        Assert.All(ShellColours, c => Assert.Same(conch, byType.MemberOf(ShellKey("conch", c))));
        // The level has no single colour per member, so the rarity rule does not apply.
        Assert.All(byType.Members, m => Assert.Empty(m.Attributes));
        Assert.Equal(byType.Members, byType.Order);
        Assert.Equal("game:seashell-scallop-turquoise", shells.Definition.Dimensions[1].ValueIcon("turquoise"));
    }

    [Fact]
    public void Every_seashell_is_a_member_and_rare_colours_are_marked()
    {
        var all = Built(DefinedCollection.Build(Seashells, ShellPoints)).Level("all")!;

        Assert.Equal(42, all.Members.Count);
        Assert.Equal("scallop-latte", all.Order[0].Id);
        Assert.Equal("scallop-plain", all.Order[1].Id);
        Assert.Equal(14, all.Members.Count(m => m.Attributes.GetValueOrDefault("rarity") == "rare"));
        Assert.Equal("rare", all.Find("volute-turquoise")!.Attributes["rarity"]);
    }

    // --- Tapestries: hierarchical dimensions and a picture's shape, made-up pieces ---

    private static ItemKey PieceKey(string piece) => new(CollectibleKind.Block, "game:tapestry-north", $"{{\"type\":\"{piece}\"}}");

    private static SourcePoint Piece(string piece, string picture, string size, int x, int y) =>
        new(piece, V(("piece", piece), ("picture", picture), ("size", size)), PieceKey(piece), piece,
            V(("x", x.ToString()), ("y", y.ToString())));

    // As the block's sizes would give them: ambush "3x1", salvation "2x2" (x then y in the name), notice "1x1".
    private static readonly IReadOnlyList<SourcePoint> PiecePoints =
    [
        Piece("ambush1", "ambush", "3x1", 1, 1), Piece("ambush2", "ambush", "3x1", 2, 1), Piece("ambush3", "ambush", "3x1", 3, 1),
        Piece("salvation11", "salvation", "2x2", 1, 1), Piece("salvation12", "salvation", "2x2", 1, 2),
        Piece("salvation21", "salvation", "2x2", 2, 1), Piece("salvation22", "salvation", "2x2", 2, 2),
        Piece("notice", "notice", "1x1", 1, 1),
    ];

    private static CollectionDefinition Tapestries(params string[] exclude) => new(
        "tapestries",
        [new DimensionDef("piece", []), new DimensionDef("picture", [], NameKey: "tapestry-{value}"), new DimensionDef("size", [])],
        [
            new LevelDef("pieces", ["piece"], D(("picture", Drop.Any), ("size", Drop.Any)),
            [
                new LayoutDef("by-picture", ["picture"], new Arrangement.Shape()),
                new LayoutDef("by-size", ["size", "picture"], new Arrangement.Shape()),
            ]),
        ],
        [],
        exclude);

    [Fact]
    public void Tapestry_pieces_are_grouped_by_picture_in_its_shape()
    {
        var warnings = new List<string>();
        var pieces = Built(DefinedCollection.Build(Tapestries(), PiecePoints), warnings).Levels.Single();

        Assert.Empty(warnings);
        Assert.Equal(PiecePoints.Select(p => p.Name), pieces.Members.Select(m => m.Id));
        Assert.Equal("salvation", pieces.Find("salvation12")!.Values["picture"]);
        Assert.Equal("2x2", pieces.Find("salvation12")!.Values["size"]);

        var pictures = pieces.Groups(pieces.DefaultLayout);
        Assert.Equal(["ambush", "salvation", "notice"], pictures.Select(g => g.Values.Single()));
        // Reading order: row by row.
        Assert.Equal(["salvation11", "salvation21", "salvation12", "salvation22"], pictures[1].Members.Select(m => m.Id));
        Assert.Equal(["1", "2"], new[] { pictures[1].Members[2].Attributes["x"], pictures[1].Members[2].Attributes["y"] });

        var bySize = pieces.Groups(pieces.Layouts[1]);
        Assert.Equal([["3x1", "ambush"], ["2x2", "salvation"], ["1x1", "notice"]], bySize.Select(g => g.Values));

        Assert.Equal(new LevelProgress(3, 8, 1, 3), pieces.Progress(["ambush1", "ambush2", "ambush3"]));
        Assert.Equal("tapestry-ambush", Tapestries().Dimensions[1].ValueNameKey(pieces.Members[0].Values["picture"]));
    }

    [Fact]
    public void A_piece_is_named_by_the_values_it_shares_in_dimensions_dropped_as_any()
    {
        var tapestries = Built(DefinedCollection.Build(Tapestries(), PiecePoints));

        // The level keeps the piece (no names here) and drops the picture as any; a piece is of one picture, so it is said.
        Assert.Equal([("picture", "salvation")], NameParts(tapestries, "pieces", "salvation21"));
    }

    [Fact]
    public void Excluded_pieces_are_left_out_by_their_name()
    {
        var pieces = Built(DefinedCollection.Build(Tapestries("SALVATION*"), PiecePoints)).Levels.Single();

        Assert.Equal(["ambush1", "ambush2", "ambush3", "notice"], pieces.Members.Select(m => m.Id));
        Assert.Null(pieces.MemberOf(PieceKey("salvation11")));
    }

    // --- The check: a key fills at most one member in a level ---

    [Fact]
    public void A_key_filling_two_members_of_a_level_makes_the_collection_invalid()
    {
        // Two pieces of one stack (a source or the config's Add getting it wrong).
        var points = PiecePoints.Append(new SourcePoint("ambush4", V(("piece", "ambush4"), ("picture", "ambush"), ("size", "3x1")),
            PieceKey("ambush1"), "ambush4", V(("x", "4"), ("y", "1"))));

        var errors = Errors(DefinedCollection.Build(Tapestries(), points));

        Assert.Contains(errors, e => e.Contains("ambush1") && e.Contains("ambush4") && e.Contains("pieces"));
    }

    [Fact]
    public void A_key_may_fill_one_member_through_several_points()
    {
        // The same shell given twice under two names lands in one member: no conflict.
        var points = ShellPoints.Append(new SourcePoint("again", V(("type", "clam"), ("color", "plain")), ShellKey("clam", "plain"), "again"));

        var all = Built(DefinedCollection.Build(Seashells, points)).Level("all")!;

        Assert.Equal([ShellKey("clam", "plain")], all.Find("clam-plain")!.Fillers);
    }

    [Fact]
    public void A_point_given_twice_by_name_keeps_the_first_with_a_warning()
    {
        var warnings = new List<string>();
        var points = PiecePoints.Append(Piece("notice", "other", "1x1", 1, 1));

        var pieces = Built(DefinedCollection.Build(Tapestries(), points), warnings).Levels.Single();

        Assert.Equal("notice", pieces.Find("notice")!.Values["picture"]);
        Assert.Single(warnings);
    }

    // --- Definitions that cannot be built ---

    public static TheoryData<string, CollectionDefinition> BadDefinitions => new()
    {
        { "unknown dimension 'colour'", Seashells with { Levels = [new LevelDef("x", ["type"], D(("colour", Drop.Any)), [])] } },
        { "neither keeps nor drops 'color'", Seashells with { Levels = [new LevelDef("x", ["type"], D(), [])] } },
        { "keeps no dimension", Seashells with { Levels = [new LevelDef("x", [], D(("type", Drop.Any), ("color", Drop.Any)), [])] } },
        { "both keeps and drops 'type'", Seashells with { Levels = [new LevelDef("x", ["type", "color"], D(("type", Drop.Max)), [])] } },
        { "No levels", Seashells with { Levels = [] } },
        { "unknown dimension 'shape'", Seashells with
            { Levels = [new LevelDef("x", ["type", "color"], D(), [new LayoutDef("l", ["shape"], new Arrangement.InOrder())])] } },
        { "'color' is not one value", Seashells with
            { Levels = [new LevelDef("x", ["type"], D(("color", Drop.Any)), [new LayoutDef("l", [], new Arrangement.Table("type", "color"))])] } },
        { "no integer x and y", Seashells with
            { Levels = [new LevelDef("x", ["type", "color"], D(), [new LayoutDef("l", [], new Arrangement.Shape())])] } },
        { "lists 'plain' twice", Seashells with { Dimensions = [new DimensionDef("type", ShellTypes), new DimensionDef("color", ["plain", "plain"])] } },
        { "unknown dimension 'colour'", Seashells with
            { Rules = [new AttributeRule(new Dictionary<string, IReadOnlyList<string>> { ["colour"] = ["plain"] }, V())] } },
        // K7 (T3): max with no list of values would follow the source's order, for variants the world's item ids.
        { "drops 'color' as max, but the dimension lists no values", Seashells with
            { Dimensions = [new DimensionDef("type", ShellTypes), new DimensionDef("color", [])],
              Levels = [new LevelDef("x", ["type"], D(("color", Drop.Max)), [])] } },
        // K7: an album with no level chosen stores "".
        { "A level has an empty id", Seashells with { Levels = [new LevelDef("", ["type", "color"], D(), [])] } },
    };

    [Theory]
    [MemberData(nameof(BadDefinitions))]
    public void A_bad_definition_is_invalid_and_says_why(string why, CollectionDefinition definition)
    {
        var errors = Errors(DefinedCollection.Build(definition, ShellPoints));

        Assert.Contains(errors, e => e.Contains(why));
    }

    [Fact]
    public void A_point_without_a_value_in_a_dimension_is_invalid()
    {
        var points = ShellPoints.Append(new SourcePoint("odd", V(("type", "clam")), ShellKey("clam", "odd"), "odd"));

        var errors = Errors(DefinedCollection.Build(Seashells, points));

        Assert.Contains(errors, e => e.Contains("'odd'") && e.Contains("color"));
    }

    [Fact]
    public void A_dimension_names_a_value_short_for_a_heading_or_as_in_full_when_it_has_no_short_name()
    {
        var full = new DimensionDef("ore", [], NameKey: "k:ore-{value}");
        Assert.Equal("k:ore-galena", full.ValueShortNameKey("galena"));
        Assert.Equal("k:ore-short-galena", (full with { ShortNameKey = "k:ore-short-{value}" }).ValueShortNameKey("galena"));
        Assert.Null(new DimensionDef("ore", []).ValueShortNameKey("galena"));
    }

    [Fact]
    public void A_value_s_icon_is_its_own_from_the_table_else_the_template_s()
    {
        var icons = new Dictionary<string, string> { ["quartz_nativegold"] = "game:nugget-nativegold" };
        var ore = new DimensionDef("ore", [], Icon: "game:nugget-{value}", Icons: icons);
        Assert.Equal("game:nugget-galena", ore.ValueIcon("galena"));
        Assert.Equal("game:nugget-nativegold", ore.ValueIcon("quartz_nativegold"));

        // A table alone: the values it lacks have no icon, but the dimension is still headed by icons.
        var byHand = new DimensionDef("ore", [], Icons: icons);
        Assert.Null(byHand.ValueIcon("galena"));
        Assert.True(byHand.HasIcons);
        Assert.False(new DimensionDef("ore", [], Icons: new Dictionary<string, string>()).HasIcons);
        Assert.False(new DimensionDef("ore", []).HasIcons);
    }

    [Theory]
    [InlineData("curiocatalogue:textures/icons/grade-poor.svg", true)]
    [InlineData("curiocatalogue:textures/icons/grade-poor.SVG", true)]
    [InlineData("game:nugget-galena", false)]
    [InlineData("game:svgplant", false)]
    public void An_icon_is_an_svg_by_its_path_s_ending(string icon, bool svg) => Assert.Equal(svg, DimensionDef.IsSvg(icon));
}
