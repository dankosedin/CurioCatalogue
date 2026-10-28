using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;

namespace CurioCatalogue.Gui;

/// <summary>
/// The ledger window (domain.md, Windows): name, sheets and entries, search, sorting, a read-only grid of the entries,
/// and "Clear". Shows the ledger in a hand; renaming and clearing go to the server as requests.
/// </summary>
public class LedgerDialog : GridDialog
{
    private enum SortBy { Name, Mod, Count }

    private readonly CurioCatalogueModSystem mod;
    private SortBy sort = SortBy.Name;
    private string search = "";
    private Ledger ledger = null!;

    private static LedgerDialog? open;

    private LedgerDialog(ICoreClientAPI capi, CurioCatalogueModSystem mod, ItemSlot slot, bool offHand) : base(capi, slot, offHand)
    {
        this.mod = mod;
        Refresh();
    }

    /// <param name="offHand">The ledger is in the off-hand (opened with an empty main hand).</param>
    public static void Open(ICoreClientAPI capi, CurioCatalogueModSystem mod, ItemSlot slot, bool offHand)
    {
        open?.TryClose();
        open = new LedgerDialog(capi, mod, slot, offHand);
        open.TryOpen();
    }

    private LedgerMode Mode => ((ItemLedger)Slot.Itemstack!.Collectible).LedgerMode;

    protected override bool StillShows(ItemStack? stack) => stack?.Collectible is ItemLedger;

    protected override void Refresh()
    {
        if (!StillShows(Slot.Itemstack)) return;
        ledger = mod.Store.ReadLedger(Slot.Itemstack!, Mode);
        Compose();
    }

    private List<ItemStack> Stacks()
    {
        var stacks = ledger.Entries.Values.Select(entry =>
        {
            var shown = ((ItemStack)entry.Sample).Clone();
            shown.StackSize = entry.Count; // drawn in the slot corner; types mode has 1, drawn as nothing
            return shown;
        });
        var byName = StringComparer.CurrentCultureIgnoreCase;
        return (sort switch
        {
            SortBy.Mod => stacks.OrderBy(s => s.Collectible?.Code?.Domain ?? "").ThenBy(ItemInvoice.NameOf, byName),
            SortBy.Count => stacks.OrderByDescending(s => s.StackSize).ThenBy(ItemInvoice.NameOf, byName),
            _ => stacks.OrderBy(ItemInvoice.NameOf, byName),
        }).ToList();
    }

    private static string T(string key, params object[] args) => Lang.Get("curiocatalogue:" + key, args);

    private void Compose()
    {
        int rows = FillView(Stacks());

        var font = CairoFont.WhiteSmallText();
        double width = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, Cols, 1).fixedWidth + 12;
        const double line = 30;
        double y = GuiStyle.TitleBarHeight + 5;

        var content = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        content.BothSizing = ElementSizing.FitToChildren;

        var nameLabel = ElementBounds.Fixed(0, y + 5, 70, line);
        var nameInput = ElementBounds.Fixed(75, y, width - 75 - 115, line);
        var saveButton = ElementBounds.Fixed(width - 105, y, 105, line);
        y += line + 8;
        var info = ElementBounds.Fixed(0, y, width, 44); // two lines: mode (and album progress), then sheets and entries
        y += 48;
        var searchLabel = ElementBounds.Fixed(0, y + 5, 70, line);
        var searchInput = ElementBounds.Fixed(75, y, width / 2 - 85, line);
        var sortDrop = ElementBounds.Fixed(width / 2, y, width / 2, line);
        y += line + 10;
        ElementBounds? missingLabel = null;
        if (ledger.Album != null)
        {
            missingLabel = ElementBounds.Fixed(0, y, width, 22);
            y += 24;
        }
        content.WithChildren(nameLabel, nameInput, saveButton, info, searchLabel, searchInput, sortDrop);
        if (missingLabel != null) content.WithChildren(missingLabel);

        string[] sortCodes = Mode == LedgerMode.Counts ? ["name", "mod", "count"] : ["name", "mod"];
        string[] sortNames = sortCodes.Select(c => T("ledgerwindow-sort-" + c)).ToArray();

        var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
        string title = ledger.Name.Length > 0 ? ledger.Name : Slot.Itemstack!.GetName();

        var composer = capi.Gui
            .CreateCompo("curiocatalogue-ledger", dialogBounds)
            .AddShadedDialogBG(content)
            .AddDialogTitleBar(title, () => TryClose())
            .BeginChildElements(content)
            .AddStaticText(T("ledgerwindow-name"), font, nameLabel)
            .AddTextInput(nameInput, null, font, "name")
            .AddSmallButton(T("ledgerwindow-save"), OnSave, saveButton, EnumButtonStyle.Normal, "save")
            .AddDynamicText(InfoText(), font, info, "info")
            .AddStaticText(T("ledgerwindow-search"), font, searchLabel)
            .AddTextInput(searchInput, OnSearch, font, "search")
            .AddDropDown(sortCodes, sortNames, Math.Max(0, Array.IndexOf(sortCodes, sort.ToString().ToLowerInvariant())), OnSort, sortDrop, "sort");
        if (missingLabel != null) composer.AddStaticText(T("ledgerwindow-missing"), font, missingLabel);

        var inset = AddGrid(composer, content, y, rows);
        // For an album "Clear" means "start over": every member missing again (emptying the list would instead
        // mark the whole collection collected).
        var clearButton = ElementBounds.Fixed(width - 160, inset.fixedY + inset.fixedHeight + 10, 160, line);
        content.WithChildren(clearButton);
        composer.AddSmallButton(T(Mode == LedgerMode.Album ? "ledgerwindow-restart" : "ledgerwindow-clear"), OnClear, clearButton, EnumButtonStyle.Normal, "clear");
        if (Mode == LedgerMode.Album)
        {
            // The "missing" list for another player, for one sheet (domain.md, Albums and collections).
            var wantListButton = ElementBounds.Fixed(width - 160 - 10 - 220, clearButton.fixedY, 220, line);
            content.WithChildren(wantListButton);
            composer.AddSmallButton(T("ledgerwindow-wantlist"), OnWantList, wantListButton, EnumButtonStyle.Normal, "wantlist");
        }

        SetComposer(composer.EndChildElements());
        SingleComposer.GetTextInput("name").SetMaxLength(ModNetwork.MaxNameLength); // the server cuts longer names anyway
        SingleComposer.GetTextInput("name").SetValue(ledger.Name);
        SingleComposer.GetTextInput("search").SetValue(search);
        ApplySearch();
        SingleComposer.UnfocusOwnElements();
    }

    private string InfoText()
    {
        var config = mod.Config;
        // The mode first: a named ledger shows its name in the title, so the window must say what kind it is (domain.md, Windows).
        string text = T(Mode switch
        {
            LedgerMode.Counts => "ledgerwindow-mode-counts",
            LedgerMode.Album => "ledgerwindow-mode-album",
            _ => "ledgerwindow-mode-types",
        });
        // Against the collection as the game has it now, with pictures: "Collected 57 of 88 · pictures 12 of 31".
        if (((ItemLedger)Slot.Itemstack!.Collectible).ProgressOf(ledger) is { } progress)
            text += " · " + T("ledgerwindow-collected", progress.Collected, progress.Total, progress.GroupsComplete, progress.GroupsTotal);
        // An album's entries are what it still misses, already in the progress, and its limit does not matter: sheets only.
        return text + "\n" + (Mode == LedgerMode.Album
            ? T("ledger-info-sheets", ledger.Sheets, config.MaxSheets)
            : T("ledgerwindow-info", ledger.Sheets, config.MaxSheets, ledger.Entries.Count, config.SoftEntryLimit));
    }

    private void OnSearch(string text)
    {
        if (text == search) return;
        search = text;
        ApplySearch();
    }

    private void ApplySearch() => FilterByName(search);

    private void OnSort(string code, bool selected)
    {
        if (!Enum.TryParse(code, ignoreCase: true, out SortBy by) || by == sort) return;
        sort = by;
        Compose();
    }

    private bool OnSave()
    {
        string name = SingleComposer.GetTextInput("name").GetText() ?? "";
        capi.Network.GetChannel(ModNetwork.Channel).SendPacket(new RenameLedgerRequest { Name = name, Book = Shown() });
        return true;
    }

    private bool OnWantList()
    {
        capi.Network.GetChannel(ModNetwork.Channel).SendPacket(new MakeWantListRequest { Book = Shown() });
        return true;
    }

    private bool OnClear()
    {
        var shown = Shown();
        new ConfirmDialog(capi, Mode == LedgerMode.Album ? T("confirm-restart") : T("confirm-clear", ledger.Entries.Count), ok =>
        {
            if (ok) capi.Network.GetChannel(ModNetwork.Channel).SendPacket(new ClearLedgerRequest { Book = shown });
        }).TryOpen();
        return true;
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        if (open == this) open = null;
    }
}
