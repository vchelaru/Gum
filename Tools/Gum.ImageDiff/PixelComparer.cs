using System;
using SkiaSharp;

namespace Gum.ImageDiff;

/// <summary>
/// Per-pixel, per-channel comparison for two rendered images. Tolerance absorbs the
/// antialiasing/hinting drift that makes exact-pixel image comparisons brittle across
/// renderers, Skia versions, and platforms.
/// </summary>
/// <remarks>
/// Shared by <c>SkiaGum.Tests</c>' golden-image regression tests and
/// <c>gumcli diff-screenshots</c> (#4174), which decodes each backend's rendered PNG through
/// SkiaSharp purely for pixel comparison — no SkiaGum rendering happens in that path. Lives in its
/// own dependency-free project (only SkiaSharp) rather than Gum.ProjectServices.SkiaGum so it can be
/// referenced from projects that already file-link SkiaGum.Standalone's GumService.cs (e.g.
/// SkiaGum.Tests) without a duplicate-type conflict on that shared source.
/// </remarks>
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
