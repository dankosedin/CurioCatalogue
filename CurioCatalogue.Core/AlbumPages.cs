using System;
using System.Collections.Generic;
using System.Linq;
using ExhaustiveMatching;

namespace CurioCatalogue.Core;

// The album window's pages as data (docs/plans/2026-11-04-album-view.md, A2, "The view on paper"): which groups go on which
// page, where each cell and heading stands, the tabs and the pages each turns. Built when a page's size, the level or the layout changes; what is
// collected is not part of it, so taking a piece while the window is open changes no position (A2: a refresh flips flags
// without composing again). Positions are in the caller's units (the GUI's unscaled pixels); the core never draws.

/// <summary>The sizes a page is laid out by, all in the same units.</summary>
/// <param name="Width">The page's width. A table wider than this widens the page (<see cref="AlbumPages.Width"/>); members in
/// order wrap to it.</param>
/// <param name="Height">The page's height: groups go on the next page when they do not fit.</param>
/// <param name="Cell">A cell: a slot with its padding; also a heading drawn as a stack.</param>
/// <param name="Heading">A line of text: a group's heading, a table's column heading in words.</param>
/// <param name="Gap">Between groups, side by side and one under another.</param>
/// <param name="TextHeadingWidth">A table's row heading in words (a crystal's grade); the caller measures the widest name.</param>
/// <param name="MinGroupWidth">The narrowest a group with a heading is, so that its name and progress fit over one piece.</param>
public sealed record PageMetrics(int Width, int Height, int Cell, int Heading, int Gap, int TextHeadingWidth, int MinGroupWidth);

/// <summary>A member's cell on a page, at its top left corner.</summary>
public sealed record PlacedMember(Member Member, int X, int Y);

/// <summary>A table's heading: a value of its row or column dimension, drawn as the value's icon (<see cref="DimensionDef.Icon"/>)
/// or its name.</summary>
public sealed record PlacedHeading(DimensionDef Dimension, string Value, int X, int Y, int Width, int Height);

/// <summary>
/// A group on a page, or a part of it when the group is larger than a page.
/// </summary>
/// <param name="Group">The whole group: its heading shows its progress, not the part's.</param>
/// <param name="Continued">A part after the first; its heading says so.</param>
/// <param name="HeadingHeight">The line of the group's name above its cells; 0 when the layout does not group.</param>
public sealed record GroupBox(
    MemberGroup Group,
    bool Continued,
    int X,
    int Y,
    int Width,
    int Height,
    int HeadingHeight,
    IReadOnlyList<PlacedMember> Cells,
    IReadOnlyList<PlacedHeading> Headings);

public sealed record AlbumPage(IReadOnlyList<GroupBox> Groups)
{
    public IEnumerable<Member> Members => Groups.SelectMany(g => g.Cells).Select(c => c.Member);

    /// <summary>The member whose cell (<paramref name="cell"/> square) holds the point; null over background, a heading or a gap.</summary>
    public PlacedMember? CellAt(int x, int y, int cell) =>
        Groups.SelectMany(g => g.Cells).FirstOrDefault(c => Inside(x, y, c.X, c.Y, cell, cell));

    /// <summary>The table heading that holds the point; null elsewhere.</summary>
    public PlacedHeading? HeadingAt(int x, int y) =>
        Groups.SelectMany(g => g.Headings).FirstOrDefault(h => Inside(x, y, h.X, h.Y, h.Width, h.Height));

    // The right and bottom edges belong to the next cell: cells stand edge to edge (a tapestry picture).
    private static bool Inside(int x, int y, int left, int top, int width, int height) =>
        x >= left && x < left + width && y >= top && y < top + height;
}

/// <summary>
/// A cell cycling through a member's stacks (a member filled by any of several items, while missing): one stack a second,
/// held while the mouse is over it, as the handbook's slideshow (A2, "The cell").
/// </summary>
/// <param name="Index">The stack shown, of however many the cell has.</param>
/// <param name="Shown">Seconds it has been shown.</param>
public sealed record Slideshow(int Index, double Shown)
{
    public const double SecondsPerStack = 1;

    public static readonly Slideshow Start = new(0, 0);

    /// <summary>The slideshow <paramref name="seconds"/> later: the next stack after each full second, round to the first.</summary>
    public Slideshow Tick(double seconds, int count, bool hovered)
    {
        if (hovered || count < 2) return this;
        double shown = Shown + seconds;
        int steps = (int)(shown / SecondsPerStack);
        return new Slideshow((Index + steps) % count, shown - steps * SecondsPerStack);
    }
}

/// <summary>A tab: a value of the layout's outermost grouping dimension, its groups and its pages (one tab, one page; more when
/// its groups do not fit on one).</summary>
public sealed record AlbumTab(string Value, int FirstPage, int PageCount, IReadOnlyList<MemberGroup> Groups)
{
    /// <summary>Every member of every group collected: the tab gets its ✓.</summary>
    public bool IsComplete(IReadOnlySet<string> collectedIds) => Groups.All(g => g.IsComplete(collectedIds));
}

/// <summary>The pages of a level in a layout.</summary>
public sealed class AlbumPages
{
    public IReadOnlyList<AlbumPage> Pages { get; }

    /// <summary>Empty when the window shows no tabs: the outermost grouping dimension has no names, or everything fits on one page.</summary>
    public IReadOnlyList<AlbumTab> Tabs { get; }

    /// <summary>The page's width: the metrics' one, or wider for a table that does not fit it.</summary>
    public int Width { get; }

    /// <summary>The tallest page, at most the metrics' height unless a single row does not fit.</summary>
    public int Height { get; }

    /// <summary>The dimension the tabs show; null with no tabs.</summary>
    public DimensionDef? TabDimension { get; }

    private AlbumPages(IReadOnlyList<AlbumPage> pages, IReadOnlyList<AlbumTab> tabs, int width, DimensionDef? tabDimension)
    {
        Pages = pages;
        Tabs = tabs;
        Width = width;
        Height = pages.SelectMany(p => p.Groups).Select(g => g.Y + g.Height).DefaultIfEmpty(0).Max();
        TabDimension = tabDimension;
    }

    /// <summary>
    /// Lays out the groups of <paramref name="layout"/> on pages. A page holds whole groups in order, side by side while they fit
    /// its width, then on the next line; a group larger than a page goes on over the next pages by whole lines of cells, a table
    /// repeating its column headings. With tabs, a tab's groups start a page of their own (A4, the author: one tab, one page;
    /// several groups of different tabs on a page surprised).
    /// </summary>
    public static AlbumPages Build(DefinedCollection collection, CollectionLevel level, LayoutDef layout, PageMetrics metrics)
    {
        var groups = level.Groups(layout);
        var dims = collection.Definition.Dimensions.ToDictionary(d => d.Code);
        var boxes = groups.Select(g => Measure(collection, dims, layout, g, metrics)).ToList();
        int width = boxes.Select(b => b.Width).Append(metrics.Width).Max();

        DimensionDef? tabDim = layout.GroupBy.Count > 0 && dims[layout.GroupBy[0]].NameKey != null ? dims[layout.GroupBy[0]] : null;
        string? TabOf(MemberGroup g) => tabDim == null ? null : g.Values[0];

        // Tabs only when everything does not fit one page as it is.
        var together = Paginate(boxes, width, metrics, _ => null);
        if (tabDim == null || together.Count < 2) return new AlbumPages(together, [], width, null);

        var pages = Paginate(boxes, width, metrics, TabOf);
        var tabs = groups
            .GroupBy(g => TabOf(g)!)
            .Select(t =>
            {
                bool Has(AlbumPage p) => p.Groups.Any(b => TabOf(b.Group) == t.Key);
                return new AlbumTab(t.Key, pages.FindIndex(Has), pages.Count(Has), t.ToList());
            })
            .ToList();
        return new AlbumPages(pages, tabs, width, tabDim);
    }

    /// <summary>The tab a page belongs to (its index); null with no tabs.</summary>
    public int? TabAt(int page)
    {
        int i = Tabs.ToList().FindIndex(t => page >= t.FirstPage && page < t.FirstPage + t.PageCount);
        return i < 0 ? null : i;
    }

    /// <summary>The pages the page buttons turn from <paramref name="page"/>: its tab's (A4, the author: ◀ ▶ turn the pages of
    /// the tab, "N / M" counts them), or all with no tabs.</summary>
    public (int First, int Count) PagerAt(int page) =>
        TabAt(page) is { } tab ? (Tabs[tab].FirstPage, Tabs[tab].PageCount) : (0, Pages.Count);

    /// <summary>The page with this member; null when no page has it.</summary>
    public int? PageOf(string memberId)
    {
        int i = Pages.ToList().FindIndex(p => p.Members.Any(m => m.Id == memberId));
        return i < 0 ? null : i;
    }

    /// <summary>The first page after <paramref name="page"/> with a member that matches, going round to the start and ending with
    /// <paramref name="page"/> itself (Enter in the search box); null when no page has one.</summary>
    public int? NextPageWith(Func<Member, bool> matches, int page)
    {
        for (int step = 1; step <= Pages.Count; step++)
        {
            int i = (page + step) % Pages.Count;
            if (Pages[i].Members.Any(matches)) return i;
        }
        return null;
    }

    // --- Measuring a group: its heading, its table headings, and its cells as lines, so that a group can be cut between lines ---

    /// <summary>A group laid out at (0, 0) before it is placed: a top part (its heading and a table's column headings) and lines
    /// of cells under it.</summary>
    private sealed record Measured(MemberGroup Group, int Width, int HeadingHeight, int Top, IReadOnlyList<PlacedHeading> TopHeadings,
        IReadOnlyList<Line> Lines)
    {
        public int Height => Top + Lines.Sum(l => l.Height);
    }

    /// <summary>A line of cells and the row heading beside it, at Y relative to the line's top.</summary>
    private sealed record Line(int Height, IReadOnlyList<PlacedMember> Cells, IReadOnlyList<PlacedHeading> Headings);

    private static Measured Measure(DefinedCollection collection, IReadOnlyDictionary<string, DimensionDef> dims, LayoutDef layout,
        MemberGroup group, PageMetrics m)
    {
        int heading = layout.GroupBy.Count > 0 ? m.Heading : 0;
        int minWidth = heading > 0 ? m.MinGroupWidth : 0;
        var members = group.Members;
        switch (layout.Arrange)
        {
            case Arrangement.InOrder _:
            {
                int perLine = Math.Max(1, m.Width / m.Cell);
                var lines = members.Chunk(perLine)
                    .Select(chunk => new Line(m.Cell, chunk.Select((mem, i) => new PlacedMember(mem, i * m.Cell, 0)).ToList(), []))
                    .ToList();
                int width = Math.Min(members.Count, perLine) * m.Cell;
                return new Measured(group, Math.Max(width, minWidth), heading, heading, [], lines);
            }
            case Arrangement.Shape _:
            {
                // Coordinates are 1-based (a tapestry piece's digits); a point of the shape with no member stays background.
                int X(Member mem) => int.Parse(mem.Attributes[Arrangement.Shape.X], System.Globalization.CultureInfo.InvariantCulture);
                int Y(Member mem) => int.Parse(mem.Attributes[Arrangement.Shape.Y], System.Globalization.CultureInfo.InvariantCulture);
                int cols = members.Max(X), rows = members.Max(Y);
                var lines = Enumerable.Range(1, rows)
                    .Select(y => new Line(m.Cell, members.Where(mem => Y(mem) == y)
                        .Select(mem => new PlacedMember(mem, (X(mem) - 1) * m.Cell, 0)).ToList(), []))
                    .ToList();
                return new Measured(group, Math.Max(cols * m.Cell, minWidth), heading, heading, [], lines);
            }
            case Arrangement.Table t:
            {
                DimensionDef rowDim = dims[t.Rows], colDim = dims[t.Columns];
                // Only the values that occur in the group, in the dimension's order.
                var rowValues = collection.Values[t.Rows].Where(v => members.Any(mem => mem.Values[t.Rows] == v)).ToList();
                var colValues = collection.Values[t.Columns].Where(v => members.Any(mem => mem.Values[t.Columns] == v)).ToList();
                int rowHeadWidth = rowDim.Icon != null ? m.Cell : m.TextHeadingWidth;
                int colHeadHeight = colDim.Icon != null ? m.Cell : m.Heading;
                int top = heading + colHeadHeight;
                var colHeadings = colValues
                    .Select((v, i) => new PlacedHeading(colDim, v, rowHeadWidth + i * m.Cell, heading, m.Cell, colHeadHeight))
                    .ToList();
                var lines = rowValues
                    .Select(rv => new Line(m.Cell,
                        members.Where(mem => mem.Values[t.Rows] == rv)
                            .Select(mem => new PlacedMember(mem, rowHeadWidth + colValues.IndexOf(mem.Values[t.Columns]) * m.Cell, 0))
                            .ToList(),
                        [new PlacedHeading(rowDim, rv, 0, 0, rowHeadWidth, m.Cell)]))
                    .ToList();
                return new Measured(group, Math.Max(rowHeadWidth + colValues.Count * m.Cell, minWidth), heading, top, colHeadings, lines);
            }
            default:
                throw ExhaustiveMatch.Failed(layout.Arrange);
        }
    }

    // --- Pages: groups side by side in rows ("shelves"), a shelf under another, a new page when the next does not fit ---

    private static List<AlbumPage> Paginate(IReadOnlyList<Measured> boxes, int width, PageMetrics m, Func<MemberGroup, string?> pageKey)
    {
        var pages = new List<AlbumPage>();
        var page = new List<GroupBox>();
        int shelfY = 0, shelfX = 0, shelfH = 0;
        string? key = null;

        void NewPage()
        {
            if (page.Count > 0) pages.Add(new AlbumPage(page));
            page = [];
            shelfY = shelfX = shelfH = 0;
        }

        void NewShelf()
        {
            if (shelfX == 0) return;
            shelfY += shelfH + m.Gap;
            shelfX = shelfH = 0;
        }

        void Put(GroupBox box)
        {
            page.Add(box);
            shelfX = box.X + box.Width;
            shelfH = Math.Max(shelfH, box.Height);
        }

        for (int i = 0; i < boxes.Count; i++)
        {
            var box = boxes[i];
            var k = pageKey(box.Group);
            if (i > 0 && k != key) NewPage();
            key = k;

            if (shelfX > 0 && shelfX + m.Gap + box.Width > width) NewShelf();
            int x = shelfX == 0 ? 0 : shelfX + m.Gap;

            if (shelfY + box.Height <= m.Height)
            {
                Put(Place(box, false, x, shelfY, 0, box.Lines.Count));
                continue;
            }
            if (box.Height <= m.Height || box.Lines.Count < 2)
            {
                // It fits a page of its own (or cannot be cut): never split, it starts the next page.
                if (page.Count > 0) NewPage();
                Put(Place(box, false, 0, 0, 0, box.Lines.Count));
                continue;
            }

            // Larger than a page: cut between lines, the first part where it would stand if there is room for a line.
            NewShelf();
            int from = 0;
            while (from < box.Lines.Count)
            {
                int room = m.Height - shelfY - box.Top;
                int to = from, used = 0;
                while (to < box.Lines.Count && used + box.Lines[to].Height <= room) used += box.Lines[to++].Height;
                if (to == from)
                {
                    if (page.Count > 0) { NewPage(); continue; }
                    to = from + 1; // A single line taller than a page: shown, cut by the window.
                }
                Put(Place(box, from > 0, 0, shelfY, from, to));
                from = to;
                if (from < box.Lines.Count) NewPage();
            }
        }
        NewPage();
        return pages;
    }

    /// <summary>The lines [from, to) of a group at (x, y), with its heading and column headings.</summary>
    private static GroupBox Place(Measured box, bool continued, int x, int y, int from, int to)
    {
        var cells = new List<PlacedMember>();
        var headings = box.TopHeadings.Select(h => h with { X = x + h.X, Y = y + h.Y }).ToList();
        int lineY = y + box.Top;
        for (int i = from; i < to; i++)
        {
            var line = box.Lines[i];
            cells.AddRange(line.Cells.Select(c => c with { X = x + c.X, Y = lineY + c.Y }));
            headings.AddRange(line.Headings.Select(h => h with { X = x + h.X, Y = lineY + h.Y }));
            lineY += line.Height;
        }
        return new GroupBox(box.Group, continued, x, y, box.Width, lineY - y, box.HeadingHeight, cells, headings);
    }
}
