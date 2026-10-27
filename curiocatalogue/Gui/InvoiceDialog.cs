using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;

namespace CurioCatalogue.Gui;

/// <summary>
/// The invoice window (domain.md, Windows): kind, where and when it was made, its lines in a read-only grid, and
/// "Cancel" (with confirmation; the server applies the sheet rule). Shows the invoice in the main hand.
/// </summary>
public class InvoiceDialog : GridDialog
{
    private readonly CurioCatalogueModSystem mod;
    private Invoice invoice = null!;

    private static InvoiceDialog? open;

    private InvoiceDialog(ICoreClientAPI capi, CurioCatalogueModSystem mod, ItemSlot slot) : base(capi, slot)
    {
        this.mod = mod;
        Refresh();
    }

    public static void Open(ICoreClientAPI capi, CurioCatalogueModSystem mod, ItemSlot slot)
    {
        open?.TryClose();
        open = new InvoiceDialog(capi, mod, slot);
        open.TryOpen();
    }

    protected override bool StillShows(ItemStack? stack) => stack?.Collectible is ItemInvoice;

    protected override int DataOf(ItemStack stack) => HashOf(stack.Attributes.GetTreeAttribute(LedgerStore.RootKey));

    private static string T(string key, params object[] args) => Lang.Get("curiocatalogue:" + key, args);

    protected override void Refresh()
    {
        if (!StillShows(Slot.Itemstack)) return;
        var item = (ItemInvoice)Slot.Itemstack!.Collectible;
        invoice = mod.Store.ReadInvoice(Slot.Itemstack!, item.InvoiceKind);

        // Lines keep their order, as found in the container.
        int rows = FillView(invoice.Entries.Select(line =>
        {
            var shown = ((ItemStack)line.Sample).Clone();
            shown.StackSize = line.Count;
            return shown;
        }).ToList());

        var font = CairoFont.WhiteSmallText();
        double width = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, Cols, 1).fixedWidth + 12;
        double y = GuiStyle.TitleBarHeight + 5;

        var content = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        content.BothSizing = ElementSizing.FitToChildren;

        // Coordinates as the player sees them: relative to the world spawn, like the game's own position display.
        var spawn = capi.World.DefaultSpawnPosition.AsBlockPos;
        var pos = invoice.Position;
        var origin = ElementBounds.Fixed(0, y, width, 22);
        y += 24;
        var summary = ElementBounds.Fixed(0, y, width, 22);
        y += 30;
        content.WithChildren(origin, summary);

        var composer = capi.Gui
            .CreateCompo("curiocatalogue-invoice", ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle))
            .AddShadedDialogBG(content)
            .AddDialogTitleBar(Slot.Itemstack!.GetName(), () => TryClose())
            .BeginChildElements(content)
            .AddStaticText(T("invoice-info-origin", (int)invoice.CreatedTotalDays, pos.X - spawn.X, pos.Y, pos.Z - spawn.Z), font, origin)
            .AddStaticText(T("invoicewindow-summary", invoice.Entries.Length, invoice.Entries.Sum(e => e.Count)), font, summary);

        var inset = AddGrid(composer, content, y, rows);
        var cancelButton = ElementBounds.Fixed(width - 140, inset.fixedY + inset.fixedHeight + 10, 140, 30);
        content.WithChildren(cancelButton);
        composer.AddSmallButton(T("invoicewindow-cancel"), OnCancel, cancelButton, EnumButtonStyle.Normal, "cancel");

        SingleComposer = composer.EndChildElements().Compose();
        UpdateScrollbar();
    }

    private bool OnCancel()
    {
        var wantList = Slot.Itemstack?.Collectible is ItemInvoice { InvoiceKind: InvoiceKind.WantList };
        new GuiDialogConfirm(capi, T(wantList ? "confirm-cancel-wantlist" : "confirm-cancel"), ok =>
        {
            if (ok) capi.Network.GetChannel(ModNetwork.Channel).SendPacket(new CancelInvoiceRequest());
        }).TryOpen();
        return true;
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        if (open == this) open = null;
    }
}
