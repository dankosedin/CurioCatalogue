using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CurioCatalogue.Gui;

/// <summary>
/// A dropdown whose choices have icons (album polish plan, L2c): the album window's level, tabs and layout. Closed, it shows the
/// chosen icon and name, or the icon alone when the row is short of room (the name then on hover, <see cref="AlbumControls"/>);
/// open, a line of icon and name for each choice, under the box or over the page where there is room (<see cref="DropDownList"/>),
/// scrolled by the wheel and the keys when long (17 tabs of crystals).
/// Ours, not vanilla's <c>AddDropDown</c>: that one composes its VTML names into a static texture (vsapi
/// GuiElementListMenu.cs:237), where an <c>&lt;itemstack&gt;</c> is not drawn. It behaves as vanilla's does (GuiElementDropDown.cs,
/// GuiElementListMenu.cs): a click opens and closes it, focus lost closes it, the wheel over the closed box with focus steps the
/// choice, Up, Down, Home, End and Enter as there; Escape reaches only the dialog, which closes the list first
/// (<see cref="AlbumDialog.OnEscapePressed"/>). An icon is a stack (drawn every frame) or an SVG's location (a texture).
/// </summary>
public sealed class IconDropDown : GuiElementTextBase
{
    /// <summary>A choice: its code, its name and its icon (an <see cref="ItemStack"/>, an SVG's <see cref="AssetLocation"/>, or none).</summary>
    public sealed record Option(string Code, string Name, object? Icon);

    /// <summary>A line of the open list, as vanilla's (GuiElementListMenu.cs:26).</summary>
    public const double Line = 30;

    private const double IconSize = 22, Pad = 5, ArrowWidth = 20, ScrollWidth = 4;
    private const int MaxLines = 12;
    // The open list over everything in the window (vanilla's list at 310, GuiElementListMenu.cs:353); the page's items stand at 90.
    private const float ItemZ = 90, ListZ = 310, ListIconZ = 360;

    private readonly Option[] options;
    private readonly ItemSlot?[] slots;
    private readonly LoadedTexture?[] marks;
    private readonly bool hasIcons, iconOnly;
    private readonly SelectionChangedDelegate onChosen;

    private int selected, hovered, first;
    private bool open;
    private DropDownList.Placement placement;
    private ElementBounds? shownLines;
    private LoadedTexture value, focus, list, hoverLine;

    public IconDropDown(ICoreClientAPI capi, IReadOnlyList<Option> options, int selected, bool iconOnly,
        SelectionChangedDelegate onChosen, ElementBounds bounds, CairoFont font) : base(capi, "", font, bounds)
    {
        this.options = options.ToArray();
        this.selected = hovered = Math.Clamp(selected, 0, Math.Max(0, this.options.Length - 1));
        this.iconOnly = iconOnly;
        this.onChosen = onChosen;
        hasIcons = this.options.Any(o => o.Icon != null);
        slots = this.options.Select(o => o.Icon is ItemStack stack ? new DummySlot(stack) : null).ToArray<ItemSlot?>();
        marks = new LoadedTexture?[this.options.Length];
        value = new LoadedTexture(capi);
        focus = new LoadedTexture(capi);
        list = new LoadedTexture(capi);
        hoverLine = new LoadedTexture(capi);
    }

    /// <summary>After the page (draw order 0), so that the open list lies over it.</summary>
    public override double DrawOrder => 1;

    public override bool Focusable => enabled;

    /// <summary>Whether the open list covers a point of the screen (the page then shows no tooltip there).</summary>
    public bool Covers(int x, int y) => open && shownLines != null && shownLines.PointInside(x, y);

    /// <summary>The box's width, unscaled: the icon and the longest name, or the icon alone, and the arrow.</summary>
    public static double Width(CairoFont font, IReadOnlyList<Option> options, bool iconOnly)
    {
        double icon = options.Any(o => o.Icon != null) ? IconSize + Pad : 0;
        if (iconOnly) return Pad + icon + ArrowWidth + Pad;
        double name = options.Select(o => font.GetTextExtents(o.Name).Width / scaled(1)).DefaultIfEmpty(0).Max();
        return Pad + icon + name + 3 * Pad + ArrowWidth;
    }

    /// <summary>Whether every choice has an icon: only then the closed box may show the icon alone.</summary>
    public static bool AllHaveIcons(IReadOnlyList<Option> options) => options.Count > 0 && options.All(o => o.Icon != null);

    // --- Composing ---

    public override void ComposeElements(Context ctx, ImageSurface surface)
    {
        Bounds.CalcWorldBounds();
        ctx.SetSourceRGBA(0, 0, 0, 0.2);
        RoundRectangle(ctx, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, 3);
        ctx.Fill();
        EmbossRoundRectangleElement(ctx, Bounds, true, 1, 1);

        // The arrow: a raised button on the right with a triangle pointing down.
        double arrow = scaled(ArrowWidth), ax = Bounds.drawX + Bounds.InnerWidth - arrow, height = Bounds.InnerHeight;
        ctx.SetSourceRGBA(GuiStyle.DialogHighlightColor);
        RoundRectangle(ctx, ax, Bounds.drawY, arrow, height, 1);
        ctx.Fill();
        EmbossRoundRectangleElement(ctx, ax, Bounds.drawY, arrow, height, false, 2, 1);
        double half = Math.Min(height - scaled(8), scaled(12)) / 2, cx = ax + arrow / 2, cy = Bounds.drawY + height / 2;
        ctx.NewPath();
        ctx.MoveTo(cx - half, cy - half / 2);
        ctx.LineTo(cx + half, cy - half / 2);
        ctx.LineTo(cx, cy + half / 2);
        ctx.ClosePath();
        ctx.SetSourceRGBA(1, 1, 1, 0.6);
        ctx.Fill();

        for (int i = 0; i < options.Length; i++)
        {
            marks[i]?.Dispose();
            marks[i] = options[i].Icon is AssetLocation svg ? SvgIcons.Texture(api, svg, (int)scaled(IconSize)) : null;
        }
        ComposeFocus();
        ComposeValue();
    }

    /// <summary>The light over the box while it has focus, as vanilla's highlight.</summary>
    private void ComposeFocus()
    {
        int w = Math.Max(1, (int)(Bounds.InnerWidth - scaled(ArrowWidth))), h = Math.Max(1, (int)Bounds.InnerHeight);
        using var surface = new ImageSurface(Format.Argb32, w, h);
        using var ctx = genContext(surface);
        ctx.SetSourceRGBA(1, 1, 1, 0.12);
        ctx.Paint();
        generateTexture(surface, ref focus);
    }

    /// <summary>The chosen name, after the room of the icon; the icon itself is drawn on every frame.</summary>
    private void ComposeValue()
    {
        int w = Math.Max(1, (int)(Bounds.InnerWidth - scaled(ArrowWidth))), h = Math.Max(1, (int)Bounds.InnerHeight);
        using var surface = new ImageSurface(Format.Argb32, w, h);
        using var ctx = genContext(surface);
        if (!iconOnly && options.Length > 0)
        {
            Font.SetupContext(ctx);
            DrawTextLineAt(ctx, options[selected].Name, NameX(), (h - Font.GetFontExtents().Height) / 2);
        }
        generateTexture(surface, ref value);
    }

    /// <summary>Where a name starts in the box and in a line, scaled.</summary>
    private double NameX() => scaled(Pad) + (hasIcons ? scaled(IconSize + Pad) : 0);

    /// <summary>The open list: its lines' background and names in one texture, scrolled under a scissor; and the hovered line.</summary>
    private void ComposeList()
    {
        double line = scaled(Line);
        double names = options.Select(o => Font.GetTextExtents(o.Name).Width).DefaultIfEmpty(0).Max();
        int w = (int)Math.Max(Bounds.InnerWidth, NameX() + names + scaled(2 * Pad + ScrollWidth));
        int h = Math.Max(1, (int)(options.Length * line));
        using (var surface = new ImageSurface(Format.Argb32, w, h))
        using (var ctx = genContext(surface))
        {
            ctx.SetSourceRGBA(GuiStyle.DialogStrongBgColor);
            RoundRectangle(ctx, 0, 0, w, h, 1);
            ctx.FillPreserve();
            ctx.SetSourceRGBA(0, 0, 0, 0.5);
            ctx.LineWidth = 2;
            ctx.Stroke();
            Font.SetupContext(ctx);
            double offset = (line - Font.GetFontExtents().Height) / 2;
            for (int i = 0; i < options.Length; i++) DrawTextLineAt(ctx, options[i].Name, NameX(), i * line + offset);
            generateTexture(surface, ref list);
        }
        using (var surface = new ImageSurface(Format.Argb32, w, Math.Max(1, (int)line)))
        using (var ctx = genContext(surface))
        {
            double[] colour = (double[])GuiStyle.DialogHighlightColor.Clone();
            colour[3] = 0.5;
            ctx.SetSourceRGBA(colour);
            ctx.Paint();
            generateTexture(surface, ref hoverLine);
        }

        // In the screen's units, unscaled, as the bounds count.
        double top = Bounds.absY / scaled(1), bottom = (api.Render.FrameHeight - Bounds.absY - Bounds.OuterHeight) / scaled(1);
        placement = DropDownList.Place(top, bottom, Line, options.Length, MaxLines);
        shownLines = Bounds.FlatCopy();
        shownLines.fixedWidth = w / scaled(1);
        shownLines.fixedHeight = placement.Lines * Line;
        shownLines.fixedY = placement.Above ? Bounds.fixedY - shownLines.fixedHeight : Bounds.fixedY + Bounds.fixedHeight;
        shownLines.CalcWorldBounds();
        first = DropDownList.Show(0, placement.Lines, options.Length, selected);
    }

    // --- Drawing ---

    public override void RenderInteractiveElements(float deltaTime)
    {
        double x = Bounds.renderX, y = Bounds.renderY;
        if (HasFocus || open) api.Render.Render2DTexturePremultipliedAlpha(focus.TextureId, Snap(x), Snap(y), focus.Width, focus.Height);
        api.Render.Render2DTexturePremultipliedAlpha(value.TextureId, Snap(x), Snap(y), value.Width, value.Height);
        if (options.Length > 0) RenderIcon(selected, x + scaled(Pad), y + (Bounds.InnerHeight - scaled(IconSize)) / 2, ItemZ, ItemZ + 1, deltaTime);
        if (open && shownLines != null) RenderList(deltaTime);
    }

    private void RenderList(float deltaTime)
    {
        double line = scaled(Line), x = shownLines!.renderX, top = shownLines.renderY;
        api.Render.PushScissor(shownLines);
        api.Render.Render2DTexturePremultipliedAlpha(list.TextureId, Snap(x), Snap(top - first * line), list.Width, list.Height, ListZ);
        if (hovered >= first && hovered < first + placement.Lines)
        {
            api.Render.Render2DTexturePremultipliedAlpha(hoverLine.TextureId, Snap(x), Snap(top + (hovered - first) * line),
                hoverLine.Width, hoverLine.Height, ListZ + 1);
        }
        for (int i = first; i < Math.Min(options.Length, first + placement.Lines); i++)
            RenderIcon(i, x + scaled(Pad), top + (i - first) * line + (line - scaled(IconSize)) / 2, ListIconZ, ListZ + 2, deltaTime);
        if (options.Length > placement.Lines)
        {
            // Where the lines shown are in the list: a thin bar at the right edge.
            double room = placement.Lines * line, thumb = Math.Max(scaled(8), room * placement.Lines / options.Length);
            double at = (room - thumb) * first / Math.Max(1, options.Length - placement.Lines);
            api.Render.Render2DTexturePremultipliedAlpha(hoverLine.TextureId, Snap(x + list.Width - scaled(ScrollWidth + 2)), Snap(top + at),
                scaled(ScrollWidth), thumb, ListZ + 3);
        }
        api.Render.PopScissor();
    }

    /// <summary>A choice's icon in a square of IconSize whose top left is (x, y): a stack, or an SVG's texture.</summary>
    private void RenderIcon(int index, double x, double y, float itemZ, float markZ, float deltaTime)
    {
        double size = scaled(IconSize);
        if (slots[index] is { } slot)
        {
            api.Render.RenderItemstackToGui(slot, x + size / 2, y + size / 2, itemZ, (float)size, ColorUtil.WhiteArgb, deltaTime,
                showStackSize: false);
        }
        else if (marks[index] is { } mark)
        {
            api.Render.Render2DTexturePremultipliedAlpha(mark.TextureId, Snap(x + (size - mark.Width) / 2), Snap(y + (size - mark.Height) / 2),
                mark.Width, mark.Height, markZ);
        }
    }

    /// <summary>Whole pixels: text textures are sampled without smoothing (docs/modding.md, GUI, "Text textures need whole pixels").</summary>
    private static float Snap(double position) => (float)Math.Round(position);

    // --- Opening, choosing ---

    private void Open()
    {
        ComposeList();
        hovered = selected;
        open = true;
        api.Gui.PlaySound("menubutton");
    }

    /// <summary>Closes the open list; false when it was not open.</summary>
    public bool Close()
    {
        if (!open) return false;
        open = false;
        return true;
    }

    /// <summary>Takes a choice and tells the window, which composes again (so nothing of this element is touched after it).</summary>
    private void Choose(int index, bool sound)
    {
        open = false;
        if (index < 0 || index >= options.Length) return;
        selected = hovered = index;
        ComposeValue();
        if (sound) api.Gui.PlaySound("toggleswitch");
        onChosen(options[index].Code, true);
    }

    private bool InBox(int x, int y) => Bounds.PointInside(x, y);

    public override bool IsPositionInside(int posX, int posY) => InBox(posX, posY) || Covers(posX, posY);

    // --- The mouse and the keys, as vanilla's dropdown ---

    public override void OnMouseDown(ICoreClientAPI api, MouseEvent args)
    {
        if (!enabled) return;
        if (open)
        {
            if (Covers(args.X, args.Y))
            {
                args.Handled = true;
                int? index = DropDownList.LineAt((args.Y - shownLines!.absY) / scaled(1), Line, first, placement.Lines, options.Length);
                if (index is { } i) Choose(i, sound: true);
                return;
            }
            Close();
            if (InBox(args.X, args.Y))
            {
                args.Handled = true;
                api.Gui.PlaySound("menubutton");
            }
            return;
        }
        if (InBox(args.X, args.Y) && options.Length > 0)
        {
            args.Handled = true;
            Open();
        }
    }

    /// <summary>Lights the line under the mouse. The move is not taken (<c>Handled</c>), so that the page under the list still
    /// sees it and drops its tooltip (<see cref="AlbumPageElement.Covered"/>).</summary>
    public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
    {
        if (!Covers(args.X, args.Y)) return;
        if (DropDownList.LineAt((args.Y - shownLines!.absY) / scaled(1), Line, first, placement.Lines, options.Length) is { } i) hovered = i;
    }

    public override void OnMouseWheel(ICoreClientAPI api, MouseWheelEventArgs args)
    {
        if (!enabled || options.Length == 0) return;
        int step = args.delta > 0 ? -1 : 1;
        if (open && Covers(api.Input.MouseX, api.Input.MouseY))
        {
            first = DropDownList.Clamp(first + step, placement.Lines, options.Length);
            args.SetHandled(true);
            return;
        }
        // As vanilla's (GuiElementDropDown.cs:375): over the closed box with focus, the wheel steps the choice.
        if (!open && HasFocus && InBox(api.Input.MouseX, api.Input.MouseY))
        {
            args.SetHandled(true);
            Choose(GameMath.Mod(selected + step, options.Length), sound: false);
        }
    }

    public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
    {
        if (!HasFocus || options.Length == 0) return;
        if (open && args.KeyCode is (int)GlKeys.Enter or (int)GlKeys.KeypadEnter)
        {
            args.Handled = true;
            Choose(hovered, sound: true);
            return;
        }
        int at = open ? hovered : selected;
        int? to = args.KeyCode switch
        {
            (int)GlKeys.Up => GameMath.Mod(at - 1, options.Length),
            (int)GlKeys.Down => GameMath.Mod(at + 1, options.Length),
            (int)GlKeys.Home => 0,
            (int)GlKeys.End => options.Length - 1,
            _ => null,
        };
        if (to is not { } index) return;
        args.Handled = true;
        if (!open)
        {
            Choose(index, sound: false);
            return;
        }
        hovered = index;
        first = DropDownList.Show(first, placement.Lines, options.Length, index);
    }

    public override void OnFocusLost()
    {
        base.OnFocusLost();
        Close();
    }

    public override void Dispose()
    {
        base.Dispose();
        value.Dispose();
        focus.Dispose();
        list.Dispose();
        hoverLine.Dispose();
        foreach (var mark in marks) mark?.Dispose();
    }
}
