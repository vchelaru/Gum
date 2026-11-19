using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// UseFontSmoothing = false draws text with hard (aliased) edges, for the whole Text and for a
/// [UseFontSmoothing=false] run, as MonoGame and raylib do with an unsmoothed font (#8344). An antialiased
/// pixel is one the text covers only partly: alpha strictly between 0 and 255 on a transparent background.
/// </summary>
public class TextFontSmoothingTests
{
    private static Text CreateText(string rawText)
    {
        return new Text
        {
            RawText = rawText,
            FontName = "Arial",
            FontSize = 24,
            Width = 300,
            Height = 60,
            Color = SKColors.Black,
        };
    }

    private static SKBitmap Render(Text text)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(320, 80));
        SystemManagers managers = new SystemManagers { Canvas = surface.Canvas };
        surface.Canvas.Clear(SKColors.Transparent);
        text.Render(managers);
        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    private static int CountPartialPixels(SKBitmap bitmap, Func<SKColor, bool> isRun)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);
                if (pixel.Alpha > 0 && pixel.Alpha < 255 && isRun(pixel))
                {
                    count++;
                }
            }
        }
        return count;
    }

    [Theory]
    // The first frame of a block paints through RichTextKit, later frames through the cached blobs (#7818).
    [InlineData(1)]
    [InlineData(2)]
    public void Render_UseFontSmoothingFalse_DrawsNoAntialiasedPixels(int frames)
    {
        Text text = CreateText("Wg Sample");
        using SKBitmap smoothedBitmap = Render(text);

        text.UseFontSmoothing = false;
        SKBitmap unsmoothedBitmap = Render(text);
        for (int i = 1; i < frames; i++)
        {
            unsmoothedBitmap.Dispose();
            unsmoothedBitmap = Render(text);
        }

        CountPartialPixels(smoothedBitmap, _ => true).ShouldBeGreaterThan(0, "Because smoothed text has antialiased edges");
        CountPartialPixels(unsmoothedBitmap, _ => true).ShouldBe(0, "Because unsmoothed text draws with hard edges");
        unsmoothedBitmap.Dispose();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Render_UseFontSmoothingFalseTag_DrawsOnlyThatRunWithoutAntialiasing(int frames)
    {
        Text text = CreateText(
            "[UseFontSmoothing=false][Color=Red]WgW[/Color][/UseFontSmoothing] [Color=Blue]WgW[/Color]");

        SKBitmap bitmap = Render(text);
        for (int i = 1; i < frames; i++)
        {
            bitmap.Dispose();
            bitmap = Render(text);
        }

        CountPartialPixels(bitmap, pixel => pixel.Red > 0 && pixel.Blue == 0)
            .ShouldBe(0, "Because the [UseFontSmoothing=false] run draws with hard edges");
        CountPartialPixels(bitmap, pixel => pixel.Blue > 0 && pixel.Red == 0)
            .ShouldBeGreaterThan(0, "Because the run outside the tag stays smoothed");
        bitmap.Dispose();
    }

    [Fact]
    public void Render_UseFontSmoothingTrueTagInUnsmoothedText_DrawsThatRunSmoothed()
    {
        Text text = CreateText(
            "[Color=Red]WgW[/Color] [UseFontSmoothing=true][Color=Blue]WgW[/Color][/UseFontSmoothing]");
        text.UseFontSmoothing = false;

        using SKBitmap bitmap = Render(text);

        CountPartialPixels(bitmap, pixel => pixel.Red > 0 && pixel.Blue == 0).ShouldBe(0);
        CountPartialPixels(bitmap, pixel => pixel.Blue > 0 && pixel.Red == 0).ShouldBeGreaterThan(0);
    }
}
