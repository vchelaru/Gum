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
/// Pins MonoGame's reference behavior for a render-target container with no valid baked texture —
/// one clamped to a non-positive size because it is degenerate (0x0) or entirely off-camera. Its
/// subtree renders NOTHING: the draw-list builder never recurses into render-target children
/// (<see cref="HierarchicalOrderer"/>'s <c>!IsRenderTarget</c> gate) and both composite sites skip
/// when <c>GetRenderTargetFor</c> returns null. This is the *verified* behavior raylib converges
/// onto in issue #3478 (raylib previously fell through and drew the children directly, unclamped) —
/// so the raylib <c>RenderTargetTests.Draw_DegenerateSizeRenderTarget_RendersNothing</c> is pinned
/// against something real here, not reasoned about (the #3475 tunnel-vision lesson).
/// </summary>
public class OffCameraRenderTargetConvergenceTests : BaseTestClass
{
    [Fact]
    public void NestedDegenerateRenderTarget_RendersNothing()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        // outer (100x100 RT) -> inner (RT) -> green child (100x100). The inner RT's size is flipped
        // between the two phases below; everything else stays identical.
        ContainerRuntime outer = new();
        outer.X = 0;
        outer.Y = 0;
        outer.Width = 100;
        outer.Height = 100;
        outer.IsRenderTarget = true;

        ContainerRuntime inner = new();
        inner.X = 0;
        inner.Y = 0;
        inner.Width = 100;
        inner.Height = 100;
        inner.IsRenderTarget = true;

#pragma warning disable CS0618
        ColoredRectangleRuntime green = new();
#pragma warning restore CS0618
        green.X = 0;
        green.Y = 0;
        green.Width = 100;
        green.Height = 100;
        green.Color = new Color(0, 255, 0);
        inner.AddChild(green);
        outer.AddChild(inner);
        outer.AddToManagers(managers, null);
        outer.UpdateLayout();

        // Positive control: with the inner RT sized 100x100, its green child composites up through
        // the outer RT to the screen, so the sampled pixel is green. This proves the sampler can see
        // the child at all — without it the "renders nothing" assertion below could pass vacuously.
        Color withValidInner = SampleAfterWarmup(gd, renderer, managers);
        withValidInner.G.ShouldBeGreaterThan((byte)150);
        ((int)withValidInner.G - withValidInner.R).ShouldBeGreaterThan(80);

        // Degenerate the inner RT: a 0x0 clamp bakes no texture and composites nothing. Its green
        // child must NOT appear — the sampled pixel stays the cleared-to-black background.
        inner.Width = 0;
        inner.Height = 0;
        outer.UpdateLayout();

        Color withDegenerateInner = SampleAfterWarmup(gd, renderer, managers);
        withDegenerateInner.G.ShouldBeLessThan((byte)50);

        outer.RemoveFromManagers();
    }

    private static double _hostTime;

    private static void AdvanceHostFrame(SystemManagers managers, ref double hostTime)
    {
        hostTime += 1.0;
        managers.Activity(hostTime);
    }

    // Warm-up draw (a fresh SystemManagers bakes with an uninitialized zoom on the first Draw — see
    // NestedRenderTargetTextureSourceTests) followed by a captured main-layer sample.
    private static Color SampleAfterWarmup(GraphicsDevice gd, Renderer renderer, SystemManagers managers)
    {
        renderer.Draw(managers);
        AdvanceHostFrame(managers, ref _hostTime);
        return SampleMainLayerPixel(gd, renderer, managers);
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
        // Center of the 100x100 outer render target (which sits at screen 0,0).
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
