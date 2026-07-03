using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// A Sprite's <see cref="SpriteRuntime.RenderTargetTextureSource"/> must resolve to the same
/// offscreen texture the source container itself bakes, even when the source is NESTED (added via
/// <c>AddChild</c>) rather than top-level (added via <c>AddToManagers</c>). Nested vs. top-level
/// containers are held as different concrete forms in the render tree — the
/// <see cref="Gum.Wireframe.GraphicalUiElement"/> wrapper vs. the raw contained renderable (#816)
/// — so the bake/composite path and the cross-reference lookup must key off the same resolved
/// cache owner or the reference silently misses.
/// </summary>
public class NestedRenderTargetTextureSourceTests : BaseTestClass
{
    [Fact]
    public void SpriteReferencingNestedRenderTargetContainer_ShouldDisplayBakedTexture()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        // The nested source lives in its own screen region (still inside the 128x128 capture used
        // by SampleMainLayerPixel below) so the sampled pixel can only show red if the SPRITE
        // actually rendered the referenced texture, not because the source's own on-screen
        // composite happens to occupy the same pixel.
        ContainerRuntime holder = new();
        holder.X = 64;
        holder.Y = 0;
        holder.Width = 64;
        holder.Height = 64;

        ContainerRuntime source = CreateRedRenderTargetContainer();
        holder.AddChild(source);
        holder.AddToManagers(managers, null);
        holder.UpdateLayout();

        SpriteRuntime sprite = new();
        sprite.X = 0;
        sprite.Y = 0;
        sprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = source;
        sprite.AddToManagers(managers, null);
        sprite.UpdateLayout();

        // Warm-up draw: on a fresh SystemManagers, SpriteRenderer.CurrentZoom (used to snap the
        // bake camera to whole pixels in RenderToRenderTarget) is only populated once a real
        // BeginSpriteBatch cycle has run, which happens during the *main compositing* pass after
        // baking — not during the bake pass itself. The very first Draw call therefore bakes with
        // an uninitialized zoom; a second draw re-bakes correctly. Every other render-target test
        // in this project already does this (either an explicit warm-up Draw, or the capture
        // helper drawing twice) — see CrossLayerRenderTargetTextureSourceTests.
        renderer.Draw(managers);

        AdvanceHostFrame(managers, ref _hostTime);

        Color sampled = SampleMainLayerPixel(gd, renderer, managers);

        sampled.R.ShouldBeGreaterThan((byte)150);
        ((int)sampled.R - sampled.G).ShouldBeGreaterThan(80);
    }

    private static double _hostTime;

    private static void AdvanceHostFrame(SystemManagers managers, ref double hostTime)
    {
        hostTime += 1.0;
        managers.Activity(hostTime);
    }

    private static ContainerRuntime CreateRedRenderTargetContainer()
    {
        ContainerRuntime container = new();
        container.Width = 64;
        container.Height = 64;
        container.IsRenderTarget = true;

#pragma warning disable CS0618
        ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
        red.Width = 64;
        red.Height = 64;
        red.Color = Color.Red;
        container.AddChild(red);

        return container;
    }

    private static Color SampleMainLayerPixel(GraphicsDevice gd, Renderer renderer, SystemManagers managers)
    {
        const int w = 128;
        const int h = 128;
        using RenderTarget2D capture = new(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        gd.SetRenderTarget(capture);
        gd.Clear(Color.Black);
        renderer.Draw(managers);
        gd.SetRenderTarget(null);

        Color[] pixels = new Color[w * h];
        capture.GetData(pixels);
        return pixels[(32 * w) + 32];
    }

    /// <summary>
    /// Minimal Game host that initializes a fresh <see cref="GumService"/> per test so
    /// <see cref="Renderer.Draw(SystemManagers)"/> can be invoked against a live device.
    /// </summary>
    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        public GumService GumService { get; }

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            GumService = new GumService();
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumService.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
