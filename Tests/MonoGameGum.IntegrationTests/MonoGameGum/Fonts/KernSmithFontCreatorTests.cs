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

    // Issue #4309: KernSmith 0.20.0 exposes per-glyph design-unit metrics (FontInfo.DesignMetrics)
    // without rasterizing any glyphs -- the "measurement font" source of truth this creator's
    // TryGetDesignMetrics wires up. Uses an explicit .ttf file path (BmfcSave.FontFile) rather than
    // a system font family, since KernSmith.ReadFontInfo has no family-name-resolution overload yet
    // (known gap -- TryGetDesignMetrics_ForSystemFontFamily_ReturnsNull below covers that fallback).
    private static readonly string TestFontPath = System.IO.Path.GetFullPath(
        System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkiaGum.Tests", "Assets", "Fonts", "TestFont.ttf"));

    [Fact]
    public void TryGetDesignMetrics_WithExplicitFontFile_ReturnsUnscaledPerGlyphMetrics()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        KernSmithFontCreator creator = new KernSmithFontCreator(game.GraphicsDevice);

        BmfcSave bmfcSave = new BmfcSave
        {
            FontFile = TestFontPath,
            FontName = "TestFont",
            FontSize = 32,
            Ranges = "65-90",
        };

        FontDesignMetrics? designMetrics = creator.TryGetDesignMetrics(bmfcSave);

        designMetrics.ShouldNotBeNull();
        designMetrics!.UnitsPerEm.ShouldBeGreaterThan(0);
        designMetrics.LineHeight.ShouldBeGreaterThan(0);
        designMetrics.GlyphMetrics.ContainsKey((int)'A').ShouldBeTrue();
        designMetrics.GlyphMetrics[(int)'A'].AdvanceWidth.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TryGetDesignMetrics_ForSystemFontFamily_ReturnsNull()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        KernSmithFontCreator creator = new KernSmithFontCreator(game.GraphicsDevice);

        // No FontFile -- a plain system-installed family name, like "Arial". KernSmith's
        // ReadFontInfo has no family-name overload (only GenerateFromSystem does), so this is
        // expected to gracefully return null rather than throw -- callers fall back to measuring
        // against the rasterized BitmapFont instead.
        BmfcSave bmfcSave = new BmfcSave
        {
            FontName = "Arial",
            FontSize = 32,
            Ranges = "65",
        };

        FontDesignMetrics? designMetrics = creator.TryGetDesignMetrics(bmfcSave);

        designMetrics.ShouldBeNull();
    }

    /// <summary>
    /// Minimal Game host providing a live GraphicsDevice, since KernSmithFontCreator uploads
    /// generated atlas pages to real Texture2D instances.
    /// </summary>
    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
