using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;

namespace CurioCatalogue;

/// <summary>
/// Shift + right-click with an empty main hand and a ledger in the off-hand makes an invoice, so the ledger can
/// stay in the off-hand (domain.md, Making an invoice). An empty hand has no item to catch the click, and containers handle
/// clicks differently (chests through the Container behavior, crates and ground storage directly in the block
/// entity), so the client catches the click before it reaches the block and asks the server, which checks
/// everything itself.
/// </summary>
internal static class EmptyHandGesture
{
    private const long RepeatGuardMs = 500;

    public static void StartClient(ICoreClientAPI capi, CurioCatalogueModSystem mod)
    {
        var channel = capi.Network.GetChannel(ModNetwork.Channel);
        // One request per press. While the button is held the game repeats the event every 250 ms, and does not
        // report the release (checked in game 26.10.2026). An album leaves the hand empty, so each repeat would act
        // again. An event less than RepeatGuardMs after our previous one is a repeat and is swallowed; every repeat
        // extends that, so holding for any time gives one request.
        long lastEventMs = -RepeatGuardMs;
        capi.Input.InWorldAction += (EnumEntityAction action, bool on, ref EnumHandling handled) =>
        {
            if (action != EnumEntityAction.InWorldRightMouseDown || !on) return;

            // A repeat of our press is ours whatever the hands hold now: the taken items may have filled the empty
            // hand, and a repeat let through would reach the chest and open it (checked in game 26.10.2026).
            long now = capi.ElapsedMilliseconds;
            long sinceLast = now - lastEventMs;
            if (sinceLast < RepeatGuardMs)
            {
                lastEventMs = now;
                handled = EnumHandling.PreventDefault;
                ModLog.Debug(capi, $"empty-hand request repeated after {sinceLast} ms, ignored");
                return;
            }

            var player = capi.World.Player;
            var entity = player?.Entity;
            if (entity == null || !player!.InventoryManager.ActiveHotbarSlot.Empty) return;
            if (entity.LeftHandItemSlot?.Itemstack is not { Collectible: ItemLedger ledgerItem } ledgerStack) return;

            var blockSel = player.CurrentBlockSelection;

            // Right-click into the air (no Shift; domain.md, Windows): open the ledger window. Nothing to tell the server.
            if (blockSel == null && player.CurrentEntitySelection == null)
            {
                handled = EnumHandling.PreventDefault;
                ModLog.Debug(capi, $"open ledger window from the off-hand, mode={ledgerItem.LedgerMode}");
                Gui.LedgerDialog.Open(capi, mod, entity.LeftHandItemSlot, offHand: true);
                return;
            }

            if (!entity.Controls.ShiftKey) return;
            // An album has no Shift + Ctrl action: leave that click to the game (domain.md, Albums and collections).
            if (!ledgerItem.KindFor(entity.Controls.CtrlKey, out _)) return;
            if (mod.Containers.Classify(capi.World, blockSel) != ContainerKind.Supported) return;

            // Stops the click from reaching the block: the container does not open (checked in game 26.10.2026
            // on a chest, a crate and a bag on the ground).
            handled = EnumHandling.PreventDefault;
            lastEventMs = now;
            var pos = blockSel!.Position;
            channel.SendPacket(new MakeInvoiceRequest { X = pos.X, Y = pos.Y, Z = pos.Z, Outgoing = entity.Controls.CtrlKey });
            ModLog.Debug(capi, $"empty-hand invoice request sent for {pos}");
        };
    }

    /// <summary>The client may be wrong or lying: check the hands and the reach again before doing anything.</summary>
    internal static void OnRequest(ICoreServerAPI sapi, IServerPlayer player, MakeInvoiceRequest request)
    {
        var pos = new BlockPos(request.X, request.Y, request.Z);
        var hand = player.InventoryManager.ActiveHotbarSlot;
        var offhand = player.Entity?.LeftHandItemSlot;
        var ledgerItem = offhand?.Itemstack?.Collectible as ItemLedger;

        string? rejected =
            ledgerItem == null ? "no ledger in the off-hand"
            : hand?.Empty != true ? "main hand not empty"
            : !InReach(player, pos) ? "out of reach"
            : null;
        if (rejected != null)
        {
            ModLog.Debug(sapi, $"{player.PlayerName}: empty-hand invoice request for {pos} ignored: {rejected}");
            return;
        }

        if (!ledgerItem!.KindFor(request.Outgoing, out var kind)) return;
        ledgerItem.UseOnContainer(player, offhand!, kind, pos, intoSlot: hand);
    }

    /// <summary>Like a normal click: within the player's picking range, with a little slack for movement.</summary>
    private static bool InReach(IServerPlayer player, BlockPos pos)
    {
        var eye = player.Entity.Pos.XYZ.Add(player.Entity.LocalEyePos);
        double range = player.WorldData.PickingRange + 1.5;
        return eye.SquareDistanceTo(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5) <= range * range;
    }
}
