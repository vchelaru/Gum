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
/// Proves per-layer <see cref="Renderer.Draw(SystemManagers, Layer)"/> bakes render targets on
/// other layers before compositing a sprite that references them via
/// <see cref="SpriteRuntime.RenderTargetTextureSource"/> (issue #3417).
/// </summary>
public class CrossLayerRenderTargetTextureSourceTests : BaseTestClass
{
    [Fact]
    public void SameLayerDraw_ShouldShowRenderTargetTextureSourceOnSprite()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime container = CreateRedRenderTargetContainer();
        container.AddToManagers(managers, null);
        container.UpdateLayout();

        IRenderableIpso cacheOwner = (IRenderableIpso)container.RenderableComponent;
        renderer.Draw(managers);
        renderer.HasCachedRenderTarget(cacheOwner).ShouldBeTrue();

        SpriteRuntime sprite = new();
        sprite.X = 0;
        sprite.Y = 0;
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = container;
        sprite.AddToManagers(managers, null);
        sprite.UpdateLayout();

        AdvanceHostFrame(managers, ref _hostTime);

        Color sampled = RenderLayersToCapture(
            gd, renderer, managers, new List<Layer> { renderer.MainLayer });

        sampled.R.ShouldBeGreaterThan((byte)150);
        ((int)sampled.R - sampled.G).ShouldBeGreaterThan(80);
    }

    [Fact]
    public void MultiLayerListDraw_ShouldShowCrossLayerRenderTargetReference()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        Layer sourceLayer = renderer.MainLayer;
        Layer consumerLayer = renderer.AddLayer();

        ContainerRuntime container = CreateRedRenderTargetContainer();
        container.AddToManagers(managers, sourceLayer);
        container.UpdateLayout();

        SpriteRuntime sprite = new();
        sprite.X = 0;
        sprite.Y = 0;
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = container;
        sprite.AddToManagers(managers, consumerLayer);
        sprite.UpdateLayout();

        AdvanceHostFrame(managers, ref _hostTime);

        Color sampled = RenderLayersToCapture(
            gd, renderer, managers, new List<Layer> { sourceLayer, consumerLayer });

        sampled.R.ShouldBeGreaterThan((byte)150);
        ((int)sampled.R - sampled.G).ShouldBeGreaterThan(80);
    }

    [Fact]
    public void SingleLayerDraw_ShouldShowSourceLayerRenderTarget_WhenDrawnBeforeConsumerLayer()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        Layer sourceLayer = renderer.MainLayer;
        Layer consumerLayer = renderer.AddLayer();

        ContainerRuntime container = CreateRedRenderTargetContainer();
        container.AddToManagers(managers, sourceLayer);
        container.UpdateLayout();

        SpriteRuntime sprite = new();
        sprite.X = 0;
        sprite.Y = 0;
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = container;
        sprite.AddToManagers(managers, consumerLayer);
        sprite.UpdateLayout();

        AdvanceHostFrame(managers, ref _hostTime);

        Color sampled = RenderLayerToCapture(gd, renderer, managers, sourceLayer, consumerLayer);

        sampled.R.ShouldBeGreaterThan((byte)150);
        ((int)sampled.R - sampled.G).ShouldBeGreaterThan(80);
    }

    [Fact]
    public void SingleLayerDraw_ShouldShowSourceLayerRenderTarget_WhenConsumerLayerDrawnFirst()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        Layer sourceLayer = renderer.MainLayer;
        Layer consumerLayer = renderer.AddLayer();

        ContainerRuntime container = CreateRedRenderTargetContainer();
        container.AddToManagers(managers, sourceLayer);
        container.UpdateLayout();

        SpriteRuntime sprite = new();
        sprite.X = 0;
        sprite.Y = 0;
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = container;
        sprite.AddToManagers(managers, consumerLayer);
        sprite.UpdateLayout();

        AdvanceHostFrame(managers, ref _hostTime);

        Color sampled = RenderLayerToCapture(gd, renderer, managers, consumerLayer, sourceLayer);

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
        container.X = 0;
        container.Y = 0;
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

    private static Color RenderLayersToCapture(
        GraphicsDevice gd,
        Renderer renderer,
        SystemManagers managers,
        List<Layer> layers)
    {
        const int w = 128;
        const int h = 128;
        using RenderTarget2D capture = new(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        gd.SetRenderTarget(capture);
        gd.Clear(Color.Black);
        for (int i = 0; i < 2; i++)
        {
            renderer.Draw(managers, layers);
        }
        gd.SetRenderTarget(null);

        Color[] data = new Color[w * h];
        capture.GetData(data);

        return data[(20 * w) + 20];
    }

    private static Color RenderLayerToCapture(
        GraphicsDevice gd,
        Renderer renderer,
        SystemManagers managers,
        params Layer[] drawOrder)
    {
        const int w = 128;
        const int h = 128;
        using RenderTarget2D capture = new(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        gd.SetRenderTarget(capture);
        gd.Clear(Color.Black);

        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < drawOrder.Length; j++)
            {
                renderer.Draw(managers, drawOrder[j]);
            }
        }

        gd.SetRenderTarget(null);

        Color[] data = new Color[w * h];
        capture.GetData(data);

        return data[(20 * w) + 20];
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        public GumService GumService { get; }

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            GumService = new GumService();
            _hostTime = 0;
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
