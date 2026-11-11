using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;

namespace CurioCatalogue.Gui;

/// <summary>
/// The album window's own row (album view plan, A2, "The header and the bottom of the window"): the album's name with a pencil
/// that turns it into a text input and "Save" (Enter saves, Escape cancels), the progress of the whole album as a bar with its
/// number, and the sheets as a stack of parchment, the limit on hover. At the bottom of
/// the window (variant C). A piece taken or a sheet used changes the bar and the count in place, without composing again.
/// </summary>
internal sealed class AlbumHeader
{
    public const double Height = 30;

    private const string PencilIcon = "curiocatalogue-pencil";
    // The name and its pencil have NameRoom on the left, from the window's left edge as the page and the controls (A4: one left
    // edge); the bar takes all the room between them and the sheets, so that no gap is left empty (A4).
    private const double MinBarWidth = 120, BarHeight = 20, Gap = 8, Pencil = Height, NameRoom = 230;

    // The bar's colour: a muted green, as the group's check mark says "complete".
    private static readonly double[] BarColour = [0.36, 0.55, 0.25];

    // A rename sent but not yet back from the server (A4: the old name flashed before the new one). Shown while the stored
    // name is still the one it replaces; given up after PendingMs if the server never changes it (e.g. refused).
    private const int PendingMs = 2000;
    private string? pending, replaced;
    private string stored = ""; // the album's name as last composed: what a rename replaces

    private readonly ICoreClientAPI capi;
    private readonly CairoFont font = CairoFont.WhiteDetailText();
    private readonly DummySlot parchment;
    private readonly Item? paper;
    private readonly Action<string> rename;
    private readonly Action recompose;
    private GuiComposer? composed;

    private static string T(string key, params object[] args) => Lang.Get("curiocatalogue:" + key, args);

    /// <param name="rename">Sends the new name to the server.</param>
    /// <param name="recompose">Composes the window again (the name turned into a text input, or back).</param>
    public AlbumHeader(ICoreClientAPI capi, Action<string> rename, Action recompose)
    {
        this.capi = capi;
        this.rename = rename;
        this.recompose = recompose;
        // The sheets are a stack of parchment, its count drawn by the slot as in the toolbar (A4: everybody reads that);
        // an empty book shows no stack. <see cref="Show"/> sets the size.
        paper = capi.World.GetItem(InvoicePoster.ParchmentCode);
        parchment = new DummySlot();
        // The game has no pencil icon (A1); ours goes where the icon buttons look for named icons, as the waypoint map's
        // (vsessentialsmod WaypointMapLayer.cs:116#CustomIcons; vsapi IconUtil.cs:122#CustomIcons looks them up by name).
        if (!capi.Gui.Icons.CustomIcons.ContainsKey(PencilIcon))
            capi.Gui.Icons.CustomIcons[PencilIcon] = capi.Gui.Icons.SvgIconSource(new AssetLocation("curiocatalogue", "textures/icons/pencil.svg"));
    }

    /// <summary>The name is shown as a text input.</summary>
    public bool Editing { get; private set; }

    /// <summary>The narrowest the row can be: a short name and its pencil, the bar, the sheets.</summary>
    public static double MinWidth => NameRoom + Gap + MinBarWidth + Gap + GuiElementPassiveItemSlot.unscaledSlotSize;

    /// <summary>What the row shows.</summary>
    /// <param name="Progress">The whole album, with its tooltip; null without a level (nothing to measure).</param>
    public sealed record Facts(string Name, int Sheets, int MaxSheets, LevelProgress? Progress, string? ProgressTooltip);

    public void Add(GuiComposer composer, ElementBounds content, double y, double width, Facts facts)
    {
        double side = NameRoom; // the room of the name
        stored = facts.Name;
        if (Editing)
        {
            string save = T("ledgerwindow-save");
            double saveWidth = CairoFont.ButtonText().GetTextExtents(save).Width / GuiElement.scaled(1) + 24;
            var input = ElementBounds.Fixed(0, y, side - saveWidth - 4, Height);
            var button = ElementBounds.Fixed(side - saveWidth, y, saveWidth, Height);
            content.WithChildren(input, button);
            composer
                .AddTextInput(input, null, font, "name")
                .AddSmallButton(save, Save, button, EnumButtonStyle.Normal, "save");
        }
        else
        {
            if (pending != null && facts.Name != replaced) pending = null; // the server's answer came
            string name = pending ?? facts.Name;
            bool unnamed = name.Length == 0;
            var nameFont = unnamed ? font.Clone().WithColor([1, 1, 1, 0.5]) : font;
            string text = AlbumPageElement.Fit(unnamed ? T("albumwindow-noname") : name, nameFont, side - Pencil - 4);
            double textWidth = Math.Ceiling(nameFont.GetTextExtents(text).Width / GuiElement.scaled(1)) + 2;
            var label = ElementBounds.Fixed(0, y + 7, textWidth, Height);
            var pencil = ElementBounds.Fixed(textWidth + 4, y, Pencil, Pencil);
            content.WithChildren(label, pencil);
            composer
                .AddStaticText(text, nameFont, label)
                .AddIconTextButton(PencilIcon, () => { Edit(); return true; }, pencil, "rename")
                .AddAutoSizeHoverText(T("albumwindow-rename"), font, 200, pencil.FlatCopy());
            // A long name cut with "…": its full text on hover, as a group's (A3c).
            if (text != name && !unnamed) composer.AddAutoSizeHoverText(name, font, 300, label.FlatCopy());
        }

        if (facts.Progress != null)
        {
            double barX = NameRoom + Gap, barWidth = width - GuiElementPassiveItemSlot.unscaledSlotSize - Gap - barX;
            var bar = ElementBounds.Fixed(barX, y + (Height - BarHeight) / 2, barWidth, BarHeight);
            var number = bar.FlatCopy().WithFixedOffset(0, 1);
            content.WithChildren(bar, number);
            composer
                .AddStatbar(bar, BarColour, "progress")
                .AddDynamicText("", font.Clone().WithOrientation(EnumTextOrientation.Center), number, "progress-number")
                .AddAutoSizeHoverText(facts.ProgressTooltip ?? "", font, 300, bar.FlatCopy(), "progress-tooltip");
        }

        // The slot is 48 high whatever its bounds (vsapi GuiElementPassiveItemSlot.cs:30#inventory): centred on the row, it reaches into the
        // gaps above and below, where nothing else is drawn.
        double slot = GuiElementPassiveItemSlot.unscaledSlotSize;
        var sheets = ElementBounds.Fixed(width - slot, y + (Height - slot) / 2, slot, slot);
        content.WithChildren(sheets);
        composer
            .AddPassiveItemSlot(sheets, null, parchment, drawBackground: false, "sheets")
            .AddAutoSizeHoverText("", font, 250, sheets.FlatCopy(), "sheets-tooltip");
    }

    /// <summary>After composing: the input's text and focus, the bar's settings. The facts follow by <see cref="Show"/>.</summary>
    public void Composed(GuiComposer composer, Facts facts)
    {
        composed = composer;
        if (Editing && composer.GetTextInput("name") is { } input)
        {
            input.SetMaxLength(ModNetwork.MaxNameLength); // the server cuts longer names anyway
            input.SetValue(facts.Name, setCaretPosToEnd: true);
            composer.FocusElement(input.TabIndex);
        }
        if (composer.GetStatbar("progress") is { } bar)
        {
            bar.ShowValueOnHover = false; // the number is always shown, over the bar (A2)
            bar.SetLineInterval(int.MaxValue); // no segment lines (vsapi GuiElementStatbar.cs:224#lineInterval)
        }
        // The passive slot draws its stack at z 450 (vsapi GuiElementPassiveItemSlot.cs:75#RenderItemstackToGui) and the count above that, over a
        // hover text at its default 500 (A4: "4" over "Sheets: 4 of 32"); 1000 as vanilla's own top layer
        // (vsapi GuiElementHoverText.cs:54#zPosition; 1000 is used by vanilla elements drawn over everything).
        if (composer.GetHoverText("sheets-tooltip") is { } sheetsTip) sheetsTip.ZPosition = 1000;
    }

    /// <summary>The facts that change without composing: the progress and the sheets.</summary>
    public void Show(GuiComposer composer, Facts facts)
    {
        if (facts.Progress is { } progress && composer.GetStatbar("progress") is { } bar)
        {
            bar.SetValues(progress.Collected, 0, Math.Max(1, progress.Total));
            composer.GetDynamicText("progress-number").SetNewText($"{progress.Collected}/{progress.Total}");
            composer.GetHoverText("progress-tooltip").SetNewText(facts.ProgressTooltip ?? "");
        }
        parchment.Itemstack = paper != null && facts.Sheets > 0 ? new ItemStack(paper, facts.Sheets) : null;
        composer.GetHoverText("sheets-tooltip")?.SetNewText(Lang.Get("curiocatalogue:ledger-info-sheets", facts.Sheets, facts.MaxSheets));
    }

    private void Edit()
    {
        Editing = true;
        recompose();
    }

    private bool Save()
    {
        // Read before composing again, which drops the input.
        string typed = composed?.GetTextInput("name")?.GetText() ?? "";
        rename(typed);
        Editing = false;
        pending = typed;
        replaced = stored;
        capi.Event.RegisterCallback(_ => GiveUpPending(typed), PendingMs);
        recompose();
        return true;
    }

    private void GiveUpPending(string typed)
    {
        if (pending != typed) return; // answered, or another rename since
        pending = null;
        if (!Editing) recompose();
    }

    /// <summary>Enter in the name's input: saves (a one-line input passes Enter on to the dialog, A1).</summary>
    public bool OnEnter() => Editing && composed?.GetTextInput("name") is { HasFocus: true } && Save();

    /// <summary>Escape while the name is edited: back to the label, nothing sent.</summary>
    public bool Cancel()
    {
        if (!Editing) return false;
        Editing = false;
        recompose();
        return true;
    }
}
