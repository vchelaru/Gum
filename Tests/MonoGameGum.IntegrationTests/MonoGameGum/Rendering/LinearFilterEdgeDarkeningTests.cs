using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Reproduces and pins the fix for issue #3691: a texture whose transparent texels are black
/// (RGB 0,0,0 — the shape of a KernSmith-baked font atlas) darkens at its anti-aliased edges when
/// sampled with <see cref="TextureFilter.Linear"/> under the default non-premultiplied pipeline.
/// Bilinear sampling interpolates the transparent-black texel's RGB toward the opaque glyph color,
/// so the edge blends toward gray instead of staying the glyph color — a dark fringe over light
/// backgrounds. <see cref="TextureEdgeBleed"/>, run at load time, fixes it.
///
/// The repro is a 2x1 texture (opaque white | transparent black) saved to PNG and loaded back
/// through the real <see cref="LoaderManager"/> path, stretched large over an opaque white
/// background. With the fix the whole draw stays white; without it a gray band appears down the
/// middle where linear sampling crosses the opaque/transparent boundary.
/// </summary>
public class LinearFilterEdgeDarkeningTests : BaseTestClass
{
    public override void Dispose()
    {
        Renderer.TextureFilter = TextureFilter.Point;
        Renderer.BleedTransparentTextureEdgesOnLoad = true;
        base.Dispose();
    }

    [Fact]
    public void LinearFilteredSprite_WithBlackTransparentTexels_StaysWhite_WhenEdgeBleedEnabled()
    {
        Renderer.BleedTransparentTextureEdgesOnLoad = true;

        XnaColor sampled = SampleMidpointOfLinearWhiteSpriteOverWhite();

        // With the load-time edge bleed, the sprite over white stays white everywhere.
        sampled.R.ShouldBeGreaterThanOrEqualTo((byte)250);
        sampled.G.ShouldBeGreaterThanOrEqualTo((byte)250);
        sampled.B.ShouldBeGreaterThanOrEqualTo((byte)250);
    }

    [Fact]
    public void LinearFilteredSprite_WithBlackTransparentTexels_DarkensEdge_WhenEdgeBleedDisabled()
    {
        Renderer.BleedTransparentTextureEdgesOnLoad = false;

        XnaColor sampled = SampleMidpointOfLinearWhiteSpriteOverWhite();

        // Without the bleed the midpoint is dragged toward gray (~191): the fringe the fix removes.
        sampled.R.ShouldBeLessThan((byte)210);
    }

    /// <summary>
    /// Loads a 2x1 (opaque white | transparent black) PNG through the real LoaderManager, stretches
    /// it to 100x100 over an opaque white background with Linear filtering, and returns the pixel at
    /// the horizontal midpoint — where linear sampling is halfway between the two texels (the worst
    /// case for the fringe).
    /// </summary>
    private static XnaColor SampleMidpointOfLinearWhiteSpriteOverWhite()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        Renderer.TextureFilter = TextureFilter.Linear;

        // Save the texture to a PNG and load it back through the real LoaderManager/ContentLoader so
        // the load-time edge bleed (the fix) is actually exercised — building the texture directly
        // with SetData would bypass the loader.
        string pngPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + ".png");
        using (Texture2D source = new(gd, 2, 1, false, SurfaceFormat.Color))
        {
            source.SetData(new[]
            {
                new XnaColor((byte)255, (byte)255, (byte)255, (byte)255),
                new XnaColor((byte)0,   (byte)0,   (byte)0,   (byte)0),
            });
            using System.IO.FileStream fs = System.IO.File.Create(pngPath);
            source.SaveAsPng(fs, 2, 1);
        }

        Texture2D texture = LoaderManager.Self.LoadContent<Texture2D>(pngPath);

        ContainerRuntime root = new();
        root.X = 0;
        root.Y = 0;
        root.Width = 100;
        root.Height = 100;

#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest opaque solid fill.
        root.AddChild(new ColoredRectangleRuntime
        {
            Width = 100,
            Height = 100,
            Color = new XnaColor((byte)255, (byte)255, (byte)255, (byte)255),
        });
#pragma warning restore CS0618

        SpriteRuntime sprite = new();
        sprite.Texture = texture;
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.Width = 100;
        sprite.Height = 100;
        root.AddChild(sprite);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        XnaColor sampled = RenderToCaptureAndSample(gd, renderer, managers, 100, 100, 50, 50);

        root.RemoveFromManagers();
        try { System.IO.File.Delete(pngPath); } catch { /* best effort */ }

        return sampled;
    }

    private static XnaColor RenderToCaptureAndSample(GraphicsDevice gd, Renderer renderer, SystemManagers managers, int w, int h, int sx, int sy)
    {
        using RenderTarget2D capture = new(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] data = new XnaColor[w * h];
        capture.GetData(data);

        return data[(sy * w) + sx];
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
        }

        protected override void Initialize()
        {
            base.Initialize();
            Gum.GumService.Default.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(XnaColor.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
