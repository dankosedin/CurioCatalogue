using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using CurioCatalogue.Adapters;

namespace CurioCatalogue.Tests;

public class CollectionRegistryTests
{
    private readonly GameWorld game = new();
    private readonly StackKeys keys;
    private readonly Block tapestry;

    public CollectionRegistryTests()
    {
        keys = new StackKeys(game.World, ["durability"]);
        tapestry = game.Block("tapestry-north");
        // Assumption: the tapestry block lists its pictures in the "sizes" attribute, name → "WxH"
        // (assets/survival/blocktypes/cloth/tapestry.json; modding.md, Vanilla content we rely on).
        tapestry.Attributes = new JsonObject(JToken.Parse("{ sizes: { ambush: '3x1', 'aid-arm': '1x2', notice: '1x1' } }"));
    }

    private CollectionRegistry Registry(string[]? exclude = null, string[]? add = null) =>
        new(game.World, keys, exclude ?? [], add ?? []);

    private ItemStack Tapestry(string type)
    {
        var stack = new ItemStack(tapestry);
        stack.Attributes.SetString("type", type);
        return stack;
    }

    [Fact]
    public void Tapestries_come_from_the_block_sizes()
    {
        var collection = Registry().Tapestries!;

        Assert.Equal(6, collection.Members.Count); // ambush 3 + aid-arm 2 + notice 1
        Assert.Equal(["aid-arm", "ambush", "notice"], collection.Members.Select(m => m.Group).Distinct().Order());
        Assert.Contains(keys.KeyOf(Tapestry("ambush2")), collection.Members.Select(m => m.Key));
    }

    [Fact]
    public void Config_excludes_and_adds_pieces()
    {
        var collection = Registry(exclude: ["aid-*"], add: ["mymod-banner"]).Tapestries!;

        Assert.Equal(5, collection.Members.Count);
        Assert.DoesNotContain(keys.KeyOf(Tapestry("aid-arm1")), collection.Members.Select(m => m.Key));
        Assert.Contains(keys.KeyOf(Tapestry("mymod-banner")), collection.Members.Select(m => m.Key));
    }

    [Fact]
    public void A_tapestry_belongs_to_the_tapestry_collection_and_others_do_not()
    {
        var registry = Registry();
        Assert.Equal(CollectionRegistry.TapestriesId, registry.ForSample(Tapestry("ambush1"))?.Id);
        Assert.Null(registry.ForSample(new ItemStack(game.Item("ingot-iron"))));
        Assert.Null(registry.ForSample(Tapestry("not-a-piece")));
    }

    [Fact]
    public void Members_samples_are_real_tapestry_stacks()
    {
        var member = Registry().Tapestries!.Members[0];
        var sample = Assert.IsType<ItemStack>(member.Sample);
        Assert.Equal(member.Key, keys.KeyOf(sample));
    }

    [Fact]
    public void Without_the_tapestry_block_there_is_no_collection()
    {
        var empty = new GameWorld();
        Assert.Null(new CollectionRegistry(empty.World, new StackKeys(empty.World, []), [], []).Tapestries);
    }
}
