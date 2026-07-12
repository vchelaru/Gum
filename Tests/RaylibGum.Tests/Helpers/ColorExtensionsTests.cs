using Raylib_cs;
using RaylibGum.Helpers;
using Shouldly;

namespace RaylibGum.Tests.Helpers;

public class ColorExtensionsTests : BaseTestClass
{
    [Fact]
    public void White_ShouldBeRaylibWhite()
    {
        ColorExtensions.White.ShouldBe(Color.White);
    }

    [Fact]
    public void WithAlpha_ShouldSetAlpha_AndLeaveOtherChannelsIntact()
    {
        Color color = new Color((byte)10, (byte)20, (byte)30, (byte)40);
        Color result = ColorExtensions.WithAlpha(color, 200);
        result.R.ShouldBe((byte)10);
        result.G.ShouldBe((byte)20);
        result.B.ShouldBe((byte)30);
        result.A.ShouldBe((byte)200);
    }

    [Fact]
    public void WithBlue_ShouldSetBlue_AndLeaveOtherChannelsIntact()
    {
        Color color = new Color((byte)10, (byte)20, (byte)30, (byte)40);
        Color result = ColorExtensions.WithBlue(color, 200);
        result.R.ShouldBe((byte)10);
        result.G.ShouldBe((byte)20);
        result.B.ShouldBe((byte)200);
        result.A.ShouldBe((byte)40);
    }

    [Fact]
    public void WithGreen_ShouldSetGreen_AndLeaveOtherChannelsIntact()
    {
        Color color = new Color((byte)10, (byte)20, (byte)30, (byte)40);
        Color result = ColorExtensions.WithGreen(color, 200);
        result.R.ShouldBe((byte)10);
        result.G.ShouldBe((byte)200);
        result.B.ShouldBe((byte)30);
        result.A.ShouldBe((byte)40);
    }

    [Fact]
    public void WithRed_ShouldSetRed_AndLeaveOtherChannelsIntact()
    {
        Color color = new Color((byte)10, (byte)20, (byte)30, (byte)40);
        Color result = ColorExtensions.WithRed(color, 200);
        result.R.ShouldBe((byte)200);
        result.G.ShouldBe((byte)20);
        result.B.ShouldBe((byte)30);
        result.A.ShouldBe((byte)40);
    }

    [Fact]
    public void ToRaylib_ShouldConvertChannelsFromSystemDrawingColor()
    {
        System.Drawing.Color drawingColor = System.Drawing.Color.FromArgb(40, 10, 20, 30);

        Color result = drawingColor.ToRaylib();

        result.R.ShouldBe((byte)10);
        result.G.ShouldBe((byte)20);
        result.B.ShouldBe((byte)30);
        result.A.ShouldBe((byte)40);
    }
}
