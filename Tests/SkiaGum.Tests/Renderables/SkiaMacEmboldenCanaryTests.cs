using Shouldly;
using SkiaSharp;
using System.IO;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Canary for https://github.com/mono/SkiaSharp/issues/5891: on macOS, SKFont.Embolden at Normal hinting draws no
/// heavier than regular, which GumFontMapper.GetSyntheticBoldStrokeWidth works around. Raw SkiaSharp, no Gum.
/// </summary>
public class SkiaMacEmboldenCanaryTests
{
    private static long Ink(SKTypeface typeface, bool isEmboldened)
    {
        using SKSurface surface = SKSurface.Create(new SKImageInfo(220, 40));
        surface.Canvas.Clear(SKColors.Transparent);
        using SKFont font = new SKFont(typeface, 14) { Embolden = isEmboldened, Hinting = SKFontHinting.Normal, Edging = SKFontEdging.Antialias };
        using SKPaint paint = new SKPaint { Color = SKColors.Black };
        surface.Canvas.DrawText("Dragon Armor", 5, 30, font, paint);
        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        long total = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                total += bitmap.GetPixel(x, y).Alpha;
            }
        }
        return total;
    }

    [Fact]
    public void Embolden_AtNormalHintingOnMacOS_IsStillNoHeavierThanRegular()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }
        using SKTypeface typeface = SKTypeface.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "LiberationSans-Regular.ttf"));

        long regular = Ink(typeface, isEmboldened: false);
        long bold = Ink(typeface, isEmboldened: true);

        // Measured 0.99 while the bug stands; a fixed Skia gives about 1.4, as with Hinting.None.
        bold.ShouldBeLessThan(regular * 11 / 10,
            "The upstream bug (https://github.com/mono/SkiaSharp/issues/5891) looks fixed: delete the macOS workaround " +
            "in SkiaGum GumFontMapper.GetSyntheticBoldStrokeWidth (MacSyntheticBoldStrokePerFontSize) and this test.");
    }
}
