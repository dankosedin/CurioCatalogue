using NSubstitute;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;

namespace CurioCatalogue.Tests;

public class CollectionRegistryTests
{
    private readonly GameWorld game = new();
    private readonly StackKeys keys;
    private readonly Block tapestry;
    private readonly List<DefinitionAsset> assets = [];

    public CollectionRegistryTests()
    {
        keys = new StackKeys(game.World, ["durability"]);
        tapestry = game.Block("tapestry-north");
        // Assumption: the tapestry block lists its pictures in the "sizes" attribute, name → "WxH"
        // (assets/survival/blocktypes/cloth/tapestry.json; modding.md, Vanilla content we rely on).
        tapestry.Attributes = new JsonObject(JToken.Parse("{ sizes: { ambush: '3x1', 'aid-arm': '1x2', notice: '1x1' } }"));
        // Our own definition, as shipped.
        Add("curiocatalogue", "tapestries", File.ReadAllText(Path.Combine(AssetsTests.AssetsFolder, "config", "curiocatalogue", "collections", "tapestries.json")));
    }

    private void Add(string domain, string id, string json)
    {

        assets.Add(new DefinitionAsset(new AssetLocation(domain, $"{CollectionRegistry.DefinitionsPath}{id}.json"), json));
    }

    private CollectionRegistry Registry(CollectionSettings? tapestries = null, Dictionary<string, CollectionSettings>? settings = null) =>
        new(game.World, keys, settings ?? new() { [CollectionRegistry.TapestriesId] = tapestries ?? new() }, () => assets);

    private ItemStack Tapestry(string type)
    {
        var stack = new ItemStack(tapestry);
        stack.Attributes.SetString("type", type);
        return stack;
    }

    /// <summary>The tapestries' one level, the pieces; null when there is no tapestry collection.</summary>
    private static CollectionLevel? Pieces(CollectionRegistry registry) => registry.Defined(CollectionRegistry.TapestriesId)?.Levels.Single();

    /// <summary>The lines the registry logged as errors: the format and its arguments, joined.</summary>
    private List<string> Errors() => game.Api.Logger.ReceivedCalls()
        .Where(c => c.GetMethodInfo().Name == nameof(ILogger.Error))
        .Select(c => string.Join(" ", c.GetArguments().SelectMany(a => a is object?[] arguments ? arguments : [a]).Select(a => a?.ToString())))
        .ToList();

    [Fact]
    public void Tapestries_come_from_the_block_sizes()
    {
        var collection = Pieces(Registry())!;

        Assert.Equal(6, collection.Members.Count); // ambush 3 + aid-arm 2 + notice 1
        Assert.Equal(["aid-arm", "ambush", "notice"], collection.Members.Select(m => m.Values["picture"]).Distinct().Order());
        Assert.Contains(keys.KeyOf(Tapestry("ambush2")), collection.Members.Select(m => m.Fillers[0]));
    }

    [Fact]
    public void Tapestries_come_picture_by_picture_piece_by_piece()
    {
        var order = Pieces(Registry())!.Members.Select(m => ((ItemStack)m.Stacks[0]).Attributes.GetString("type")).ToList();

        // The order of the block's sizes, a picture whole, a picture's pieces by their place (row by row).
        Assert.Equal(["ambush1", "ambush2", "ambush3", "aid-arm1", "aid-arm2", "notice"], order);
    }

    [Fact]
    public void Config_excludes_and_adds_pieces()
    {
        var collection = Pieces(Registry(new() { Exclude = ["aid-*"], Add = ["mymod-banner"] }))!;

        Assert.Equal(5, collection.Members.Count);
        Assert.DoesNotContain(keys.KeyOf(Tapestry("aid-arm1")), collection.Members.Select(m => m.Fillers[0]));
        Assert.Contains(keys.KeyOf(Tapestry("mymod-banner")), collection.Members.Select(m => m.Fillers[0]));
        // An added piece is its own picture.
        Assert.Equal("mymod-banner", collection.Members.Single(m => m.Fillers[0] == keys.KeyOf(Tapestry("mymod-banner"))).Values["picture"]);
    }

    [Fact]
    public void A_disabled_collection_is_not_there()
    {
        var registry = Registry(new() { Disabled = true });

        Assert.Null(Pieces(registry));
        Assert.Null(registry.ForSample(Tapestry("ambush1")));
        Assert.Empty(registry.All);
    }

    [Fact]
    public void The_resolver_tells_found_from_switched_off_from_unknown()
    {
        var on = Registry();
        Assert.Equal(CollectionRegistry.TapestriesId, Assert.IsType<CollectionResolution.Found>(on.Resolve(CollectionRegistry.TapestriesId)).Value.Id);
        Assert.IsType<CollectionResolution.Unknown>(on.Resolve("vanished"));

        var off = Registry(new() { Disabled = true });
        Assert.IsType<CollectionResolution.Disabled>(off.Resolve(CollectionRegistry.TapestriesId));
        Assert.Null(off.Resolve(CollectionRegistry.TapestriesId).Collection);
        // Off in the config wins over a missing definition: the admin's word is "switched off".
        Assert.IsType<CollectionResolution.Disabled>(Registry(settings: new() { ["vanished"] = new() { Disabled = true } }).Resolve("vanished"));
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
        var member = Pieces(Registry())!.Members[0];
        var sample = Assert.IsType<ItemStack>(member.Stacks[0]);
        Assert.Equal(member.Fillers[0], keys.KeyOf(sample));
    }

    [Fact]
    public void Without_the_tapestry_block_there_is_no_collection()
    {
        var empty = new GameWorld();
        Assert.Null(new CollectionRegistry(empty.World, new StackKeys(empty.World, []), new Dictionary<string, CollectionSettings>(), () => assets).Defined(CollectionRegistry.TapestriesId));
    }

    // ---- definitions of other domains, the variants source

    private const string Ores = """
        {
          "source": { "kind": "variants", "code": "game:crystalizedore-{grade}-{ore}-{rock}" },
          "dimensions": [
            { "code": "ore", "values": [] }, { "code": "rock", "values": [] },
            { "code": "grade", "values": ["poor", "medium", "rich"] }
          ],
          "levels": [
            { "id": "ores", "keep": ["ore"], "drop": { "rock": "any", "grade": "max" } },
            { "id": "all", "keep": ["ore", "rock", "grade"] }
          ],
          "exclude": ["*-granite"]
        }
        """;

    private void Codes(params string[] codes)
    {
        var all = codes.Select(c => (CollectibleObject)game.Item(c)).ToList();
        all.Add(game.Item("ingot-iron"));
        game.World.Collectibles.Returns(all);
    }

    [Fact]
    public void A_definition_of_another_domain_makes_a_collection_of_the_registered_variants()
    {
        Add("othermod", "ores", Ores);
        Codes("crystalizedore-poor-quartz-andesite", "crystalizedore-rich-quartz-andesite", "crystalizedore-rich-quartz-basalt",
            "crystalizedore-poor-quartz-granite", "crystalizedore-medium-galena-basalt");

        var registry = Registry();
        var ores = registry.Defined("ores")!;

        // The granite one is the definition's own exclusion; ingot-iron does not fit the pattern.
        Assert.Equal(["quartz", "galena"], ores.Level("ores")!.Members.Select(m => m.Id)); // the source's order: no list of ores
        Assert.Equal(4, ores.Level("all")!.Members.Count);
        // Quartz is filled by its richest grade in any rock: two codes.
        Assert.Equal(2, ores.Level("ores")!.Find("quartz")!.Fillers.Count);
        Assert.Equal(["ores", "tapestries"], registry.All.Keys.Order());
    }

    [Fact]
    public void The_config_excludes_from_a_definition_of_another_domain()
    {
        Add("othermod", "ores", Ores);
        Codes("crystalizedore-rich-quartz-andesite", "crystalizedore-rich-galena-basalt");

        var ores = Registry(settings: new() { ["ores"] = new() { Exclude = ["crystalizedore-*-galena-*"] } }).Defined("ores")!;

        Assert.Equal(["quartz"], ores.Level("ores")!.Members.Select(m => m.Id));
    }

    [Fact]
    public void A_variants_collection_with_no_variants_registered_is_not_there()
    {
        Add("othermod", "ores", Ores);
        Codes();

        Assert.Null(Registry().Defined("ores"));
    }

    // ---- a bad definition leaves the others working

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{ "source": { "kind": "nowhere" }, "dimensions": [], "levels": [] }""")]
    [InlineData("""{ "source": { "kind": "variants", "code": "game:x-{a}" }, "dimensions": [ { "code": "a" } ], "levels": [ { "id": "l", "keep": ["a"], "drop": { "b": "any" } } ] }""")]
    public void A_bad_definition_is_logged_and_the_other_collections_work(string json)
    {
        Add("badmod", "broken", json);
        Codes("x-1");

        var registry = Registry();

        Assert.Equal(6, Pieces(registry)!.Members.Count);
        Assert.Null(registry.Defined("broken"));
        Assert.Contains(Errors(), e => e.Contains("collection 'broken' (badmod:config/curiocatalogue/collections/broken.json) cannot be used"));
    }

    [Fact]
    public void A_definition_found_twice_keeps_the_first()
    {
        Add("first", "ores", Ores);
        Add("second", "ores", Ores.Replace("*-granite", "*-basalt"));
        Codes("crystalizedore-rich-quartz-basalt", "crystalizedore-rich-quartz-granite");

        var ores = Registry().Defined("ores")!;

        Assert.Equal(["basalt"], ores.Level("all")!.Members.Select(m => m.Values["rock"]));
        Assert.Contains(Errors(), e => e.Contains("collection 'ores' is defined twice"));
    }
}
