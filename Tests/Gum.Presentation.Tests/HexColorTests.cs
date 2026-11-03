using Gum.ColorPicking;
using Shouldly;

namespace Gum.Presentation.Tests;

public class HexColorTests
{
    [Theory]
    [InlineData("#F80", 255, 136, 0)]
    [InlineData("F80", 255, 136, 0)]
    [InlineData("#FF8800", 255, 136, 0)]
    [InlineData("ff8800", 255, 136, 0)]
    [InlineData("#3d9bD6", 61, 155, 214)]
    [InlineData("  #3D9BD6  ", 61, 155, 214)]
    [InlineData("#000", 0, 0, 0)]
    public void Parse_AcceptsShortAndLongRgb_WithOrWithoutHash_AnyCase(string text, int r, int g, int b)
    {
        HexColorParseResult result = HexColor.Parse(text);

        result.IsValid.ShouldBeTrue();
        result.AlphaIgnored.ShouldBeFalse();
        (result.Red, result.Green, result.Blue).ShouldBe(((byte)r, (byte)g, (byte)b));
    }

    [Theory]
    [InlineData("#AABBCCDD", 170, 187, 204)]
    [InlineData("aabbccdd", 170, 187, 204)]
    [InlineData("#FF880080", 255, 136, 0)]
    [InlineData("#FF880000", 255, 136, 0)]
    public void Parse_RrggbbAa_UsesRgb_AndFlagsTheIgnoredAlpha(string text, int r, int g, int b)
    {
        HexColorParseResult result = HexColor.Parse(text);

        result.IsValid.ShouldBeTrue();
        result.AlphaIgnored.ShouldBeTrue();
        (result.Red, result.Green, result.Blue).ShouldBe(((byte)r, (byte)g, (byte)b));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#")]
    [InlineData("#12")]
    [InlineData("#1234")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("#123456789")]
    [InlineData("#12G456")]
    [InlineData("nonsense")]
    [InlineData("##FF8800")]
    [InlineData("#FF 88 00")]
    [InlineData("0xFF8800")]
    public void Parse_RejectsEverythingElse(string text)
    {
        HexColor.Parse(text).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Parse_Null_IsInvalid()
    {
        HexColor.Parse(null).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Format_WritesUppercaseRrggbb_WithHash_AndNoAlpha()
    {
        HexColor.Format(61, 155, 214).ShouldBe("#3D9BD6");
        HexColor.Format(0, 0, 0).ShouldBe("#000000");
        HexColor.Format(255, 255, 255).ShouldBe("#FFFFFF");
        HexColor.Format(1, 2, 3).ShouldBe("#010203");
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(255, 255, 255)]
    [InlineData(61, 155, 214)]
    [InlineData(1, 2, 3)]
    public void FormatThenParse_RoundTrips(int r, int g, int b)
    {
        HexColorParseResult result = HexColor.Parse(HexColor.Format((byte)r, (byte)g, (byte)b));

        result.IsValid.ShouldBeTrue();
        (result.Red, result.Green, result.Blue).ShouldBe(((byte)r, (byte)g, (byte)b));
    }

    [Theory]
    [InlineData("#")]
    [InlineData("#F")]
    [InlineData("#1234")]
    [InlineData("12345")]
    [InlineData("#1234567")]
    [InlineData("")]
    public void IsIncomplete_IsTrueForAPrefixThatMoreDigitsCouldTurnIntoAColor(string text)
    {
        HexColor.IsIncomplete(text).ShouldBeTrue();
    }

    [Theory]
    [InlineData("#12G4")]
    [InlineData("nonsense")]
    [InlineData("#123456789")]
    [InlineData("##12")]
    public void IsIncomplete_IsFalseForTextNoMoreDigitsCouldFix(string text)
    {
        HexColor.IsIncomplete(text).ShouldBeFalse();
    }

    [Theory]
    [InlineData("#F80")]
    [InlineData("#FF8800")]
    [InlineData("#FF880080")]
    public void IsIncomplete_IsFalseForAColor(string text)
    {
        HexColor.IsIncomplete(text).ShouldBeFalse();
    }
}
