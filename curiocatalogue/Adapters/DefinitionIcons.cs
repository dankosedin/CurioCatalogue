using Vintagestory.API.Common;
using CurioCatalogue.Core;

namespace CurioCatalogue.Adapters;

/// <summary>
/// The icons a collection's definition names (docs/plans/2026-11-09-album-polish.md, L2b, L2c): a value's
/// (<see cref="DimensionDef.ValueIcon"/>), a level's (<see cref="LevelDef.Icon"/>) and a layout's (<see cref="LayoutDef.Icon"/>),
/// all in the same forms. The album window draws what this finds; the server tests check with it that every icon is in the game.
/// </summary>
public static class DefinitionIcons
{
    /// <summary>
    /// An icon by its code: a stack of the item or block, an SVG's location (<see cref="DimensionDef.IsSvg"/>), or null when there
    /// is none or the game lacks it (a removed mod's item, a missing file): the window then shows the name alone.
    /// </summary>
    public static object? Find(IWorldAccessor world, string? code)
    {
        if (code == null) return null;
        var location = new AssetLocation(code);
        if (DimensionDef.IsSvg(code)) return world.AssetManager.Exists(location) ? location : null;
        if (world.GetItem(location) is { } item) return new ItemStack(item);
        return world.GetBlock(location) is { } block ? new ItemStack(block) : null;
    }
}
