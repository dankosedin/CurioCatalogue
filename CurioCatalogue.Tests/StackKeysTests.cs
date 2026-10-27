using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;

namespace CurioCatalogue.Tests;

/// <summary>
/// Uses real game data types (TreeAttribute, ItemStack, Block, Item): they work without the engine.
/// Only block/item lookup by id goes through a mocked world (see <see cref="GameWorld"/>).
/// </summary>
public class StackKeysTests
{
    private readonly GameWorld game = new();
    private readonly StackKeys keys;
    private readonly Block tapestry;

    public StackKeysTests()
    {
        keys = new StackKeys(game.World, ["durability"]);
        tapestry = game.Block("tapestry-north");
    }

    // Assumption: a tapestry taken off a wall is "game:tapestry-north" with its picture in the string
    // attribute "type" (modding.md, Vanilla content we rely on; vssurvivalmod BlockTapestry.cs).
    private ItemStack Tapestry(string type)
    {
        var stack = new ItemStack(tapestry);
        stack.Attributes.SetString("type", type);
        return stack;
    }

    private static ItemStack ThroughBytes(ItemStack stack) => new(stack.ToBytes());

    [Fact]
    public void Tapestry_key()
    {
        Assert.Equal("block:game:tapestry-north{\"type\":\"ambush1\"}", keys.KeyOf(Tapestry("ambush1")).ToString());
    }

    [Fact]
    public void Stack_size_is_not_part_of_identity()
    {
        var many = Tapestry("ambush1");
        many.StackSize = 5;
        Assert.Equal(keys.KeyOf(Tapestry("ambush1")), keys.KeyOf(many));
    }

    [Fact]
    public void Game_ignored_attributes_and_config_extras_do_not_count()
    {
        var item = game.Item("pickaxe-iron");
        var fresh = new ItemStack(item);
        var used = new ItemStack(item);
        // Assumption: heat lives in the "temperature" subtree (GlobalConstants.IgnoredStackAttributes), wear in "durability".
        used.Attributes.GetOrAddTreeAttribute("temperature").SetFloat("temperature", 600);
        used.Attributes.SetInt("durability", 10);
        Assert.Equal(keys.KeyOf(fresh), keys.KeyOf(used));
    }

    [Fact]
    public void Converts_every_attribute_type()
    {
        var tree = new TreeAttribute();
        tree.SetString("s", "x");
        tree.SetInt("i", 1);
        tree.SetLong("l", 2);
        tree.SetFloat("f", 1.5f);
        tree.SetDouble("d", 2.5);
        tree.SetBool("b", true);
        tree.SetBytes("bytes", [1, 2]);
        tree["sa"] = new StringArrayAttribute(["a", "b"]);
        tree["ia"] = new IntArrayAttribute([3, 4]);
        tree.GetOrAddTreeAttribute("sub").SetString("k", "v");

        var key = new ItemIdentity([]).KeyOf(CollectibleKind.Item, "game:x", keys.ToAttrTree(tree));

        Assert.Equal(
            "{\"b\":true,\"bytes\":0x0102,\"d\":2.5D,\"f\":1.5F,\"i\":1,\"ia\":[3,4],\"l\":2L," +
            "\"s\":\"x\",\"sa\":[\"a\",\"b\"],\"sub\":{\"k\":\"v\"}}",
            key.Attributes);
    }

    [Fact]
    public void Key_survives_the_game_round_trip()
    {
        // Saving a world writes stacks as bytes; a stack read back must give the same key.
        var stack = Tapestry("salvation12");
        stack.Attributes.SetLong("l", 7);
        stack.Attributes.GetOrAddTreeAttribute("sub").SetDouble("d", 0.1);

        var back = ThroughBytes(stack);
        Assert.Null(back.Collectible); // read from bytes: only the id is known until resolved
        Assert.Equal(keys.KeyOf(stack), keys.KeyOf(back));
    }

    [Fact]
    public void Nested_stack_counts_with_code_size_and_attributes_even_when_unresolved()
    {
        var bag = new ItemStack(game.Item("linensack"));
        var inner = Tapestry("ambush2");
        inner.StackSize = 3;
        bag.Attributes.SetItemstack("slot0", inner);

        const string expected =
            "{\"slot0\":{\"attributes\":{\"type\":\"ambush2\"},\"class\":\"Block\",\"code\":\"game:tapestry-north\",\"size\":3}}";
        Assert.Equal(expected, keys.KeyOf(bag).Attributes);
        Assert.Equal(expected, keys.KeyOf(ThroughBytes(bag)).Attributes);
    }

    [Fact]
    public void Stack_of_a_removed_mod_gets_its_id_as_code()
    {
        var stack = new ItemStack { Class = EnumItemClass.Item, Id = 999 };
        Assert.Equal("item:#999", keys.KeyOf(stack).ToString());
    }
}
