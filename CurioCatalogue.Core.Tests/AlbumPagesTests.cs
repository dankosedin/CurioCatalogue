using System.Collections.Immutable;
using CurioCatalogue.Core;

namespace CurioCatalogue.Core.Tests;

/// <summary>
/// The album window's pages as data (docs/plans/2026-11-04-album-view.md, A2, "The view on paper"): whole groups in order, side
/// by side then under each other; a group larger than a page cut by lines; tables of the values that occur; a picture's shape;
/// tabs with pages of their own; search. On made-up collections with round sizes: a cell 10, a heading line 5, a gap 2.
/// </summary>
public class AlbumPagesTests
{
    private static PageMetrics Metrics(int width, int height, int minGroupWidth = 0) =>
        new(width, height, Cell: 10, Heading: 5, Gap: 2, TextHeadingWidth: 30, MinGroupWidth: minGroupWidth);

    private static Dictionary<string, string> V(params (string, string)[] values) => values.ToDictionary(v => v.Item1, v => v.Item2);

    private static DefinedCollection Built(CollectionDefinition definition, IEnumerable<SourcePoint> points) =>
        DefinedCollection.Build(definition, points) switch
        {
            CollectionBuild.Built b => b.Collection,
            CollectionBuild.Invalid i => throw new Xunit.Sdk.XunitException("Invalid: " + string.Join("; ", i.Errors)),
            _ => throw new InvalidOperationException(),
        };

    private static AlbumPages Pages(DefinedCollection c, string level, string layout, PageMetrics m)
    {
        var l = c.Level(level)!;
        return AlbumPages.Build(c, l, l.Layouts.Single(x => x.Id == layout), m);
    }

    private static string[][] GroupsByPage(AlbumPages pages) =>
        pages.Pages.Select(p => p.Groups.Select(g => string.Join("/", g.Group.Values) + (g.Continued ? "+" : "")).ToArray()).ToArray();

    private static (int, int) At(GroupBox box, string memberId)
    {
        var cell = box.Cells.Single(c => c.Member.Id == memberId);
        return (cell.X, cell.Y);
    }

    // --- Tapestries: pictures as their own shape; ambush 3×1, salvation 2×2, notice 1×1 ---

    private static SourcePoint Piece(string piece, string picture, int x, int y) =>
        new(piece, V(("picture", picture), ("piece", piece)), new ItemKey(CollectibleKind.Block, "game:tapestry-north", piece), piece,
            V(("x", x.ToString()), ("y", y.ToString())));

    private static readonly DefinedCollection Tapestries = Built(
        new CollectionDefinition("tapestries",
            [new DimensionDef("picture", []), new DimensionDef("piece", [])],
            [new LevelDef("pieces", ["piece"], new Dictionary<string, Drop> { ["picture"] = Drop.Any },
                [new LayoutDef("pictures", ["picture"], new Arrangement.Shape())])],
            [], []),
        [
            Piece("ambush1", "ambush", 1, 1), Piece("ambush2", "ambush", 2, 1), Piece("ambush3", "ambush", 3, 1),
            Piece("salvation11", "salvation", 1, 1), Piece("salvation21", "salvation", 1, 2),
            Piece("salvation12", "salvation", 2, 1), Piece("salvation22", "salvation", 2, 2),
            Piece("notice", "notice", 1, 1),
        ]);

    [Fact]
    public void A_picture_is_laid_out_as_its_shape_under_its_heading()
    {
        var pages = Pages(Tapestries, "pieces", "pictures", Metrics(100, 100));
        var salvation = pages.Pages[0].Groups.Single(g => g.Group.Values[0] == "salvation");

        Assert.Equal((20, 25, 5), (salvation.Width, salvation.Height, salvation.HeadingHeight));
        Assert.Equal((salvation.X, salvation.Y + 5), At(salvation, "salvation11"));
        Assert.Equal((salvation.X + 10, salvation.Y + 5), At(salvation, "salvation12"));
        Assert.Equal((salvation.X, salvation.Y + 15), At(salvation, "salvation21"));
    }

    [Fact]
    public void Groups_stand_side_by_side_while_they_fit_the_width_then_on_the_next_line()
    {
        // ambush 30 + gap 2 + salvation 20 = 52 > 50: salvation goes under ambush (15 high), notice beside it.
        var page = Pages(Tapestries, "pieces", "pictures", Metrics(50, 100)).Pages.Single();

        Assert.Equal(
            [("ambush", 0, 0), ("salvation", 0, 17), ("notice", 22, 17)],
            page.Groups.Select(g => (g.Group.Values[0], g.X, g.Y)).ToArray());
    }

    [Fact]
    public void A_group_that_does_not_fit_what_is_left_of_a_page_starts_the_next_one_whole()
    {
        // Height 35: ambush (15) on the first line; salvation (25) would end at 17 + 25 = 42. Two of its lines would fit (17 + 5 +
        // 10 = 32), but a group no larger than a page is never cut.
        var pages = Pages(Tapestries, "pieces", "pictures", Metrics(50, 35));

        Assert.Equal([["ambush"], ["salvation", "notice"]], GroupsByPage(pages));
        Assert.Equal(25, pages.Height);
    }

    [Fact]
    public void A_narrow_group_with_a_heading_is_as_wide_as_its_heading_needs()
    {
        var notice = Pages(Tapestries, "pieces", "pictures", Metrics(100, 100, minGroupWidth: 25)).Pages[0].Groups
            .Single(g => g.Group.Values[0] == "notice");

        Assert.Equal(25, notice.Width);
    }

    // --- Crystals, made up: grade × ore × rock, some points missing; ores have names, rocks an icon, grades neither ---

    private static readonly string[] Grades = ["poor", "rich"], Ores = ["copper", "tin", "iron"], Rocks = ["granite", "basalt", "slate"];

    private static readonly CollectionDefinition CrystalsDefinition =
        new("crystals",
            [
                new DimensionDef("grade", Grades),
                new DimensionDef("ore", Ores, NameKey: "game:ore-{value}"),
                new DimensionDef("rock", Rocks, NameKey: "game:rock-{value}", Icon: "game:rock-{value}"),
            ],
            [
                new LevelDef("richest", ["ore"], new Dictionary<string, Drop> { ["grade"] = Drop.Max, ["rock"] = Drop.Any },
                    [new LayoutDef("order", [], new Arrangement.InOrder())]),
                new LevelDef("all", ["grade", "ore", "rock"], new Dictionary<string, Drop>(),
                [
                    new LayoutDef("by-ore", ["ore"], new Arrangement.Table("grade", "rock")),
                    new LayoutDef("by-rock", ["rock"], new Arrangement.Table("grade", "ore")),
                ]),
            ],
            [], []);

    private static readonly SourcePoint[] CrystalPoints = (
        from g in Grades
        from o in Ores
        from r in Rocks
        // Tin never in slate; iron only in basalt and only poor.
        where !(o == "tin" && r == "slate") && !(o == "iron" && (r != "basalt" || g != "poor"))
        let code = $"crystal-{g}-{o}-{r}"
        select new SourcePoint(code, V(("grade", g), ("ore", o), ("rock", r)), new ItemKey(CollectibleKind.Item, "game:" + code, ""), code))
        .ToArray();

    private static readonly DefinedCollection Crystals = Built(CrystalsDefinition, CrystalPoints);

    [Fact]
    public void A_table_with_its_rows_reversed_has_the_highest_value_on_top_and_its_members_in_that_order()
    {
        // The crystals' grades, richest on top (L2b): the layout reverses the rows, the dimension keeps its order for max.
        var definition = CrystalsDefinition with
        {
            Levels = CrystalsDefinition.Levels
                .Select(l => l with { Layouts = l.Layouts.Select(y => y.Arrange is Arrangement.Table t ? y with { Arrange = t with { ReverseRows = true } } : y).ToList() })
                .ToList(),
        };
        var collection = Built(definition, CrystalPoints);
        var tin = Pages(collection, "all", "by-ore", Metrics(200, 200)).Pages[0].Groups.Single(g => g.Group.Values[0] == "tin");

        Assert.Equal([("rich", 15), ("poor", 25)],
            tin.Headings.Where(h => h.Dimension.Code == "grade").Select(h => (h.Value, h.Y - tin.Y)).OrderBy(h => h.Item2).ToArray());
        Assert.Equal((tin.X + 40, tin.Y + 15), At(tin, "rich-tin-basalt"));
        // The group's members, in the order taking new follows: rich before poor, by rock within each.
        Assert.Equal(["rich-tin-granite", "rich-tin-basalt", "poor-tin-granite", "poor-tin-basalt"], tin.Group.Members.Select(m => m.Id));
        // The richest of each ore is still the rich crystal: the dimension's order is unchanged.
        Assert.Equal("rich", collection.Level("richest")!.Find("copper")!.Values["grade"]);
    }

    [Fact]
    public void A_dimension_with_icons_only_by_its_table_is_headed_by_icons_a_cell_large()
    {
        // The ores given icons by a table alone, no template (L2b): by rock, the ore columns are a cell high, not a line of words.
        var definition = CrystalsDefinition with
        {
            Dimensions = CrystalsDefinition.Dimensions
                .Select(d => d.Code == "ore" ? d with { Icons = new Dictionary<string, string> { ["copper"] = "game:nugget-nativecopper" } } : d)
                .ToList(),
        };
        var basalt = Pages(Built(definition, CrystalPoints), "all", "by-rock", Metrics(200, 200)).Pages[0].Groups.Single(g => g.Group.Values[0] == "basalt");

        Assert.All(basalt.Headings.Where(h => h.Dimension.Code == "ore"), h => Assert.Equal((5, 10), (h.Y - basalt.Y, h.Height)));
        Assert.Equal(5 + 10, basalt.Headings.Where(h => h.Dimension.Code == "grade").Min(h => h.Y) - basalt.Y);
    }

    [Fact]
    public void A_table_has_only_the_values_that_occur_in_its_group_and_no_cell_where_no_member_is()
    {
        var tin = Pages(Crystals, "all", "by-ore", Metrics(200, 200)).Pages[0].Groups.Single(g => g.Group.Values[0] == "tin");

        // Rows by grade in words (30 wide), columns by rock as icons (a cell high): granite and basalt, no slate.
        Assert.Equal(
            [("grade", "poor", 0, 15), ("grade", "rich", 0, 25), ("rock", "granite", 30, 5), ("rock", "basalt", 40, 5)],
            tin.Headings.Select(h => (h.Dimension.Code, h.Value, h.X - tin.X, h.Y - tin.Y)).OrderBy(h => h.Item1).ThenBy(h => h.Item4).ThenBy(h => h.Item3).ToArray());
        Assert.Equal((50, 35), (tin.Width, tin.Height));
        Assert.Equal(4, tin.Cells.Count);
        Assert.Equal((tin.X + 40, tin.Y + 25), At(tin, "rich-tin-basalt"));
    }

    [Fact]
    public void A_point_of_the_table_with_no_member_is_left_empty()
    {
        // By rock: basalt has copper, tin and iron, but iron only poor.
        var basalt = Pages(Crystals, "all", "by-rock", Metrics(200, 200)).Pages[0].Groups.Single(g => g.Group.Values[0] == "basalt");

        Assert.Equal(5, basalt.Cells.Count);
        Assert.DoesNotContain(basalt.Cells, c => c.Member.Id == "rich-iron-basalt");
        // Columns by ore in words: a heading line high, not a cell.
        Assert.Equal(5 + 5, basalt.Cells.Min(c => c.Y) - basalt.Y);
    }

    [Fact]
    public void A_table_wider_than_the_page_widens_the_page()
    {
        var pages = Pages(Crystals, "all", "by-ore", Metrics(40, 200));

        Assert.Equal(60, pages.Width); // 30 for the grades, three rocks of copper.
    }

    [Fact]
    public void A_group_larger_than_a_page_goes_on_over_the_next_pages_by_lines_repeating_its_column_headings()
    {
        // Copper: heading 5 + rocks 10 + two grade lines of 10 = 35; a page of 25 holds the heading, the rocks and one line.
        var pages = Pages(Crystals, "all", "by-ore", Metrics(200, 25));
        var copper = pages.Pages.Take(2).Select(p => p.Groups.Single(g => g.Group.Values[0] == "copper")).ToArray();

        Assert.Equal([false, true], copper.Select(g => g.Continued).ToArray());
        Assert.All(copper, part => Assert.Equal(3, part.Headings.Count(h => h.Dimension.Code == "rock")));
        Assert.Equal(["poor-copper-granite", "poor-copper-basalt", "poor-copper-slate"], copper[0].Cells.Select(c => c.Member.Id).ToArray());
        Assert.Equal((0, 15), At(copper[1], "rich-copper-granite") switch { var (x, y) => (x - 30, y) });
        Assert.Equal(copper[0].Group, copper[1].Group);
    }

    [Fact]
    public void Members_in_order_wrap_to_the_page_and_a_layout_without_groups_has_no_heading()
    {
        var group = Pages(Crystals, "richest", "order", Metrics(25, 100)).Pages.Single().Groups.Single();

        Assert.Equal(0, group.HeadingHeight);
        Assert.Equal([(0, 0), (10, 0), (0, 10)], group.Cells.Select(c => (c.X, c.Y)).ToArray());
        Assert.Equal((20, 20), (group.Width, group.Height));
    }

    /// <summary>Members in order only, <paramref name="count"/> of them (crystals' `richest` has 16, butterflies 169).</summary>
    private static DefinedCollection Numbers(int count)
    {
        string[] values = Enumerable.Range(1, count).Select(i => "n" + i).ToArray();
        return Built(
            new CollectionDefinition("numbers", [new DimensionDef("n", values)],
                [new LevelDef("all", ["n"], new Dictionary<string, Drop>(), [new LayoutDef("order", [], new Arrangement.InOrder())])],
                [], []),
            values.Select(v => new SourcePoint(v, V(("n", v)), new ItemKey(CollectibleKind.Item, "game:" + v, ""), v)));
    }

    [Theory]
    [InlineData(16, 120, new[] { 8, 8 })] // `richest` (L6): 12 + 4 would leave the second line a third full
    [InlineData(25, 120, new[] { 9, 9, 7 })] // a grid: only the last line is shorter
    [InlineData(24, 120, new[] { 12, 12 })]
    [InlineData(5, 120, new[] { 5 })]
    public void Members_in_order_spread_evenly_over_the_lines_the_width_needs(int count, int width, int[] perLine)
    {
        var pages = Pages(Numbers(count), "all", "order", Metrics(width, 1000));
        var group = pages.Pages.Single().Groups.Single();

        Assert.Equal(perLine, group.Cells.GroupBy(c => c.Y).Select(l => l.Count()).ToArray());
        Assert.Equal(perLine[0] * 10, group.Width);
        Assert.Equal((width, perLine[0] * 10), (pages.Width, pages.UsedWidth));
    }

    [Fact]
    public void The_used_width_is_the_widest_page_s_and_never_more_than_the_groups_take()
    {
        // Ambush 30 + 2 + salvation 20 + 2 + notice 10 = 64 on a page of 100.
        var pages = Pages(Tapestries, "pieces", "pictures", Metrics(100, 100));

        Assert.Equal((100, 64), (pages.Width, pages.UsedWidth));
    }

    // --- Tabs ---

    [Fact]
    public void No_tabs_when_everything_fits_one_page_nor_for_a_grouping_without_names()
    {
        Assert.Empty(Pages(Crystals, "all", "by-ore", Metrics(200, 500)).Tabs);
        Assert.Empty(Pages(Tapestries, "pieces", "pictures", Metrics(50, 30)).Tabs);
    }

    [Fact]
    public void A_tab_has_pages_of_its_own_though_its_groups_would_share_a_page()
    {
        // Copper 60 wide, tin 50, iron 40: all together copper and tin would stand side by side (35 high), iron on a second page.
        var pages = Pages(Crystals, "all", "by-ore", Metrics(130, 60));

        Assert.Equal("ore", pages.TabDimension!.Code);
        Assert.Equal([["copper"], ["tin"], ["iron"]], GroupsByPage(pages));
        Assert.Equal([("copper", 0, 1), ("tin", 1, 1), ("iron", 2, 1)], pages.Tabs.Select(t => (t.Value, t.FirstPage, t.PageCount)).ToArray());
        Assert.Equal(1, pages.TabAt(1));
    }

    [Fact]
    public void Inside_tabs_a_group_s_heading_leaves_the_tab_s_dimension_to_the_tab_when_it_has_more()
    {
        // By ore, then grade (the butterflies' family, then species, L4a): the tab names the ore, the frame the grade alone.
        var definition = CrystalsDefinition with
        {
            Levels = CrystalsDefinition.Levels
                .Select(l => l.Id != "all" ? l : l with { Layouts = [.. l.Layouts, new LayoutDef("by-ore-grade", ["ore", "grade"], new Arrangement.InOrder())] })
                .ToList(),
        };
        var collection = Built(definition, CrystalPoints);

        Assert.Equal(1, Pages(collection, "all", "by-ore-grade", Metrics(40, 30)).HeadingFrom);
        // No tabs (all on one page): the heading names both. One grouping dimension: the frame keeps it, though the tab says it too.
        Assert.Equal(0, Pages(collection, "all", "by-ore-grade", Metrics(200, 500)).HeadingFrom);
        Assert.Equal(0, Pages(collection, "all", "by-ore", Metrics(130, 60)).HeadingFrom);
    }

    [Fact]
    public void The_page_buttons_turn_the_pages_of_the_tab_and_without_tabs_all_pages()
    {
        // A page of 25: copper (35 high) and tin (35) take two pages each, iron (25) one.
        var pages = Pages(Crystals, "all", "by-ore", Metrics(200, 25));

        Assert.Equal([["copper"], ["copper+"], ["tin"], ["tin+"], ["iron"]], GroupsByPage(pages));
        Assert.Equal([(0, 2), (0, 2), (2, 2), (2, 2), (4, 1)], Enumerable.Range(0, 5).Select(pages.PagerAt).ToArray());

        var untabbed = Pages(Tapestries, "pieces", "pictures", Metrics(30, 15));
        Assert.Empty(untabbed.Tabs);
        Assert.Null(untabbed.TabAt(1));
        Assert.Equal((0, untabbed.Pages.Count), untabbed.PagerAt(1));
    }

    [Fact]
    public void A_tab_is_complete_when_all_its_members_are_collected()
    {
        var iron = Pages(Crystals, "all", "by-ore", Metrics(130, 60)).Tabs.Single(t => t.Value == "iron");

        Assert.False(iron.IsComplete(ImmutableHashSet<string>.Empty));
        Assert.True(iron.IsComplete(ImmutableHashSet.Create("poor-iron-basalt")));
        Assert.Equal(1, iron.Groups[0].CollectedCount(ImmutableHashSet.Create("poor-iron-basalt", "poor-tin-basalt")));
    }

    // --- Search and turning to a member ---

    [Fact]
    public void Search_turns_to_the_next_page_with_a_match_going_round_and_ending_with_the_page_itself()
    {
        var pages = Pages(Crystals, "all", "by-ore", Metrics(130, 60));
        bool Tin(Member m) => m.Values["ore"] == "tin";

        Assert.Equal(1, pages.NextPageWith(Tin, 0));
        Assert.Equal(1, pages.NextPageWith(Tin, 2));
        Assert.Equal(1, pages.NextPageWith(Tin, 1));
        Assert.Equal(2, pages.NextPageWith(m => m.Values["ore"] != "tin", 0));
        Assert.Null(pages.NextPageWith(_ => false, 0));
        Assert.Equal(2, pages.PageOf("poor-iron-basalt"));
        Assert.Null(pages.PageOf("rich-iron-basalt"));
    }

    // --- What a cell shows ---

    [Fact]
    public void A_collected_cell_shows_the_stack_put_in_and_a_missing_one_the_members_stacks()
    {
        var level = Crystals.Level("richest")!;
        var copper = level.Find("copper")!;
        var album = Album.New(Crystals, 5, "").ChooseLevel(Crystals, "richest") switch
        {
            LevelChoice.Chosen c => c.Album,
            _ => throw new InvalidOperationException(),
        };

        Assert.Equal(["crystal-rich-copper-granite", "crystal-rich-copper-basalt", "crystal-rich-copper-slate"], album.StacksToShow(copper));
        var collected = album.Collect([(copper.Id, new FilledBy(copper.Fillers[1], "the basalt one put in"))]);
        Assert.Equal(["the basalt one put in"], collected.StacksToShow(copper));
    }

    // --- The mouse over a page (A3b: the element finds what is under the mouse here) ---

    [Fact]
    public void A_point_finds_the_cell_it_is_in_and_cells_stand_edge_to_edge()
    {
        // Ambush at (0, 0): its heading 5 high, then three cells 10 wide in a row.
        var page = Pages(Tapestries, "pieces", "pictures", Metrics(50, 100)).Pages.Single();

        Assert.Equal("ambush1", page.CellAt(0, 5, 10)?.Member.Id);
        Assert.Equal("ambush1", page.CellAt(9, 14, 10)?.Member.Id);
        Assert.Equal("ambush2", page.CellAt(10, 5, 10)?.Member.Id);
        Assert.Null(page.CellAt(5, 4, 10)); // the heading
        Assert.Null(page.CellAt(5, 15, 10)); // the gap under the picture
    }

    [Fact]
    public void A_point_of_a_table_with_no_member_is_background_and_a_heading_is_found_by_its_box()
    {
        // Iron: one row (poor) and one column (basalt). By rock, basalt's table has copper, tin and iron as columns, and only
        // poor iron; rich iron is background.
        var basalt = Pages(Crystals, "all", "by-rock", Metrics(200, 200)).Pages.Single(p => p.Groups.Any(g => g.Group.Values[0] == "basalt"));
        var box = basalt.Groups.Single(g => g.Group.Values[0] == "basalt");
        var richTin = box.Cells.Single(c => c.Member.Id.Contains("rich") && c.Member.Id.Contains("tin"));
        int ironX = box.Headings.Single(h => h.Value == "iron").X;

        Assert.Null(basalt.CellAt(ironX + 1, richTin.Y + 1, 10));
        Assert.Equal("iron", basalt.HeadingAt(ironX + 1, box.Y + 5)?.Value);
        Assert.Equal("rich", basalt.HeadingAt(box.X + 1, richTin.Y + 1)?.Value);
    }
}
