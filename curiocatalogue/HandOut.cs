using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CurioCatalogue;

/// <summary>
/// Hands a stack the mod made (a new list) to the player: into the given hand if it is empty, else into the hotbar (without
/// the off-hand: it is for the book) and the backpack, else what is left goes on the ground.
/// Not the game's <c>TryGiveItemstack</c>: it offers the stack to every player inventory the player has opened, and in creative
/// that includes the creative inventory, a black hole that swallows anything, and the off-hand. In creative the stack on the
/// ground is still lost: the player picks it up at once into that black hole (docs/modding.md, Containers and inventories;
/// accepted, the author, 13.11.2026).
/// </summary>
public static class HandOut
{
    public static void Give(IServerPlayer player, ItemStack stack, ItemSlot? hand)
    {
        if (hand is { Empty: true })
        {
            hand.Itemstack = stack;
            hand.MarkDirty();
            return;
        }

        var manager = player.InventoryManager;
        var source = new DummySlot(stack);
        // The slots tried already and the off-hand, as the game's own loop skips the slots it has tried (PlayerInventoryManager.TryTransferAway, decompiled 1.22.7).
        var skip = new List<ItemSlot> { manager.OffhandHotbarSlot };
        var inventories = new[] { manager.GetHotbarInventory(), manager.GetOwnInventory(GlobalConstants.backpackInvClassName) }
            .Where(inventory => inventory != null).ToList();

        while (!source.Empty && BestSlot(inventories, source, skip) is { } target)
        {
            skip.Add(target);
            var op = new ItemStackMoveOperation(player.Entity.World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge, source.StackSize);
            if (source.TryPutInto(target, ref op) > 0)
            {
                target.MarkDirty();
                manager.NotifySlot(player, target); // the slot's highlight, as TryGiveItemstack(stack, slotNotifyEffect: true)
            }
        }

        if (!source.Empty) player.Entity.World.SpawnItemEntity(source.Itemstack, player.Entity.Pos.XYZ);
    }

    /// <summary>The slot the game itself would choose among these inventories: each one's best, the heaviest wins
    /// (<c>PlayerInventoryManager.GetBestSuitedSlot</c>, decompiled 1.22.7; <c>InventoryBase.cs:329#GetBestSuitedSlot</c>).</summary>
    private static ItemSlot? BestSlot(IEnumerable<IInventory> inventories, ItemSlot source, List<ItemSlot> skip)
    {
        var best = new WeightedSlot();
        foreach (var inventory in inventories)
        {
            var candidate = inventory.GetBestSuitedSlot(source, null, skip);
            if (candidate.slot != null && candidate.weight > best.weight) best = candidate;
        }
        return best.slot;
    }
}
