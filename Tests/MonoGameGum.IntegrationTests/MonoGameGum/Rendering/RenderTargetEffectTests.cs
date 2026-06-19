using System.Linq;
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
/// Proves that a render-target container's <see cref="ContainerRuntime.RenderTargetEffect"/> is
/// bound to the SpriteBatch when the container's cached texture is blitted back to the screen
/// (issue #816). A real <see cref="BasicEffect"/> stands in for a user post-process shader so the
/// binding can be verified without shipping a compiled .fx in the test project.
/// </summary>
public class RenderTargetEffectTests : BaseTestClass
{
    [Fact]
    public void RenderTargetEffect_ShouldBeBound_WhenRenderTargetContainerIsDrawn()
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

        BasicEffect effect = new BasicEffect(game.GraphicsDevice);
        container.RenderTargetEffect = effect;

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        try
        {
            // First Draw does one-time render-target setup; draw twice so the effect-bound blit
            // is captured in LastFrameDrawStates on a steady-state frame.
            renderer.Draw(managers);
            renderer.Draw(managers);

            bool effectWasBound = renderer.SpriteRenderer.LastFrameDrawStates
                .Any(state => ReferenceEquals(state.Effect, effect));

            effectWasBound.ShouldBeTrue();
        }
        finally
        {
            effect.Dispose();
        }
    }

    [Fact]
    public void RenderTargetEffect_ShouldNotBeBound_WhenContainerHasNoEffect()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        BasicEffect effect = new BasicEffect(game.GraphicsDevice);

        ContainerRuntime container = new();
        container.Width = 100;
        container.Height = 100;
        container.IsRenderTarget = true;

        TextRuntime text = new();
        text.Text = "Hello";
        container.AddChild(text);

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        try
        {
            renderer.Draw(managers);
            renderer.Draw(managers);

            bool effectWasBound = renderer.SpriteRenderer.LastFrameDrawStates
                .Any(state => ReferenceEquals(state.Effect, effect));

            effectWasBound.ShouldBeFalse();
        }
        finally
        {
            effect.Dispose();
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
