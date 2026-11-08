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
/// (<see cref="AlbumPages"/>, drawn by <see cref="AlbumPageElement"/>), tabs at the side when the grouping has names (a dropdown
/// in the row of controls when they are taller than the window), each tab with pages of its own, and a row of controls: the
/// level while the album is empty, the layout, search, the page buttons of the tab, "Make a want list" and "Start over";
/// and the album's own row (<see cref="AlbumHeader"/>: the name, the progress, the sheets). The title bar names the collection.
/// A piece taken while the window is open flips its cell without composing again; turning a page, switching the layout or the
/// level composes. An album whose collection the game does not have (switched off, or gone with its mod) shows what it holds.
/// </summary>
public sealed class AlbumDialog : HeldItemDialog
{
    /// <summary>The layout the player chose, by collection and level, for the game session (A2: presentation only, kept on the
    /// client; an album handed over opens in the receiver's own).</summary>
    private static readonly Dictionary<string, string> chosenLayouts = new();

    private static readonly double Cell = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
    // The page holds GroupsAcross of the narrowest groups side by side, GroupCells each, and so has no empty strip on the right
    // (A4: at 12 cells three groups of 3 left a fourth of the page empty); a group's name has 4 cells of room.
    private const int GroupCells = 4, GroupsAcross = 3, GroupGap = 20;
    private const double Line = 30;

    // What the window needs besides the page: screen and title bar padding, the dialog's padding, the line over the page, the rows
    // of controls under it, the album's own row (A3d) and the room round the groups (A1, "How much fits": at 768 about 11 rows
    // of cells are left). A4 gave the page the 40 of the row of temporary switches.
    private const double AroundThePage = 282;

    // Vanilla's vertical tabs, one under another (AddTabs), from under the title bar.
    private const double TabHeight = 30, TabsTop = 15;

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
    /// <summary>The tabs' ✓ as the dropdown that stands for them was composed with; null when no dropdown shows them.</summary>
    private bool[]? tabChoiceComplete;
    private readonly AlbumHeader header;

    /// <summary>What the window shows: the collection laid out, or (with no level) nothing, and the line above the page.</summary>
    private sealed record View(ItemLedger.AlbumState State, DefinedCollection? Collection, CollectionLevel? Level,
        LayoutDef? Layout, AlbumPages? Pages, PageMetrics? Metrics, string? Line);

    internal AlbumDialog(ICoreClientAPI capi, CurioCatalogueModSystem mod, ItemSlot slot, bool offHand) : base(capi, slot, offHand)
    {
        this.mod = mod;
        header = new AlbumHeader(capi, OnRename, () => { if (IsOpened()) Compose(); });
        // The game's undo arrow is only a part of a circle, its redo the other way round: ours is a whole ↺ (A4).
        if (!capi.Gui.Icons.CustomIcons.ContainsKey(RestartIcon))
            capi.Gui.Icons.CustomIcons[RestartIcon] = capi.Gui.Icons.SvgIconSource(new AssetLocation("curiocatalogue", "textures/icons/restart.svg"));
        Refresh();
    }

    private const string RestartIcon = "curiocatalogue-restart";

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
        return new View(state, collection, level, layout, AlbumPages.Build(collection, level, layout, metrics), metrics, line);
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
            Width: GroupsAcross * GroupCells * cell + (GroupsAcross - 1) * GroupGap,
            Height: Math.Max(3 * cell, (int)(screen - AroundThePage)),
            Cell: cell,
            Heading: 24,
            Gap: GroupGap, // between groups: their frames take 6 on each side
            TextHeadingWidth: (int)Math.Ceiling(words) + 8,
            MinGroupWidth: GroupCells * cell);
    }

    /// <summary>A text's width in the window's font, unscaled.</summary>
    private double Width(string text) => font.GetTextExtents(text).Width / GuiElement.scaled(1);

    /// <summary>What is collected now, on the composed page and the tabs.</summary>
    private void ShowCollected()
    {
        pageElement?.Show(album);
        pageElement?.Highlight(Matcher());
        if (tabs != null) tabs.SetComplete(TabsComplete());
        header.Show(SingleComposer, Facts());
        // The dropdown's names carry the ✓: a tab completed while the window is open composes it again.
        if (tabChoiceComplete != null && !tabChoiceComplete.SequenceEqual(TabsComplete())) Compose();
    }

    private bool[] TabsComplete()
    {
        if (shown.Pages is not { } pages) return [];
        var collected = album.Collected.Keys.ToHashSet();
        return pages.Tabs.Select(t => t.IsComplete(collected)).ToArray();
    }

    /// <summary>What the album's own row shows: the progress only at a level of a collection the game has.</summary>
    private AlbumHeader.Facts Facts()
    {
        LevelProgress? progress = null;
        string? tooltip = null;
        if (shown is { State: ItemLedger.AlbumState.AtLevel, Level: { } level })
        {
            progress = album.Progress(level);
            // The level is named here, being fixed once something is collected (A2, "Levels").
            string text = ItemLedger.ProgressText(level, progress);
            tooltip = shown.Collection!.Levels.Count > 1 && level.Definition.NameKey is { } key ? Lang.Get(key) + "\n" + text : text;
        }
        return new AlbumHeader.Facts(album.Name, album.Sheets, mod.Config.MaxSheets, progress, tooltip);
    }

    /// <summary>The title bar: the kind of book and the collection ("Album: tapestries"); the name is in the album's own row.</summary>
    private string Title() => mod.Collections.Defined(album.CollectionId)?.Definition.NameKey is { } key
        ? T("album-name", Lang.Get(key))
        : T("ledgerwindow-mode-album");

    // --- Composing ---

    private void Compose()
    {
        pageElement?.OnGuiClosed(); // the old page's tooltip goes with it
        var pages = shown.Pages;
        double y = GuiStyle.TitleBarHeight + 5;

        var content = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        content.BothSizing = ElementSizing.FitToChildren;
        var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

        var composer = capi.Gui
            .CreateCompo("curiocatalogue-album", dialogBounds)
            .AddShadedDialogBG(content)
            .AddDialogTitleBar(Title(), () => TryClose());
        tabs = null;
        tabChoiceComplete = null;
        // Tabs at the side while they fit the tallest window the screen allows, the page stretched down to them; taller, they
        // become a dropdown in the row of controls (A4, the author).
        double tabsHeight = pages is { Tabs.Count: > 0 } ? TabsTop + pages.Tabs.Count * TabHeight : 0;
        bool tabsAside = tabsHeight > 0 && tabsHeight <= WindowHeight(pages!, shown.Metrics!.Height);
        if (tabsAside) AddTabs(composer, pages!);
        else if (tabsHeight > 0) tabChoiceComplete = TabsComplete();
        double stretch = tabsAside ? Math.Max(0, tabsHeight - WindowHeight(pages!, pages!.Height)) : 0;
        composer.BeginChildElements(content);

        var controls = Controls();
        double width = Math.Max(Math.Max(pages == null ? 0 : pages.Width + 2 * AlbumPageElement.PadOf(pages), controls.Width), AlbumHeader.MinWidth);
        // The page buttons stand in the row of controls, in the middle of the room between its left and right parts (A4, the
        // author: between the search and the want list), when that room holds them; else in a row of their own, centred.
        bool pagerInRow = pages != null && pages.PagerAt(page).Count > 1 && controls.Left + PagerWidth + controls.Right <= width;
        double pagerCentre = pagerInRow ? (controls.Left + width - controls.Right) / 2 : width / 2;
        var facts = Facts();

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
            var pageBounds = ElementBounds.Fixed(0, y, pages.Width + 2 * AlbumPageElement.PadOf(pages), pages.Height + 2 * AlbumPageElement.PadOf(pages) + stretch);
            content.WithChildren(pageBounds);
            composer.AddAlbumPage(pageBounds, pages, page, shown.Metrics!, Look(), "page", out var element);
            pageElement = element;
            // The full text of a name cut with "…" (A3b).
            foreach (var cut in element.CutTexts)
            {
                var hover = ElementBounds.Fixed(cut.X, y + cut.Y, cut.Width, cut.Height);
                content.WithChildren(hover);
                composer.AddAutoSizeHoverText(cut.Text, font, 300, hover);
            }
            y += pages.Height + 2 * AlbumPageElement.PadOf(pages) + stretch + 6;
            if (pages.PagerAt(page).Count > 1 && !pagerInRow)
            {
                AddPager(composer, content, pages, pagerCentre, y);
                y += Line + 8;
            }
        }

        controls.Add(composer, content, y, width);
        if (pagerInRow) AddPager(composer, content, pages!, pagerCentre, y);
        y += Line + 8;

        // The album's own row, at the bottom (A4: the author chose it over the one under the title).
        y += 2; // the stack of sheets reaches above the row
        header.Add(composer, content, y, width, facts);

        SetComposer(composer.EndChildElements());
        if (SingleComposer.GetTextInput("search") is { } input)
        {
            input.SetPlaceHolderText(T("albumwindow-search"));
            input.SetValue(search, setCaretPosToEnd: true);
        }
        SingleComposer.UnfocusOwnElements();
        header.Composed(SingleComposer, facts);
        ShowCollected();
    }

    /// <summary>The window's height from under the title bar to its bottom with a page <paramref name="pageHeight"/> high, as
    /// <see cref="Compose"/> lays it out, without the row of its own the page buttons take when the row of controls is short of
    /// room (so the tabs are measured against no more than the window has). With the metrics' height: the tallest window the
    /// screen allows.</summary>
    private double WindowHeight(AlbumPages pages, double pageHeight) =>
        5 + (shown.Line != null ? 26 : 0) + pageHeight + 2 * AlbumPageElement.PadOf(pages) + 6 + Line + 8 + 2 + AlbumHeader.Height;

    /// <summary>A tab's name: its value's.</summary>
    private static string TabName(AlbumPages pages, AlbumTab tab) =>
        pages.TabDimension!.ValueNameKey(tab.Value) is { } key ? Lang.Get(key) : tab.Value;

    private void AddTabs(GuiComposer composer, AlbumPages pages)
    {
        int? lit = pages.TabAt(page);
        var guiTabs = pages.Tabs.Select((t, i) => new GuiTab { Name = TabName(pages, t), DataInt = i }).ToArray();
        // Outside the dialog on its left, as the handbook's (vssurvivalmod GuiDialogHandbook.cs:119#tabBounds).
        var bounds = ElementBounds.Fixed(-200, GuiStyle.TitleBarHeight + TabsTop, 200, pages.Tabs.Count * TabHeight);
        tabs = new AlbumTabs(capi, guiTabs, bounds, OnTab);
        // Set after the constructor, which lights the first tab (vsapi GuiElementVerticalTabs.cs:52).
        for (int i = 0; i < guiTabs.Length; i++) guiTabs[i].Active = i == lit;
        composer.AddInteractiveElement(tabs, "tabs");
    }

    /// <summary>◀ N / M ▶ over the pages of the tab, PagerWidth wide, centred on <paramref name="centre"/>.</summary>
    private void AddPager(GuiComposer composer, ElementBounds content, AlbumPages pages, double centre, double y)
    {
        const double button = 30, number = PagerWidth - 2 * button;
        var (first, count) = pages.PagerAt(page);
        double x = centre - number / 2;
        var prev = ElementBounds.Fixed(x - button, y, button, Line);
        var text = ElementBounds.Fixed(x, y + 6, number, Line);
        var next = ElementBounds.Fixed(x + number, y, button, Line);
        content.WithChildren(prev, text, next);
        composer
            .AddIconTextButton("left", () => { Turn(page - 1); return true; }, prev, "prev")
            .AddStaticText(T("albumwindow-page", page - first + 1, count), font.Clone().WithOrientation(EnumTextOrientation.Center), text)
            .AddIconTextButton("right", () => { Turn(page + 1); return true; }, next, "next");
    }

    /// <summary>The row under the page: what to add, and its width, before the page's width is known. Left and Right are the
    /// room its left and right parts take, each with a gap towards the middle.</summary>
    private sealed record Row(double Width, double Left, double Right, Action<GuiComposer, ElementBounds, double, double> Add);

    private const double PagerWidth = 140;

    private Row Controls()
    {
        var parts = new List<(double Width, bool Right, Action<GuiComposer, ElementBounds> Add)>();
        void Part(double w, bool right, Action<GuiComposer, ElementBounds> add) => parts.Add((w, right, add));

        // The level leftmost, the tabs' dropdown right after it (A4, the author).
        if (LevelChoices() is { } levels)
        {
            double w = levels.Names.Select(Width).Max() + 40;
            Part(w, false, (c, b) => c.AddDropDown(levels.Codes, levels.Names, levels.Selected, OnLevel, b, "level"));
        }
        if (tabChoiceComplete != null && shown.Pages is { } pages)
        {
            // The tabs as a dropdown: the tab's name, ✓ when all its groups are complete.
            string[] ids = pages.Tabs.Select((_, i) => i.ToString()).ToArray();
            string[] names = pages.Tabs.Select((t, i) => TabName(pages, t) + (tabChoiceComplete[i] ? " " + CompleteMark : "")).ToArray();
            Part(names.Select(Width).Max() + 40, false,
                (c, b) => c.AddDropDown(ids, names, pages.TabAt(page) ?? 0, OnTabChosen, b, "tab"));
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
            // The place of ↺ stays when there is nothing to start over, so that the want list does not jump (A4).
            bool collected = !album.IsEmpty;
            Part(Line, true, (c, b) =>
            {
                if (collected)
                    c.AddIconTextButton(RestartIcon, () => { OnStartOver(); return true; }, b, "restart")
                        .AddAutoSizeHoverText(T("ledgerwindow-restart"), font, 200, b.FlatCopy());
            });
        }

        const double gap = 8;
        double total = parts.Sum(p => p.Width) + gap * Math.Max(0, parts.Count - 1);
        double leftWidth = parts.Where(p => !p.Right).Sum(p => p.Width + gap), rightWidth = parts.Where(p => p.Right).Sum(p => p.Width + gap);
        return new Row(total, leftWidth, rightWidth, (composer, content, y, width) =>
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
            GroupName: g => string.Join(" · ", groupBy.Select((d, i) => ValueName(collection, dims[d], g.Values[i]))),
            GroupHint: g =>
            {
                var parts = groupBy.Select((d, i) => (Dimension: dims[d], Value: g.Values[i])).ToList();
                if (!parts.Any(p => SharesName(collection, p.Dimension, p.Value))) return null;
                return string.Join(" · ", parts.Select(p => ValueName(collection, p.Dimension, p.Value)
                    + (SharesName(collection, p.Dimension, p.Value) ? $" ({p.Value})" : "")));
            },
            Freedom: m => shown.State == ItemLedger.AlbumState.AtLevel ? ItemInvoice.Freedom(collection, level, m) : null,
            Rarity: _ => null); // no source of rarity yet (item 6)
    }

    /// <summary>A value's name for a group's heading: its lang key, or for a tapestry picture the game's own name (the keys are
    /// wildcards, "tapestry-salvation*", as BlockTapestry.cs:359 looks them up); the value itself when nothing names it.</summary>
    private static string ValueName(DefinedCollection collection, DimensionDef dimension, string value)
    {
        if (dimension.ValueNameKey(value) is { } key) return Lang.Get(key);
        if (collection.Id == "tapestries" && dimension.Code == "picture")
        {
            string name = Lang.GetMatching("tapestry-" + value);
            return name == "tapestry-" + value ? value : name;
        }
        return value;
    }

    /// <summary>Whether another value of the dimension has the same name: the game names "schematic-a" to "schematic-d" alike.
    /// The heading keeps the short name; the hint on hover adds the value (A4, the author: the code only on hover).</summary>
    private static bool SharesName(DefinedCollection collection, DimensionDef dimension, string value)
    {
        string name = ValueName(collection, dimension, value);
        return collection.Values[dimension.Code].Count(v => ValueName(collection, dimension, v) == name) > 1;
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

    /// <summary>Turns to a page of the tab shown (◀ ▶ do not leave the tab).</summary>
    private void Turn(int to)
    {
        if (shown.Pages is not { } pages || to == page) return;
        var (first, count) = pages.PagerAt(page);
        if (to < first || to >= first + count) return;
        page = to;
        Compose();
    }

    private void OnTab(int index, GuiTab tab)
    {
        if (shown.Pages is not { } pages) return;
        page = pages.Tabs[index].FirstPage;
        Compose(); // also when the page stays: the lit tabs are set again
    }

    /// <summary>Marks a complete tab in the dropdown. Vanilla's texts never use "✓" (A3b), so whether the font has the glyph is
    /// for the in-game check of A4.</summary>
    private const string CompleteMark = "✓";

    private void OnTabChosen(string index, bool selected)
    {
        if (shown.Pages is not { } pages || !int.TryParse(index, out int i) || i < 0 || i >= pages.Tabs.Count) return;
        page = pages.Tabs[i].FirstPage;
        Compose();
    }

    private void OnLayout(string id, bool selected)
    {
        if (shown is not { Collection: { } collection, Level: { } level, Pages: { } pages } || id == shown.Layout?.Id) return;
        chosenLayouts[collection.Id + "/" + level.Id] = id;
        RelayOut(id, pages);
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
        if (enter && header.OnEnter())
        {
            args.Handled = true;
            return;
        }
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

    /// <summary>
    /// Escape while the name is edited goes back to the label and keeps the window open. False then: the game takes true for
    /// "closed" and steps back in its list of open dialogs (decompiled GuiManager.OnEscapePressed, <c>i--</c>), which would
    /// call this again and again.
    /// </summary>
    public override bool OnEscapePressed() => !header.Cancel() && base.OnEscapePressed();

    // --- Actions: requests to the server about the album as shown ---

    private void OnRename(string name) =>
        capi.Network.GetChannel(ModNetwork.Channel).SendPacket(new RenameLedgerRequest { Name = name, Book = Shown() });

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
