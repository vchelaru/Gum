using Gum.ProjectServices.Screenshot;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="ScreenshotColor.TryParse"/>, which parses the <c>gumcli screenshot
/// --background</c> option so screenshots can be rendered against an opaque backdrop instead of
/// the default transparent one (#4172 - transparent vs. opaque compositing look very different,
/// which made raylib-vs-tool comparisons misleading).
/// </summary>
public class ScreenshotColorTests
{
    [Fact]
    public void TryParse_WithSixDigitHex_ReturnsOpaqueColor()
    {
        bool success = ScreenshotColor.TryParse("1E2A38", out ScreenshotColor color);

        success.ShouldBeTrue();
        color.R.ShouldBe((byte)0x1E);
        color.G.ShouldBe((byte)0x2A);
        color.B.ShouldBe((byte)0x38);
        color.A.ShouldBe((byte)255);
    }

    [Fact]
    public void TryParse_WithLeadingHash_StripsItAndParsesSameAsWithout()
    {
        ScreenshotColor.TryParse("#1E2A38", out ScreenshotColor withHash);
        ScreenshotColor.TryParse("1E2A38", out ScreenshotColor withoutHash);

        withHash.R.ShouldBe(withoutHash.R);
        withHash.G.ShouldBe(withoutHash.G);
        withHash.B.ShouldBe(withoutHash.B);
        withHash.A.ShouldBe(withoutHash.A);
    }

    [Fact]
    public void TryParse_WithEightDigitHex_ReturnsColorWithParsedAlpha()
    {
        bool success = ScreenshotColor.TryParse("1E2A3880", out ScreenshotColor color);

        success.ShouldBeTrue();
        color.R.ShouldBe((byte)0x1E);
        color.G.ShouldBe((byte)0x2A);
        color.B.ShouldBe((byte)0x38);
        color.A.ShouldBe((byte)0x80);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1E2A3")]
    [InlineData("1E2A388")]
    [InlineData("GGHHII")]
    public void TryParse_WithInvalidInput_ReturnsFalse(string? invalidHex)
    {
        bool success = ScreenshotColor.TryParse(invalidHex, out ScreenshotColor color);

        success.ShouldBeFalse();
        color.ShouldBe(default);
    }
}
