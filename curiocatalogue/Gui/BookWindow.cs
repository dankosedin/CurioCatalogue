using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace CurioCatalogue.Gui;

/// <summary>Opens the window of the book in a hand: the album's own (<see cref="AlbumDialog"/>) or the ledger's
/// (<see cref="LedgerDialog"/>). One book window at a time: opening one closes the other.</summary>
public static class BookWindow
{
    private static GuiDialog? open;

    /// <param name="offHand">The book is in the off-hand (opened with an empty main hand).</param>
    public static void Open(ICoreClientAPI capi, CurioCatalogueModSystem mod, ItemSlot slot, bool offHand)
    {
        open?.TryClose();
        open = slot.Itemstack?.Collectible is ItemLedger { IsAlbum: true }
            ? new AlbumDialog(capi, mod, slot, offHand)
            : new LedgerDialog(capi, mod, slot, offHand);
        open.TryOpen();
    }

    /// <summary>A window says it closed; the next opening has nothing to close.</summary>
    internal static void Closed(GuiDialog dialog)
    {
        if (open == dialog) open = null;
    }
}
