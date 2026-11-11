using System.Collections.Generic;
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

    private InvoiceDialog(ICoreClientAPI capi, CurioCatalogueModSystem mod, ItemSlot slot) : base(capi, slot, offHand: false)
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

    private static string T(string key, params object[] args) => Lang.Get("curiocatalogue:" + key, args);

    protected override void Refresh()
    {
        if (!StillShows(Slot.Itemstack)) return;
        var item = (ItemInvoice)Slot.Itemstack!.Collectible;
        invoice = mod.Store.ReadInvoice(Slot.Itemstack!, item.InvoiceKind);

        // Lines keep their order, as found in the container.
        // A want list's line says its member's name over the item's own text ("Native copper, any rock"): the stack shown may be
        // one of several.
        // A line any of several items fills shows them in turn, as the album's cells do (L7 of the album polish plan).
        var wantedIn = mod.Store.WantedIn(invoice);
        cycles = invoice.Entries.Select(line => new Cycle(ItemInvoice.SlotStacks(wantedIn, line), line.Count)).ToList();
        int rows = FillView(cycles.Select(c => c.StackAt(0)).ToList(),
            invoice.Entries.Select(line => ItemInvoice.SlotLine(wantedIn, line)).ToList());

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

        SetComposer(composer.EndChildElements());
        UpdateScrollbar();
    }

    /// <summary>The stacks a slot shows in turn, at the line's count, and where the slideshow stands.</summary>
    private sealed class Cycle(IReadOnlyList<ItemStack> stacks, int count)
    {
        public readonly IReadOnlyList<ItemStack> Stacks = stacks;
        public Slideshow Show = Slideshow.Start;

        public ItemStack StackAt(int index)
        {
            var shown = Stacks[index].Clone();
            shown.StackSize = count;
            return shown;
        }
    }

    private List<Cycle> cycles = [];

    public override void OnRenderGUI(float deltaTime)
    {
        // The slot under the mouse holds still, so its tooltip stays on one item (as vanilla's firepit reads the hovered slot,
        // vssurvivalmod GuiDialogBlockEntityFirepit.cs:42).
        var hovered = capi.World.Player.InventoryManager.CurrentHoveredSlot;
        for (int i = 0; i < cycles.Count; i++)
        {
            var cycle = cycles[i];
            if (cycle.Stacks.Count < 2) continue;
            var slot = ViewSlot(i);
            var next = cycle.Show.Tick(deltaTime, cycle.Stacks.Count, hovered == slot);
            if (next.Index != cycle.Show.Index) slot.Itemstack = cycle.StackAt(next.Index);
            cycle.Show = next;
        }
        base.OnRenderGUI(deltaTime);
    }

    private bool OnCancel()
    {
        var shown = Shown();
        var wantList = Slot.Itemstack?.Collectible is ItemInvoice { InvoiceKind: InvoiceKind.WantList };
        new ConfirmDialog(capi, T(wantList ? "confirm-cancel-wantlist" : "confirm-cancel"), ok =>
        {
            if (ok) capi.Network.GetChannel(ModNetwork.Channel).SendPacket(new CancelInvoiceRequest { Invoice = shown });
        }).TryOpen();
        return true;
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        if (open == this) open = null;
    }
}
