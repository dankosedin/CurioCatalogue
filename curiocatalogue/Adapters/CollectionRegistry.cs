using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using CurioCatalogue.Core;

namespace CurioCatalogue.Adapters;

/// <summary>
/// The collections the mod knows (domain.md, Albums and collections). In the MVP one, the tapestries, found in the game's own data
/// and adjusted by the config. Built on first use, the same on the server and the client.
/// </summary>
public sealed class CollectionRegistry
{
    public const string TapestriesId = "tapestries";

    public static readonly AssetLocation TapestryCode = new("game", "tapestry-north");

    private readonly IWorldAccessor world;
    private readonly StackKeys keys;
    private readonly IReadOnlyList<string> exclude;
    private readonly IReadOnlyList<string> add;
    private Collection? tapestries;

    public CollectionRegistry(IWorldAccessor world, StackKeys keys, IEnumerable<string> tapestryExclude, IEnumerable<string> tapestryAdd)
    {
        this.world = world;
        this.keys = keys;
        exclude = tapestryExclude.ToList();
        add = tapestryAdd.ToList();
    }

    public Collection? Get(string id) => id == TapestriesId ? Tapestries : null;

    /// <summary>The collection a sample item belongs to (the narrowest, once there are several), or null.</summary>
    public Collection? ForSample(ItemStack sample) =>
        Tapestries is { } t && t.Contains(keys.KeyOf(sample)) ? t : null;

    /// <summary>
    /// All tapestry pieces: from the tapestry block's "sizes" attribute, minus the config's exclusions, plus its additions.
    /// Assumption: a piece is "game:tapestry-north" with its name in the string attribute "type"; a tapestry taken
    /// off a wall always drops as tapestry-north (modding.md, Vanilla content we rely on; vssurvivalmod BlockTapestry.cs).
    /// </summary>
    public Collection? Tapestries => tapestries ??= BuildTapestries();

    private Collection? BuildTapestries()
    {
        var block = world.GetBlock(TapestryCode);
        var sizes = block?.Attributes?["sizes"].AsObject<Dictionary<string, string>>();
        if (block == null || sizes == null) return null;

        var pieces = TapestryPieces.WithPictures(sizes)
            .Where(p => !exclude.Any(pattern => Collection.Matches(pattern, p.Piece)))
            .Concat(add.Select(name => new TapestryPiece(name, name)));

        var collection = new Collection(TapestriesId, pieces.Select(p =>
        {
            var stack = new ItemStack(block);
            stack.Attributes.SetString("type", p.Piece);
            return new CollectionMember(keys.KeyOf(stack), p.Picture, stack);
        }));
        ModLog.Debug(world.Api, $"tapestry collection: {collection.Members.Count} pieces, {collection.Members.Select(m => m.Group).Distinct().Count()} pictures");
        return collection;
    }
}
