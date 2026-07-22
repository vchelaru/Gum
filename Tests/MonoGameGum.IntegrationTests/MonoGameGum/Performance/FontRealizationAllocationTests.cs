using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Performance;

/// <summary>
/// Per-realization allocation baseline and regression guard for font/string realization on the Text
/// path (issue #1934): rebuilding a Text's render target from its wrapped lines. This is the
/// RenderTarget path (<see cref="Text.UpdateTextureToRender"/> → <see cref="BitmapFont"/>'s
/// RenderToTexture2D), which needs a live GraphicsDevice, hence the integration project rather than
/// the headless suite. The target is zero managed bytes per realization when the text and its
/// dimensions are unchanged (the render target is reused); the guard ratchets down as allocation
/// sources are removed.
/// </summary>
public class FontRealizationAllocationTests : BaseTestClass
{
    private readonly ITestOutputHelper _output;

    public FontRealizationAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void RenderTargetRealization_UnchangedText_IsZeroAllocation()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        FieldInfo needsRefreshField = typeof(Text).GetField(
            "mNeedsBitmapFontRefresh", BindingFlags.Instance | BindingFlags.NonPublic)!;

        TextRenderingMode originalMode = Text.TextRenderingMode;
        Text.TextRenderingMode = TextRenderingMode.RenderTarget;
        try
        {
            Text text = new Text();
            text.BitmapFont = Text.DefaultBitmapFont;
            text.Width = null; // size to content so wrapping stays single-line
            text.RawText = "Realize this text";

            // Warm up so the render target exists and is reused (its dimensions do not change
            // across iterations because the text is constant).
            text.SetNeedsRefreshToTrue();
            text.TryUpdateTextureToRender();

            // Liveness: a realization must actually run — the refresh flag we set is consumed
            // (true -> false) and a texture is produced. A silent no-op would make the
            // zero-allocation result meaningless.
            text.SetNeedsRefreshToTrue();
            ((bool)needsRefreshField.GetValue(text)!).ShouldBeTrue();
            text.TryUpdateTextureToRender();
            ((bool)needsRefreshField.GetValue(text)!).ShouldBeFalse();
            text.WrappedTextWidth.ShouldBeGreaterThan(0);

            Renderer renderer = SystemManagers.Default.Renderer;

            AllocationResult result = AllocationMeasurer.MeasureMinimum(
                () =>
                {
                    // Reset the per-frame render-state recording so the SpriteBatch machinery (pooled
                    // change-record lists, the per-frame draw-state list) does not accumulate across
                    // iterations — a real frame does this reset, and it isolates the measurement to the
                    // Text/BitmapFont realization work this test targets.
                    SpriteBatchStack.PerformStartOfLayerRenderingLogic();
                    renderer.ClearPerformanceRecordingVariables();

                    text.SetNeedsRefreshToTrue();
                    text.TryUpdateTextureToRender();
                },
                warmupIterations: 50,
                measuredIterations: 300);

            _output.WriteLine($"Render-target realization of an unchanged single-line Text: " +
                $"{result.BytesPerIteration:N0} bytes/realization " +
                $"({result.TotalBytes:N0} bytes over {result.Iterations} realizations)");

            result.TotalBytes.ShouldBe(0);
        }
        finally
        {
            Text.TextRenderingMode = originalMode;
        }
    }

    /// <summary>
    /// Minimal Game host that initializes the default <see cref="global::Gum.GumService"/> against a
    /// live device so text realization (which requires a GraphicsDevice) can be driven manually.
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
