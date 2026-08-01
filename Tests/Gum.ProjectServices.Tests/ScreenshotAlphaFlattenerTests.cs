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

    [Fact]
    public void FlattenToOpaque_WithEmptyBuffer_DoesNotThrow()
    {
        Should.NotThrow(() => ScreenshotAlphaFlattener.FlattenToOpaque(Array.Empty<byte>()));
    }
}
