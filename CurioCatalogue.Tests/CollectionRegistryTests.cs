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
    private List<string> Errors() => Logged(nameof(ILogger.Error));

    private List<string> Warnings() => Logged(nameof(ILogger.Warning));

    private List<string> Logged(string method) => game.Api.Logger.ReceivedCalls()
        .Where(c => c.GetMethodInfo().Name == method)
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
        Assert.Empty(registry.All);
        // Its album recipe is still known as its own: the server takes it away at load (AlbumRecipes).
        Assert.Equal(CollectionRegistry.TapestriesId, registry.AlbumOf(new AssetLocation("curiocatalogue", "ledger-album")));
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
        var tapestries = registry.Defined(CollectionRegistry.TapestriesId)!;
        Assert.True(registry.HasMember(tapestries, Tapestry("ambush1")));
        Assert.False(registry.HasMember(tapestries, new ItemStack(game.Item("ingot-iron"))));
        Assert.False(registry.HasMember(tapestries, Tapestry("not-a-piece")));
    }

    // ---- the album recipe (K7, P1): named by the definition

    [Fact]
    public void The_album_recipe_is_the_one_the_definition_names_and_no_other()
    {
        var registry = Registry();

        Assert.Equal(CollectionRegistry.TapestriesId, registry.AlbumOf(new AssetLocation("curiocatalogue", "ledger-album")));
        Assert.Null(registry.AlbumOf(new AssetLocation("othermod", "ledger-album")));
        Assert.Null(registry.AlbumOf(new AssetLocation("curiocatalogue", "ledger-fallback")));
        Assert.Null(registry.AlbumOf(null));
    }

    [Fact]
    public void Another_mods_collection_brings_its_album_recipe_by_json_alone()
    {
        Add("othermod", "ores", Ores.Replace("\"exclude\"", "\"albumRecipe\": \"othermod:album-ores\", \"exclude\""));

        Assert.Equal("ores", Registry().AlbumOf(new AssetLocation("othermod", "album-ores")));
    }

    [Fact]
    public void A_recipe_two_definitions_claim_is_the_first_ones_and_logged()
    {
        Add("othermod", "ores", Ores.Replace("\"exclude\"", "\"albumRecipe\": \"curiocatalogue:ledger-album\", \"exclude\""));

        Assert.Equal(CollectionRegistry.TapestriesId, Registry().AlbumOf(new AssetLocation("curiocatalogue", "ledger-album")));
        Assert.Contains(Errors(), e => e.Contains("collection 'ores'") && e.Contains("already 'tapestries''s"));
    }

    [Fact]
    public void An_album_recipe_without_its_domain_makes_the_definition_bad()
    {
        Add("othermod", "ores", Ores.Replace("\"exclude\"", "\"albumRecipe\": \"album-ores\", \"exclude\""));
        Codes("crystalizedore-rich-quartz-basalt");

        Assert.Null(Registry().Defined("ores"));
        Assert.Contains(Errors(), e => e.Contains("collection 'ores'") && e.Contains("albumRecipe"));
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
            { "code": "ore", "values": [] }, { "code": "rock", "values": [], "inLists": true },
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
        // The rock is said in the lists; the others are not (no "inLists").
        Assert.Equal(["rock"], ores.Definition.Dimensions.Where(d => d.InLists).Select(d => d.Code));
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
    // A pattern naming one dimension twice, and a dimension name .NET cannot take as a group name (QA K7).
    [InlineData("""{ "source": { "kind": "variants", "code": "game:x-{a}-{a}" }, "dimensions": [ { "code": "a" } ], "levels": [ { "id": "l", "keep": ["a"] } ] }""")]
    [InlineData("""{ "source": { "kind": "variants", "code": "game:x-{1a}" }, "dimensions": [ { "code": "1a" } ], "levels": [ { "id": "l", "keep": ["1a"] } ] }""")]
    // A tapestry source pointed at a block whose "sizes" is not names to sizes (QA K7).
    [InlineData("""{ "source": { "kind": "tapestry", "block": "game:oddblock" }, "dimensions": [ { "code": "piece" }, { "code": "picture" } ], "levels": [ { "id": "l", "keep": ["piece"], "drop": { "picture": "any" } } ] }""")]
    // K7 (T3): max on a dimension with no list of values; an empty level id.
    [InlineData("""{ "source": { "kind": "variants", "code": "game:x-{a}-{b}" }, "dimensions": [ { "code": "a" }, { "code": "b" } ], "levels": [ { "id": "l", "keep": ["a"], "drop": { "b": "max" } } ] }""")]
    [InlineData("""{ "source": { "kind": "variants", "code": "game:x-{a}" }, "dimensions": [ { "code": "a" } ], "levels": [ { "id": "", "keep": ["a"] } ] }""")]
    // V2b: a flag that is not true or false.
    [InlineData("""{ "source": { "kind": "variants", "code": "game:x-{a}" }, "dimensions": [ { "code": "a", "inLists": "yes" } ], "levels": [ { "id": "l", "keep": ["a"] } ] }""")]
    public void A_bad_definition_is_logged_and_the_other_collections_work(string json)
    {
        Add("badmod", "broken", json);
        Codes("x-1-1", "x-1");
        // Not names to sizes: the game's AsObject throws on it (JsonObject.AsObject, Newtonsoft deserialising an array into a dictionary).
        game.Block("oddblock").Attributes = new JsonObject(JToken.Parse("{ sizes: [ '3x1' ] }"));

        var registry = Registry();

        Assert.Equal(6, Pieces(registry)!.Members.Count);
        Assert.Null(registry.Defined("broken"));
        Assert.Contains(Errors(), e => e.Contains("collection 'broken' (badmod:config/curiocatalogue/collections/broken.json) cannot be used"));
    }

    [Fact]
    public void A_pattern_naming_a_dimension_twice_is_logged_and_the_other_collections_work()
    {
        // A typo in a definition (or a wrong patch): {a} twice in the code. .NET regex allows a repeated group name (Teammate K7).
        Add("badmod", "twice", """{ "source": { "kind": "variants", "code": "game:x-{a}-{a}" }, "dimensions": [ { "code": "a" } ], "levels": [ { "id": "l", "keep": ["a"] } ] }""");
        Codes("x-1-2");

        var registry = Registry();

        Assert.Equal(6, Pieces(registry)!.Members.Count);
        Assert.Null(registry.Defined("twice"));
        Assert.Contains(Errors(), e => e.Contains("collection 'twice'"));
    }

    [Fact]
    public void A_bad_definition_does_not_shadow_a_good_one_of_the_same_id()
    {
        Add("badmod", "ores", "{ not json");
        Add("goodmod", "ores", Ores);
        Codes("crystalizedore-rich-quartz-basalt");

        Assert.NotNull(Registry().Defined("ores"));
    }

    [Fact]
    public void Our_definition_wins_over_another_domains_found_before_it()
    {
        // K7 (R2): the order of the game's assets is not ours to choose; our domain comes first.
        assets.Insert(0, new DefinitionAsset(new AssetLocation("othermod", $"{CollectionRegistry.DefinitionsPath}tapestries.json"),
            """{ "source": { "kind": "tapestry", "block": "game:tapestry-north" }, "dimensions": [ { "code": "picture" }, { "code": "piece" } ], "levels": [ { "id": "pictures", "keep": ["picture"], "drop": { "piece": "any" } } ] }"""));

        Assert.Equal("pieces", Pieces(Registry())!.Id);
        Assert.Contains(Errors(), e => e.Contains("collection 'tapestries' is defined twice (curiocatalogue:"));
    }

    [Fact]
    public void A_definition_that_throws_while_building_is_logged_and_the_other_collections_work()
    {
        // The guard: a source whose game data breaks something we did not foresee (here the world throws on the items).
        Add("othermod", "ores", Ores);
        game.World.Collectibles.Returns(_ => throw new InvalidOperationException("boom"));

        var registry = Registry();

        Assert.Equal(6, Pieces(registry)!.Members.Count);
        Assert.Null(registry.Defined("ores"));
        Assert.Contains(Errors(), e => e.Contains("collection 'ores'") && e.Contains("boom"));
    }

    [Fact]
    public void Each_problem_is_logged_once_however_often_the_registry_is_built()
    {
        // K7 (R4): the client builds its registry again when the server's settings come and at LevelFinalize.
        Add("badmod", "broken", "{ not json");
        var problems = new ProblemLog(game.Api.Logger);

        for (int i = 0; i < 3; i++)
            new CollectionRegistry(game.World, keys, new Dictionary<string, CollectionSettings>(), () => assets, problems).Defined("broken");

        Assert.Single(Errors(), e => e.Contains("collection 'broken'"));
    }

    [Fact]
    public void The_config_naming_no_collection_or_adding_to_variants_is_warned_about()
    {
        // K7 (H2): settings that do nothing.
        Add("othermod", "ores", Ores);
        Codes("crystalizedore-rich-quartz-basalt");

        Registry(settings: new() { ["tapestry"] = new(), ["ores"] = new() { Add = ["crystalizedore-rich-gold-basalt"] } }).Defined("ores");

        Assert.Contains(Warnings(), w => w.Contains("Collections.tapestry in the config: no collection has this id"));
        Assert.Contains(Warnings(), w => w.Contains("Collections.ores.Add in the config is ignored"));
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
