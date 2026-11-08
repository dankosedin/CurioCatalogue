using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using CurioCatalogue.Core;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace CurioCatalogue.Gui;

/// <summary>What the window knows of a page beyond its layout (album view, A2, "The cell"): names and lines.</summary>
/// <param name="GroupName">A group's heading.</param>
/// <param name="GroupHint">The text on hover over a group's name when the name alone does not tell it from another
/// ("Schematic (schematic-a)"); null when it does.</param>
/// <param name="Freedom">What a member of several items leaves free ("any rock"); null for a member of one item.</param>
/// <param name="Rarity">The colour of a member's corner (ARGB) and its tooltip line; null for none.</param>
public sealed record AlbumPageLook(
    System.Func<MemberGroup, string> GroupName,
    System.Func<MemberGroup, string?> GroupHint,
    System.Func<Member, string?> Freedom,
    System.Func<Member, (int Argb, string Line)?> Rarity);

/// <summary>
/// One page of the album window, drawn by us (album view, A2, "The cell"): stacks in slots, missing ones translucent; a
/// piece with a flat image (<see cref="Member.FlatTexture"/>) as that image, edge to edge, so the pieces make the picture;
/// group headings with their progress and a check mark; table headings as stacks or words; the rarity corner; a missing
/// member of several items cycling through them. Hover shows the game's tooltip through an <see cref="AlbumSlot"/>.
/// <see cref="Show"/> changes what is collected without composing again (A1: composing costs per grid; here only the
/// headings whose count changed are redrawn).
/// </summary>
public sealed class AlbumPageElement : GuiElement
{
    public const float MissingAlpha = 0.35f;

    // The game's slot (vsapi GuiElementPassiveItemSlot.cs, unscaledSlotSize, unscaledItemSize); a cell is a slot and its padding.
    private static readonly double SlotSize = GuiElementPassiveItemSlot.unscaledSlotSize;
    private static readonly double ItemSize = GuiElementPassiveItemSlot.unscaledItemSize;
    private const double Corner = 12;
    private const float ItemZ = 90, MarkZ = 250; // as the slot grid draws items and its cross (GuiElementItemSlotGridBase.cs:498, 512)

    // A cell the search does not match is drawn dark (A2, "What stays of today's window": the matches lit, the rest dimmed).
    private const float Dim = 0.3f;
    private static readonly Vec4f Opaque = new(1, 1, 1, 1), Translucent = new(1, 1, 1, MissingAlpha);
    private static readonly Vec4f OpaqueDim = new(Dim, Dim, Dim, 1), TranslucentDim = new(Dim, Dim, Dim, MissingAlpha);
    private static readonly int OpaqueArgb = ColorUtil.WhiteArgb, TranslucentArgb = Argb(1, MissingAlpha);
    private static readonly int OpaqueDimArgb = Argb(Dim, 1), TranslucentDimArgb = Argb(Dim, MissingAlpha);

    private static int Argb(float grey, float alpha) =>
        ColorUtil.ToRgba((int)(255 * alpha), (int)(255 * grey), (int)(255 * grey), (int)(255 * grey));

    private sealed class Cell(PlacedMember placed, AlbumSlot slot)
    {
        public readonly PlacedMember Placed = placed;
        public readonly AlbumSlot Slot = slot;
        public IReadOnlyList<ItemStack> Stacks = [];
        public Slideshow Show = Slideshow.Start;
        public bool Collected;
        public bool Lit = true;
        public int FlatTexture;
    }

    private sealed class Heading(PlacedHeading placed, AlbumSlot? icon, string name, string fitted)
    {
        public readonly PlacedHeading Placed = placed;
        public readonly AlbumSlot? Icon = icon;
        public readonly string Name = name;
        /// <summary>The name in words, cut with "…" to the heading's width.</summary>
        public readonly string Fitted = fitted;
        public LoadedTexture? Text;
    }

    private sealed class Group(GroupBox box, string name, string fitted, string? hint)
    {
        public readonly GroupBox Box = box;
        public readonly string FullName = name;
        public readonly string Fitted = fitted;
        /// <summary>The text on hover: the hint, else the full name when it is cut; null for none.</summary>
        public readonly string? Hover = hint ?? (fitted != name ? name : null);
        public int Count = -1;
        public bool Complete;
        public LoadedTexture? Name, Progress;
    }

    private readonly AlbumPage page;
    private readonly PageMetrics metrics;
    private readonly AlbumPageLook look;
    private readonly Cell[] cells;
    private readonly Heading[] headings;
    private readonly Group[] groups;
    private static readonly CairoFont GroupFont = CairoFont.WhiteSmallText(), HeadingFont = CairoFont.WhiteDetailText();
    private static readonly CairoFont ProgressFont = CairoFont.WhiteSmallText().WithColor([1, 1, 1, 0.6]);
    private const double CheckSize = 16, NameGap = 8;

    /// <summary>The room around the groups (unscaled): their frames reach this far beyond the group's own box, so the element is
    /// twice this larger than the page (A4). None when no group has a heading, so no frame: the cells then start at the
    /// window's left edge, as the rows of controls do (A4, one left edge).</summary>
    public static int PadOf(AlbumPages pages) => pages.Pages.Any(p => p.Groups.Any(g => g.HeadingHeight > 0)) ? FramePad : 0;

    private const int FramePad = 6, TextPad = 2;
    private readonly int pad;
    private static readonly TextBackground Padded = new() { HorPadding = TextPad, VerPadding = TextPad };

    private IReadOnlySet<string> collected = new HashSet<string>();
    private System.Func<Member, bool>? matches;
    private AlbumSlot? hovered;
    private bool composed;
    private int slotTexture, cornerTexture;
    private LoadedTexture? check;

    /// <param name="pages">All pages of the level, so that every page is as large as the largest (the window keeps its size).</param>
    public AlbumPageElement(ICoreClientAPI capi, ElementBounds bounds, AlbumPages pages, int pageIndex, PageMetrics metrics,
        AlbumPageLook look) : base(capi, bounds)
    {
        page = pages.Pages[pageIndex];
        this.metrics = metrics;
        this.look = look;
        pad = PadOf(pages);
        bounds.fixedWidth = pages.Width + 2 * pad;
        bounds.fixedHeight = pages.Height + 2 * pad;

        var placedCells = page.Groups.SelectMany(g => g.Cells).ToList();
        var placedHeadings = page.Groups.SelectMany(g => g.Headings).ToList();
        // The slots exist on the client only and are never part of a player's inventory (as GridDialog's view).
        var inventory = new InventoryGeneric(placedCells.Count + placedHeadings.Count, "curiocatalogue-albumpage",
            "albumpage-" + capi.World.Player.PlayerUID, capi, (_, inv) => new AlbumSlot(inv));
        int slot = 0;
        cells = placedCells.Select(c => new Cell(c, (AlbumSlot)inventory[slot++])).ToArray();
        headings = placedHeadings.Select(h =>
        {
            var s = (AlbumSlot)inventory[slot++];
            string name = h.Dimension.ValueNameKey(h.Value) is { } key ? Lang.Get(key) : h.Value;
            if (IconOf(h) is not { } icon) return new Heading(h, null, name, Fit(name, HeadingFont, h.Width));
            s.Itemstack = icon;
            s.HeadingName = name;
            return new Heading(h, s, name, name);
        }).ToArray();
        groups = page.Groups.Select(g =>
        {
            string continued = g.Continued ? " " + Lang.Get("curiocatalogue:albumwindow-continued") : "";
            string name = look.GroupName(g.Group) + continued;
            string? hint = look.GroupHint(g.Group) is { } h ? h + continued : null;
            return new Group(g, name, g.HeadingHeight > 0 ? Fit(name, GroupFont, NameRoom(g)) : name, hint);
        }).ToArray();
    }

    /// <summary>The width a group's name has beside its progress and check mark. Measured with every member collected
    /// ("34/34"), the widest the progress gets, so that the name is cut once and does not move as pieces come.</summary>
    private static double NameRoom(GroupBox box)
    {
        int total = box.Group.Members.Count;
        return box.Width - ProgressFont.GetTextExtents($"{total}/{total}").Width / scaled(1) - CheckSize - 2 - NameGap - 4;
    }

    /// <summary>
    /// The texts cut with "…" (a group's name, a heading in words) and the group names with a hint, with their full text and
    /// where they stand on the page (unscaled, from its top left): the window puts a hover text over each (A3b, "Words that do
    /// not fit").
    /// </summary>
    public IEnumerable<(double X, double Y, double Width, double Height, string Text)> CutTexts =>
        groups.Where(g => g.Box.HeadingHeight > 0 && g.Hover != null)
            .Select(g => ((double)g.Box.X + pad, (double)g.Box.Y + pad, NameRoom(g.Box), (double)g.Box.HeadingHeight, g.Hover!))
            .Concat(headings.Where(h => h.Icon == null && h.Fitted != h.Name)
                .Select(h => ((double)h.Placed.X + pad, (double)h.Placed.Y + pad, (double)h.Placed.Width, (double)h.Placed.Height, h.Name)));

    /// <summary>Lights the cells whose member matches the search and dims the rest; null lights all. Kept over <see cref="Show"/>.</summary>
    public void Highlight(System.Func<Member, bool>? matching)
    {
        matches = matching;
        foreach (var cell in cells) cell.Lit = matching?.Invoke(cell.Placed.Member) ?? true;
    }

    private ItemStack? IconOf(PlacedHeading heading)
    {
        if (heading.Dimension.ValueIcon(heading.Value) is not { } code) return null;
        var location = new AssetLocation(code);
        if (api.World.GetItem(location) is { } item) return new ItemStack(item);
        return api.World.GetBlock(location) is { } block ? new ItemStack(block) : null;
    }

    /// <summary>What the album has collected: flips the cells, their stacks and the headings' progress; no composing.</summary>
    public void Show(Album album)
    {
        collected = album.Collected.Keys.ToHashSet();
        foreach (var cell in cells)
        {
            var member = cell.Placed.Member;
            cell.Collected = collected.Contains(member.Id);
            // A stack of a removed mod cannot be drawn or named; its cell stays empty.
            var stacks = album.StacksToShow(member).OfType<ItemStack>().Where(s => s.Collectible != null).ToList();
            if (!stacks.SequenceEqual(cell.Stacks)) cell.Show = Slideshow.Start;
            cell.Stacks = stacks;
            cell.Slot.Itemstack = stacks.Count > 0 ? stacks[cell.Show.Index % stacks.Count] : null;
            cell.Slot.Lines = Lines(member, cell.Collected);
        }
        Highlight(matches); // the stack put in may match where the member did not
        if (composed) ComposeGroupHeadings();
    }

    private string Lines(Member member, bool isCollected)
    {
        var lines = new List<string>
        {
            isCollected
                ? $"<font color=\"#84ff84\">{Lang.Get("curiocatalogue:albumwindow-collected")}</font>"
                : $"<font color=\"#ff8484\">{Lang.Get("curiocatalogue:albumwindow-missing")}</font>",
        };
        if (!isCollected && look.Freedom(member) is { } free) lines.Add(free);
        if (look.Rarity(member) is { } rarity) lines.Add(rarity.Line);
        return string.Join("\n", lines);
    }

    // --- Composing: textures made once per page (slot, corner, check, words), the headings' progress again on a change ---

    public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
    {
        Bounds.CalcWorldBounds();
        slotTexture = GuiElementItemSlotGridBase.DrawSlotBackground(api, scaled(SlotSize), scaled(SlotSize),
            GuiStyle.DialogSlotBackColor, GuiStyle.DialogSlotFrontColor, null);
        cornerTexture = CornerTexture();
        DrawFrames(ctxStatic);
        check = api.Gui.LoadSvg(new AssetLocation("game:textures/icons/checkmark.svg"), (int)scaled(CheckSize), (int)scaled(CheckSize),
            (int)scaled(CheckSize), (int)scaled(CheckSize), ColorUtil.ToRgba(255, 132, 255, 132));
        foreach (var cell in cells)
        {
            if (cell.Placed.Member.Attributes.TryGetValue(Member.FlatTexture, out var texture))
                cell.FlatTexture = api.Render.GetOrLoadTexture(new AssetLocation(texture)); // cached by the game, not ours to free
        }
        foreach (var heading in headings.Where(h => h.Icon == null))
            heading.Text = api.Gui.TextTexture.GenTextTexture(heading.Fitted, HeadingFont, Padded);
        composed = true;
        ComposeGroupHeadings();
    }

    /// <summary>A frame round each group, so that one picture is told from the next (A4): a soft dark fill and a thin edge.</summary>
    private void DrawFrames(Context ctx)
    {
        foreach (var group in groups.Where(g => g.Box.HeadingHeight > 0))
        {
            var box = group.Box;
            double x = Bounds.drawX + scaled(pad + box.X - FramePad), y = Bounds.drawY + scaled(pad + box.Y - FramePad);
            double w = scaled(box.Width + 2 * FramePad), h = scaled(box.Height + 2 * FramePad);
            GuiElement.RoundRectangle(ctx, x, y, w, h, scaled(4));
            ctx.SetSourceRGBA(0, 0, 0, 0.14);
            ctx.FillPreserve();
            ctx.SetSourceRGBA(0, 0, 0, 0.35);
            ctx.LineWidth = scaled(1);
            ctx.Stroke();
        }
    }

    /// <summary>A white triangle in the top right corner; drawn tinted, its colour is the tier's.</summary>
    private int CornerTexture()
    {
        int size = (int)scaled(Corner);
        using var surface = new ImageSurface(Format.Argb32, size, size);
        using var ctx = GenContext(surface);
        ctx.SetSourceRGBA(1, 1, 1, 1);
        ctx.MoveTo(0, 0);
        ctx.LineTo(size, 0);
        ctx.LineTo(size, size);
        ctx.ClosePath();
        ctx.Fill();
        int id = 0;
        generateTexture(surface, ref id);
        return id;
    }

    private void ComposeGroupHeadings()
    {
        foreach (var group in groups.Where(g => g.Box.HeadingHeight > 0))
        {
            var members = group.Box.Group;
            int count = members.CollectedCount(collected);
            if (count == group.Count) continue;
            group.Count = count;
            group.Complete = count == members.Members.Count;
            group.Progress?.Dispose();
            group.Progress = api.Gui.TextTexture.GenTextTexture($"{count}/{members.Members.Count}", ProgressFont, Padded);
            if (group.Name != null) continue;
            group.Name = api.Gui.TextTexture.GenTextTexture(group.Fitted, GroupFont, Padded);
        }
    }

    /// <summary>The text, cut with "…" to fit the unscaled width.</summary>
    internal static string Fit(string text, CairoFont font, double width)
    {
        double max = scaled(width);
        if (font.GetTextExtents(text).Width <= max) return text;
        for (int n = text.Length - 1; n > 0; n--)
        {
            string cut = text[..n].TrimEnd() + "…";
            if (font.GetTextExtents(cut).Width <= max) return cut;
        }
        return "…";
    }

    // --- Drawing, every frame ---

    public override void RenderInteractiveElements(float deltaTime)
    {
        // Whole pixels for the page's corner, so that the flat images of the pieces keep their pixels too (see Snap).
        double x0 = Snap(Bounds.renderX + scaled(pad)), y0 = Snap(Bounds.renderY + scaled(pad));
        foreach (var group in groups) RenderGroupHeading(group, x0, y0);
        foreach (var heading in headings) RenderTableHeading(heading, x0, y0, deltaTime);
        foreach (var cell in cells) RenderCell(cell, x0, y0, deltaTime);
    }

    private void RenderGroupHeading(Group group, double x0, double y0)
    {
        if (group.Name == null || group.Progress == null) return;
        var box = group.Box;
        double top = y0 + scaled(box.Y);
        double middle = top + scaled(box.HeadingHeight) / 2;
        // The count stands right after the name, dimmer: at the group's right edge it read as the next group's (A4). The textures
        // have TextPad of room around the words, so that no letter is cut.
        double pad = scaled(TextPad), nameX = x0 + scaled(box.X), wordsEnd = nameX + group.Name.Width - 2 * pad;
        double progressX = wordsEnd + scaled(NameGap);
        api.Render.Render2DLoadedTexture(group.Name, Snap(nameX - pad), Snap(middle - group.Name.Height / 2.0));
        api.Render.Render2DLoadedTexture(group.Progress, Snap(progressX - pad), Snap(middle - group.Progress.Height / 2.0));
        if (group.Complete && check != null)
        {
            api.Render.Render2DLoadedTexture(check, Snap(progressX + group.Progress.Width - 2 * pad + scaled(2)),
                Snap(middle - check.Height / 2.0));
        }
    }

    private void RenderTableHeading(Heading heading, double x0, double y0, float deltaTime)
    {
        var h = heading.Placed;
        double x = x0 + scaled(h.X), y = y0 + scaled(h.Y);
        if (heading.Icon != null)
        {
            api.Render.RenderItemstackToGui(heading.Icon, x + scaled(SlotSize) / 2, y + scaled(SlotSize) / 2, ItemZ,
                (float)scaled(ItemSize), ColorUtil.WhiteArgb, deltaTime, showStackSize: false);
        }
        else if (heading.Text is { } text)
        {
            // A column's word at the bottom of its line, centred; a row's at the left, in the middle of its cell.
            bool column = h.Width <= metrics.Cell && h.Height < metrics.Cell;
            double tx = (column ? x + (scaled(h.Width) - text.Width) / 2 : x) - scaled(TextPad);
            double ty = y + (scaled(h.Height) - text.Height) / 2; // the texture is TextPad larger all round, centred as it is
            api.Render.Render2DLoadedTexture(text, Snap(tx), Snap(ty));
        }
    }

    /// <summary>
    /// A position on the screen in whole pixels. Text textures are sampled without smoothing (TextTextureUtil.cs:85, linearMag
    /// false), so one drawn half a pixel off loses rows and columns of its letters: an "i" without its dot (A4, checked in game
    /// 08.11.2026, when the window's height made it stand half a pixel off).
    /// </summary>
    private static float Snap(double position) => (float)Math.Round(position);

    private void RenderCell(Cell cell, double x0, double y0, float deltaTime)
    {
        double x = x0 + scaled(cell.Placed.X), y = y0 + scaled(cell.Placed.Y);
        if (!cell.Collected && cell.Stacks.Count > 1)
        {
            var next = cell.Show.Tick(deltaTime, cell.Stacks.Count, hovered == cell.Slot);
            if (next.Index != cell.Show.Index) cell.Slot.Itemstack = cell.Stacks[next.Index];
            cell.Show = next;
        }

        if (cell.FlatTexture != 0)
        {
            // Edge to edge over the whole cell, padding included, so the pieces make the picture.
            api.Render.Render2DTexture(cell.FlatTexture, (float)x, (float)y, (float)scaled(metrics.Cell), (float)scaled(metrics.Cell),
                ItemZ, (cell.Collected, cell.Lit) switch
                {
                    (true, true) => Opaque,
                    (true, false) => OpaqueDim,
                    (false, true) => Translucent,
                    (false, false) => TranslucentDim,
                });
        }
        else
        {
            api.Render.Render2DTexturePremultipliedAlpha(slotTexture, x, y, scaled(SlotSize), scaled(SlotSize));
            if (cell.Slot.Itemstack != null)
            {
                api.Render.RenderItemstackToGui(cell.Slot, x + scaled(SlotSize) / 2, y + scaled(SlotSize) / 2, ItemZ,
                    (float)scaled(ItemSize), (cell.Collected, cell.Lit) switch
                    {
                        (true, true) => OpaqueArgb,
                        (true, false) => OpaqueDimArgb,
                        (false, true) => TranslucentArgb,
                        (false, false) => TranslucentDimArgb,
                    }, deltaTime, showStackSize: false);
            }
        }

        if (look.Rarity(cell.Placed.Member) is { } rarity)
        {
            double size = scaled(Corner), right = x + (cell.FlatTexture != 0 ? scaled(metrics.Cell) : scaled(SlotSize));
            api.Render.Render2DTexture(cornerTexture, (float)(right - size), (float)y, (float)size, (float)size, MarkZ, ColorOf(rarity.Argb));
        }
    }

    private static Vec4f ColorOf(int argb) =>
        new(((argb >> 16) & 0xff) / 255f, ((argb >> 8) & 0xff) / 255f, (argb & 0xff) / 255f, ((argb >> 24) & 0xff) / 255f);

    // --- The mouse: the game's tooltip for the cell or icon heading under it ---

    public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
    {
        AlbumSlot? now = null;
        if (Bounds.PointInside(args.X, args.Y))
        {
            int x = (int)((args.X - Bounds.renderX) / scaled(1)) - pad, y = (int)((args.Y - Bounds.renderY) / scaled(1)) - pad;
            if (page.CellAt(x, y, metrics.Cell) is { } placed) now = cells.First(c => c.Placed == placed).Slot;
            else if (page.HeadingAt(x, y) is { } heading) now = headings.First(h => h.Placed == heading).Icon;
        }
        if (now?.Itemstack == null) now = null;
        if (now == hovered) return;
        if (hovered != null) api.Input.TriggerOnMouseLeaveSlot(hovered);
        if (now != null) api.Input.TriggerOnMouseEnterSlot(now);
        hovered = now;
    }

    /// <summary>The window calls it on closing, as vanilla does for its grids (GuiElementItemSlotGridBase.cs#OnGuiClosed): the
    /// tooltip goes away with the window.</summary>
    public void OnGuiClosed()
    {
        if (hovered != null) api.Input.TriggerOnMouseLeaveSlot(hovered);
        hovered = null;
    }

    public override void Dispose()
    {
        base.Dispose();
        if (slotTexture != 0) api.Render.GLDeleteTexture(slotTexture);
        if (cornerTexture != 0) api.Render.GLDeleteTexture(cornerTexture);
        check?.Dispose();
        foreach (var heading in headings) heading.Text?.Dispose();
        foreach (var group in groups)
        {
            group.Name?.Dispose();
            group.Progress?.Dispose();
        }
    }
}

public static class AlbumPageComposerExtensions
{
    /// <summary>Adds a page of the album (<see cref="AlbumPageElement"/>); its size is the largest page's.</summary>
    public static GuiComposer AddAlbumPage(this GuiComposer composer, ElementBounds bounds, AlbumPages pages, int pageIndex,
        PageMetrics metrics, AlbumPageLook look, string key, out AlbumPageElement page)
    {
        page = new AlbumPageElement(composer.Api, bounds, pages, pageIndex, metrics, look);
        composer.AddInteractiveElement(page, key);
        return composer;
    }
}
