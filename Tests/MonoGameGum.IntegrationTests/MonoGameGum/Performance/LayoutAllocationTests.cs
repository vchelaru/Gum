using System;
using Microsoft.Xna.Framework;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Performance;

/// <summary>
/// Per-layout-pass allocation baseline and regression guard for <see cref="GraphicalUiElement.UpdateLayout()"/>
/// over a realistic Forms control tree with real Text (buttons, a text box, a combo box, a list box, and a
/// floating window). Part of the runtime allocation pass, issue #1934. Unlike the headless container-only
/// layout test in <c>MonoGameGum.Tests</c> (which reads zero because it has no Text and no Forms controls),
/// this lives in the integration project so a real <see cref="Microsoft.Xna.Framework.Graphics.GraphicsDevice"/>
/// backs Text measurement/glyph realization — i.e. it measures what laying out an actual game UI costs in GC.
/// These are baselines, not zero-allocation assertions; each guard is a ratchet to drive down in a follow-up
/// optimization PR.
/// </summary>
public class RealisticLayoutAllocationTests : BaseTestClass
{
    private const string TreeDescription =
        "StackPanel(3 Buttons, TextBox, ComboBox[4 items], ListBox[15 items]) + floating Window(2 Buttons)";

    private readonly ITestOutputHelper _output;

    public RealisticLayoutAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void PropertyChange_TriggeringRelayout_RealisticTree_AllocationBaseline()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        Window floatingWindow = BuildRealisticTree();
        global::Gum.GumService.Default.Root.UpdateLayout();

        const int measuredIterations = 500;

        // Alternate the floating window's height every frame so the change is real: the setter
        // re-lays-out the window and its children (title bar, eight resize borders, inner panel and
        // its two buttons). This exercises a property-driven relayout rooted at a live Forms control.
        float height = floatingWindow.Height;
        int layoutCallsBefore = GraphicalUiElement.UpdateLayoutCallCount;

        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                height = height == 200 ? 201 : 200;
                floatingWindow.Height = height;
            },
            warmupIterations: 50,
            measuredIterations: measuredIterations);

        int layoutCalls = GraphicalUiElement.UpdateLayoutCallCount - layoutCallsBefore;

        _output.WriteLine($"Height change on the floating Window of a realistic Forms tree " +
            $"[{TreeDescription}]: {result.BytesPerIteration:N0} bytes/frame " +
            $"({result.TotalBytes:N0} bytes over {result.Iterations} frames, {layoutCalls} layout calls total)");

        // Liveness: prove the scenario actually drove relayout each frame, so a silent no-op setter
        // cannot make the allocation result meaningless.
        layoutCalls.ShouldBeGreaterThanOrEqualTo(measuredIterations);

        // Baseline ratchet (#1934): a height change that relays out the floating Window subtree
        // allocates a deterministic 40 bytes/frame locally. This pins that cost plus headroom for
        // JIT/runner variance so a regression that grows it fails the build; tighten as sources are
        // removed. No fix here.
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(200);
    }

    [Fact]
    public void UpdateLayout_RealisticTree_AllocationBaseline()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        BuildRealisticTree();
        global::Gum.GumService.Default.Root.UpdateLayout();

        AllocationResult result = AllocationMeasurer.Measure(
            () => global::Gum.GumService.Default.Root.UpdateLayout(),
            warmupIterations: 50,
            measuredIterations: 500);

        _output.WriteLine($"Full relayout of a realistic Forms tree [{TreeDescription}]: " +
            $"{result.BytesPerIteration:N0} bytes/frame " +
            $"({result.TotalBytes:N0} bytes over {result.Iterations} frames)");

        // Ratchet (#1934): a full relayout of a real Forms tree with Text allocates a deterministic
        // 240 bytes/frame locally — down from 1,584 after the single-line-wrap fast path in
        // IWrappedTextExtensions.UpdateLines (pinned by
        // TextTests.UpdateWrappedText_WhenTextFitsOnOneLine_DoesNotAllocate). The residual is the one
        // genuinely-wrapping Text — the TextBox content re-wrapping at its constrained width each frame,
        // which still tokenizes/Splits/concatenates; tighten further as that word-by-word path is made
        // allocation-free. Pins the current cost plus headroom for runtime/runner variance.
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(400);
    }

    /// <summary>
    /// Builds a typical game-UI Forms layout in code and adds it to the default root: a vertical
    /// <see cref="StackPanel"/> holding three buttons, a text box, a combo box, and a fifteen-item
    /// list box, plus a separate floating <see cref="Window"/> containing two child buttons. Every
    /// control carries real text so Text realization is exercised during layout. Returns the floating
    /// window so callers can mutate it to trigger a property-driven relayout.
    /// </summary>
    private static Window BuildRealisticTree()
    {
        StackPanel panel = new();
        panel.X = 10;
        panel.Y = 10;
        panel.Spacing = 4;
        panel.AddToRoot();

        string[] buttonLabels = { "Play", "Options", "Quit" };
        foreach (string label in buttonLabels)
        {
            Button button = new();
            button.Text = label;
            panel.AddChild(button);
        }

        TextBox textBox = new();
        textBox.Text = "Player One";
        panel.AddChild(textBox);

        ComboBox comboBox = new();
        comboBox.Items!.Add("Easy");
        comboBox.Items!.Add("Normal");
        comboBox.Items!.Add("Hard");
        comboBox.Items!.Add("Insane");
        panel.AddChild(comboBox);

        ListBox listBox = new();
        listBox.Height = 200;
        for (int i = 0; i < 15; i++)
        {
            listBox.Items!.Add("Save Slot " + i);
        }
        panel.AddChild(listBox);

        Window floatingWindow = new();
        floatingWindow.X = 300;
        floatingWindow.Y = 40;
        floatingWindow.Width = 240;
        floatingWindow.Height = 200;
        floatingWindow.AddToRoot();

        Button okButton = new();
        okButton.Text = "OK";
        floatingWindow.AddChild(okButton);

        Button cancelButton = new();
        cancelButton.Text = "Cancel";
        cancelButton.Y = 40;
        floatingWindow.AddChild(cancelButton);

        return floatingWindow;
    }

    /// <summary>
    /// Minimal Game host that initializes the default <see cref="global::Gum.GumService"/> against a
    /// live device so layout of a Text-bearing Forms tree can be driven manually after
    /// <see cref="Game.RunOneFrame"/>.
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
            global::Gum.GumService.Default.Initialize(this, DefaultVisualsVersion.Newest);
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
