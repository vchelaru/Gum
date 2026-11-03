using Gum.ColorPicking;
using Shouldly;

namespace Gum.Presentation.Tests;

public class HsvRgbTests
{
    [Theory]
    [InlineData(0, 1, 1, 255, 0, 0)]
    [InlineData(60, 1, 1, 255, 255, 0)]
    [InlineData(120, 1, 1, 0, 255, 0)]
    [InlineData(180, 1, 1, 0, 255, 255)]
    [InlineData(240, 1, 1, 0, 0, 255)]
    [InlineData(300, 1, 1, 255, 0, 255)]
    [InlineData(360, 1, 1, 255, 0, 0)]
    [InlineData(0, 0, 1, 255, 255, 255)]
    [InlineData(200, 1, 0, 0, 0, 0)]
    public void ToRgb_MatchesTheKnownColors(double h, double s, double v, int r, int g, int b)
    {
        (byte Red, byte Green, byte Blue) rgb = HsvRgb.ToRgb(h, s, v);

        rgb.ShouldBe(((byte)r, (byte)g, (byte)b));
    }

    [Theory]
    [InlineData(255, 0, 0, 0)]
    [InlineData(0, 255, 0, 120)]
    [InlineData(0, 0, 255, 240)]
    [InlineData(255, 136, 0, 32)]
    [InlineData(163, 71, 217, 277.8)]
    public void ToHsv_FindsTheHue(int r, int g, int b, double hue)
    {
        (double H, double S, double V) hsv = HsvRgb.ToHsv((byte)r, (byte)g, (byte)b);

        hsv.H.ShouldBe(hue, tolerance: 1);
    }

    [Fact]
    public void ToHsv_WhiteAndBlackHaveNoSaturationOrValueToSpeakOf()
    {
        HsvRgb.ToHsv(255, 255, 255).ShouldBe((0, 0, 1));
        HsvRgb.ToHsv(0, 0, 0).ShouldBe((0, 0, 0));
    }

    [Fact]
    public void EveryRgbRoundTripsThroughHsv_OnASampledGrid()
    {
        for (int r = 0; r < 256; r += 17)
        {
            for (int g = 0; g < 256; g += 17)
            {
                for (int b = 0; b < 256; b += 17)
                {
                    (double H, double S, double V) hsv = HsvRgb.ToHsv((byte)r, (byte)g, (byte)b);
                    HsvRgb.ToRgb(hsv.H, hsv.S, hsv.V).ShouldBe(((byte)r, (byte)g, (byte)b));
                }
            }
        }
    }

    [Fact]
    public void ToRgb_WrapsNegativeAndLargeHues()
    {
        HsvRgb.ToRgb(-120, 1, 1).ShouldBe(HsvRgb.ToRgb(240, 1, 1));
        HsvRgb.ToRgb(480, 1, 1).ShouldBe(HsvRgb.ToRgb(120, 1, 1));
    }
}
