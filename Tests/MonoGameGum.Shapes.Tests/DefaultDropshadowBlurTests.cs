using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

#pragma warning disable CS0618
/// <summary>
/// A shape whose project never sets DropshadowBlur must draw the tool's authored default (3, an
/// isotropic soft shadow) on every shape, not a hard edge on one axis (#7299).
/// </summary>
public class DefaultDropshadowBlurTests
{
    [Fact]
    public void Arc_DefaultBlur_IsIsotropic3()
    {
        AssertIsotropic3(new ArcRuntime());
    }

    [Fact]
    public void RoundedRectangle_DefaultBlur_IsIsotropic3()
    {
        AssertIsotropic3(new RoundedRectangleRuntime());
    }

    [Fact]
    public void ColoredCircle_DefaultBlur_IsIsotropic3()
    {
        AssertIsotropic3(new ColoredCircleRuntime());
    }

    [Fact]
    public void Line_DefaultBlur_IsIsotropic3()
    {
        AssertIsotropic3(new LineRuntime());
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

    private static void AssertIsotropic3(GraphicalUiElement sut)
    {
        RenderableShapeBase shape = (RenderableShapeBase)sut.RenderableComponent;
        shape.DropshadowBlurX.ShouldBe(3f);
        shape.DropshadowBlurY.ShouldBe(3f);
    }
}
