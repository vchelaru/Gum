using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #6112: moving a text block by a sub-pixel amount at a non-100% camera zoom must not change
/// how its letters sit relative to each other. Every shifted render has to be the reference render
/// translated by a whole number of screen pixels; any other difference means a letter hopped on its
/// own (uneven spacing or a baseline that no longer lines up).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class TextSubpixelShiftTests : BaseTestClass
{
    private const int CaptureWidth = 240;
    private const int CaptureHeight = 120;
    private const int MaxWholePixelTranslation = 5;
    private const string MultiLineText = "Hamburgefonstiv 0123\nQuick brown fox jumps\nover the lazy dog, Wyg";

    [Theory]
    [InlineData(1f)]
    [InlineData(0.75f)]
    [InlineData(1.5f)]
    public void ShiftedTextBlock_IsWholePixelTranslationOfReference_AtZoom(float zoom)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        managers.Renderer.Camera.Zoom = zoom;

        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Text = MultiLineText;
        text.AddToManagers(managers, null);

        try
        {
            XnaColor[] reference = RenderAt(game.GraphicsDevice, managers, text, 20f, 20f);

            float[] shifts = { 0.1f, 0.25f, 0.4f, 0.5f, 0.6f, 0.75f, 0.9f, 1.1f, 1.33f, 2.7f };
            List<string> failures = new();

            foreach (float shiftX in shifts)
            {
                foreach (float shiftY in shifts)
                {
                    XnaColor[] shifted = RenderAt(game.GraphicsDevice, managers, text, 20f + shiftX, 20f + shiftY);

                    if (!MatchesReferenceTranslatedByWholePixels(reference, shifted))
                    {
                        failures.Add($"({shiftX}, {shiftY})");
                    }
                }
            }

            failures.ShouldBeEmpty(
                $"at zoom {zoom} these shifts changed the letters relative to each other, " +
                $"not just moved the whole block by whole pixels: {string.Join(", ", failures)}");
        }
        finally
        {
            text.RemoveFromManagers();
            managers.Renderer.Camera.Zoom = 1f;
        }
    }

    [Theory]
    [InlineData(0.75f, 5)]
    [InlineData(1.5f, 3)]
    public void GlyphsOnTheSameBaseline_HaveTheirBottomOnTheSamePixelRow_AtZoom(float zoom, int lineCount)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        managers.Renderer.Camera.Zoom = zoom;

        // None of these glyphs has a descender, and the spaces keep the glyphs apart on screen.
        const string line = "H T h t W";
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Text = string.Join("\n", System.Linq.Enumerable.Repeat(line, lineCount));
        text.AddToManagers(managers, null);

        try
        {
            List<string> failures = new();

            foreach (float shift in new[] { 0f, 0.3f, 0.6f, 0.9f })
            {
                XnaColor[] pixels = RenderAt(game.GraphicsDevice, managers, text, 5f + shift, 5f + shift);

                List<(int Top, int Bottom)> bands = FindLitRuns(pixels, isRow: true, 0, CaptureWidth, 0, CaptureHeight);
                bands.Count.ShouldBe(lineCount, $"expected one band of glyphs per line at zoom {zoom}");

                for (int bandIndex = 0; bandIndex < bands.Count; bandIndex++)
                {
                    (int top, int bottom) = bands[bandIndex];
                    List<(int Left, int Right)> glyphs = FindLitRuns(pixels, isRow: false, 0, CaptureWidth, top, bottom + 1);
                    glyphs.Count.ShouldBe(5, $"expected 5 glyphs in line {bandIndex} at zoom {zoom}");

                    List<int> bottoms = new();
                    foreach ((int left, int right) in glyphs)
                    {
                        List<(int Top, int Bottom)> rows = FindLitRuns(pixels, isRow: true, left, right + 1, top, bottom + 1);
                        bottoms.Add(rows[rows.Count - 1].Bottom);
                    }

                    if (bottoms.Distinct().Count() > 1)
                    {
                        failures.Add($"shift {shift}, line {bandIndex}: bottom rows {string.Join(",", bottoms)}");
                    }
                }
            }

            failures.ShouldBeEmpty($"glyphs on one baseline ended on different pixel rows at zoom {zoom}: " +
                string.Join("; ", failures));
        }
        finally
        {
            text.RemoveFromManagers();
            managers.Renderer.Camera.Zoom = 1f;
        }
    }

    /// <summary>
    /// Finds runs of consecutive rows (or columns) in the region that contain a lit pixel, separated by
    /// fully dark rows (or columns).
    /// </summary>
    private static List<(int Start, int End)> FindLitRuns(XnaColor[] pixels, bool isRow, int left, int right, int top, int bottom)
    {
        List<(int Start, int End)> runs = new();
        int runStart = -1;
        int from = isRow ? top : left;
        int to = isRow ? bottom : right;

        for (int i = from; i < to; i++)
        {
            bool lit = false;
            int crossFrom = isRow ? left : top;
            int crossTo = isRow ? right : bottom;
            for (int j = crossFrom; j < crossTo && !lit; j++)
            {
                XnaColor pixel = isRow ? pixels[(i * CaptureWidth) + j] : pixels[(j * CaptureWidth) + i];
                lit = pixel.R > 0;
            }

            if (lit && runStart < 0)
            {
                runStart = i;
            }
            else if (!lit && runStart >= 0)
            {
                runs.Add((runStart, i - 1));
                runStart = -1;
            }
        }

        if (runStart >= 0)
        {
            runs.Add((runStart, to - 1));
        }

        return runs;
    }

    private static XnaColor[] RenderAt(GraphicsDevice gd, SystemManagers managers, TextRuntime text, float x, float y)
    {
        text.X = x;
        text.Y = y;
        text.UpdateLayout();

        using RenderTarget2D capture = new(gd, CaptureWidth, CaptureHeight, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] data = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(data);
        return data;
    }

    private static bool MatchesReferenceTranslatedByWholePixels(XnaColor[] reference, XnaColor[] shifted)
    {
        for (int dy = -MaxWholePixelTranslation; dy <= MaxWholePixelTranslation; dy++)
        {
            for (int dx = -MaxWholePixelTranslation; dx <= MaxWholePixelTranslation; dx++)
            {
                if (IsTranslation(reference, shifted, dx, dy))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsTranslation(XnaColor[] reference, XnaColor[] shifted, int dx, int dy)
    {
        // The text never reaches the border, so skip a margin wider than any translation tried.
        const int margin = MaxWholePixelTranslation + 1;

        for (int y = margin; y < CaptureHeight - margin; y++)
        {
            for (int x = margin; x < CaptureWidth - margin; x++)
            {
                XnaColor expected = reference[((y - dy) * CaptureWidth) + (x - dx)];
                XnaColor actual = shifted[(y * CaptureWidth) + x];

                if (expected != actual)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private sealed class MinimalGame : SharedGameLease
    {
        protected override void OnInitialize() => Gum.GumService.Default.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
