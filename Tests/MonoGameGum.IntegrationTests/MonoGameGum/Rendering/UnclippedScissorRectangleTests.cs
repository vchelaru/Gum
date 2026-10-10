using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Pins that an unclipped <see cref="SpriteRenderer.BeginSpriteBatch"/> leaves the device's scissor
/// rectangle equal to the device viewport's bounds (the whole backbuffer or bound render target by
/// default), not the camera's client rect (#5950). MonoGame 3.8.6 DesktopGL's Clear can re-enable
/// the GL scissor test with whatever rect is set, so any rect smaller than the target clips
/// everything outside it.
/// </summary>
public class UnclippedScissorRectangleTests : BaseTestClass
{
    [Fact]
    public void BeginSpriteBatch_UnclippedWithSmallerCamera_ScissorCoversBackbuffer()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        PresentationParameters presentation = game.GraphicsDevice.PresentationParameters;
        Renderer renderer = SystemManagers.Default.Renderer;
        renderer.Camera.ClientLeft = 20;
        renderer.Camera.ClientTop = 10;
        renderer.Camera.ClientWidth = presentation.BackBufferWidth / 2;
        renderer.Camera.ClientHeight = presentation.BackBufferHeight / 2;

        BeginUnclipped(renderer);

        game.GraphicsDevice.ScissorRectangle.ShouldBe(
            new Rectangle(0, 0, presentation.BackBufferWidth, presentation.BackBufferHeight));
        renderer.SpriteRenderer.EndSpriteBatch();
    }

    [Fact]
    public void BeginSpriteBatch_UnclippedWithRenderTargetBound_ScissorCoversRenderTarget()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        using RenderTarget2D renderTarget = new(game.GraphicsDevice, 64, 32);
        game.GraphicsDevice.SetRenderTarget(renderTarget);
        Renderer renderer = SystemManagers.Default.Renderer;

        BeginUnclipped(renderer);

        game.GraphicsDevice.ScissorRectangle.ShouldBe(new Rectangle(0, 0, 64, 32));
        renderer.SpriteRenderer.EndSpriteBatch();
        game.GraphicsDevice.SetRenderTarget(null);
    }

    [Fact]
    public void BeginSpriteBatch_UnclippedWithSmallerViewport_ScissorMatchesViewport()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        game.GraphicsDevice.Viewport = new Viewport(10, 5, 300, 200);
        Renderer renderer = SystemManagers.Default.Renderer;

        BeginUnclipped(renderer);

        game.GraphicsDevice.ScissorRectangle.ShouldBe(new Rectangle(10, 5, 300, 200));
        renderer.SpriteRenderer.EndSpriteBatch();
    }

    private static void BeginUnclipped(Renderer renderer)
    {
        RenderStateVariables renderStates = new()
        {
            BlendState = Gum.BlendState.NonPremultiplied,
            ClipRectangle = null,
        };

        renderer.SpriteRenderer.BeginSpriteBatch(
            renderStates, renderer.MainLayer, BeginType.Push, renderer.Camera, objectStartingSpriteBatch: null);
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        public Gum.GumService GumService { get; }

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            GumService = new Gum.GumService();
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumService.Initialize(this, Gum.Forms.DefaultVisualsVersion.V3);
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
