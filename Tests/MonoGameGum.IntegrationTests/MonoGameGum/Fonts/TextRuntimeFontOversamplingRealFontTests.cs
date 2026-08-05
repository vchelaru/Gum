using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Fonts;

/// <summary>
/// Issue #4309 end-to-end: reproduces the manual-test demo's exact scenario (a RelativeToChildren
/// TextRuntime, real KernSmith rasterization, real .ttf file, continuous scroll-wheel-style zooming)
/// headlessly, through the real IInMemoryFontCreator.TryCreateFont path -- not a synthetic fake --
/// so a pass here is strong evidence the visible demo is fixed too.
/// </summary>
public class TextRuntimeFontOversamplingRealFontTests : BaseTestClass
{
    private static readonly string TestFontPath = System.IO.Path.GetFullPath(
        System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkiaGum.Tests", "Assets", "Fonts", "TestFont.ttf"));

    private static readonly string LogPath = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(), "gum-4309-zoom-sequence-log.txt");

    [Fact]
    public void RegenerateOversampledFont_RealFont_ContinuousZoomSequence_MeasuredSizeNeverDriftsFromNative()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
            TextRuntime.UseFontOversampling = true;

            TextRuntime adjusted = new();
            adjusted.UseCustomFont = true;
            adjusted.CustomFontFile = TestFontPath;
            adjusted.FontSize = 12;
            adjusted.WidthUnits = DimensionUnitType.RelativeToChildren;
            adjusted.HeightUnits = DimensionUnitType.RelativeToChildren;
            adjusted.Text = "Stack Ag 12pt";
            game.GumService.Root.Children.Add(adjusted);

            TextRuntime control = new();
            control.UseCustomFont = true;
            control.CustomFontFile = TestFontPath;
            control.FontSize = 12;
            control.WidthUnits = DimensionUnitType.RelativeToChildren;
            control.HeightUnits = DimensionUnitType.RelativeToChildren;
            control.Text = "Stack Ag 12pt";
            game.GumService.Root.Children.Add(control);

            Text adjustedText = (Text)adjusted.RenderableComponent;
            Text controlText = (Text)control.RenderableComponent;

            float nativeWidth = adjustedText.WrappedTextWidth;
            float nativeHeight = adjustedText.WrappedTextHeight;

            StringBuilder log = new();
            log.AppendLine($"native (zoom=1, before any RegenerateOversampledFont call): W={nativeWidth} H={nativeHeight}");
            log.AppendLine($"control's native measurement matches adjusted's: {controlText.WrappedTextWidth == nativeWidth}");

            List<string> failures = new();

            foreach (float zoom in BuildContinuousZoomSequence())
            {
                // Mirrors the real per-frame render hook (issue #4317), the same one a running game
                // uses -- not a hand-picked call to RegenerateOversampledFont directly.
                adjusted.UpdateAutomaticFontOversampling(zoom);

                float width = adjustedText.WrappedTextWidth;
                float height = adjustedText.WrappedTextHeight;
                log.AppendLine($"zoom={zoom:0.000}: adjusted W={width} H={height}  |  control W={controlText.WrappedTextWidth} H={controlText.WrappedTextHeight}");

                if (System.MathF.Abs(width - nativeWidth) > 0.01f || System.MathF.Abs(height - nativeHeight) > 0.01f)
                {
                    failures.Add($"zoom={zoom:0.000}: adjusted W={width} (expected {nativeWidth}), H={height} (expected {nativeHeight})");
                }

                // The control (never oversampled) must ALSO always match its own native measurement --
                // proves the fix doesn't leak into/corrupt instances that never actually zoom.
                if (controlText.WrappedTextWidth != nativeWidth || controlText.WrappedTextHeight != nativeHeight)
                {
                    failures.Add($"zoom={zoom:0.000}: control drifted to W={controlText.WrappedTextWidth} H={controlText.WrappedTextHeight}");
                }
            }

            File.WriteAllText(LogPath, log.ToString());

            failures.ShouldBeEmpty(
                $"measured size drifted from the native {nativeWidth}x{nativeHeight} -- see {LogPath} for the full zoom trace:\n" +
                string.Join("\n", failures));
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Reproduces the exact manual-test finding: FontScale 2, zoomed to ~2.93x. Confirms the drawn text
    // exactly fills its own pinned box with the real KernSmith rasterizer, not just a synthetic fake.
    [Fact]
    public void RegenerateOversampledFont_RealFont_FontScale2AtNonRoundZoom_DrawnTextExactlyFillsPinnedBox()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
            TextRuntime.UseFontOversampling = true;

            TextRuntime textRuntime = new();
            textRuntime.UseCustomFont = true;
            textRuntime.CustomFontFile = TestFontPath;
            textRuntime.FontSize = 12;
            textRuntime.FontScale = 2f;
            textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.HeightUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.Text = "Stack Ag 12pt";
            game.GumService.Root.Children.Add(textRuntime);

            Text text = (Text)textRuntime.RenderableComponent;
            float pinnedWidth = text.WrappedTextWidth;

            textRuntime.UpdateAutomaticFontOversampling(2.93f);

            float drawnWidth = text.BitmapFont.MeasureString("Stack Ag 12pt") * ((IText)text).FontScale;

            drawnWidth.ShouldBe(pinnedWidth, tolerance: 0.01f,
                "because the drawn text must exactly fill its own box at this zoom level -- reproduces " +
                "the manual-test gap found at FontScale 2, zoom~2.93 with the real KernSmith rasterizer");
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    [Fact]
    public void RegenerateOversampledFont_RealFont_UseFontOversamplingEnabledAfterFontResolved_NoOrderingLandmine()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
            // Flag OFF while the font first resolves -- reproduces the exact ordering that broke the
            // demo (UseFontOversampling set AFTER BuildStackRow already ran FontSize/CustomFontFile).
            TextRuntime.UseFontOversampling = false;

            TextRuntime textRuntime = new();
            textRuntime.UseCustomFont = true;
            textRuntime.CustomFontFile = TestFontPath;
            textRuntime.FontSize = 12;
            textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.HeightUnits = DimensionUnitType.RelativeToChildren;
            textRuntime.Text = "Stack Ag 12pt";
            game.GumService.Root.Children.Add(textRuntime);

            Text text = (Text)textRuntime.RenderableComponent;
            float nativeWidth = text.WrappedTextWidth;
            float nativeHeight = text.WrappedTextHeight;

            TextRuntime.UseFontOversampling = true;
            bool result = textRuntime.RegenerateOversampledFont(1.127f);

            result.ShouldBeTrue();
            text.WrappedTextWidth.ShouldBe(nativeWidth, tolerance: 0.01f);
            text.WrappedTextHeight.ShouldBe(nativeHeight, tolerance: 0.01f);
        }
        finally
        {
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    private static IEnumerable<float> BuildContinuousZoomSequence()
    {
        List<float> zooms = new();
        for (float z = 1f; z <= 8f; z += 0.037f)
        {
            zooms.Add(z);
        }
        for (float z = 8f; z >= 1f; z -= 0.061f)
        {
            zooms.Add(z);
        }
        return zooms;
    }

    /// <summary>
    /// Minimal Game host providing a live GraphicsDevice and an initialized GumService (real
    /// TextRuntimes need SystemManagers.Default wired up for the full font-property-resolution
    /// cascade -- KernSmithFontCreatorTests.cs's plain KernSmithFontCreator.TryCreateFont calls don't
    /// go through that cascade, so they don't need this). Uses a fresh GumService instance, not the
    /// static Default, since that singleton can only be initialized once per process.
    /// </summary>
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
