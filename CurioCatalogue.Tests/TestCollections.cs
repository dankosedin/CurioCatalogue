using Vintagestory.API.Common;
using CurioCatalogue.Adapters;
using CurioCatalogue.Core;

namespace CurioCatalogue.Tests;

/// <summary>Collections for the adapter tests, built by the core as the registry builds them (CollectionRegistry).</summary>
internal static class TestCollections
{
    /// <summary>
    /// Tapestry pieces as members of one level, each its own picture, in the order given (the order of taking new); as
    /// <c>tapestries.json</c> but without a picture's shape.
    /// </summary>
    public static DefinedCollection Tapestries(StackKeys keys, System.Func<string, ItemStack> tapestry, params string[] pieces) =>
        Build(keys, tapestry, pieces, ["pieces"]);

    /// <summary>The same with a second level of the same members: a collection where an album must choose its level.</summary>
    public static DefinedCollection TwoLevels(StackKeys keys, System.Func<string, ItemStack> tapestry, params string[] pieces) =>
        Build(keys, tapestry, pieces, ["pieces", "again"]);

    private static DefinedCollection Build(StackKeys keys, System.Func<string, ItemStack> tapestry, string[] pieces, string[] levels)
    {
        var definition = new CollectionDefinition("tapestries",
            [new DimensionDef("picture", []), new DimensionDef("piece", [])],
            levels.Select(id => new LevelDef(id, ["piece"], new Dictionary<string, Drop> { ["picture"] = Drop.Any }, [])).ToList(),
            [], []);
        var points = pieces.Select(p => new SourcePoint(p,
            new Dictionary<string, string> { ["picture"] = p, ["piece"] = p }, keys.KeyOf(tapestry(p)), tapestry(p)));
        return DefinedCollection.Build(definition, points) is CollectionBuild.Built built
            ? built.Collection
            : throw new InvalidOperationException("the test collection does not build");
    }
}
