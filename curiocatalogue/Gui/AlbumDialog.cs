using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using CurioCatalogue.Core;

namespace CurioCatalogue.Gui;

/// <summary>
/// The album window, as a stamp album (domain.md, Windows; album view plan, A2): the collection laid out on pages
/// (<see cref="AlbumPages"/>, drawn by <see cref="AlbumPageElement"/>), tabs at the side when the grouping has names, and a row
/// of controls: the level while the album is empty, the layout, search, the page buttons, "Make a want list" and "Start over".
/// A piece taken while the window is open flips its cell without composing again; turning a page, switching the layout or the
/// level composes. An album whose collection the game does not have (switched off, or gone with its mod) shows what it holds.
/// </summary>
public sealed class AlbumDialog : HeldItemDialog
{
    /// <summary>
    /// TEMPORARY, until A4 of the album view plan: how tabs and pages go together, both built for the author to compare in game
    /// (A2, "Tabs"); switched by a button in the window, kept for the game session. A4 keeps one and removes the switch.
    /// </summary>
    private static TabPaging paging = TabPaging.Shared;

    /// <summary>The layout the player chose, by collection and level, for the game session (A2: presentation only, kept on the
    /// client; an album handed over opens in the receiver's own).</summary>
    private static readonly Dictionary<string, string> chosenLayouts = new();

    private static readonly double Cell = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
    private const int PageCells = 12;
    private const double Line = 30;

    // What the window needs besides the page: screen and title bar padding, the dialog's padding, the line over the page, the rows
    // of controls under it (A1, "How much fits": at 768 about 11 rows of cells are left).
    private const double AroundThePage = 270;

    private readonly CurioCatalogueModSystem mod;
    private readonly CairoFont font = CairoFont.WhiteDetailText();

    private Album album = null!;
    private View shown = null!;
    private string? composedFor;
    private int page;
    private string search = "";
    private readonly Dictionary<string, string> searchNames = new();
    private AlbumPageElement? pageElement;
    private AlbumTabs? tabs;

    /// <summary>What the window shows: the collection laid out, or (with no level) nothing, and the line above the page.</summary>
    private sealed record View(ItemLedger.AlbumState State, DefinedCollection? Collection, CollectionLevel? Level,
        LayoutDef? Layout, AlbumPages? Pages, PageMetrics? Metrics, string? Line);

    internal AlbumDialog(ICoreClientAPI capi, CurioCatalogueModSystem mod, ItemSlot slot, bool offHand) : base(capi, slot, offHand)
    {
        this.mod = mod;
        Refresh();
    }

    private ItemLedger Item => (ItemLedger)Slot.Itemstack!.Collectible;

    protected override bool StillShows(ItemStack? stack) => stack?.Collectible is ItemLedger { IsAlbum: true };

    private static string T(string key, params object[] args) => Lang.Get("curiocatalogue:" + key, args);

    // --- Reading the album: a change of what is collected flips cells; a change of what is laid out composes ---

    protected override void Refresh()
    {
        if (!StillShows(Slot.Itemstack)) return;
        album = (Album)mod.Store.ReadBook(Slot.Itemstack!, LedgerMode.Album);
        searchNames.Clear();
        var (state, collection, level) = Item.StateOf(album);
        // Without the collection, what it holds is all there is to lay out; it changes only by starting over.
        string key = $"{state}|{collection?.Id}|{level?.Id}|{album.IsEmpty}|{album.Name}|"
            + (state == ItemLedger.AlbumState.AtLevel ? "" : album.Collected.Count.ToString());
        if (key == composedFor && pageElement != null)
        {
            ShowCollected();
            return;
        }
        composedFor = key;
        shown = Read(state, collection, level, keepLayout: null);
        page = Math.Min(page, Math.Max(0, (shown.Pages?.Pages.Count ?? 1) - 1));
        Compose();
    }

    private View Read(ItemLedger.AlbumState state, DefinedCollection? collection, CollectionLevel? level, string? keepLayout)
    {
        string? line = state switch
        {
            ItemLedger.AlbumState.Disabled => T("ledger-info-disabled"),
            ItemLedger.AlbumState.NoCollection => T("albumwindow-unknown"),
            ItemLedger.AlbumState.NoLevel => T("albumwindow-nolevel"),
            _ => null,
        };
        if (state is ItemLedger.AlbumState.Disabled or ItemLedger.AlbumState.NoCollection)
        {
            collection = album.CollectedOnly();
            level = collection == null ? null : album.LevelIn(collection);
        }
        if (collection == null || level == null) return new View(state, collection, null, null, null, null, line);

        string layoutKey = collection.Id + "/" + level.Id;
        var layout = level.Layouts.FirstOrDefault(l => l.Id == (keepLayout ?? chosenLayouts.GetValueOrDefault(layoutKey))) ?? level.DefaultLayout;
        var metrics = Metrics(collection);
        return new View(state, collection, level, layout, AlbumPages.Build(collection, level, layout, metrics, paging), metrics, line);
    }

    private PageMetrics Metrics(DefinedCollection collection)
    {
        double screen = capi.Render.FrameHeight / RuntimeEnv.GUIScale;
        int cell = (int)Cell;
        // The widest name of a value headed by words (a crystal's grade), for a table's row headings.
        double words = collection.Definition.Dimensions.Where(d => d.Icon == null)
            .SelectMany(d => collection.Values[d.Code].Select(v => d.ValueNameKey(v) is { } k ? Lang.Get(k) : v))
            .Select(Width).DefaultIfEmpty(0).Max();
        return new PageMetrics(
            Width: PageCells * cell,
            Height: Math.Max(3 * cell, (int)(screen - AroundThePage)),
            Cell: cell,
            Heading: 24,
            Gap: 12,
            TextHeadingWidth: (int)Math.Ceiling(words) + 8,
            MinGroupWidth: 3 * cell);
    }

    /// <summary>A text's width in the window's font, unscaled.</summary>
    private double Width(string text) => font.GetTextExtents(text).Width / GuiElement.scaled(1);

    /// <summary>What is collected now, on the composed page and the tabs.</summary>
    private void ShowCollected()
    {
        pageElement?.Show(album);
        pageElement?.Highlight(Matcher());
        if (tabs != null && shown.Pages is { } pages)
        {
            var collected = album.Collected.Keys.ToHashSet();
            tabs.SetComplete(pages.Tabs.Select(t => t.IsComplete(collected)).ToList());
        }
    }

    // --- Composing ---

    private void Compose()
    {
        pageElement?.OnGuiClosed(); // the old page's tooltip goes with it
        var pages = shown.Pages;
        double y = GuiStyle.TitleBarHeight + 5;

        var content = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        content.BothSizing = ElementSizing.FitToChildren;
        var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
        string title = album.Name.Length > 0 ? album.Name : Slot.Itemstack!.GetName();

        var composer = capi.Gui
            .CreateCompo("curiocatalogue-album", dialogBounds)
            .AddShadedDialogBG(content)
            .AddDialogTitleBar(title, () => TryClose());
        tabs = null;
        if (pages is { Tabs.Count: > 0 }) AddTabs(composer, pages);
        composer.BeginChildElements(content);

        var controls = Controls();
        double width = Math.Max(pages?.Width ?? 0, controls.Width);

        if (shown.Line is { } text)
        {
            var lineBounds = ElementBounds.Fixed(0, y, width, 22);
            content.WithChildren(lineBounds);
            composer.AddStaticText(text, font, lineBounds);
            y += 26;
        }

        pageElement = null;
        if (pages != null && pages.Pages.Count > 0)
        {
            var pageBounds = ElementBounds.Fixed(0, y, pages.Width, pages.Height);
            content.WithChildren(pageBounds);
            composer.AddAlbumPage(pageBounds, pages, page, shown.Metrics!, Look(), "page", out var element);
            pageElement = element;
            // The full text of a name cut with "…" (A3b).
            foreach (var cut in element.CutTexts)
            {
                var hover = ElementBounds.Fixed(cut.X, y + cut.Y, cut.Width, cut.Height);
                content.WithChildren(hover);
                composer.AddHoverText(cut.Text, font, 300, hover);
            }
            y += pages.Height + 10;
            if (pages.Pages.Count > 1)
            {
                AddPager(composer, content, pages, width, y);
                y += Line + 8;
            }
        }

        controls.Add(composer, content, y, width);
        y += Line + 8;

        if (pages is { Tabs.Count: > 0 })
        {
            // TEMPORARY until A4: the switch between the two tab variants; English only, never translated.
            var switchBounds = ElementBounds.Fixed(0, y, 260, Line);
            content.WithChildren(switchBounds);
            composer.AddSmallButton(paging == TabPaging.Shared ? "Tabs: several groups a page" : "Tabs: one tab, one page",
                OnSwitchPaging, switchBounds, EnumButtonStyle.Small, "paging");
        }

        SetComposer(composer.EndChildElements());
        if (SingleComposer.GetTextInput("search") is { } input)
        {
            input.SetPlaceHolderText(T("albumwindow-search"));
            input.SetValue(search, setCaretPosToEnd: true);
        }
        SingleComposer.UnfocusOwnElements();
        ShowCollected();
    }

    private void AddTabs(GuiComposer composer, AlbumPages pages)
    {
        var dim = pages.TabDimension!;
        var lit = pages.TabsOn(page);
        var guiTabs = pages.Tabs.Select((t, i) => new GuiTab
        {
            Name = dim.ValueNameKey(t.Value) is { } key ? Lang.Get(key) : t.Value,
            DataInt = i,
        }).ToArray();
        // Outside the dialog on its left, as the handbook's (vssurvivalmod GuiDialogHandbook.cs:119#tabBounds).
        var bounds = ElementBounds.Fixed(-200, GuiStyle.TitleBarHeight + 15, 200, pages.Tabs.Count * 30);
        tabs = new AlbumTabs(capi, guiTabs, bounds, OnTab);
        // Set after the constructor, which lights the first tab (vsapi GuiElementVerticalTabs.cs:52).
        for (int i = 0; i < guiTabs.Length; i++) guiTabs[i].Active = lit.Contains(pages.Tabs[i]);
        composer.AddInteractiveElement(tabs, "tabs");
    }

    private void AddPager(GuiComposer composer, ElementBounds content, AlbumPages pages, double width, double y)
    {
        const double button = 30, number = 80;
        double x = (width - number) / 2;
        var prev = ElementBounds.Fixed(x - button, y, button, Line);
        var text = ElementBounds.Fixed(x, y + 6, number, Line);
        var next = ElementBounds.Fixed(x + number, y, button, Line);
        content.WithChildren(prev, text, next);
        composer
            .AddIconButton("left", _ => Turn(page - 1), prev, "prev")
            .AddStaticText(T("albumwindow-page", page + 1, pages.Pages.Count), font.Clone().WithOrientation(EnumTextOrientation.Center), text)
            .AddIconButton("right", _ => Turn(page + 1), next, "next");
    }

    /// <summary>The row under the page: what to add, and its width, before the page's width is known.</summary>
    private sealed record Row(double Width, Action<GuiComposer, ElementBounds, double, double> Add);

    private Row Controls()
    {
        var parts = new List<(double Width, bool Right, Action<GuiComposer, ElementBounds> Add)>();
        void Part(double w, bool right, Action<GuiComposer, ElementBounds> add) => parts.Add((w, right, add));

        if (LevelChoices() is { } levels)
        {
            double w = levels.Names.Select(Width).Max() + 40;
            Part(w, false, (c, b) => c.AddDropDown(levels.Codes, levels.Names, levels.Selected, OnLevel, b, "level"));
        }
        if (shown is { State: ItemLedger.AlbumState.AtLevel, Level.Layouts.Count: > 1 } && shown.Layout is { } layout)
        {
            var all = shown.Level.Layouts;
            string[] ids = all.Select(l => l.Id).ToArray();
            string[] names = all.Select(l => l.NameKey is { } k ? Lang.Get(k) : l.Id).ToArray();
            Part(names.Select(Width).Max() + 40, false,
                (c, b) => c.AddDropDown(ids, names, Array.IndexOf(ids, layout.Id), OnLayout, b, "layout"));
        }
        if (shown.Pages is { Pages.Count: > 0 }) Part(140, false, (c, b) => c.AddTextInput(b, OnSearch, font, "search"));
        if (shown.State == ItemLedger.AlbumState.AtLevel)
        {
            // The want list: the main action, a text button (A2); none without a level, nor for a switched-off collection.
            string wantList = T("ledgerwindow-wantlist");
            Part(CairoFont.ButtonText().GetTextExtents(wantList).Width / GuiElement.scaled(1) + 24, true,
                (c, b) => c.AddSmallButton(wantList, OnWantList, b, EnumButtonStyle.Normal, "wantlist"));
            if (!album.IsEmpty)
            {
                Part(Line, true, (c, b) => c
                    .AddIconButton("undo", _ => OnStartOver(), b, "restart")
                    .AddHoverText(T("ledgerwindow-restart"), font, 200, b.FlatCopy()));
            }
        }

        const double gap = 8;
        double total = parts.Sum(p => p.Width) + gap * Math.Max(0, parts.Count - 1);
        return new Row(total, (composer, content, y, width) =>
        {
            double left = 0, right = width;
            foreach (var p in parts.Where(p => !p.Right))
            {
                var b = ElementBounds.Fixed(left, y, p.Width, Line);
                content.WithChildren(b);
                p.Add(composer, b);
                left += p.Width + gap;
            }
            foreach (var p in parts.Where(p => p.Right).Reverse())
            {
                right -= p.Width;
                var b = ElementBounds.Fixed(right, y, p.Width, Line);
                content.WithChildren(b);
                p.Add(composer, b);
                right -= gap;
            }
        });
    }

    private AlbumPageLook Look()
    {
        var collection = shown.Collection!;
        var level = shown.Level!;
        var dims = collection.Definition.Dimensions.ToDictionary(d => d.Code);
        var groupBy = shown.Layout!.GroupBy;
        return new AlbumPageLook(
            GroupName: g => string.Join(" · ", groupBy.Select((d, i) => dims[d].ValueNameKey(g.Values[i]) is { } k ? Lang.Get(k) : g.Values[i])),
            Freedom: m => shown.State == ItemLedger.AlbumState.AtLevel ? ItemInvoice.Freedom(collection, level, m) : null,
            Rarity: _ => null); // no source of rarity yet (item 6)
    }

    // --- The level, as the ledger window chose it before 0.2 ---

    /// <summary>The levels to choose from (codes, names, the chosen one), or null when there is no choice: an album that has
    /// collected something, or a collection of one level. With none chosen, the first entry asks for one.</summary>
    private (string[] Codes, string[] Names, int Selected)? LevelChoices()
    {
        if (!album.IsEmpty || shown.State is not (ItemLedger.AlbumState.AtLevel or ItemLedger.AlbumState.NoLevel)
            || shown.Collection is not { Levels.Count: > 1 } collection) return null;
        var levels = collection.Levels;
        string[] codes = [.. (album.LevelIn(collection) == null ? [""] : Array.Empty<string>()), .. levels.Select(l => l.Id)];
        string[] names = codes.Select(c => c.Length == 0 ? T("ledgerwindow-level-none")
            : collection.Level(c)!.Definition.NameKey is { } key ? Lang.Get(key) : c).ToArray();
        return (codes, names, Math.Max(0, Array.IndexOf(codes, album.LevelIn(collection)?.Id ?? "")));
    }

    private void OnLevel(string code, bool selected)
    {
        if (code.Length == 0 || album.Level == code) return;
        capi.Network.GetChannel(ModNetwork.Channel).SendPacket(new ChooseLevelRequest { Book = Shown(), Level = code });
    }

    // --- Pages, tabs, layouts ---

    private void Turn(int to)
    {
        if (shown.Pages is not { } pages || to < 0 || to >= pages.Pages.Count || to == page) return;
        page = to;
        Compose();
    }

    private void OnTab(int index, GuiTab tab)
    {
        if (shown.Pages is not { } pages) return;
        page = pages.Tabs[index].FirstPage;
        Compose(); // also when the page stays: the lit tabs are set again
    }

    private void OnLayout(string id, bool selected)
    {
        if (shown is not { Collection: { } collection, Level: { } level, Pages: { } pages } || id == shown.Layout?.Id) return;
        chosenLayouts[collection.Id + "/" + level.Id] = id;
        RelayOut(id, pages);
    }

    private bool OnSwitchPaging()
    {
        paging = paging == TabPaging.Shared ? TabPaging.PerTab : TabPaging.Shared;
        if (shown.Pages is { } pages) RelayOut(shown.Layout?.Id, pages);
        return true;
    }

    /// <summary>Lays the level out again and stays with the first piece of the page shown.</summary>
    private void RelayOut(string? layout, AlbumPages before)
    {
        string? first = before.Pages[page].Members.FirstOrDefault()?.Id;
        shown = Read(shown.State, shown.Collection, shown.Level, layout) with { Line = shown.Line };
        page = (first != null ? shown.Pages?.PageOf(first) : null) ?? 0;
        Compose();
    }

    // --- Search: the matches lit, the rest dimmed; Enter turns to the next page with a match (A2) ---

    private void OnSearch(string text)
    {
        if (text == search) return;
        search = text;
        pageElement?.Highlight(Matcher());
    }

    private System.Func<Member, bool>? Matcher()
    {
        string text = search.Trim().ToSearchFriendly().ToLowerInvariant();
        if (text.Length == 0) return null;
        return member => SearchName(member).Contains(text);
    }

    /// <summary>The names of what a cell shows (the stack put in, or every stack that fills the member), search-friendly as
    /// vanilla's search makes them (see <see cref="GridDialog"/>'s FilterByName).</summary>
    private string SearchName(Member member)
    {
        if (searchNames.TryGetValue(member.Id, out var name)) return name;
        name = string.Join("\n", album.StacksToShow(member).OfType<ItemStack>().Where(s => s.Collectible != null)
            .Select(s => s.GetName().ToSearchFriendly().ToLowerInvariant()));
        return searchNames[member.Id] = name;
    }

    public override void OnKeyDown(KeyEvent args)
    {
        // A one-line input passes Enter on to the dialog (vsapi GuiElementEditableTextBase.cs:656#handled; A1).
        bool enter = args.KeyCode is (int)GlKeys.Enter or (int)GlKeys.KeypadEnter;
        if (enter && SingleComposer?.GetTextInput("search") is { HasFocus: true } input && Matcher() is { } matches
            && shown.Pages?.NextPageWith(matches, page) is { } next)
        {
            args.Handled = true;
            if (next != page)
            {
                page = next;
                Compose();
                SingleComposer.FocusElement(SingleComposer.GetTextInput("search").TabIndex);
            }
            return;
        }
        base.OnKeyDown(args);
    }

    // --- Actions: requests to the server about the album as shown ---

    private bool OnWantList()
    {
        capi.Network.GetChannel(ModNetwork.Channel).SendPacket(new MakeWantListRequest { Book = Shown() });
        return true;
    }

    private void OnStartOver()
    {
        var item = Shown();
        new ConfirmDialog(capi, T("confirm-restart"), ok =>
        {
            if (ok) capi.Network.GetChannel(ModNetwork.Channel).SendPacket(new ClearLedgerRequest { Book = item });
        }).TryOpen();
    }

    public override void OnGuiClosed()
    {
        pageElement?.OnGuiClosed();
        base.OnGuiClosed();
        BookWindow.Closed(this);
    }
}
