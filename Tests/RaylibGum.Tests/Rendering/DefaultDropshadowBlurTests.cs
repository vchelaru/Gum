using Gum.GueDeriving;
using Shouldly;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// A shape whose project never sets DropshadowBlur must draw the tool's authored default (3, an
/// isotropic soft shadow), matching MonoGame and Skia (#7299).
/// </summary>
public class DefaultDropshadowBlurTests : BaseTestClass
{
    [Fact]
    public void Arc_DefaultBlur_Is3()
    {
        new ArcRuntime().DropshadowBlur.ShouldBe(3f);
    }

    [Fact]
    public void Circle_DefaultBlur_Is3()
    {
        new CircleRuntime().DropshadowBlur.ShouldBe(3f);
    }

    [Fact]
    public void Rectangle_DefaultBlur_Is3()
    {
        new RectangleRuntime().DropshadowBlur.ShouldBe(3f);
    }
}
