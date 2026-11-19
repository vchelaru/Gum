using Gum.ProjectServices.Screenshot;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="ScreenshotAlphaFlattener.FlattenToOpaque"/>, which guarantees a screenshot
/// rendered against a requested opaque <c>--background</c> has no leftover partial transparency
/// from translucent content blended over that background (#4172).
/// </summary>
public class ScreenshotAlphaFlattenerTests
{
    [Fact]
    public void FlattenToOpaque_SetsEveryAlphaByteTo255_PreservingColorBytes()
    {
        byte[] rgba =
        {
            10, 20, 30, 40,     // pixel 0: partially transparent
            50, 60, 70, 255,    // pixel 1: already opaque
            80, 90, 100, 0,     // pixel 2: fully transparent
        };

        ScreenshotAlphaFlattener.FlattenToOpaque(rgba);

        rgba.ShouldBe(new byte[]
        {
            10, 20, 30, 255,
            50, 60, 70, 255,
            80, 90, 100, 255,
        });
    }

    // The flattener runs on every opaque canvas frame, so it works a vector of pixels at a time;
    // a buffer longer than any vector width with a leftover tail covers both paths.
    [Theory]
    [InlineData(1)]
    [InlineData(37)]
    [InlineData(1000)]
    public void FlattenToOpaque_ManyPixels_SetsOnlyAlphaBytes(int pixelCount)
    {
        byte[] rgba = new byte[pixelCount * 4];
        for (int i = 0; i < rgba.Length; i++)
        {
            rgba[i] = (byte)(i * 7 + 3);
        }
        byte[] expected = (byte[])rgba.Clone();
        for (int i = 3; i < expected.Length; i += 4)
        {
            expected[i] = 255;
        }

        ScreenshotAlphaFlattener.FlattenToOpaque(rgba);

        rgba.ShouldBe(expected);
    }

    [Fact]
    public void FlattenToOpaque_WithEmptyBuffer_DoesNotThrow()
    {
        Should.NotThrow(() => ScreenshotAlphaFlattener.FlattenToOpaque(Array.Empty<byte>()));
    }
}
