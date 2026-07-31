using Gum.ImageDiff;
using Shouldly;
using SkiaSharp;

namespace Gum.ImageDiff.Tests;

/// <summary>
/// Tests for <see cref="PixelComparer.CompareApproximate"/>, the cross-renderer image comparer used
/// by <c>gumcli diff-screenshots</c> (#4174). Unlike <see cref="PixelComparer.Compare"/> (exact
/// same-coordinate comparison for single-renderer golden-image regression tests), this tolerates a
/// small positional shift between two different renderers' antialiasing/rounding before counting a
/// pixel as a real mismatch, and reports aggregate stats instead of only the first differing pixel.
/// </summary>
public class ImageDiffResultTests
{
    [Fact]
    public void CompareApproximate_IdenticalBitmaps_ReturnsMatchWithZeroMismatches()
    {
        using SKBitmap expected = CreateSolidBitmap(10, 10, SKColors.Blue);
        using SKBitmap actual = CreateSolidBitmap(10, 10, SKColors.Blue);

        ImageDiffResult result = PixelComparer.CompareApproximate(expected, actual);

        result.Matches.ShouldBeTrue();
        result.MismatchedPixelCount.ShouldBe(0);
        result.TotalPixelCount.ShouldBe(100);
    }

    [Fact]
    public void CompareApproximate_ContentShiftedByOnePixel_AbsorbedAsMatch()
    {
        // A single red pixel on a white background, shifted one pixel to the right between the two
        // images — exactly the antialiasing/rounding jitter a proximity check exists to absorb.
        using SKBitmap expected = CreateSolidBitmap(5, 5, SKColors.White);
        expected.SetPixel(2, 2, SKColors.Red);
        using SKBitmap actual = CreateSolidBitmap(5, 5, SKColors.White);
        actual.SetPixel(3, 2, SKColors.Red);

        ImageDiffResult result = PixelComparer.CompareApproximate(expected, actual, colorTolerance: 2, proximityRadius: 1);

        result.Matches.ShouldBeTrue();
        result.MismatchedPixelCount.ShouldBe(0);
    }

    [Fact]
    public void CompareApproximate_ContentMissingEntirely_ReportsRealMismatchWithBoundingBox()
    {
        // A 3x3 red block present in expected but entirely absent from actual — no amount of nearby
        // searching finds a match, so this must be reported as a real, sizeable mismatch.
        using SKBitmap expected = CreateSolidBitmap(10, 10, SKColors.White);
        FillRegion(expected, 3, 3, 3, 3, SKColors.Red);
        using SKBitmap actual = CreateSolidBitmap(10, 10, SKColors.White);

        ImageDiffResult result = PixelComparer.CompareApproximate(expected, actual, colorTolerance: 2, proximityRadius: 1);

        result.Matches.ShouldBeFalse();
        result.MismatchedPixelCount.ShouldBe(9);
        result.BoundingBoxMinX.ShouldBe(3);
        result.BoundingBoxMinY.ShouldBe(3);
        result.BoundingBoxMaxX.ShouldBe(5);
        result.BoundingBoxMaxY.ShouldBe(5);
    }

    [Fact]
    public void CompareApproximate_DifferentDimensions_ReturnsMismatchWithDescription()
    {
        using SKBitmap expected = CreateSolidBitmap(4, 4, SKColors.Red);
        using SKBitmap actual = CreateSolidBitmap(4, 5, SKColors.Red);

        ImageDiffResult result = PixelComparer.CompareApproximate(expected, actual);

        result.Matches.ShouldBeFalse();
        result.DimensionMismatchDescription.ShouldNotBeNull();
    }

    [Fact]
    public void CompareApproximate_ShiftBeyondProximityRadius_ReportsRealMismatch()
    {
        // Same shift as the "absorbed" test above, but with proximityRadius: 0 (no neighborhood
        // search at all) the shift must NOT be absorbed — proves the radius parameter is load-bearing.
        using SKBitmap expected = CreateSolidBitmap(5, 5, SKColors.White);
        expected.SetPixel(2, 2, SKColors.Red);
        using SKBitmap actual = CreateSolidBitmap(5, 5, SKColors.White);
        actual.SetPixel(3, 2, SKColors.Red);

        ImageDiffResult result = PixelComparer.CompareApproximate(expected, actual, colorTolerance: 2, proximityRadius: 0);

        result.Matches.ShouldBeFalse();
        result.MismatchedPixelCount.ShouldBe(2);
    }

    private static SKBitmap CreateSolidBitmap(int width, int height, SKColor color)
    {
        SKBitmap bitmap = new(width, height);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(color);
        return bitmap;
    }

    private static void FillRegion(SKBitmap bitmap, int x, int y, int width, int height, SKColor color)
    {
        for (int i = x; i < x + width; i++)
        {
            for (int j = y; j < y + height; j++)
            {
                bitmap.SetPixel(i, j, color);
            }
        }
    }
}
