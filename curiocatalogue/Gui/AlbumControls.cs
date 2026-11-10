using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CurioCatalogue.Gui;

/// <summary>
/// The album window's rows of controls under the page (album view plan, A2, A4; L9): the dropdowns (level, tabs, layout), search,
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

    /// <summary>A dropdown: the codes, the names shown, and the chosen index.</summary>
    public sealed record Choice(string[] Codes, string[] Names, int Selected);

    /// <summary>What the row holds now. A null choice is a dropdown the window does not have.</summary>
    /// <param name="Search">The search input is shown (there are pages to search).</param>
    /// <param name="WantList">"Make a want list" is shown (none without a level, nor for a switched-off collection).</param>
    /// <param name="CanStartOver">Something is collected; without it the place of ↺ stays empty, so that the want list does not jump.</param>
    public sealed record Content(Choice? Level, Choice? Tab, Choice? Layout, bool Search, bool WantList, bool CanStartOver);

    /// <summary>The row: what to add, and its width, before the page's width is known. Left and Right are the room its left and
    /// right parts take, each with a gap towards the middle.</summary>
    public sealed record Row(double Width, double Left, double Right, Action<GuiComposer, ElementBounds, double, double> Add);

    private readonly CairoFont font = CairoFont.WhiteDetailText();
    private readonly SelectionChangedDelegate onLevel, onTab, onLayout;
    private readonly Action<string> onSearch;
    private readonly ActionConsumable onWantList;
    private readonly Action onStartOver;

    private static string T(string key, params object[] args) => Lang.Get("curiocatalogue:" + key, args);

    public AlbumControls(ICoreClientAPI capi, SelectionChangedDelegate onLevel, SelectionChangedDelegate onTab, SelectionChangedDelegate onLayout,
        Action<string> onSearch, ActionConsumable onWantList, Action onStartOver)
    {
        (this.onLevel, this.onTab, this.onLayout) = (onLevel, onTab, onLayout);
        (this.onSearch, this.onWantList, this.onStartOver) = (onSearch, onWantList, onStartOver);
        // The game's undo arrow is only a part of a circle, its redo the other way round: ours is a whole ↺ (A4).
        if (!capi.Gui.Icons.CustomIcons.ContainsKey(RestartIcon))
            capi.Gui.Icons.CustomIcons[RestartIcon] = capi.Gui.Icons.SvgIconSource(new AssetLocation("curiocatalogue", "textures/icons/restart.svg"));
    }

    /// <summary>A name's width in a dropdown: vanilla's AddDropDown draws it in WhiteSmallText (vsapi GuiElementDropDown.cs:516),
    /// larger than the window's font; measured with that, a long level name ran under the arrow (L6, seen in game 10.11.2026).</summary>
    private static double DropDownWidth(string text) => DropDownFont.GetTextExtents(text).Width / GuiElement.scaled(1);

    private static readonly CairoFont DropDownFont = CairoFont.WhiteSmallText();

    private static double DropDownRoom(Choice choice) => choice.Names.Select(DropDownWidth).Max() + 40;

    public Row Build(Content content)
    {
        var parts = new List<(double Width, bool Right, Action<GuiComposer, ElementBounds> Add)>();
        void Part(double w, bool right, Action<GuiComposer, ElementBounds> add) => parts.Add((w, right, add));
        void Dropdown(Choice choice, SelectionChangedDelegate chosen, string key) =>
            Part(DropDownRoom(choice), false, (c, b) => c.AddDropDown(choice.Codes, choice.Names, choice.Selected, chosen, b, key));

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
