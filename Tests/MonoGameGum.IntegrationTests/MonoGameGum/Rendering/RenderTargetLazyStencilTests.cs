using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.Forms;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Content;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #5696: a render-target container's bake target only gets a stencil buffer when a baked
/// shape draws through it (MinAlpha / ReplaceAlpha bounds mask), and keeps it once it has one.
/// </summary>
public class RenderTargetLazyStencilTests : BaseTestClass
{
    private const int CaptureSize = 100;
    private static readonly XnaColor BackdropColor = new XnaColor((byte)70, (byte)70, (byte)90, (byte)255);
    private static readonly XnaColor FillColor = new XnaColor((byte)200, (byte)30, (byte)30, (byte)255);

    private sealed class Scene : System.IDisposable
    {
        public MinimalGame Game = new();
        public SystemManagers Managers = null!;
        public Renderer Renderer = null!;
        public ContainerRuntime Root = null!;
        public ContainerRuntime RenderTargetContainer = null!;
        public CircleRuntime Circle = null!;
        public RenderTarget2D Capture = null!;

        public Scene(Gum.RenderingLibrary.Blend circleBlend)
        {
            Game.RunOneFrame();
            Managers = SystemManagers.Default;
            Renderer = Managers.Renderer;
            Root = new();
            RenderTargetContainer = new();
            Circle = new();

            Root.Width = CaptureSize;
            Root.Height = CaptureSize;
#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill.
            Root.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = BackdropColor });
            RenderTargetContainer.Width = CaptureSize;
            RenderTargetContainer.Height = CaptureSize;
            RenderTargetContainer.IsRenderTarget = true;
            RenderTargetContainer.AddChild(new ColoredRectangleRuntime { Width = CaptureSize, Height = CaptureSize, Color = FillColor });
#pragma warning restore CS0618
            Circle.X = 20;
            Circle.Y = 20;
            Circle.Width = 60;
            Circle.Height = 60;
            Circle.IsFilled = true;
            Circle.StrokeWidth = 0;
            Circle.FillColor = XnaColor.White;
            Circle.Blend = circleBlend;
            RenderTargetContainer.AddChild(Circle);
            Root.AddChild(RenderTargetContainer);

            Root.AddToManagers(Managers, null);
            Root.UpdateLayout();

            Capture = new RenderTarget2D(Game.GraphicsDevice, CaptureSize, CaptureSize, false,
                SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }

        public RenderTarget2D? BakedTarget => Renderer.TryGetBakedRenderTargetFor(RenderTargetContainer);

        public XnaColor[] DrawFrames(int count)
        {
            GraphicsDevice gd = Game.GraphicsDevice;
            for (int i = 0; i < count; i++)
            {
                gd.SetRenderTarget(Capture);
                gd.Clear(XnaColor.Black);
                Renderer.Draw(Managers);
            }
            gd.SetRenderTarget(null);
            XnaColor[] pixels = new XnaColor[CaptureSize * CaptureSize];
            Capture.GetData(pixels);
            return pixels;
        }

        public void Dispose()
        {
            Root.RemoveFromManagers();
            Capture.Dispose();
            Game.Dispose();
        }
    }

    [Fact]
    public void NormalContainer_BakesToTargetWithoutStencil()
    {
        using Scene scene = new(Gum.RenderingLibrary.Blend.Normal);

        scene.DrawFrames(2);

        scene.BakedTarget.ShouldNotBeNull();
        scene.BakedTarget!.DepthStencilFormat.ShouldBe(DepthFormat.None);
    }

    [Theory]
    [InlineData(Gum.RenderingLibrary.Blend.MinAlpha)]
    [InlineData(Gum.RenderingLibrary.Blend.ReplaceAlpha)]
    public void ContainerWithAlphaMaskCircle_BakesToTargetWithStencil(Gum.RenderingLibrary.Blend blend)
    {
        using Scene scene = new(blend);

        scene.DrawFrames(2);

        scene.BakedTarget.ShouldNotBeNull();
        scene.BakedTarget!.DepthStencilFormat.ShouldBe(DepthFormat.Depth24Stencil8);
    }

    [Fact]
    public void ChildBlendBecomesMinAlphaAfterFirstBake_UpgradesTargetOnceAndMasksCorner()
    {
        using Scene scene = new(Gum.RenderingLibrary.Blend.Normal);
        XnaColor[] before = scene.DrawFrames(2);
        RenderTarget2D firstTarget = scene.BakedTarget!;
        firstTarget.DepthStencilFormat.ShouldBe(DepthFormat.None);
        before[(22 * CaptureSize) + 22].R.ShouldBe(FillColor.R);

        scene.Circle.Blend = Gum.RenderingLibrary.Blend.MinAlpha;
        XnaColor[] after = scene.DrawFrames(2);
        RenderTarget2D upgraded = scene.BakedTarget!;

        upgraded.ShouldNotBeSameAs(firstTarget);
        firstTarget.IsDisposed.ShouldBeTrue();
        upgraded.DepthStencilFormat.ShouldBe(DepthFormat.Depth24Stencil8);
        // Corner of the bounds outside the circle is erased, so the backdrop shows through.
        System.Math.Abs(after[(22 * CaptureSize) + 22].R - BackdropColor.R).ShouldBeLessThanOrEqualTo(8);
        System.Math.Abs(after[(50 * CaptureSize) + 50].R - FillColor.R).ShouldBeLessThanOrEqualTo(8);

        scene.DrawFrames(3);
        scene.BakedTarget.ShouldBeSameAs(upgraded);
    }

    [Fact]
    public void StencilTarget_KeepsStencilWhenMaskBlendIsRemoved_WithoutRecreating()
    {
        using Scene scene = new(Gum.RenderingLibrary.Blend.MinAlpha);
        scene.DrawFrames(2);
        RenderTarget2D target = scene.BakedTarget!;

        scene.Circle.Blend = Gum.RenderingLibrary.Blend.Normal;
        scene.DrawFrames(3);

        scene.BakedTarget.ShouldBeSameAs(target);
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
