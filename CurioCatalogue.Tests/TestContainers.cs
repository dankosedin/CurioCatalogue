using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CurioCatalogue.Tests;

/// <summary>
/// A block entity with an inventory of slots: an IBlockEntityContainer, like every vanilla container
/// (vsapi Common/Collectible/IBlockEntityContainer.cs; vssurvivalmod BlockEntityContainer).
/// The real chest classes create their inventory only in Initialize(api), so tests use these instead
/// and tell the reader which of them count as storage. Pass the api when items will really move:
/// a changed slot reports through its inventory's api (vsapi ItemSlot.cs:463#inventory.Api).
/// </summary>
internal abstract class TestInventoryBlock : BlockEntity, IBlockEntityContainer
{
    protected TestInventoryBlock(ICoreAPI? api, ItemStack?[] stacks)
    {
        Inventory = new InventoryGeneric(stacks.Length + 2, "testinventory-1", api!);
        for (int i = 0; i < stacks.Length; i++) Inventory[i]!.Itemstack = stacks[i];
    }

    public IInventory Inventory { get; }
    public string InventoryClassName => "testinventory";
    public void DropContents(Vec3d atPos) { }
    public void CheckInventoryClearedMidTick() { }

    /// <summary>For the reader in tests: only chests are storage.</summary>
    public static bool IsStorage(BlockEntity be) => be is TestChest or TestPerPlayerChest;
}

/// <summary>Stands for a chest or a crate.</summary>
internal class TestChest : TestInventoryBlock
{
    public TestChest(params ItemStack?[] stacks) : base(null, stacks) { }
    public TestChest(ICoreAPI api, params ItemStack?[] stacks) : base(api, stacks) { }
}

/// <summary>Chests, baskets and vessels allow claim traverse access (vssurvivalmod BlockEntityGenericTypedContainer).</summary>
internal sealed class TraverseableChest(params ItemStack?[] stacks) : TestChest(stacks), IClaimTraverseable;

/// <summary>Stands for a firepit, quern or barrel: an inventory, but not storage.</summary>
internal sealed class TestMachine(params ItemStack?[] stacks) : TestInventoryBlock(null, stacks);

/// <summary>A loot chest from world generation: each player has their own share (vsapi InventoryPerPlayer).</summary>
internal sealed class TestPerPlayerChest : BlockEntity, IBlockEntityContainer
{
    public IInventory Inventory { get; } = new InventoryPerPlayer(4, "testperplayer-1", null);
    public string InventoryClassName => "testperplayer";
    public void DropContents(Vec3d atPos) { }
    public void CheckInventoryClearedMidTick() { }
}
