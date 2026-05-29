using Gum.Controls.DataUi;
using Shouldly;

namespace GumToolUnitTests.Controls;

public class HexColorParserTests
{
    public static IEnumerable<object[]> InvalidHexData()
    {
        yield return new object[] { "" };
        yield return new object[] { "   " };
        yield return new object[] { "#" };
        yield return new object[] { "12345" };    // 5 digits
        yield return new object[] { "1234567" };   // 7 digits
        yield return new object[] { "123456789" }; // 9 digits
        yield return new object[] { "GGGGGG" };     // non-hex characters
        yield return new object[] { "12 456" };     // embedded space
        yield return new object[] { "+12345" };     // sign
    }

    [Fact]
    public void ToHexRgb_RoundTripsThroughTryParse()
    {
        string formatted = HexColorParser.ToHexRgb(18, 52, 86);

        bool result = HexColorParser.TryParse(formatted, out byte r, out byte g, out byte b);

        result.ShouldBeTrue();
        r.ShouldBe<byte>(18);
        g.ShouldBe<byte>(52);
        b.ShouldBe<byte>(86);
    }

    [Fact]
    public void ToHexRgb_ShouldFormatAsSixUppercaseDigitsWithoutHash()
    {
        string formatted = HexColorParser.ToHexRgb(0, 17, 147);

        formatted.ShouldBe("001193");
    }

    [Fact]
    public void TryParse_ShouldAcceptLeadingHash()
    {
        bool result = HexColorParser.TryParse("#001193", out byte r, out byte g, out byte b);

        result.ShouldBeTrue();
        r.ShouldBe<byte>(0);
        g.ShouldBe<byte>(17);
        b.ShouldBe<byte>(147);
    }

    [Fact]
    public void TryParse_ShouldParseEightDigitHexAndStripAlpha()
    {
        bool result = HexColorParser.TryParse("#0011937F", out byte r, out byte g, out byte b);

        result.ShouldBeTrue();
        r.ShouldBe<byte>(0);
        g.ShouldBe<byte>(17);
        b.ShouldBe<byte>(147);
    }

    [Fact]
    public void TryParse_ShouldParseSixDigitHex()
    {
        bool result = HexColorParser.TryParse("FF8000", out byte r, out byte g, out byte b);

        result.ShouldBeTrue();
        r.ShouldBe<byte>(255);
        g.ShouldBe<byte>(128);
        b.ShouldBe<byte>(0);
    }

    [Theory]
    [MemberData(nameof(InvalidHexData))]
    public void TryParse_ShouldRejectInvalidInput(string input)
    {
        bool result = HexColorParser.TryParse(input, out byte r, out byte g, out byte b);

        result.ShouldBeFalse();
        r.ShouldBe<byte>(0);
        g.ShouldBe<byte>(0);
        b.ShouldBe<byte>(0);
    }

    [Fact]
    public void TryParse_ShouldRejectNull()
    {
        bool result = HexColorParser.TryParse(null, out byte r, out byte g, out byte b);

        result.ShouldBeFalse();
    }

    [Fact]
    public void TryParse_ShouldTrimSurroundingWhitespace()
    {
        bool result = HexColorParser.TryParse("  #001193  ", out byte r, out byte g, out byte b);

        result.ShouldBeTrue();
        r.ShouldBe<byte>(0);
        g.ShouldBe<byte>(17);
        b.ShouldBe<byte>(147);
    }
}
