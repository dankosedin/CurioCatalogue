using Vintagestory.API.Client;
using Vintagestory.API.Common;
using CurioCatalogue.Adapters;

namespace CurioCatalogue.Gui;

/// <summary>
/// Base for the windows about an item in a hand (the book, album and invoice windows): watches the item and refreshes or closes,
/// and names it in requests (<see cref="Shown"/>): the server acts only on that item. A new window per opening, freed on close.
/// </summary>
public abstract class HeldItemDialog : GuiDialog
{
    /// <summary>The slot holding the shown item (a hand).</summary>
    protected readonly ItemSlot Slot;

    /// <summary>The item is in the off-hand (opened with an empty main hand).</summary>
    protected readonly bool OffHand;

    private readonly int hotbarSlot;

    private long tickListener;
    private int fingerprint;

    public override string ToggleKeyCombinationCode => null!;

    protected HeldItemDialog(ICoreClientAPI capi, ItemSlot slot, bool offHand) : base(capi)
    {
        Slot = slot;
        OffHand = offHand;
        hotbarSlot = capi.World.Player.InventoryManager.ActiveHotbarSlotNumber;
    }

    /// <summary>Is the slot still holding an item this window can show?</summary>
    protected abstract bool StillShows(ItemStack? stack);

    /// <summary>Read the item again and show it.</summary>
    protected abstract void Refresh();

    /// <summary>
    /// Every opening makes a new window, so a closed one is never used again: the game drops it from LoadedGuis
    /// (decompiled GuiManager.OnGuiClosed; TryOpen registers it, vsapi GuiDialog.cs:289), and OnGuiClosed frees it.
    /// </summary>
    public override bool UnregisterOnClose => true;

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        fingerprint = Slot.Itemstack is { } stack ? LedgerStore.Fingerprint(stack) : 0;
        tickListener = capi.Event.RegisterGameTickListener(_ => Watch(), 500);
        ModLog.Debug(capi, $"{GetType().Name} opened, loaded dialogs: {capi.Gui.LoadedGuis.Count}");
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();
        capi.Event.UnregisterGameTickListener(tickListener);
        Dispose();
    }

    /// <summary>
    /// Shows a newly built window. The old composer is disposed first: the setter does not (vsapi GuiDialog.cs:129, only
    /// ClearComposers and Dispose do, :42-59), and its textures would stay until the game quits. Vanilla does the same
    /// before recomposing (vsessentialsmod GuiDialogAddWayPoint.cs:164).
    /// </summary>
    protected void SetComposer(GuiComposer composer)
    {
        SingleComposer?.Dispose();
        SingleComposer = composer.Compose();
    }

    private void Watch()
    {
        // The hotbar keys and the wheel switch the main hand while the window is open (decompiled HudHotbar, OnKeySlot;
        // checked in game 28.10.2026).
        bool switched = !OffHand && capi.World.Player.InventoryManager.ActiveHotbarSlotNumber != hotbarSlot;
        if (switched || !StillShows(Slot.Itemstack)) { TryClose(); return; }
        int now = LedgerStore.Fingerprint(Slot.Itemstack!);
        if (now == fingerprint) return;
        fingerprint = now;
        Refresh();
    }

    /// <summary>The item as this window shows it, for a request about it. Take it on the click, before any confirmation.</summary>
    protected ShownItem Shown() => new() { OffHand = OffHand, HotbarSlot = hotbarSlot, Fingerprint = fingerprint };
}
