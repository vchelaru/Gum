using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// End-to-end tests that drive a real MonoGame render pass via <see cref="global::Gum.GumService"/>
/// and assert the draw-call count <see cref="Renderer.RenderStateChangeStatistics"/> reports.
/// <see cref="RenderStateChangeStatistics.DrawCallCount"/> was previously populated only by the
/// raylib renderer; these pin the MonoGame wiring (issue #2697), sourced from
/// <see cref="GraphicsDevice.Metrics"/>. Counts are asserted as deltas against a baseline frame so
/// each test is isolated from residue left by earlier tests.
/// </summary>
public class RendererDrawCallCountTests : BaseTestClass
{
    private static int DrawAndCountDrawCalls()
    {
        global::Gum.GumService.Default.Draw();
        return SystemManagers.Default.Renderer.RenderStateChangeStatistics.DrawCallCount;
    }

    [Fact]
    public void Draw_AddingColoredRectangle_AddsExactlyOneDrawCall()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        int baseline = DrawAndCountDrawCalls();

        ColoredRectangleRuntime rectangle = new();
        rectangle.Width = 50;
        rectangle.Height = 50;
        rectangle.AddToRoot();
        global::Gum.GumService.Default.Root.UpdateLayout();

        int withRectangle = DrawAndCountDrawCalls();

        (withRectangle - baseline).ShouldBe(1);
    }

    [Fact]
    public void Draw_Twice_DoesNotAccumulateDrawCallCount()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        int first = DrawAndCountDrawCalls();
        int second = DrawAndCountDrawCalls();

        second.ShouldBe(first);
    }

    /// <summary>
    /// Characterizes the exact perf problem in issue #2697: a stack of sprites alternating between
    /// two textures (standing in for a StackPanel mixing frame images and text, each backed by a
    /// different texture/font atlas) cannot be merged by SpriteBatch's consecutive-same-texture
    /// batching, so every element costs its own GPU draw call. This pins that the counter reports
    /// the real per-element cost, not an optimistic estimate.
    /// </summary>
    [Fact]
    public void Draw_SpritesAlternatingBetweenTwoTextures_AddsOneDrawCallPerSprite()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        int baseline = DrawAndCountDrawCalls();

        Texture2D textureA = new(game.GraphicsDevice, 1, 1);
        textureA.SetData(new[] { Color.Red });
        Texture2D textureB = new(game.GraphicsDevice, 1, 1);
        textureB.SetData(new[] { Color.Blue });

        const int spriteCount = 10;
        try
        {
            for (int i = 0; i < spriteCount; i++)
            {
                SpriteRuntime sprite = new();
                sprite.Texture = i % 2 == 0 ? textureA : textureB;
                sprite.Y = i * 10;
                sprite.AddToRoot();
            }
            global::Gum.GumService.Default.Root.UpdateLayout();

            int withSprites = DrawAndCountDrawCalls();

            (withSprites - baseline).ShouldBe(spriteCount);
        }
        finally
        {
            textureA.Dispose();
            textureB.Dispose();
        }
    }

    /// <summary>
    /// The fix for #2697: opting into <see cref="BatchKeyGroupedOrderer"/> reorders same-texture
    /// draws into contiguous runs (via each renderable's <c>BatchSortKey</c>), so the same
    /// alternating-texture scene collapses from one draw call per sprite to one per distinct
    /// texture. Resets <see cref="Renderer.SiblingOrdering"/> in finally since it's a static shared
    /// across tests.
    /// </summary>
    [Fact]
    public void Draw_SpritesAlternatingBetweenTwoTextures_WithBatchKeyGroupedOrderer_AddsOneDrawCallPerTexture()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        Texture2D textureA = new(game.GraphicsDevice, 1, 1);
        textureA.SetData(new[] { Color.Red });
        Texture2D textureB = new(game.GraphicsDevice, 1, 1);
        textureB.SetData(new[] { Color.Blue });

        IRenderableOrderer originalOrdering = Renderer.SiblingOrdering;
        try
        {
            Renderer.SiblingOrdering = BatchKeyGroupedOrderer.Instance;

            int baseline = DrawAndCountDrawCalls();

            const int spriteCount = 10;
            for (int i = 0; i < spriteCount; i++)
            {
                SpriteRuntime sprite = new();
                sprite.Texture = i % 2 == 0 ? textureA : textureB;
                sprite.Y = i * 10;
                sprite.AddToRoot();
            }
            global::Gum.GumService.Default.Root.UpdateLayout();

            int withSprites = DrawAndCountDrawCalls();

            (withSprites - baseline).ShouldBe(2);
        }
        finally
        {
            Renderer.SiblingOrdering = originalOrdering;
            textureA.Dispose();
            textureB.Dispose();
        }
    }

    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
        }

        protected override void Initialize()
        {
            base.Initialize();
            global::Gum.GumService.Default.Initialize(this, global::Gum.Forms.DefaultVisualsVersion.V3);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (global::Gum.GumService.Default.IsInitialized)
            {
                global::Gum.GumService.Default.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
