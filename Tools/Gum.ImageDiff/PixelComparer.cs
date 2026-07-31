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

    /// <summary>
    /// Aggregate, proximity-tolerant comparison for two images rendered by different backends.
    /// </summary>
    /// <remarks>
    /// A pixel that fails the strict same-coordinate tolerance check is only counted as a real
    /// mismatch if no pixel within <paramref name="proximityRadius"/> of it in <paramref
    /// name="actual"/> matches <paramref name="expected"/>'s color — this absorbs the few-pixel
    /// positional jitter two different renderers' antialiasing/rounding produces at edges without
    /// masking pixels that are actually wrong. Reports the mismatch count/percentage and bounding
    /// box of all real mismatches, rather than <see cref="Compare"/>'s single first-differing pixel
    /// — a global shift or one missing region look identical under "first pixel that differs."
    /// </remarks>
    public static ImageDiffResult CompareApproximate(
        SKBitmap expected, SKBitmap actual, byte colorTolerance = 2, int proximityRadius = 1)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
        {
            return ImageDiffResult.DimensionMismatch(expected.Width, expected.Height, actual.Width, actual.Height);
        }

        int width = expected.Width;
        int height = expected.Height;
        int mismatchCount = 0;
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SKColor expectedPixel = expected.GetPixel(x, y);
                if (MaxChannelDifference(expectedPixel, actual.GetPixel(x, y)) <= colorTolerance)
                {
                    continue;
                }

                if (HasNearbyMatch(actual, expectedPixel, x, y, colorTolerance, proximityRadius, width, height))
                {
                    continue;
                }

                mismatchCount++;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        int totalPixelCount = width * height;
        return mismatchCount == 0
            ? ImageDiffResult.Match(totalPixelCount)
            : ImageDiffResult.Mismatch(mismatchCount, totalPixelCount, minX, minY, maxX, maxY);
    }

    private static bool HasNearbyMatch(
        SKBitmap actual, SKColor expectedPixel, int x, int y, byte colorTolerance, int radius, int width, int height)
    {
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                int nx = x + dx;
                int ny = y + dy;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                {
                    continue;
                }

                if (MaxChannelDifference(expectedPixel, actual.GetPixel(nx, ny)) <= colorTolerance)
                {
                    return true;
                }
            }
        }

        return false;
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
