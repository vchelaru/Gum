using System.Linq;
using Microsoft.Xna.Framework;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// End-to-end proof of the off-screen render cull (#2998): renders one real frame of a clipped
/// tree (a parent clip container holding several child clip containers, each wrapping a Text) and
/// compares the number of SpriteBatch state changes with the cull off vs on. Children scrolled
/// outside the parent's clip rect should contribute no state changes when culling is enabled.
/// </summary>
public class OffscreenCullRenderTests : BaseTestClass
{
    [Fact]
    public void Cull_ReducesSpriteBatchStateChanges_ForOffscreenClippedChildren()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        // Parent clips a 200px-tall band. Six child clip containers are stacked every 100px, so
        // two are inside the band and four are scrolled off below it (the cull targets).
        ContainerRuntime parent = new();
        parent.Width = 200;
        parent.Height = 200;
        parent.ClipsChildren = true;

        for (int i = 0; i < 6; i++)
        {
            ContainerRuntime childClip = new();
            childClip.Width = 180;
            childClip.Height = 80;
            childClip.Y = i * 100;   // 0, 100 inside; 200, 300, 400, 500 below the 200px band
            childClip.ClipsChildren = true;

            TextRuntime text = new();
            text.Text = "Item " + i;
            childClip.AddChild(text);

            parent.AddChild(childClip);
        }

        parent.AddToManagers(managers, null);
        parent.UpdateLayout();

        try
        {
            // Warm up: the first Draw after RunOneFrame does one-time work (font loads, render-target
            // setup) that inflates its state-change count. Discard it so the measured pair below
            // differ only by the cull flag, not by first-frame warmup.
            CameraScissorExtensions.CullOffscreenWhenClipped = false;
            renderer.Draw(managers);

            CameraScissorExtensions.CullOffscreenWhenClipped = false;
            renderer.Draw(managers);
            int withoutCull = renderer.SpriteRenderer.LastFrameDrawStates.Count();

            CameraScissorExtensions.CullOffscreenWhenClipped = true;
            renderer.Draw(managers);
            int withCull = renderer.SpriteRenderer.LastFrameDrawStates.Count();

            withCull.ShouldBeLessThan(withoutCull);
        }
        finally
        {
            CameraScissorExtensions.CullOffscreenWhenClipped = true;
        }
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
