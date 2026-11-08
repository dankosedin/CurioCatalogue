using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace CurioCatalogue.Gui;

/// <summary>
/// A button with an icon instead of text that looks as the game's text buttons ("Make a want list"): their brown background,
/// shine and shade, the lighter face on hover and the darker one pressed (vsapi GuiElementTextButton.cs:102-258). The game's
/// own icon button (GuiElementToggleButton) draws another background, so a row of both read as two kinds of buttons (album view
/// plan, A4). The icon is drawn in the buttons' text colour over the button, inset as the game's icon button insets it
/// (GuiElementToggleButton.cs:102).
/// </summary>
internal sealed class IconTextButton : GuiElementTextButton
{
    private readonly string icon;
    private LoadedTexture iconTexture;

    public IconTextButton(ICoreClientAPI capi, string icon, ActionConsumable onClick, ElementBounds bounds)
        : base(capi, "", CairoFont.SmallButtonText(), CairoFont.SmallButtonText(), onClick, bounds, EnumButtonStyle.Normal)
    {
        this.icon = icon;
        iconTexture = new LoadedTexture(capi);
    }

    public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
    {
        base.ComposeElements(ctxStatic, surfaceStatic);
        using var surface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
        using var ctx = genContext(surface);
        double inset = scaled(4);
        api.Gui.Icons.DrawIcon(ctx, icon, inset, inset, Bounds.OuterWidth - 2 * inset, Bounds.OuterHeight - 2 * inset,
            GuiStyle.ButtonTextColor);
        generateTexture(surface, ref iconTexture);
    }

    public override void RenderInteractiveElements(float deltaTime)
    {
        base.RenderInteractiveElements(deltaTime);
        if (Visible) api.Render.Render2DTexturePremultipliedAlpha(iconTexture.TextureId, Bounds);
    }

    public override void Dispose()
    {
        base.Dispose();
        iconTexture.Dispose();
    }
}

internal static class IconTextButtonComposer
{
    /// <summary>An icon button that looks as the game's text buttons (<see cref="IconTextButton"/>).</summary>
    public static GuiComposer AddIconTextButton(this GuiComposer composer, string icon, ActionConsumable onClick, ElementBounds bounds,
        string? key = null)
    {
        if (!composer.Composed) composer.AddInteractiveElement(new IconTextButton(composer.Api, icon, onClick, bounds), key);
        return composer;
    }
}
