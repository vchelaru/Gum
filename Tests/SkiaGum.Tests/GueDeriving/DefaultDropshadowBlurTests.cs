using Gum.GueDeriving;
using Shouldly;

namespace SkiaGum.Tests.GueDeriving;

#pragma warning disable CS0618
/// <summary>
/// A shape whose project never sets DropshadowBlur must draw the tool's authored default (3, an
/// isotropic soft shadow) on every shape, matching MonoGame and raylib (#7299).
/// </summary>
public class DefaultDropshadowBlurTests
{
    [Fact]
    public void Arc_DefaultBlur_IsIsotropic3()
    {
        ArcRuntime sut = new();
        sut.DropshadowBlurX.ShouldBe(3f);
        sut.DropshadowBlurY.ShouldBe(3f);
    }

    [Fact]
    public void RoundedRectangle_DefaultBlur_IsIsotropic3()
    {
        RoundedRectangleRuntime sut = new();
        sut.DropshadowBlurX.ShouldBe(3f);
        sut.DropshadowBlurY.ShouldBe(3f);
    }

    [Fact]
    public void ColoredCircle_DefaultBlur_IsIsotropic3()
    {
        ColoredCircleRuntime sut = new();
        sut.DropshadowBlurX.ShouldBe(3f);
        sut.DropshadowBlurY.ShouldBe(3f);
    }

    [Fact]
    public void Line_DefaultBlur_IsIsotropic3()
    {
        LineRuntime sut = new();
        sut.DropshadowBlurX.ShouldBe(3f);
        sut.DropshadowBlurY.ShouldBe(3f);
    }
}
