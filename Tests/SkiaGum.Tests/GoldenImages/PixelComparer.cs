using SkiaSharp;

namespace SkiaGum.Tests.GoldenImages;

/// <summary>
/// Per-pixel, per-channel comparison for golden-image tests. Tolerance absorbs the
/// antialiasing/hinting drift that makes exact-pixel image comparisons brittle across
/// Skia versions and platforms.
/// </summary>
public static class PixelComparer
{
    public static PixelDiffResult Compare(SKBitmap expected, SKBitmap actual, byte tolerance = 2)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
        {
            return PixelDiffResult.DimensionMismatch(expected.Width, expected.Height, actual.Width, actual.Height);
        }

        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                int diff = MaxChannelDifference(expected.GetPixel(x, y), actual.GetPixel(x, y));
                if (diff > tolerance)
                {
                    return PixelDiffResult.Mismatch(x, y, diff);
                }
            }
        }

        return PixelDiffResult.Match();
    }

    private static int MaxChannelDifference(SKColor expected, SKColor actual)
    {
        int diff = Math.Abs(expected.Red - actual.Red);
        diff = Math.Max(diff, Math.Abs(expected.Green - actual.Green));
        diff = Math.Max(diff, Math.Abs(expected.Blue - actual.Blue));
        diff = Math.Max(diff, Math.Abs(expected.Alpha - actual.Alpha));
        return diff;
    }
}
