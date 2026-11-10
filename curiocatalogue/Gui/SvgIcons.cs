using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace CurioCatalogue.Gui;

/// <summary>
/// An SVG icon of a definition (<see cref="Adapters.DefinitionIcons"/>) as a texture, in the file's own colours, so that a mod's
/// definition sets the look by its files alone (L2b). Drawn into a Cairo surface, as vanilla's icons are (vsapi IconUtil.cs:31):
/// with no colour given the game swaps red and blue for Cairo's byte order, and <c>LoadSvg</c> uploads those bytes as RGBA, so it
/// draws yellow as blue (decompiled SvgLoader.rasterizeSvg and LoadSvg; seen in game 10.11.2026; docs/modding.md, GUI).
/// </summary>
internal static class SvgIcons
{
    /// <summary>The icon <paramref name="size"/> pixels square (scaled already).</summary>
    public static LoadedTexture Texture(ICoreClientAPI api, AssetLocation location, int size)
    {
        size = System.Math.Max(1, size);
        using var surface = new ImageSurface(Format.Argb32, size, size);
        api.Gui.DrawSvg(api.Assets.Get(location), surface, 0, 0, size, size);
        var texture = new LoadedTexture(api);
        // As GuiElement.generateTexture does it (vsapi GuiElement.cs:190), linear.
        api.Gui.LoadOrUpdateCairoTexture(surface, true, ref texture);
        return texture;
    }
}
