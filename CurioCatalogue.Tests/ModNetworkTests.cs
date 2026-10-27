using Vintagestory.API.Common;
using CurioCatalogue.Core;

namespace CurioCatalogue.Tests;

/// <summary>The refusal "missing" travels as stacks, so that the client names them in its own language.</summary>
public class ModNetworkTests
{
    private readonly GameWorld game = new();

    private static Entry EntryOf(ItemStack stack, int count) =>
        new(new ItemKey(CollectibleKind.Item, stack.Collectible.Code.ToString(), ""), count, stack);

    [Fact]
    public void Refusal_names_the_first_lacking_items_and_counts_the_rest()
    {
        var items = new[] { "ingot-iron", "ingot-copper", "plank-oak", "nail-iron", "rope" }.Select(code => game.Item(code)).ToList();
        var lacking = items.Select((item, i) => EntryOf(new ItemStack(item), i + 1)).ToList();

        var refusal = ModNetwork.MissingRefusal(lacking);

        Assert.Equal(ModNetwork.LackingShown, refusal.Stacks.Count);
        Assert.Equal([1, 2, 3], refusal.Counts);
        Assert.Equal(2, refusal.More);
    }

    [Fact]
    public void Lacking_stacks_come_back_as_the_same_items_with_their_attributes()
    {
        // Tapestries are named from the attribute "type", so it must survive the trip.
        var tapestry = game.Block("tapestry-north");
        var stack = new ItemStack(tapestry);
        stack.Attributes.SetString("type", "ambush1");

        var refusal = ModNetwork.MissingRefusal([EntryOf(stack, 2)]);
        var back = new ItemStack(refusal.Stacks[0]);

        // Resolves through world.GetBlock(id) (GameWorld; vsapi ItemStack.cs:354#ResolveBlockOrItem).
        Assert.True(back.ResolveBlockOrItem(game.World));
        Assert.Same(tapestry, back.Collectible);
        Assert.Equal("ambush1", back.Attributes.GetString("type"));
        Assert.Equal(0, refusal.More);
    }
}
