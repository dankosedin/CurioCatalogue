using NSubstitute;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using CurioCatalogue.Adapters;

namespace CurioCatalogue.Tests;

public class ContainerReaderTests
{
    private readonly GameWorld game = new();
    private readonly Item ingot;
    private readonly Item backpack;
    private readonly IHeldBag bag = Substitute.For<IHeldBag>();
    private readonly ContainerReader reader;

    public ContainerReaderTests()
    {
        ingot = game.Item("ingot-iron");
        backpack = game.Item("backpack");
        reader = new ContainerReader(game.World, stack => stack.Collectible == backpack ? bag : null, TestInventoryBlock.IsStorage);
    }

    private BlockEntityGroundStorage GroundWith(params ItemStack[] stacks)
    {
        // Assumption: a bag placed on the ground sits in a BlockEntityGroundStorage slot as an item (modding.md, Containers and inventories).
        var ground = new BlockEntityGroundStorage();
        for (int i = 0; i < stacks.Length; i++) ground.Inventory[i].Itemstack = stacks[i];
        return ground;
    }

    [Fact]
    public void Chest_gives_its_non_empty_slots()
    {
        var chest = new TestChest(new ItemStack(ingot, 3), null, new ItemStack(ingot, 5));
        Assert.Equal(ContainerKind.Supported, reader.Classify(chest));
        Assert.Equal([3, 5], reader.Read(chest)!.Select(s => s.StackSize));
    }

    [Fact]
    public void Bag_in_a_chest_is_one_item()
    {
        var chest = new TestChest(new ItemStack(backpack));
        Assert.Equal([backpack.Code], reader.Read(chest)!.Select(s => s.Collectible.Code));
        bag.DidNotReceiveWithAnyArgs().GetContents(default!, default!);
    }

    [Fact]
    public void Machine_with_an_inventory_is_unsupported_and_not_read()
    {
        var machine = new TestMachine(new ItemStack(ingot, 3));
        Assert.Equal(ContainerKind.Unsupported, reader.Classify(machine));
        Assert.Null(reader.Read(machine));
    }

    [Fact]
    public void Per_player_loot_chest_is_unsupported()
    {
        Assert.Equal(ContainerKind.Unsupported, reader.Classify(new TestPerPlayerChest()));
    }

    [Fact]
    public void Block_without_an_inventory_is_none()
    {
        Assert.Equal(ContainerKind.None, reader.Classify(new BlockEntitySign()));
        Assert.Equal(ContainerKind.None, reader.Classify(null));
        Assert.Null(reader.Read(null));
    }

    [Fact]
    public void Vanilla_storage_is_chests_and_crates()
    {
        // The real classes, without reading them: their inventory is made only in Initialize(api).
        Assert.True(ContainerReader.IsVanillaStorage(new BlockEntityGenericTypedContainer()));
        Assert.True(ContainerReader.IsVanillaStorage(new BlockEntityLabeledChest()));
        Assert.True(ContainerReader.IsVanillaStorage(new BlockEntityCrate()));
        Assert.False(ContainerReader.IsVanillaStorage(new BlockEntityQuern()));
        Assert.False(ContainerReader.IsVanillaStorage(new BlockEntityBarrel()));
    }

    [Fact]
    public void Hints_go_to_storage_and_ground_storage_blocks_only()
    {
        Assert.True(ContainerReader.MayBeStorage(typeof(BlockEntityGenericTypedContainer)));
        Assert.True(ContainerReader.MayBeStorage(typeof(BlockEntityLabeledChest)));
        Assert.True(ContainerReader.MayBeStorage(typeof(BlockEntityCrate)));
        Assert.True(ContainerReader.MayBeStorage(typeof(BlockEntityGroundStorage)));
        Assert.False(ContainerReader.MayBeStorage(typeof(BlockEntityFirepit)));
        Assert.False(ContainerReader.MayBeStorage(typeof(BlockEntitySign)));
    }

    [Fact]
    public void Ground_storage_gives_the_contents_of_its_bags()
    {
        var bagStack = new ItemStack(backpack);
        var ground = GroundWith(bagStack);
        // An empty bag slot comes back as null (vsessentialsmod CollectibleBehaviorHeldBag.cs:56-76#GetContents: "contents.Add(cstack)").
        bag.GetContents(bagStack, game.World).Returns([new ItemStack(ingot, 2), null!, new ItemStack(ingot, 7)]);

        Assert.Equal(ContainerKind.Supported, reader.Classify(ground));
        Assert.Equal([2, 7], reader.Read(ground)!.Select(s => s.StackSize));
    }

    [Fact]
    public void Ground_storage_without_bags_is_left_to_the_game()
    {
        // E.g. a pile of ingots: Shift + right-click must still add to the pile.
        Assert.Equal(ContainerKind.None, reader.Classify(GroundWith(new ItemStack(ingot, 10))));
    }

    [Fact]
    public void Bag_never_opened_is_empty()
    {
        // Assumption: GetContents returns null when the bag has no "backpack" tree yet (vsessentialsmod CollectibleBehaviorHeldBag.cs:59#backpackTree).
        var bagStack = new ItemStack(backpack);
        var ground = GroundWith(bagStack);
        bag.GetContents(bagStack, game.World).Returns((ItemStack[]?)null);

        Assert.Empty(reader.Read(ground)!);
    }
}
