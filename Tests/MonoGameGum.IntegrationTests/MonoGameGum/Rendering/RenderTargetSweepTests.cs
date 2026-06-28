using Microsoft.Xna.Framework;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Proves that single-layer <see cref="Renderer.Draw(SystemManagers, Layer)"/> runs the
/// frame-boundary render-target sweep so removed <see cref="ContainerRuntime.IsRenderTarget"/>
/// containers do not leak GPU targets (issue #3416).
/// </summary>
public class RenderTargetSweepTests : BaseTestClass
{
    [Fact]
    public void SingleLayerDraw_ShouldDisposeCachedRenderTarget_WhenRenderTargetContainerIsRemoved()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        Layer mainLayer = renderer.MainLayer;

        ContainerRuntime container = new();
        container.Width = 100;
        container.Height = 100;
        container.IsRenderTarget = true;

        TextRuntime text = new();
        text.Text = "Hello";
        container.AddChild(text);

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        IRenderableIpso owner = (IRenderableIpso)container.RenderableComponent;

        // FRB's default UI path: one Draw(MainLayer) per frame.
        renderer.Draw(managers, mainLayer);
        renderer.HasCachedRenderTarget(owner).ShouldBeTrue();

        container.RemoveFromManagers();

        // One frame after removal: sweep keeps the entry (marked used on the prior frame).
        renderer.Draw(managers, mainLayer);
        renderer.HasCachedRenderTarget(owner).ShouldBeTrue();

        // Next frame boundary: unused owner is swept.
        renderer.Draw(managers, mainLayer);
        renderer.HasCachedRenderTarget(owner).ShouldBeFalse();
    }

    [Fact]
    public void MultiLayerDraw_ShouldStillDisposeCachedRenderTarget_WhenRenderTargetContainerIsRemoved()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime container = new();
        container.Width = 100;
        container.Height = 100;
        container.IsRenderTarget = true;

        TextRuntime text = new();
        text.Text = "Hello";
        container.AddChild(text);

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        IRenderableIpso owner = (IRenderableIpso)container.RenderableComponent;

        renderer.Draw(managers);
        renderer.HasCachedRenderTarget(owner).ShouldBeTrue();

        container.RemoveFromManagers();

        renderer.Draw(managers);
        renderer.HasCachedRenderTarget(owner).ShouldBeTrue();

        renderer.Draw(managers);
        renderer.HasCachedRenderTarget(owner).ShouldBeFalse();
    }

    /// <summary>
    /// Minimal Game host that initializes a fresh <see cref="GumService"/> per test so
    /// <see cref="Renderer.Draw(SystemManagers, Layer)"/> can be invoked against a live device.
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
