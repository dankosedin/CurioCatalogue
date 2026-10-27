using Vintagestory.API.Common;

namespace CurioCatalogue.Gui;

/// <summary>
/// A slot that only shows its stack: nothing can be taken out, put in or swapped, by any mouse button or with
/// Shift. Used for the ledger and invoice windows, whose inventories exist on the client only. The grid's own
/// packets go to our handler, which drops them, so the server never hears of these slots.
/// </summary>
public class ReadOnlySlot(InventoryBase inventory) : ItemSlot(inventory)
{
    // Assumption: every move through a slot passes these checks or ActivateSlot (vsapi ItemSlot.cs:110#CanTakeFrom, 137, 162, 317).
    public override bool CanTake() => false;
    public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge) => false;
    public override bool CanHold(ItemSlot sourceSlot) => false;
    public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op) { }
}
