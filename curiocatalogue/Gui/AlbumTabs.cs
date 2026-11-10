using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CurioCatalogue.Gui;

/// <summary>
/// The album window's tabs (album view, A2: vertical, at the side, as the handbook's): vanilla's vertical tabs with a check
/// mark beside a complete tab, and the tab's icon before its name (L2c: an ore's nugget, a rock's stone). The mark is vanilla's
/// SVG, not a "✓" in the name: the font may lack the glyph (A3b). The window lights the tab of the page shown by
/// <see cref="GuiTab.Active"/>.
/// </summary>
public sealed class AlbumTabs : GuiElementVerticalTabs
{
    private const double CheckSize = 16, IconSize = 18, IconGap = 4;
    private const double BasePadding = 3; // the base's unscaledTabPadding (GuiElementVerticalTabs.cs:19)
    private const float ItemZ = 90; // over the tabs' textures (z 50), as the page's items

    private bool[] complete;
    private LoadedTexture? check;
    private readonly ItemSlot?[] slots;
    private readonly AssetLocation?[] svgs;
    private readonly LoadedTexture?[] marks;

    /// <param name="icons">Per tab: an <see cref="ItemStack"/>, an SVG's <see cref="AssetLocation"/> or null
    /// (<see cref="Adapters.DefinitionIcons.Find"/>).</param>
    public AlbumTabs(ICoreClientAPI capi, GuiTab[] tabs, IReadOnlyList<object?> icons, ElementBounds bounds, Action<int, GuiTab> onTabClicked)
        // The fonts of vanilla's AddVerticalTabs (vsapi GuiElementVerticalTabs.cs:364#AddVerticalTabs) at 14, not 17: A2's size for tabs.
        : base(capi, tabs, CairoFont.WhiteDetailText(), CairoFont.WhiteDetailText().WithColor(GuiStyle.ActiveButtonTextColor), bounds,
            onTabClicked)
    {
        complete = new bool[tabs.Length];
        slots = tabs.Select((_, i) => i < icons.Count && icons[i] is ItemStack stack ? new DummySlot(stack) : null).ToArray<ItemSlot?>();
        svgs = tabs.Select((_, i) => i < icons.Count ? icons[i] as AssetLocation : null).ToArray();
        marks = new LoadedTexture?[tabs.Length];
        // The room of the icon. The base sizes a tab by its name's ink and the padding on both sides, and draws the name after the
        // left padding (GuiElementVerticalTabs.cs:76, :121); leading spaces move the ink but are no ink (Cairo's text extents).
        // So half the room is spaces and half more padding: the name starts after the icon, the right margin stays the padding.
        if (slots.Any(s => s != null) || svgs.Any(s => s != null))
        {
            double space = Math.Max(1, Font.GetTextExtents("  ").XAdvance / 2);
            int count = (int)Math.Ceiling(scaled(IconSize + IconGap) / 2 / space);
            unscaledTabPadding = BasePadding + count * space / scaled(1);
            string indent = new(' ', count);
            foreach (var tab in tabs) tab.Name = indent + tab.Name;
        }
    }

    /// <summary>Which tabs show the check mark; read on every frame, so no composing.</summary>
    public void SetComplete(IReadOnlyList<bool> isComplete)
    {
        for (int i = 0; i < complete.Length && i < isComplete.Count; i++) complete[i] = isComplete[i];
    }

    public override void ComposeTextElements(Cairo.Context ctxStatic, Cairo.ImageSurface surfaceStatic)
    {
        base.ComposeTextElements(ctxStatic, surfaceStatic);
        int size = (int)scaled(CheckSize);
        check?.Dispose();
        check = api.Gui.LoadSvg(new AssetLocation("game:textures/icons/checkmark.svg"), size, size, size, size,
            ColorUtil.ToRgba(255, 132, 255, 132));
        for (int i = 0; i < marks.Length; i++)
        {
            marks[i]?.Dispose();
            marks[i] = svgs[i] is { } svg ? SvgIcons.Texture(api, svg, (int)scaled(IconSize)) : null;
        }
    }

    public override void RenderInteractiveElements(float deltaTime)
    {
        base.RenderInteractiveElements(deltaTime);
        // Where the base draws the tabs: right-aligned to the bounds, one under another, the name from the tab's left edge plus
        // the padding (GuiElementVerticalTabs.cs:210#RenderInteractiveElements, :121).
        double spacing = scaled(unscaledTabSpacing), right = Bounds.renderX + (int)Bounds.InnerWidth, y = Bounds.renderY;
        double icon = scaled(IconSize);
        for (int i = 0; i < tabs.Length; i++)
        {
            y += tabs[i].PaddingTop;
            if (complete[i] && check != null)
            {
                api.Render.Render2DLoadedTexture(check, (float)(right - tabWidths[i] - check.Width - scaled(3)),
                    (float)(y + (tabHeight - check.Height) / 2));
            }
            double ix = right - tabWidths[i] + scaled(BasePadding + 1), iy = y + (tabHeight - icon) / 2;
            if (slots[i] is { } slot)
            {
                api.Render.RenderItemstackToGui(slot, ix + icon / 2, iy + icon / 2, ItemZ, (float)icon, ColorUtil.WhiteArgb, deltaTime,
                    showStackSize: false);
            }
            else if (marks[i] is { } mark)
            {
                api.Render.Render2DLoadedTexture(mark, (float)Math.Round(ix + (icon - mark.Width) / 2), (float)Math.Round(iy + (icon - mark.Height) / 2));
            }
            y += tabHeight + spacing;
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        check?.Dispose();
        foreach (var mark in marks) mark?.Dispose();
    }
}
