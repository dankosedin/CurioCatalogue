using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace CurioCatalogue.Gui;

/// <summary>
/// Base for the ledger and invoice windows: a scrolling, read-only grid of stacks. The grid's inventory exists on
/// the client only (<see cref="ReadOnlySlot"/>); its packets are dropped. Built like vanilla's container dialog
/// (vsapi GuiDialogBlockEntityInventory). Also watches the item the window shows and refreshes or closes.
/// </summary>
public abstract class GridDialog : GuiDialog
{
    protected const int Cols = 10;
    protected const int VisibleRows = 6;
    protected const string GridKey = "slotgrid";
    protected const string ScrollKey = "scrollbar";

    /// <summary>The slot holding the shown item (a hand).</summary>
    protected readonly ItemSlot Slot;

    private InventoryGeneric? view;
    private double gridHeight;
    private long tickListener;
    private int fingerprint;

    public override string ToggleKeyCombinationCode => null!;

    protected GridDialog(ICoreClientAPI capi, ItemSlot slot) : base(capi)
    {
        Slot = slot;
    }

    /// <summary>Is the slot still holding an item this window can show?</summary>
    protected abstract bool StillShows(ItemStack? stack);

    /// <summary>Read the item and compose the window again.</summary>
    protected abstract void Refresh();

    /// <summary>The stack's own data, to notice changes made while the window is open (posting, renaming...).</summary>
    protected abstract int DataOf(ItemStack stack);

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        fingerprint = Slot.Itemstack is { } stack ? DataOf(stack) : 0;
        tickListener = capi.Event.RegisterGameTickListener(_ => Watch(), 500);
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        capi.Event.UnregisterGameTickListener(tickListener);
    }

    private void Watch()
    {
        if (!StillShows(Slot.Itemstack)) { TryClose(); return; }
        int now = DataOf(Slot.Itemstack!);
        if (now == fingerprint) return;
        fingerprint = now;
        Refresh();
    }

    /// <summary>Fills the client-only inventory with the stacks to show, at least one row.</summary>
    protected int FillView(IReadOnlyList<ItemStack> stacks)
    {
        int slots = Math.Max(Cols, (stacks.Count + Cols - 1) / Cols * Cols);
        view = new InventoryGeneric(slots, "curiocatalogue-view", "view-" + capi.World.Player.PlayerUID, capi, (_, inv) => new ReadOnlySlot(inv))
        {
            TakeLocked = true,
            PutLocked = true,
        };
        for (int i = 0; i < stacks.Count; i++) view[i].Itemstack = stacks[i];
        return slots / Cols;
    }

    /// <summary>Adds the inset, scrollbar and grid at <paramref name="y"/> inside the dialog's content bounds.</summary>
    /// <returns>The bounds of the inset, to place things below it.</returns>
    protected ElementBounds AddGrid(GuiComposer composer, ElementBounds parent, double y, int rows)
    {
        double pad = GuiElementItemSlotGrid.unscaledSlotPadding;
        var slotGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, y + pad, Cols, Math.Min(rows, VisibleRows));
        var fullGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, Cols, rows);
        var insetBounds = slotGridBounds.ForkBoundingParent(6, 6, 6, 6);
        var clippingBounds = slotGridBounds.CopyOffsetedSibling();
        clippingBounds.fixedHeight -= 3;
        gridHeight = slotGridBounds.fixedHeight;
        scrollable = rows > VisibleRows;

        composer.AddInset(insetBounds);
        if (!scrollable)
        {
            // Everything fits: no scrollbar, as vanilla does (vsapi GuiDialogBlockEntityInventory, "visibleRows < rows").
            parent.WithChildren(insetBounds);
            composer.AddItemSlotGrid(view!, DropPacket, Cols, slotGridBounds, GridKey);
            return insetBounds;
        }

        var scrollbarBounds = ElementStdBounds.VerticalScrollbar(insetBounds);
        parent.WithChildren(insetBounds, scrollbarBounds);
        composer
            .AddVerticalScrollbar(OnScroll, scrollbarBounds, ScrollKey)
            .BeginClip(clippingBounds)
            .AddItemSlotGrid(view!, DropPacket, Cols, fullGridBounds, GridKey)
            .EndClip();
        return insetBounds;
    }

    private bool scrollable;

    /// <summary>
    /// Shows only the slots whose item name contains <paramref name="text"/>. Vanilla's filter needs the names
    /// handed in: without them it skips every slot for a non-empty search (vsapi GuiElementItemSlotGridBase.cs:575#searchCacheNames,
    /// "if (searchCacheNames == null) continue"). Names go in the same search-friendly form vanilla uses for the
    /// text; descriptions are left empty, so only names are searched (domain.md, Windows). Matches come ordered by
    /// relevance, as in the creative inventory; an empty search keeps the chosen order.
    /// </summary>
    protected void FilterByName(string text)
    {
        var names = new Dictionary<int, string>();
        var descriptions = new Dictionary<int, string>();
        for (int i = 0; i < view!.Count; i++)
        {
            if (view[i].Itemstack is not { Collectible: not null } stack) continue;
            names[i] = stack.GetName().ToSearchFriendly().ToLowerInvariant();
            descriptions[i] = "";
        }
        SingleComposer.GetSlotGrid(GridKey).FilterItemsBySearchText(text, descriptions, names);
        UpdateScrollbar();
    }

    /// <summary>After composing or filtering: size the scrollbar to the rows actually shown.</summary>
    protected void UpdateScrollbar()
    {
        if (!scrollable) return;
        var grid = SingleComposer.GetSlotGrid(GridKey);
        int rows = Math.Max(1, (grid.renderedSlots.Count + Cols - 1) / Cols);
        double full = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, Cols, rows).fixedHeight;
        SingleComposer.GetScrollbar(ScrollKey).SetHeights((float)gridHeight, (float)(full + GuiElementItemSlotGrid.unscaledSlotPadding));
        OnScroll(0);
    }

    private void DropPacket(object packet) =>
        ModLog.Debug(capi, $"{GetType().Name}: slot packet dropped ({packet.GetType().Name})");

    private void OnScroll(float value)
    {
        var bounds = SingleComposer.GetSlotGrid(GridKey).Bounds;
        bounds.fixedY = -value;
        bounds.CalcWorldBounds();
    }

    /// <summary>A cheap hash of an attribute subtree's bytes, to notice that it changed.</summary>
    protected static int HashOf(Vintagestory.API.Datastructures.ITreeAttribute? tree)
    {
        if (tree is not Vintagestory.API.Datastructures.TreeAttribute t) return 0;
        unchecked
        {
            int hash = (int)2166136261;
            foreach (byte b in t.ToBytes()) hash = (hash ^ b) * 16777619;
            return hash;
        }
    }
}
