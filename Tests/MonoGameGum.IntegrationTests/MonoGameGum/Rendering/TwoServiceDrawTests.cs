using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Two <see cref="global::Gum.GumService"/> instances in one process (#6066): each service's
/// Draw must render its own tree, not the tree of whichever service initialized last.
/// </summary>
public class TwoServiceDrawTests : BaseTestClass
{
    private const int CaptureWidth = 400;
    private const int CaptureHeight = 300;

    private static RectangleRuntime AddRectangle(global::Gum.GumService service, int x, int y, byte red, byte blue)
    {
        RectangleRuntime rectangle = new();
        rectangle.X = x;
        rectangle.Y = y;
        rectangle.Width = 100;
        rectangle.Height = 50;
        rectangle.IsFilled = true;
        rectangle.FillColor = new XnaColor(red, (byte)0, blue);
        service.Root.AddChild(rectangle);
        return rectangle;
    }

    [Fact]
    public void ServiceDraw_OnFirstService_ShouldDrawOnlyItsOwnTree()
    {
        using TwoServiceGame game = new();
        game.RunOneFrame();

        // Red belongs to service A, blue to service B (the last service initialized).
        AddRectangle(game.ServiceA, x: 20, y: 20, red: 255, blue: 0);
        AddRectangle(game.ServiceB, x: 200, y: 100, red: 0, blue: 255);

        GraphicsDevice gd = game.GraphicsDevice;
        using RenderTarget2D capture = new(gd, CaptureWidth, CaptureHeight, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        gd.SetRenderTarget(capture);
        gd.Clear(XnaColor.Black);
        game.ServiceA.Draw();
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(pixels);

        XnaColor insideA = pixels[45 * CaptureWidth + 70];
        XnaColor insideB = pixels[125 * CaptureWidth + 250];
        insideA.R.ShouldBe((byte)255, "A.Draw() should draw A's red rectangle");
        insideB.B.ShouldBe((byte)0, "A.Draw() should not draw B's blue rectangle");
    }

    [Fact]
    public void ServiceDraw_OnFirstService_ShouldBakeItsRenderTargetThroughItsOwnRenderer()
    {
        using TwoServiceGame game = new();
        game.RunOneFrame();

        ContainerRuntime container = new();
        container.X = 20;
        container.Y = 20;
        container.Width = 100;
        container.Height = 50;
        container.IsRenderTarget = true;
        game.ServiceA.Root.AddChild(container);
        RectangleRuntime rectangle = new();
        rectangle.Width = 0;
        rectangle.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        rectangle.Height = 0;
        rectangle.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        rectangle.IsFilled = true;
        rectangle.FillColor = new XnaColor((byte)255, (byte)0, (byte)0);
        container.AddChild(rectangle);

        // A different camera on B, so a bake that goes through B's managers lands elsewhere.
        game.ServiceB.SystemManagers.Renderer.Camera.Zoom = 2;
        Camera cameraB = game.ServiceB.SystemManagers.Renderer.Camera;
        int clientWidthBBefore = cameraB.ClientWidth;

        GraphicsDevice gd = game.GraphicsDevice;
        using RenderTarget2D capture = new(gd, CaptureWidth, CaptureHeight, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        gd.SetRenderTarget(capture);
        gd.Clear(XnaColor.Black);
        game.ServiceA.Draw();
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(pixels);

        pixels[45 * CaptureWidth + 70].R.ShouldBe((byte)255, "A's baked render-target container should draw red");
        cameraB.ClientWidth.ShouldBe(clientWidthBBefore, "baking A's render target should not touch B's camera");
    }

    private sealed class TwoServiceGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public global::Gum.GumService ServiceA { get; } = new();
        public global::Gum.GumService ServiceB { get; } = new();

        public TwoServiceGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
        }

        protected override void Initialize()
        {
            base.Initialize();
            ServiceA.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);
            ServiceB.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(XnaColor.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (ServiceB.IsInitialized) { ServiceB.Uninitialize(); }
            if (ServiceA.IsInitialized) { ServiceA.Uninitialize(); }
            LoaderManager.Self?.DisposeAndClear();
            SystemManagers.Default = null!;
            ISystemManagers.Default = null!;
            IGumService.Default = null;
            base.Dispose(disposing);
        }
    }
}
