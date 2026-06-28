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
/// Proves invisible <see cref="ContainerRuntime.IsRenderTarget"/> containers skip the bake pass
/// unless a visible <see cref="IRenderTargetTextureReferencer"/> references them (issue #1643).
/// </summary>
public class InvisibleRenderTargetOptimizationTests : BaseTestClass
{
    [Fact]
    public void InvisibleUnreferencedRenderTarget_ShouldNotBake()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime container = CreateRedRenderTargetContainer();
        container.Visible = false;
        container.AddToManagers(managers, null);
        container.UpdateLayout();

        IRenderableIpso cacheOwner = (IRenderableIpso)container.RenderableComponent;

        AdvanceHostFrame(managers, ref _hostTime);
        renderer.Draw(managers);

        renderer.HasCachedRenderTarget(cacheOwner).ShouldBeFalse();
    }

    [Fact]
    public void InvisibleReferencedRenderTarget_ShouldBake()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime container = CreateRedRenderTargetContainer();
        container.Visible = false;
        container.AddToManagers(managers, null);
        container.UpdateLayout();

        IRenderableIpso cacheOwner = (IRenderableIpso)container.RenderableComponent;

        SpriteRuntime sprite = new();
        sprite.X = 0;
        sprite.Y = 0;
        sprite.Width = 64;
        sprite.Height = 64;
        sprite.RenderTargetTextureSource = container;
        sprite.AddToManagers(managers, null);
        sprite.UpdateLayout();

        AdvanceHostFrame(managers, ref _hostTime);
        renderer.Draw(managers);

        renderer.HasCachedRenderTarget(cacheOwner).ShouldBeTrue();

        Color sampled = SampleMainLayerPixel(gd, renderer, managers);
        sampled.R.ShouldBeGreaterThan((byte)150);
        ((int)sampled.R - sampled.G).ShouldBeGreaterThan(80);
    }

    [Fact]
    public void VisibleUnreferencedRenderTarget_ShouldStillBake()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime container = CreateRedRenderTargetContainer();
        container.AddToManagers(managers, null);
        container.UpdateLayout();

        IRenderableIpso cacheOwner = (IRenderableIpso)container.RenderableComponent;

        AdvanceHostFrame(managers, ref _hostTime);
        renderer.Draw(managers);

        renderer.HasCachedRenderTarget(cacheOwner).ShouldBeTrue();
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
        return pixels[32 * w + 32];
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
