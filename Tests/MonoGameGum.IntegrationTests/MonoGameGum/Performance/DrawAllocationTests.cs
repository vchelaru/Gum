using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Performance;

/// <summary>
/// Per-frame allocation baseline and regression guard for the idle-frame API
/// (<see cref="global::Gum.GumService.Update(GameTime)"/> + <see cref="global::Gum.GumService.Draw()"/>)
/// over a representative, unchanging Forms scene. Part of the runtime allocation pass, issue #1934:
/// the target is zero managed bytes allocated per steady-state idle frame; the guard is a ratchet
/// that tightens as allocation sources are removed in follow-up optimization PRs.
/// </summary>
public class DrawAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public DrawAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void IdleUpdateAndDraw_RepresentativeFormsScene_AllocationBaseline()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        BuildScene();

        // A single fixed GameTime reused every frame keeps the scene idle (no time advance, no
        // animation) and avoids allocating a GameTime per iteration, so the measured delta reflects
        // only the Update/Draw walk itself.
        GameTime gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                global::Gum.GumService.Default.Update(gameTime);
                global::Gum.GumService.Default.Draw();
            },
            warmupIterations: 50,
            measuredIterations: 500);

        int drawStateCount = SystemManagers.Default.Renderer.SpriteRenderer.LastFrameDrawStates.Count();

        _output.WriteLine($"Idle Update+Draw of a representative Forms scene (20-item ListBox, labels, " +
            $"a Text, and a filled rectangle): {result.BytesPerIteration:N0} bytes/frame " +
            $"({result.TotalBytes:N0} bytes over {result.Iterations} frames, {drawStateCount} draw states last frame)");

        // Liveness: the Draw walk must actually render something each frame, otherwise a silent
        // no-op would make a low/zero allocation result meaningless.
        drawStateCount.ShouldBeGreaterThan(0);

        // Baseline ratchet (#1934): the idle Update/Draw walk over an unchanging Forms scene still
        // allocates today — measured at a deterministic 831 bytes/frame locally. The likely sources
        // are the per-frame input/cursor pass in GumService.Update (FormsUtilities.Update) and
        // per-frame collections in the Draw walk; driving it toward zero is deferred to follow-up
        // optimization PRs. This guard pins the current cost so a regression that grows it fails the
        // build; tighten the bound as sources are removed. Headroom is for JIT/runner variance.
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(1200);
    }

    private static void BuildScene()
    {
        // A ListBox with ~20 string items exercises ListBoxItem visual realization and Text layout.
        ListBox listBox = new ListBox();
        listBox.X = 10;
        listBox.Y = 10;
        listBox.Width = 200;
        listBox.Height = 300;
        listBox.AddToRoot();

        for (int i = 0; i < 20; i++)
        {
            listBox.Items!.Add("Item " + i);
        }

        Label titleLabel = new Label();
        titleLabel.X = 230;
        titleLabel.Y = 10;
        titleLabel.Text = "Inventory";
        titleLabel.AddToRoot();

        Label subtitleLabel = new Label();
        subtitleLabel.X = 230;
        subtitleLabel.Y = 40;
        subtitleLabel.Text = "Select an item to view details";
        subtitleLabel.AddToRoot();

        TextRuntime footer = new TextRuntime();
        footer.X = 230;
        footer.Y = 80;
        footer.Text = "20 items in inventory";
        footer.AddToRoot();

        RectangleRuntime panel = new RectangleRuntime();
        panel.X = 230;
        panel.Y = 120;
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
