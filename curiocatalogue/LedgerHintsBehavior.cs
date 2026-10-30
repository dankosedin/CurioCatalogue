using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using CurioCatalogue.Adapters;

namespace CurioCatalogue;

/// <summary>
/// Shows our hints over a container the ledger works with ("Shift + right-click: incoming invoice"),
/// but only while the player holds a ledger or an invoice. The game shows a held item's own hints at the bottom of the
/// screen; hints over a block come from the block and its behaviors, hence this behavior.
/// Added to the storage blocks at load on the client (<see cref="AddTo"/>).
/// </summary>
public class LedgerHintsBehavior(Block owner) : BlockBehavior(owner)
{
    public const string ClassName = "CurioCatalogue.LedgerHints";

    private static readonly WorldInteraction[] LedgerHints =
    [
        new() { ActionLangCode = "curiocatalogue:heldhelp-incoming", HotKeyCode = "shift", MouseButton = EnumMouseButton.Right },
        new() { ActionLangCode = "curiocatalogue:heldhelp-outgoing", HotKeyCodes = ["shift", "ctrl"], MouseButton = EnumMouseButton.Right },
    ];

    private static readonly WorldInteraction[] AlbumHints =
    [
        new() { ActionLangCode = "curiocatalogue:heldhelp-collect", HotKeyCode = "shift", MouseButton = EnumMouseButton.Right },
    ];

    private static readonly WorldInteraction[] InvoiceHints =
    [
        new() { ActionLangCode = "curiocatalogue:heldhelp-post", HotKeyCode = "shift", MouseButton = EnumMouseButton.Right },
    ];

    private static readonly WorldInteraction[] WantListHints =
    [
        new() { ActionLangCode = "curiocatalogue:heldhelp-take", HotKeyCode = "shift", MouseButton = EnumMouseButton.Right },
    ];

    // The game asks only when the player looks at another block, not when the held item changes
    // (checked in game 26.10.2026), so after taking the ledger in hand the hints show on the next block looked at.
    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
    {
        var hints = HintsFor(world, selection, forPlayer);
        ModLog.Debug(world.Api, $"hints asked for {block.Code}, held={forPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Collectible?.Code}: {hints.Length}");
        return hints;
    }

    // The game first asks for the count to size its array, then for the hints (vsapi Block.cs:2195#GetPlacedBlockInteractionHelpCount, Block.cs:2207#interactionsIndex); both must agree.
    // The default count calls GetPlacedBlockInteractionHelp, which would log every request twice.
    public override int GetPlacedBlockInteractionHelpCount(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling) =>
        HintsFor(world, selection, forPlayer).Length;

    private static WorldInteraction[] HintsFor(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
        var hand = forPlayer?.InventoryManager?.ActiveHotbarSlot;
        // Empty main hand with the ledger in the off-hand: the same gestures (EmptyHandGesture).
        var item = hand?.Empty == true ? forPlayer?.Entity?.LeftHandItemSlot?.Itemstack?.Collectible as ItemLedger : hand?.Itemstack?.Collectible;
        var hints = item switch
        {
            ItemLedger { IsAlbum: true } => AlbumHints,
            ItemLedger => LedgerHints,
            ItemInvoice { InvoiceKind: Core.InvoiceKind.WantList } when hand?.Empty != true => WantListHints,
            ItemInvoice when hand?.Empty != true => InvoiceHints,
            _ => null,
        };
        if (hints == null) return [];
        // Ground storage is one block for everything lying on the ground: only a bag on it is a container.
        var containers = world.Api.ModLoader.GetModSystem<CurioCatalogueModSystem>().Containers;
        return containers.Classify(world, selection) == ContainerKind.Supported ? hints : [];
    }

    /// <summary>
    /// Adds the behavior to every block whose block entity is storage or ground storage. Client only, in
    /// AssetsFinalize: behaviors added in code on the server do not reach the client.
    /// </summary>
    public static void AddTo(ICoreAPI api)
    {
        foreach (var block in api.World.Blocks)
        {
            if (block.Code == null || block.Id == 0 || block.EntityClass == null) continue;
            Type? beType = api.ClassRegistry.GetBlockEntity(block.EntityClass);
            if (beType == null || !ContainerReader.MayBeStorage(beType)) continue;
            block.BlockBehaviors = [.. block.BlockBehaviors, new LedgerHintsBehavior(block)];
        }

        if (ModLog.DebugEnabled)
        {
            var with = api.World.Blocks.Where(b => b.BlockBehaviors?.Any(bh => bh is LedgerHintsBehavior) == true).ToList();
            ModLog.Debug(api, $"hints on {with.Count} blocks, e.g. {string.Join(", ", with.Take(3).Select(b => b.Code))}");
        }
    }
}
