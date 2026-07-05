using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Performance;

/// <summary>
/// Direct leak guards for the <see cref="global::RenderingLibrary.Graphics.SpriteBatchStack"/>
/// render-state stack (issues #1934, #3515). On NET8+ the per-layer <c>EndSpriteBatch</c> pop that
/// would balance <c>RenderLayer</c>'s <c>BeginSpriteBatch(Push)</c> is compiled out, so the pushed
/// entry accumulates one per layer per frame — an unbounded <see cref="List{T}"/> rooted by the
/// long-lived Renderer. These tests pin the collection depth itself (rather than a bytes/frame
/// proxy): the depth after a late frame must equal the depth after an early frame, i.e. it does not
/// scale with frame count. Two entry paths are covered: the full-frame <c>GumService.Draw()</c> path
/// (kept bounded by the per-frame reset in <c>ClearPerformanceRecordingVariables</c>, #1934) and the
/// per-layer <c>Renderer.Draw(SystemManagers, Layer)</c> path used by FRB, which never runs that
/// reset and is instead balanced by the NET8+ pop in <c>RenderLayer</c> (#3515).
/// </summary>
public class RenderStateStackGrowthTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public RenderStateStackGrowthTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SpriteBatchStack_StateStackDoesNotGrowAcrossFrames_OnFullFrameDrawPath()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        BuildScene();

        // A single fixed GameTime keeps the scene idle so the only thing that could change the
        // stack depth from frame to frame is the leak under test, not layout/animation churn.
        GameTime gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

        // Warm up: the first few Draws after RunOneFrame do one-time work (font loads, render-target
        // setup) that can vary the per-frame push count. Discard them so the measured run reflects
        // only the steady-state stack behavior.
        for (int i = 0; i < 5; i++)
        {
            global::Gum.GumService.Default.Update(gameTime);
            global::Gum.GumService.Default.Draw();
        }

        const int measuredFrames = 200;
        List<int> depths = new List<int>(measuredFrames);
        for (int i = 0; i < measuredFrames; i++)
        {
            global::Gum.GumService.Default.Update(gameTime);
            global::Gum.GumService.Default.Draw();
            depths.Add(SystemManagers.Default.Renderer.SpriteRenderer.RenderStateStackDepth);
        }

        int earlyDepth = depths.First();
        int lateDepth = depths.Last();
        int maxDepth = depths.Max();
        int minDepth = depths.Min();

        _output.WriteLine($"RenderStateStackDepth over {measuredFrames} idle frames: " +
            $"first={earlyDepth}, last={lateDepth}, min={minDepth}, max={maxDepth}");

        // The stack must stay bounded: the depth after the last frame equals the depth after the
        // first measured frame, and never climbs in between. Without the per-frame reset this would
        // grow by one (per rendered layer) every frame, so lateDepth would be ~measuredFrames higher.
        maxDepth.ShouldBe(minDepth);
        lateDepth.ShouldBe(earlyDepth);
    }

    [Fact]
    public void SpriteBatchStack_StateStackDoesNotGrowAcrossFrames_OnPerLayerDrawPath()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        BuildScene();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;
        Layer mainLayer = renderer.MainLayer;

        // One layout pass for the idle scene, then measure the per-layer Draw path only
        // (Renderer.Draw(SystemManagers, Layer) — the FRB / GumIdb entry point). That path never
        // runs the full-frame ClearPerformanceRecordingVariables reset, so on NET8+ it relies on
        // RenderLayer's own push balance to stay bounded (#3515).
        GameTime gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));
        global::Gum.GumService.Default.Update(gameTime);

        double hostTime = 0;

        // Warm up: the first Draws do one-time work (font loads, render-target setup) that can vary
        // the per-frame push count. Discard them so the measured run reflects only steady state.
        for (int i = 0; i < 5; i++)
        {
            AdvanceHostFrame(managers, ref hostTime);
            renderer.Draw(managers, mainLayer);
        }

        const int measuredFrames = 200;
        List<int> depths = new List<int>(measuredFrames);
        for (int i = 0; i < measuredFrames; i++)
        {
            AdvanceHostFrame(managers, ref hostTime);
            renderer.Draw(managers, mainLayer);
            depths.Add(renderer.SpriteRenderer.RenderStateStackDepth);
        }

        int earlyDepth = depths.First();
        int lateDepth = depths.Last();
        int maxDepth = depths.Max();
        int minDepth = depths.Min();

        _output.WriteLine($"RenderStateStackDepth over {measuredFrames} per-layer draws: " +
            $"first={earlyDepth}, last={lateDepth}, min={minDepth}, max={maxDepth}");

        // The stack must stay bounded across per-layer draws: without the NET8+ balance in
        // RenderLayer, each Draw(Layer) pushes an entry that is never popped, so lateDepth would be
        // ~measuredFrames higher than earlyDepth. The fix keeps the depth constant.
        maxDepth.ShouldBe(minDepth);
        lateDepth.ShouldBe(earlyDepth);
    }

    private static void AdvanceHostFrame(SystemManagers managers, ref double hostTime)
    {
        hostTime += 1.0 / 60.0;
        managers.Activity(hostTime);
    }

    private static void BuildScene()
    {
        // A ListBox with a handful of items renders through the full clipped-tree path (outer layer
        // push + per-container clip begins), which is exactly what drives the state stack.
        ListBox listBox = new ListBox();
        listBox.X = 10;
        listBox.Y = 10;
        listBox.Width = 200;
        listBox.Height = 200;
        listBox.AddToRoot();

        for (int i = 0; i < 10; i++)
        {
            listBox.Items!.Add("Item " + i);
        }

        RectangleRuntime panel = new RectangleRuntime();
        panel.X = 230;
        panel.Y = 10;
        panel.Width = 150;
        panel.Height = 120;
        panel.IsFilled = true;
        panel.FillColor = Color.CornflowerBlue;
        panel.AddToRoot();
    }

    /// <summary>
    /// Minimal Game host that initializes the default <see cref="global::Gum.GumService"/> against a
    /// live device so <see cref="global::Gum.GumService.Update(GameTime)"/> / <c>Draw()</c> can be
    /// driven manually after <see cref="Game.RunOneFrame"/>.
    /// </summary>
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
            global::Gum.GumService.Default.Initialize(this, global::Gum.Forms.DefaultVisualsVersion.Newest);
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
