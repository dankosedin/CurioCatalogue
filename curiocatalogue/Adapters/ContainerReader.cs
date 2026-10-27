using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace CurioCatalogue.Adapters;

public enum ContainerKind
{
    /// <summary>Not a container (or ground storage without bags): the click is left to the game.</summary>
    None,

    /// <summary>Has an inventory, but the ledger does not work with it (a firepit, a quern, a barrel, a shelf...).</summary>
    Unsupported,

    /// <summary>A container the ledger reads (domain.md, Containers).</summary>
    Supported,
}

/// <summary>
/// Tells which containers the ledger works with and reads their top level: the stacks a player would see on
/// opening it. Never descends into containers inside it (a vessel or a bag in a chest is one item).
/// </summary>
public sealed class ContainerReader
{
    private readonly IWorldAccessor world;
    private readonly System.Func<ItemStack, IHeldBag?> bagOf;
    private readonly System.Func<BlockEntity, bool> isStorage;

    /// <param name="bagOf">How to tell a bag; by default the game's collectible interface. Replaceable in tests.</param>
    /// <param name="isStorage">Which inventory block entities are storage; by default <see cref="IsVanillaStorage"/>. Replaceable in tests.</param>
    public ContainerReader(IWorldAccessor world, System.Func<ItemStack, IHeldBag?>? bagOf = null, System.Func<BlockEntity, bool>? isStorage = null)
    {
        this.world = world;
        // Assumption: a backpack or basket exposes its contents through IHeldBag (modding.md, Containers and inventories; vsessentialsmod CollectibleBehaviorHeldBag).
        this.bagOf = bagOf ?? (stack => stack.Collectible?.GetCollectibleInterface<IHeldBag>());
        this.isStorage = isStorage ?? IsVanillaStorage;
    }

    /// <summary>
    /// Chests, trunks, labeled chests, stationary baskets and storage vessels share BlockEntityGenericTypedContainer;
    /// crates have BlockEntityCrate (modding.md, Containers and inventories). Storage blocks of other mods usually derive from the first.
    /// Firepits, querns, barrels and the like have inventories too, but are machines, not storage.
    /// </summary>
    public static bool IsVanillaStorage(BlockEntity be) => be is BlockEntityGenericTypedContainer or BlockEntityCrate;

    /// <summary>Can a block entity of this class be a supported container (storage, or ground storage holding a bag)?</summary>
    public static bool MayBeStorage(System.Type beType) =>
        typeof(BlockEntityGenericTypedContainer).IsAssignableFrom(beType)
        || typeof(BlockEntityCrate).IsAssignableFrom(beType)
        || typeof(BlockEntityGroundStorage).IsAssignableFrom(beType);

    public ContainerKind Classify(BlockEntity? be) => be switch
    {
        // Ground storage is an IBlockEntityContainer too, but its slots hold the bags themselves.
        // Other things lying on the ground are not containers (domain.md, Containers).
        BlockEntityGroundStorage ground => Bags(ground.Inventory).Any() ? ContainerKind.Supported : ContainerKind.None,
        // Loot chests placed by world generation give each player their own share; their slots show the full
        // amounts, not what is left for this player (vssurvivalmod BEGenericTypedContainer.cs:202#isPerPlayer; domain.md, Containers).
        IBlockEntityContainer { Inventory: InventoryPerPlayer } => ContainerKind.Unsupported,
        IBlockEntityContainer when isStorage(be) => ContainerKind.Supported,
        IBlockEntityContainer => ContainerKind.Unsupported,
        _ => ContainerKind.None,
    };

    /// <summary>What the player is aiming at; no block selected counts as <see cref="ContainerKind.None"/>.</summary>
    public ContainerKind Classify(IWorldAccessor world, BlockSelection? blockSel) =>
        blockSel == null ? ContainerKind.None : Classify(world.BlockAccessor.GetBlockEntity(MainPos(world.BlockAccessor, blockSel.Position)));

    /// <summary>
    /// Where the block entity of the aimed block lives. A block bigger than one cell (a trunk) is its main block plus
    /// invisible fillers without a block entity, which forward clicks and hints to the main block
    /// (vsessentialsmod BlockMultiblock.cs:59#BlockMultiblock, 121).
    /// </summary>
    public static BlockPos MainPos(IBlockAccessor blocks, BlockPos pos) =>
        blocks.GetBlock(pos) is BlockMultiblock filler ? pos.AddCopy(filler.OffsetInv) : pos;

    /// <inheritdoc cref="MainPos(IBlockAccessor, BlockPos)"/>
    public BlockPos MainPos(BlockPos pos) => MainPos(world.BlockAccessor, pos);

    /// <summary>The stacks in a supported container, or null for anything else.</summary>
    public List<ItemStack>? Read(BlockEntity? be)
    {
        if (Classify(be) != ContainerKind.Supported) return null;
        if (be is BlockEntityGroundStorage ground)
        {
            // Assumption: GetContents resolves the stacks and returns null for a bag that was never opened
            // (vsessentialsmod CollectibleBehaviorHeldBag.GetContents).
            return NonEmpty(Bags(ground.Inventory).SelectMany(b => b.Bag.GetContents(b.Stack, world) ?? []));
        }
        return NonEmpty(((IBlockEntityContainer)be!).Inventory.Select(slot => slot.Itemstack));
    }

    /// <summary>
    /// The slots of a supported container, to take items out of (posting an invoice), or null for anything else.
    /// Call <see cref="ContainerSlots.Save"/> for every slot changed: a bag's contents live in the bag's attributes.
    /// </summary>
    public ContainerSlots? Open(BlockEntity? be)
    {
        if (Classify(be) != ContainerKind.Supported) return null;
        if (be is not BlockEntityGroundStorage ground)
            return new ContainerSlots(((IBlockEntityContainer)be!).Inventory.ToList(), slot => slot.MarkDirty());

        // Assumption: a bag's slots come from IHeldBag.GetOrCreateSlots, and a changed slot is written back with
        // IHeldBag.Store; vanilla's BagInventory does just that (vsapi BagInventory.cs:128#SaveSlotIntoBag, 152). The slots need an
        // inventory of their own that knows them: a changed slot reports to its inventory, which throws for a slot
        // it does not hold (vsapi InventoryBase.cs:488#ArgumentException). Vanilla wraps them the same way (BlockEntityContainedBagWorkspace).
        var wrapper = new InventoryGeneric(world.Api);
        var slots = new List<ItemSlot>();
        var saveTo = new Dictionary<ItemSlot, (ItemSlot BagSlot, IHeldBag Bag)>();
        int bagIndex = 0;
        foreach (var bagSlot in ground.Inventory)
        {
            if (bagSlot.Itemstack is { } bagStack && bagOf(bagStack) is { } bag)
            {
                foreach (var slot in bag.GetOrCreateSlots(bagStack, wrapper, bagIndex, world))
                {
                    slots.Add(slot);
                    saveTo[slot] = (bagSlot, bag);
                }
            }
            bagIndex++;
        }
        wrapper.Init(slots.Count, "curiocataloguebag", "curiocataloguebag-" + ground.Pos?.ToString(), (i, _) => slots[i]);
        return new ContainerSlots(slots, slot =>
        {
            var (bagSlot, bag) = saveTo[slot];
            bag.Store(bagSlot.Itemstack, (ItemSlotBagContent)slot);
            bagSlot.MarkDirty();
        });
    }

    private IEnumerable<(ItemStack Stack, IHeldBag Bag)> Bags(IInventory inventory)
    {
        foreach (var slot in inventory)
        {
            if (slot.Itemstack is { } stack && bagOf(stack) is { } bag) yield return (stack, bag);
        }
    }

    private static List<ItemStack> NonEmpty(IEnumerable<ItemStack?> stacks) =>
        stacks.Where(s => s != null && s.StackSize > 0).Select(s => s!).ToList();
}

/// <summary>The slots of a container opened for taking items out, and how to save a changed one.</summary>
public sealed class ContainerSlots(IReadOnlyList<ItemSlot> slots, System.Action<ItemSlot> save)
{
    public IReadOnlyList<ItemSlot> Slots { get; } = slots;

    public void Save(ItemSlot slot) => save(slot);
}
