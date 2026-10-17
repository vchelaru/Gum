using KernSmith.Gum;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Fonts;

/// <summary>
/// End-to-end regression for issue #4061: the shadow AtlasVariant KernSmith generates for a
/// dropshadow font must be attached as <see cref="BitmapFont.ShadowFont"/>, not silently discarded.
/// Mirrors RaylibGum.Tests's KernSmithRaylibFontCreatorTests.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class KernSmithFontCreatorTests : BaseTestClass
{
    [Fact]
    public void TryCreateFont_WithDropshadow_AttachesShadowFont()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        KernSmithFontCreator creator = new KernSmithFontCreator(game.GraphicsDevice);

        BmfcSave bmfcSave = new BmfcSave
        {
            FontName = "Arial",
            FontSize = 32,
            UseSmoothing = true,
            Ranges = "65",
            HasDropshadow = true,
            DropshadowOffsetX = 2f,
            DropshadowOffsetY = 2f,
            DropshadowBlur = 2f,
            DropshadowAlpha = 255,
        };

        BitmapFont? font = creator.TryCreateFont(bmfcSave);

        font.ShouldNotBeNull();
        font!.ShadowFont.ShouldNotBeNull();
        font.ShadowFont!.Characters.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TryCreateFont_WithoutDropshadow_DoesNotAttachShadowFont()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        KernSmithFontCreator creator = new KernSmithFontCreator(game.GraphicsDevice);

        BmfcSave bmfcSave = new BmfcSave
        {
            FontName = "Arial",
            FontSize = 32,
            UseSmoothing = true,
            Ranges = "65",
        };

        BitmapFont? font = creator.TryCreateFont(bmfcSave);

        font.ShouldNotBeNull();
        font!.ShadowFont.ShouldBeNull();
    }

    /// <summary>
    /// Minimal host providing a live GraphicsDevice, since KernSmithFontCreator uploads
    /// generated atlas pages to real Texture2D instances.
    /// </summary>
    private sealed class MinimalGame : SharedGameLease
    {
    }
}
