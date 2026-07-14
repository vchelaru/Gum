using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.GoldenImages;

public class PixelComparerTests
{
    [Fact]
    public void Compare_DifferentDimensions_ReturnsMismatch()
    {
        using SKBitmap expected = CreateSolidBitmap(4, 4, SKColors.Red);
        using SKBitmap actual = CreateSolidBitmap(4, 5, SKColors.Red);

        PixelDiffResult result = PixelComparer.Compare(expected, actual);

        result.Matches.ShouldBeFalse();
    }

    [Fact]
    public void Compare_DiffWithinTolerance_ReturnsMatch()
    {
        using SKBitmap expected = CreateSolidBitmap(4, 4, new SKColor(100, 100, 100));
        using SKBitmap actual = CreateSolidBitmap(4, 4, new SKColor(101, 99, 100));

        PixelDiffResult result = PixelComparer.Compare(expected, actual, tolerance: 2);

        result.Matches.ShouldBeTrue();
    }

    [Fact]
    public void Compare_IdenticalBitmaps_ReturnsMatch()
    {
        using SKBitmap expected = CreateSolidBitmap(4, 4, SKColors.Blue);
        using SKBitmap actual = CreateSolidBitmap(4, 4, SKColors.Blue);

        PixelDiffResult result = PixelComparer.Compare(expected, actual);

        result.Matches.ShouldBeTrue();
    }

    [Fact]
    public void Compare_SinglePixelBeyondTolerance_ReportsThatPixelsCoordinates()
    {
        using SKBitmap expected = CreateSolidBitmap(4, 4, SKColors.Black);
        using SKBitmap actual = CreateSolidBitmap(4, 4, SKColors.Black);
        actual.SetPixel(2, 3, new SKColor(50, 0, 0));

        PixelDiffResult result = PixelComparer.Compare(expected, actual, tolerance: 2);

        result.Matches.ShouldBeFalse();
        result.DiffX.ShouldBe(2);
        result.DiffY.ShouldBe(3);
    }

    private static SKBitmap CreateSolidBitmap(int width, int height, SKColor color)
    {
        SKBitmap bitmap = new(width, height);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(color);
        return bitmap;
    }
}
