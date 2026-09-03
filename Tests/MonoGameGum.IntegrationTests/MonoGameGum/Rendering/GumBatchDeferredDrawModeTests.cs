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
/// Pins <see cref="Renderer.GumBatchDrawMode.Deferred"/> (issue #4573): multiple <see cref="Renderer.Draw(IRenderableIpso)"/>
/// calls within one <see cref="Renderer.Begin"/>/<see cref="Renderer.End"/> cycle accumulate and run
/// through the active <see cref="Renderer.SiblingOrdering"/> once at <c>End</c>, instead of each call
/// submitting immediately and unbatched. <see cref="Renderer.GumBatchDrawMode.Immediate"/> (the
/// default, no mode argument) is pinned unchanged here - this feature is purely additive.
/// </summary>
public class GumBatchDeferredDrawModeTests : BaseTestClass
{
    private static void AdvanceHostFrame(SystemManagers managers, ref double hostTime)
    {
        hostTime += 1.0 / 60.0;
        managers.Activity(hostTime);
    }

    private static SpriteRuntime CreateSprite(Texture2D texture, float y)
    {
        SpriteRuntime sprite = new();
        sprite.Texture = texture;
        sprite.Y = y;
        return sprite;
    }

    [Fact]
    public void Immediate_AlternatingTextureSprites_StillAddsOneDrawCallPerSprite()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;

        Texture2D textureA = new(game.GraphicsDevice, 1, 1);
        textureA.SetData(new[] { Color.Red });
        Texture2D textureB = new(game.GraphicsDevice, 1, 1);
        textureB.SetData(new[] { Color.Blue });

        IRenderableOrderer originalOrdering = Renderer.SiblingOrdering;
        try
        {
            Renderer.SiblingOrdering = BatchKeyGroupedOrderer.Instance;

            const int spriteCount = 10;
            SpriteRuntime[] sprites = new SpriteRuntime[spriteCount];
            for (int i = 0; i < spriteCount; i++)
            {
                sprites[i] = CreateSprite(i % 2 == 0 ? textureA : textureB, i * 10);
            }

            AdvanceHostFrame(managers, ref hostTime);
            renderer.Begin(); // Immediate is the default - no mode argument.
            foreach (SpriteRuntime sprite in sprites)
            {
                renderer.Draw(sprite);
            }
            renderer.End();

            renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(spriteCount);
        }
        finally
        {
            Renderer.SiblingOrdering = originalOrdering;
            textureA.Dispose();
            textureB.Dispose();
        }
    }

    [Fact]
    public void Deferred_AlternatingTextureSprites_CollapsesToOneDrawCallPerTexture()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;

        Texture2D textureA = new(game.GraphicsDevice, 1, 1);
        textureA.SetData(new[] { Color.Red });
        Texture2D textureB = new(game.GraphicsDevice, 1, 1);
        textureB.SetData(new[] { Color.Blue });

        IRenderableOrderer originalOrdering = Renderer.SiblingOrdering;
        try
        {
            Renderer.SiblingOrdering = BatchKeyGroupedOrderer.Instance;

            const int spriteCount = 10;
            SpriteRuntime[] sprites = new SpriteRuntime[spriteCount];
            for (int i = 0; i < spriteCount; i++)
            {
                sprites[i] = CreateSprite(i % 2 == 0 ? textureA : textureB, i * 10);
            }

            AdvanceHostFrame(managers, ref hostTime);
            renderer.Begin(mode: Renderer.GumBatchDrawMode.Deferred);
            foreach (SpriteRuntime sprite in sprites)
            {
                renderer.Draw(sprite);
            }
            renderer.End();

            // Same alternating-texture scene as the Immediate test above, but now batched across
            // the separate Draw() calls since they share one End()-time BuildDrawList/Submit pass:
            // one draw call per distinct texture instead of one per sprite.
            renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(2);
        }
        finally
        {
            Renderer.SiblingOrdering = originalOrdering;
            textureA.Dispose();
            textureB.Dispose();
        }
    }

    /// <summary>
    /// Ground-truth check prompted by an FRB2 report of RenderStateChangeStatistics.DrawCallCount
    /// (13) exceeding the real GraphicsDevice.Metrics.DrawCount total for the whole frame (8) - a
    /// subset can never legitimately exceed the whole. This independently measures the true
    /// Metrics delta across two Begin/End cycles in one host frame (mirroring FRB2's per-camera +
    /// overlay shape) and asserts Gum's own tally matches it exactly, to prove or disprove that
    /// Renderer's own delta-tracking is the source of the discrepancy.
    /// </summary>
    [Fact]
    public void MultipleCyclesInOneHostFrame_DrawCallCountMatchesTrueGpuTotalForTheFrame()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;

        Texture2D texture = new(game.GraphicsDevice, 1, 1);
        texture.SetData(new[] { Color.Red });

        RectangleRuntime cardA = new();
        cardA.IsFilled = true;
        cardA.Width = 10;
        cardA.Height = 10;
        RectangleRuntime cardB = new();
        cardB.IsFilled = true;
        cardB.Width = 10;
        cardB.Height = 10;
        cardB.X = 50;
        SpriteRuntime overlayText = CreateSprite(texture, 0);

        try
        {
            // Warm-up cycle: discard first-time costs (e.g. default texture load) before
            // measuring, matching the pattern used elsewhere in this file.
            AdvanceHostFrame(managers, ref hostTime);
            renderer.Begin();
            renderer.End();

            AdvanceHostFrame(managers, ref hostTime);
            long trueDrawCountBefore = game.GraphicsDevice.Metrics.DrawCount;

            // Cycle 1: "main camera" pass drawing two cards, Deferred (FRB2's actual mode).
            renderer.Begin(mode: Renderer.GumBatchDrawMode.Deferred);
            renderer.Draw(cardA);
            renderer.Draw(cardB);
            renderer.End();

            // Cycle 2: "overlay" pass - a second Begin/End cycle in the SAME host frame, matching
            // FRB2's per-camera-plus-overlay shape.
            renderer.Begin();
            renderer.Draw(overlayText);
            renderer.End();

            long trueDrawCountAfter = game.GraphicsDevice.Metrics.DrawCount;
            int trueDrawCallCountThisFrame = (int)(trueDrawCountAfter - trueDrawCountBefore);

            renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(trueDrawCallCountThisFrame);
        }
        finally
        {
            texture.Dispose();
        }
    }

    /// <summary>
    /// Pins the Renderer-side wiring for issue #4575's follow-up: Renderer resets
    /// BatchKeyGroupedOrderer's break tally at the same points it resets
    /// RenderStateChangeStatistics - once per host frame on this (Deferred) path - so it
    /// accumulates across multiple Begin/End cycles in one frame instead of the second cycle
    /// wiping out the first's tally, and still resets cleanly on the next frame.
    /// </summary>
    [Fact]
    public void Deferred_MultipleCyclesInOneHostFrame_AccumulateOrdererTally_ResetsNextFrame()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        double hostTime = 0;

        Texture2D textureA = new(game.GraphicsDevice, 1, 1);
        textureA.SetData(new[] { Color.Red });
        Texture2D textureB = new(game.GraphicsDevice, 1, 1);
        textureB.SetData(new[] { Color.Blue });

        IRenderableOrderer originalOrdering = Renderer.SiblingOrdering;
        try
        {
            Renderer.SiblingOrdering = BatchKeyGroupedOrderer.Instance;

            void RunOneCycleOfAlternatingSprites()
            {
                renderer.Begin(mode: Renderer.GumBatchDrawMode.Deferred);
                for (int i = 0; i < 4; i++)
                {
                    renderer.Draw(CreateSprite(i % 2 == 0 ? textureA : textureB, i * 10));
                }
                renderer.End();
            }

            AdvanceHostFrame(managers, ref hostTime);
            RunOneCycleOfAlternatingSprites();
            ((BatchKeyGroupedOrderer)Renderer.SiblingOrdering).NoCandidateInWindowBreakCount.ShouldBe(1);

            RunOneCycleOfAlternatingSprites(); // same host frame - no AdvanceHostFrame call
            ((BatchKeyGroupedOrderer)Renderer.SiblingOrdering).NoCandidateInWindowBreakCount.ShouldBe(2);

            AdvanceHostFrame(managers, ref hostTime);
            RunOneCycleOfAlternatingSprites();
            ((BatchKeyGroupedOrderer)Renderer.SiblingOrdering).NoCandidateInWindowBreakCount.ShouldBe(1);
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
