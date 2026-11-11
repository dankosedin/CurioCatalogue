using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using CurioCatalogue.Core;

namespace CurioCatalogue.Gui;

/// <summary>
/// The window of a ledger or a bill of materials (domain.md, Windows): name, sheets and entries, search, sorting, a read-only
/// grid of the entries, and "Clear". Shows the book in a hand; renaming and clearing go to the server as requests. An album has
/// a window of its own (<see cref="AlbumDialog"/>); <see cref="BookWindow.Open"/> chooses.
/// </summary>
public class LedgerDialog : GridDialog
{
    private enum SortBy { Name, Mod, Count }

    private readonly CurioCatalogueModSystem mod;
    private SortBy sort = SortBy.Name;
    private string search = "";
    private Ledger book = null!;

    internal LedgerDialog(ICoreClientAPI capi, CurioCatalogueModSystem mod, ItemSlot slot, bool offHand) : base(capi, slot, offHand)
    {
        this.mod = mod;
        Refresh();
    }

    private LedgerMode Mode => ((ItemLedger)Slot.Itemstack!.Collectible).LedgerMode;

    protected override bool StillShows(ItemStack? stack) => stack?.Collectible is ItemLedger { IsAlbum: false };

    protected override void Refresh()
    {
        if (!StillShows(Slot.Itemstack)) return;
        book = (Ledger)mod.Store.ReadBook(Slot.Itemstack!, Mode);
        Compose();
    }

    private List<ItemStack> Stacks()
    {
        var stacks = book.Entries.Values.Select(entry =>
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
        double width = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, MaxCols, 1).fixedWidth + 12;
        const double line = 30;
        double y = GuiStyle.TitleBarHeight + 5;

        var content = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        content.BothSizing = ElementSizing.FitToChildren;

        var nameLabel = ElementBounds.Fixed(0, y + 5, 70, line);
        var nameInput = ElementBounds.Fixed(75, y, width - 75 - 115, line);
        var saveButton = ElementBounds.Fixed(width - 105, y, 105, line);
        y += line + 8;
        var info = ElementBounds.Fixed(0, y, width, 44); // two lines: mode, then sheets and entries
        y += 48;
        var searchLabel = ElementBounds.Fixed(0, y + 5, 70, line);
        var searchInput = ElementBounds.Fixed(75, y, width / 2 - 85, line);
        var sortDrop = ElementBounds.Fixed(width / 2, y, width / 2, line);
        y += line + 10;
        content.WithChildren(nameLabel, nameInput, saveButton, info, searchLabel, searchInput, sortDrop);

        string[] sortCodes = Mode == LedgerMode.Counts ? ["name", "mod", "count"] : ["name", "mod"];
        string[] sortNames = sortCodes.Select(c => T("ledgerwindow-sort-" + c)).ToArray();

        var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
        string title = book.Name.Length > 0 ? book.Name : Slot.Itemstack!.GetName();

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

        var inset = AddGrid(composer, content, y, rows);
        var clearButton = ElementBounds.Fixed(width - 160, inset.fixedY + inset.fixedHeight + 10, 160, line);
        content.WithChildren(clearButton);
        composer.AddSmallButton(T("ledgerwindow-clear"), OnClear, clearButton, EnumButtonStyle.Normal, "clear");

        SetComposer(composer.EndChildElements());
        SingleComposer.GetTextInput("name").SetMaxLength(ModNetwork.MaxNameLength); // the server cuts longer names anyway
        SingleComposer.GetTextInput("name").SetValue(book.Name);
        SingleComposer.GetTextInput("search").SetValue(search);
        ApplySearch();
        SingleComposer.UnfocusOwnElements();
    }

    private string InfoText()
    {
        var config = mod.Config;
        // The mode first: a named book shows its name in the title, so the window must say what kind it is (domain.md, Windows).
        string text = T(Mode == LedgerMode.Counts ? "ledgerwindow-mode-counts" : "ledgerwindow-mode-types");
        return text + "\n" + T("ledgerwindow-info", book.Sheets, config.MaxSheets, book.Entries.Count, config.SoftEntryLimit);
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

    private bool OnClear()
    {
        var shown = Shown();
        new ConfirmDialog(capi, T("confirm-clear", book.Entries.Count), ok =>
        {
            if (ok) capi.Network.GetChannel(ModNetwork.Channel).SendPacket(new ClearLedgerRequest { Book = shown });
        }).TryOpen();
        return true;
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        BookWindow.Closed(this);
    }
}
