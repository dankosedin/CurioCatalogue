using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace CurioCatalogue.Gui;

/// <summary>
/// The album window's tabs (album view, A2: vertical, at the side, as the handbook's): vanilla's vertical tabs with a check
/// mark beside a complete tab. The mark is vanilla's SVG, not a "✓" in the name: the font may lack the glyph (A3b). Several
/// tabs may be lit at once (the tabs of every group on a page, <see cref="CurioCatalogue.Core.TabPaging.Shared"/>): the window
/// sets <see cref="GuiTab.Active"/> itself.
/// </summary>
public sealed class AlbumTabs : GuiElementVerticalTabs
{
    private const double CheckSize = 16;

    private bool[] complete;
    private LoadedTexture? check;

    public AlbumTabs(ICoreClientAPI capi, GuiTab[] tabs, ElementBounds bounds, Action<int, GuiTab> onTabClicked)
        // The fonts of vanilla's AddVerticalTabs (vsapi GuiElementVerticalTabs.cs:364#AddVerticalTabs) at 14, not 17: A2's size for tabs.
        : base(capi, tabs, CairoFont.WhiteDetailText(), CairoFont.WhiteDetailText().WithColor(GuiStyle.ActiveButtonTextColor), bounds,
            onTabClicked)
    {
        complete = new bool[tabs.Length];
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
    }

    public override void RenderInteractiveElements(float deltaTime)
    {
        base.RenderInteractiveElements(deltaTime);
        if (check == null) return;
        // Where the base draws the tabs: right-aligned to the bounds, one under another (GuiElementVerticalTabs.cs:210#RenderInteractiveElements).
        double spacing = scaled(unscaledTabSpacing), right = Bounds.renderX + (int)Bounds.InnerWidth, y = Bounds.renderY;
        for (int i = 0; i < tabs.Length; i++)
        {
            y += tabs[i].PaddingTop;
            if (complete[i])
            {
                api.Render.Render2DLoadedTexture(check, (float)(right - tabWidths[i] - check.Width - scaled(3)),
                    (float)(y + (tabHeight - check.Height) / 2));
            }
            y += tabHeight + spacing;
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        check?.Dispose();
    }
}
