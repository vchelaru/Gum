using System.Windows.Media;
using Gum.Controls.DataUi;
using Shouldly;

namespace GumToolUnitTests.Controls;

public class ColorDisplayLogicTests
{
    [Fact]
    public void ToOpaqueSwatchColor_FullyTransparentColor_ReturnsOpaqueRgb()
    {
        Color transparent = Color.FromArgb(0, 10, 20, 30);

        Color result = ColorDisplayLogic.ToOpaqueSwatchColor(transparent);

        result.A.ShouldBe((byte)255);
        result.R.ShouldBe((byte)10);
        result.G.ShouldBe((byte)20);
        result.B.ShouldBe((byte)30);
    }

    [Fact]
    public void ToOpaqueSwatchColor_OpaqueColor_ReturnsSameRgb()
    {
        Color opaque = Color.FromArgb(255, 100, 150, 200);

        Color result = ColorDisplayLogic.ToOpaqueSwatchColor(opaque);

        result.ShouldBe(opaque);
    }

    [Fact]
    public void ToOpaqueSwatchColor_PartiallyTransparentColor_DiscardsAlpha()
    {
        Color partial = Color.FromArgb(128, 200, 50, 75);

        Color result = ColorDisplayLogic.ToOpaqueSwatchColor(partial);

        result.A.ShouldBe((byte)255);
        result.R.ShouldBe((byte)200);
        result.G.ShouldBe((byte)50);
        result.B.ShouldBe((byte)75);
    }
}
