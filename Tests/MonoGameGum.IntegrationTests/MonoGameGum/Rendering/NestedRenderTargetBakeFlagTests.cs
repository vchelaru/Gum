using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.Forms;
using Gum.GueDeriving;
using MonoGameAndGum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #5677: <c>Renderer.IsBakingRenderTarget</c> is a single bool, so a bake that started inside
/// another bake would clear it for the outer bake's remaining children. Bakes run post-order in
/// PreRender and never nest, so a <c>SubtractAlpha</c> child drawn after a nested render-target
/// container in the outer bake must still punch its hole. Pins that, for SpriteBatch and Apos.Shapes.
/// </summary>
public class NestedRenderTargetBakeFlagTests : BaseTestClass
{
    private const int CaptureSize = 100;
    private static readonly XnaColor BackdropColor = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);
    private static readonly XnaColor FillColor = new XnaColor((byte)200, (byte)30, (byte)30, (byte)255);

    [Fact]
    public void SubtractAlphaRectangleAfterNestedRenderTarget_ShowsBackdropThroughHole()
    {
        XnaColor[] pixels = Render(useCircle: false);

        AssertHoleShowsBackdropAndRestIsFill(pixels);
    }

    [Fact]
    public void SubtractAlphaCircleAfterNestedRenderTarget_ShowsBackdropThroughHole()
    {
        XnaColor[] pixels = Render(useCircle: true);

        AssertHoleShowsBackdropAndRestIsFill(pixels);
    }

    private static void AssertHoleShowsBackdropAndRestIsFill(XnaColor[] pixels)
    {
        const int tolerance = 6;

        XnaColor hole = pixels[(50 * CaptureSize) + 50];
        System.Math.Abs(hole.R - BackdropColor.R).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(hole.G - BackdropColor.G).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(hole.B - BackdropColor.B).ShouldBeLessThanOrEqualTo(tolerance);

        XnaColor outsideHole = pixels[(5 * CaptureSize) + 5];
        System.Math.Abs(outsideHole.R - FillColor.R).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(outsideHole.G - FillColor.G).ShouldBeLessThanOrEqualTo(tolerance);
        System.Math.Abs(outsideHole.B - FillColor.B).ShouldBeLessThanOrEqualTo(tolerance);
    }

    private static XnaColor[] Render(bool useCircle)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime root = new();
        root.Width = CaptureSize;
        root.Height = CaptureSize;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
        root.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = BackdropColor });

        ContainerRuntime renderTarget = new();
        renderTarget.Width = CaptureSize;
        renderTarget.Height = CaptureSize;
        renderTarget.IsRenderTarget = true;

        // Inner render target, baked (and its flag reset) before the outer bake starts.
        ContainerRuntime inner = new();
        inner.Width = CaptureSize;
        inner.Height = CaptureSize;
        inner.IsRenderTarget = true;
        inner.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = FillColor });
        renderTarget.AddChild(inner);

        if (useCircle)
        {
            CircleRuntime circle = new();
            circle.X = 20;
            circle.Y = 20;
            circle.Width = 60;
            circle.Height = 60;
            circle.IsFilled = true;
            circle.StrokeWidth = 0;
            circle.Color = XnaColor.White;
            circle.Blend = Gum.RenderingLibrary.Blend.SubtractAlpha;
            renderTarget.AddChild(circle);
        }
        else
        {
            ColoredRectangleRuntime mask = new() { X = 20, Y = 20, Width = 60, Height = 60, Color = XnaColor.White };
            mask.Blend = Gum.RenderingLibrary.Blend.SubtractAlpha;
            renderTarget.AddChild(mask);
        }
#pragma warning restore CS0618
        root.AddChild(renderTarget);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        using RenderTarget2D capture = new(gd, CaptureSize, CaptureSize, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[CaptureSize * CaptureSize];
        capture.GetData(pixels);

        root.RemoveFromManagers();
        return pixels;
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this)
            {
                // Apos.Shapes uses an SM4 effect the default Reach profile can't load - #4403.
                GraphicsProfile = GraphicsProfile.HiDef,
            };
        }

        protected override void Initialize()
        {
            base.Initialize();
            Gum.GumService.Default.Initialize(this, DefaultVisualsVersion.V3);
            ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(XnaColor.Black);

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
