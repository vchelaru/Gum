using Raylib_cs;
using Shouldly;

namespace RaylibGum.Tests.Helpers;

/// <summary>
/// A read-back copy of rendered pixels with (0,0) at the top-left, as drawn. Built by
/// <see cref="RaylibPixelHarness"/>; assertions use a per-channel tolerance so they survive
/// differences between GPU drivers and Mesa llvmpipe. Probe interior pixels, not antialiased edges.
/// </summary>
public class PixelGrid
{
    private readonly Color[] _pixels;

    public int Width { get; }
    public int Height { get; }

    public PixelGrid(int width, int height, Color[] pixels)
    {
        Width = width;
        Height = height;
        _pixels = pixels;
    }

    public Color this[int x, int y] => _pixels[y * Width + x];

    /// <summary>Fails unless every channel of the pixel is within <paramref name="tolerance"/> of <paramref name="expected"/>.</summary>
    public void ShouldMatchPixel(int x, int y, Color expected, int tolerance = 12)
    {
        Color actual = this[x, y];
        string message = $"pixel ({x},{y}) was ({actual.R},{actual.G},{actual.B},{actual.A}), expected ({expected.R},{expected.G},{expected.B},{expected.A}) +/- {tolerance}";
        Math.Abs(actual.R - expected.R).ShouldBeLessThanOrEqualTo(tolerance, message);
        Math.Abs(actual.G - expected.G).ShouldBeLessThanOrEqualTo(tolerance, message);
        Math.Abs(actual.B - expected.B).ShouldBeLessThanOrEqualTo(tolerance, message);
        Math.Abs(actual.A - expected.A).ShouldBeLessThanOrEqualTo(tolerance, message);
    }

    /// <summary>Fails unless every pixel in the region (x, y, width, height) is within tolerance of <paramref name="expected"/>.</summary>
    public void ShouldMatchRegion(int x, int y, int width, int height, Color expected, int tolerance = 12)
    {
        for (int row = y; row < y + height; row++)
        {
            for (int column = x; column < x + width; column++)
            {
                ShouldMatchPixel(column, row, expected, tolerance);
            }
        }
    }

    /// <summary>The number of pixels whose channels differ from <paramref name="other"/> by more than <paramref name="tolerance"/>.</summary>
    public int CountDifferences(PixelGrid other, int tolerance = 0)
    {
        other.Width.ShouldBe(Width);
        other.Height.ShouldBe(Height);
        int count = 0;
        for (int i = 0; i < _pixels.Length; i++)
        {
            Color a = _pixels[i];
            Color b = other._pixels[i];
            if (Math.Abs(a.R - b.R) > tolerance || Math.Abs(a.G - b.G) > tolerance ||
                Math.Abs(a.B - b.B) > tolerance || Math.Abs(a.A - b.A) > tolerance)
            {
                count++;
            }
        }
        return count;
    }
}
