using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using CurioCatalogue.Adapters;

namespace CurioCatalogue.Gui;

/// <summary>
/// The album window's rows of controls under the page (album view plan, A2, A4; L9): the dropdowns (level, tabs, layout; ours,
/// with icons, <see cref="IconDropDown"/>, L2c), search,
/// "Make a want list", "Start over" and the page buttons. <see cref="AlbumDialog"/> says which of them the window has now and what
/// each does; this class measures them (<see cref="Row.Width"/>, before the page's width is known) and puts them in the window's
/// width (<see cref="Row.Add"/>): the left parts from the left edge, the right parts from the right one, the page buttons between.
/// </summary>
internal sealed class AlbumControls
{
    /// <summary>The height of a row of controls.</summary>
    public const double Line = 30;

    /// <summary>The page buttons ◀ N / M ▶.</summary>
    public const double PagerWidth = 140;

    private const string RestartIcon = "curiocatalogue-restart";
    private const double Gap = 8, SearchWidth = 140;

    /// <summary>A dropdown: the codes, the names shown, the icons' codes (the definition's, <see cref="DefinitionIcons"/>; null:
    /// none), and the chosen index. <paramref name="IconOnly"/>: closed, the icon alone whatever the room (when every choice has
    /// one), its name on hover and in the open list.</summary>
    public sealed record Choice(string[] Codes, string[] Names, string?[] Icons, int Selected, bool IconOnly = false);

    /// <summary>What the row holds now. A null choice is a dropdown the window does not have.</summary>
    /// <param name="Search">The search input is shown (there are pages to search).</param>
    /// <param name="WantList">"Make a want list" is shown (none without a level, nor for a switched-off collection).</param>
    /// <param name="CanStartOver">Something is collected; without it the place of ↺ stays empty, so that the want list does not jump.</param>
    public sealed record Content(Choice? Level, Choice? Tab, Choice? Layout, bool Search, bool WantList, bool CanStartOver);

    /// <summary>The row: what to add, and its width, before the page's width is known. Left and Right are the room its left and
    /// right parts take, each with a gap towards the middle.</summary>
    public sealed record Row(double Width, double Left, double Right, Action<GuiComposer, ElementBounds, double, double> Add);

    private readonly CairoFont font = CairoFont.WhiteDetailText();
    private readonly ICoreClientAPI capi;
    private readonly SelectionChangedDelegate onLevel, onTab, onLayout;
    /// <summary>The dropdowns of the row last added: the window closes an open list on Escape, and the page shows no tooltip under it.</summary>
    private readonly List<IconDropDown> dropDowns = new();
    private readonly Action<string> onSearch;
    private readonly ActionConsumable onWantList;
    private readonly Action onStartOver;

    private static string T(string key, params object[] args) => Lang.Get("curiocatalogue:" + key, args);

    public AlbumControls(ICoreClientAPI capi, SelectionChangedDelegate onLevel, SelectionChangedDelegate onTab, SelectionChangedDelegate onLayout,
        Action<string> onSearch, ActionConsumable onWantList, Action onStartOver)
    {
        this.capi = capi;
        (this.onLevel, this.onTab, this.onLayout) = (onLevel, onTab, onLayout);
        (this.onSearch, this.onWantList, this.onStartOver) = (onSearch, onWantList, onStartOver);
        // The game's undo arrow is only a part of a circle, its redo the other way round: ours is a whole ↺ (A4).
        if (!capi.Gui.Icons.CustomIcons.ContainsKey(RestartIcon))
            capi.Gui.Icons.CustomIcons[RestartIcon] = capi.Gui.Icons.SvgIconSource(new AssetLocation("curiocatalogue", "textures/icons/restart.svg"));
    }

    /// <summary>The dropdowns' font: vanilla's AddDropDown draws in WhiteSmallText (vsapi GuiElementDropDown.cs:516), and the
    /// names are measured in the font they are drawn in (L6: measured in a smaller one, a long level name ran under the arrow).</summary>
    private static readonly CairoFont DropDownFont = CairoFont.WhiteSmallText();

    /// <summary>
    /// The row for the room the page leaves (its width in the window): when the row with the dropdowns' names is wider, the
    /// dropdowns whose every choice has an icon show the icon alone, closed (L2c), if that makes the row narrower.
    /// </summary>
    public Row Build(Content content, double room)
    {
        var full = Build(content, iconOnly: false);
        if (full.Width <= room) return full;
        var compact = Build(content, iconOnly: true);
        return compact.Width < full.Width ? compact : full;
    }

    private Row Build(Content content, bool iconOnly)
    {
        var parts = new List<(double Width, bool Right, Action<GuiComposer, ElementBounds> Add)>();
        void Part(double w, bool right, Action<GuiComposer, ElementBounds> add) => parts.Add((w, right, add));
        void Dropdown(Choice choice, SelectionChangedDelegate chosen, string key)
        {
            var options = choice.Codes.Select((code, i) =>
                new IconDropDown.Option(code, choice.Names[i], DefinitionIcons.Find(capi.World, choice.Icons[i]))).ToArray();
            bool only = (iconOnly || choice.IconOnly) && IconDropDown.AllHaveIcons(options);
            Part(IconDropDown.Width(DropDownFont, options, only), false, (c, b) =>
            {
                var dropDown = new IconDropDown(capi, options, choice.Selected, only, chosen, b, DropDownFont.Clone());
                c.AddInteractiveElement(dropDown, key);
                dropDowns.Add(dropDown);
                // The icon alone: its name on hover.
                if (only) c.AddAutoSizeHoverText(choice.Names[choice.Selected], font, 300, b.FlatCopy());
            });
        }

        // The level leftmost, the tabs' dropdown right after it (A4, the author).
        if (content.Level is { } level) Dropdown(level, onLevel, "level");
        if (content.Tab is { } tab) Dropdown(tab, onTab, "tab");
        if (content.Layout is { } layout) Dropdown(layout, onLayout, "layout");
        if (content.Search) Part(SearchWidth, false, (c, b) => c.AddTextInput(b, onSearch, font, "search"));
        if (content.WantList)
        {
            // The want list: the main action, a text button (A2).
            string wantList = T("ledgerwindow-wantlist");
            Part(CairoFont.ButtonText().GetTextExtents(wantList).Width / GuiElement.scaled(1) + 24, true,
                (c, b) => c.AddSmallButton(wantList, onWantList, b, EnumButtonStyle.Normal, "wantlist"));
            // The place of ↺ stays when there is nothing to start over, so that the want list does not jump (A4).
            Part(Line, true, (c, b) =>
            {
                if (content.CanStartOver)
                    c.AddIconTextButton(RestartIcon, () => { onStartOver(); return true; }, b, "restart")
                        .AddAutoSizeHoverText(T("ledgerwindow-restart"), font, 200, b.FlatCopy());
            });
        }

        double total = parts.Sum(p => p.Width) + Gap * Math.Max(0, parts.Count - 1);
        double leftWidth = parts.Where(p => !p.Right).Sum(p => p.Width + Gap), rightWidth = parts.Where(p => p.Right).Sum(p => p.Width + Gap);
        return new Row(total, leftWidth, rightWidth, (composer, container, y, width) =>
        {
            dropDowns.Clear();
            double left = 0, right = width;
            foreach (var p in parts.Where(p => !p.Right))
            {
                var b = ElementBounds.Fixed(left, y, p.Width, Line);
                container.WithChildren(b);
                p.Add(composer, b);
                left += p.Width + Gap;
            }
            foreach (var p in parts.Where(p => p.Right).Reverse())
            {
                right -= p.Width;
                var b = ElementBounds.Fixed(right, y, p.Width, Line);
                container.WithChildren(b);
                p.Add(composer, b);
                right -= Gap;
            }
        });
    }

    /// <summary>Closes a dropdown's open list (Escape); false when none was open.</summary>
    public bool CloseList() => dropDowns.Aggregate(false, (closed, d) => d.Close() | closed);

    /// <summary>Whether an open dropdown's list covers a point of the screen.</summary>
    public bool Covers(int x, int y) => dropDowns.Any(d => d.Covers(x, y));

    /// <summary>◀ N / M ▶, PagerWidth wide, centred on <paramref name="centre"/>; <paramref name="position"/> is the page's place
    /// among the <paramref name="count"/> pages of its tab.</summary>
    public void AddPager(GuiComposer composer, ElementBounds container, int position, int count, Action<int> turn, double centre, double y)
    {
        const double button = 30, number = PagerWidth - 2 * button;
        double x = centre - number / 2;
        var prev = ElementBounds.Fixed(x - button, y, button, Line);
        var text = ElementBounds.Fixed(x, y + 6, number, Line);
        var next = ElementBounds.Fixed(x + number, y, button, Line);
        container.WithChildren(prev, text, next);
        composer
            .AddIconTextButton("left", () => { turn(-1); return true; }, prev, "prev")
            .AddStaticText(T("albumwindow-page", position + 1, count), font.Clone().WithOrientation(EnumTextOrientation.Center), text)
            .AddIconTextButton("right", () => { turn(1); return true; }, next, "next");
    }
}
