using System;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Built-in BBCode effects on Skia are applied at draw time to the cached blobs, from each glyph's laid-out x, so
/// they match MonoGame and raylib without laying the text out again.
/// </summary>
public class TextBuiltInEffectTests : BaseTestClass
{
    readonly Func<double> _originalClock = BbCodeTextEffect.TimeSeconds;

    public override void Dispose()
    {
        BbCodeTextEffect.TimeSeconds = _originalClock;
        base.Dispose();
    }

    private static Text MakeText(string rawText)
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 40;
        text.Width = 1000;
        text.Color = SKColors.Black;
        text.X = 20;
        text.Y = 40;
        text.RawText = rawText;
        return text;
    }

    private static SKBitmap Draw(SKSurface surface, Text text)
    {
        surface.Canvas.Clear(SKColors.Transparent);
        SystemManagers managers = new() { Canvas = surface.Canvas };
        text.Render(managers);
        using SKImage image = surface.Snapshot();
        return SKBitmap.FromImage(image);
    }

    // The topmost inked row in columns [left, right).
    private static int InkTop(SKBitmap bitmap, int left = 0, int right = int.MaxValue, int threshold = 128)
    {
        right = Math.Min(right, bitmap.Width);
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = left; x < right; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha > threshold)
                {
                    return y;
                }
            }
        }
        return -1;
    }

    private static SKColor FirstInk(SKBitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);
                if (pixel.Alpha == 255)
                {
                    return pixel;
                }
            }
        }
        return SKColor.Empty;
    }

    [Fact]
    public void Render_Wave_MovesTheLetterOnTheFirstAndLaterFrames()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 200));
        int plainTop = InkTop(Draw(surface, MakeText("I")));
        Text text = MakeText("[Wave amp=200 freq=1]I[/Wave]");

        // sin(freq * t + x / 50) * amp / 10 with the letter at x = 0.
        BbCodeTextEffect.TimeSeconds = () => Math.PI / 2;
        using SKBitmap first = Draw(surface, text);
        using SKBitmap second = Draw(surface, text);
        BbCodeTextEffect.TimeSeconds = () => 3 * Math.PI / 2;
        using SKBitmap third = Draw(surface, text);

        ((float)(InkTop(first) - plainTop)).ShouldBe(20f, tolerance: 1);
        ((float)(InkTop(second) - plainTop)).ShouldBe(20f, tolerance: 1);
        ((float)(InkTop(third) - plainTop)).ShouldBe(-20f, tolerance: 1);
    }

    [Fact]
    public void Render_Wave_PhasesEachLetterByItsRealX()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(400, 200));
        // freq 0 leaves only the x / 50 phase: the first letter (x = 0) does not move, a later one does.
        Text text = MakeText("[Wave amp=200 freq=0]IIIIIII[/Wave]");
        Text plain = MakeText("IIIIIII");

        using SKBitmap plainBitmap = Draw(surface, plain);
        int plainFirst = InkTop(plainBitmap, 0, 30);
        using SKBitmap waved = Draw(surface, text);

        ((float)InkTop(waved, 0, 30)).ShouldBe(plainFirst, tolerance: 1);
        InkTop(waved, 60, 200).ShouldBeGreaterThan(plainFirst + 5);
    }

    [Fact]
    public void Render_Rainbow_ColorsTheLetterWithoutRelayout()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 200));
        Text text = MakeText("[Rainbow]I[/Rainbow]");
        BbCodeTextEffect.TimeSeconds = () => 0;

        using SKBitmap first = Draw(surface, text);
        Topten.RichTextKit.TextBlock block = text.GetCachedTextBlock();
        BbCodeTextEffect.TimeSeconds = () => 0.5;
        using SKBitmap later = Draw(surface, text);

        // Okhsv hue 0 at t = 0 and hue 0.5 at t = 0.5, sat 0.8, val 0.8 (the parity table's values).
        FirstInk(first).ShouldBe(new SKColor(201, 66, 119));
        FirstInk(later).ShouldBe(new SKColor(59, 199, 177));
        text.GetCachedTextBlock().ShouldBeSameAs(block);
    }

    [Fact]
    public void Render_FadeWithMaxLettersToShow_DrawsTheRevealedLetter()
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(400, 200));
        Text text = MakeText("[Fade length=1]II[/Fade]III");
        text.MaxLettersToShow = 1;

        using SKBitmap bitmap = Draw(surface, text);

        // The only revealed letter is the fade's first, at full alpha; nothing else is drawn.
        InkTop(bitmap).ShouldBeGreaterThan(-1);
        InkTop(bitmap, 40, 400).ShouldBe(-1);
    }

    // The same table runs on MonoGame and raylib (BbCodeEffectParityCases), so every runtime draws the same offsets
    // and colors.
    [Theory]
    [MemberData(nameof(BbCodeEffectParityCases.All), MemberType = typeof(BbCodeEffectParityCases))]
    public void Render_Effect_MatchesTheParityTable(string open, string close, double time, float dx, float dy,
        int red, int green, int blue, int alpha)
    {
        Text.Customizations[BbCodeEffectParityCases.ShiftFunctionName] = (_, _) =>
            new LetterCustomization { YOffset = BbCodeEffectParityCases.ShiftYOffset, Color = System.Drawing.Color.Red };
        using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 200));
        Text plain = MakeText("A");
        plain.Color = SKColors.White;
        using SKBitmap plainBitmap = Draw(surface, plain);
        Text text = MakeText($"{open}A{close}");
        text.Color = SKColors.White;
        BbCodeTextEffect.TimeSeconds = () => time;

        using SKBitmap bitmap = Draw(surface, text);
        Text.Customizations.Remove(BbCodeEffectParityCases.ShiftFunctionName);

        // Ink edges are whole pixels.
        ((float)(InkLeft(bitmap) - InkLeft(plainBitmap))).ShouldBe(dx, 1f);
        ((float)(InkTop(bitmap, 0, 200, 8) - InkTop(plainBitmap, 0, 200, 8))).ShouldBe(dy, 1f);
        SKColor color = MostOpaque(bitmap);
        ((float)color.Red).ShouldBe(red, 2f);
        ((float)color.Green).ShouldBe(green, 2f);
        ((float)color.Blue).ShouldBe(blue, 2f);
        ((float)color.Alpha).ShouldBe(alpha, 2f);
    }

    private static int InkLeft(SKBitmap bitmap)
    {
        for (int x = 0; x < bitmap.Width; x++)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 8)
                {
                    return x;
                }
            }
        }
        return -1;
    }

    private static SKColor MostOpaque(SKBitmap bitmap)
    {
        SKColor best = SKColor.Empty;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);
                if (pixel.Alpha > best.Alpha)
                {
                    best = pixel;
                }
            }
        }
        return best;
    }
}
