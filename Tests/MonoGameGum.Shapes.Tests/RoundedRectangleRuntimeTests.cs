using Gum.GueDeriving;
using MonoGameAndGum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Per-corner radii were Skia-only until Apos.Shapes 0.6.9 exposed CornerRadii. These tests guard
// the Apos-side parity wiring: the runtime exposes the four nullable Custom* properties and
// forwards each to the underlying RoundedRectangle renderable.
// These tests cover the obsolete RoundedRectangleRuntime itself.
#pragma warning disable CS0618
public class RoundedRectangleRuntimeTests
{
    [Fact]
    public void CustomRadiusBottomLeft_ShouldForwardTo_Renderable()
    {
        var sut = new RoundedRectangleRuntime();
        sut.CustomRadiusBottomLeft = 12f;
        var renderable = (RoundedRectangle)sut.RenderableComponent;
        renderable.CustomRadiusBottomLeft.ShouldBe(12f);
    }

    [Fact]
    public void CustomRadiusBottomRight_ShouldForwardTo_Renderable()
    {
        var sut = new RoundedRectangleRuntime();
        sut.CustomRadiusBottomRight = 2f;
        var renderable = (RoundedRectangle)sut.RenderableComponent;
        renderable.CustomRadiusBottomRight.ShouldBe(2f);
    }

    [Fact]
    public void CustomRadiusTopLeft_ShouldDefaultToNull()
    {
        var sut = new RoundedRectangleRuntime();
        sut.CustomRadiusTopLeft.ShouldBeNull();
        sut.CustomRadiusTopRight.ShouldBeNull();
        sut.CustomRadiusBottomRight.ShouldBeNull();
        sut.CustomRadiusBottomLeft.ShouldBeNull();
    }

    [Fact]
    public void CustomRadiusTopLeft_ShouldForwardTo_Renderable()
    {
        var sut = new RoundedRectangleRuntime();
        sut.CustomRadiusTopLeft = 2f;
        var renderable = (RoundedRectangle)sut.RenderableComponent;
        renderable.CustomRadiusTopLeft.ShouldBe(2f);
    }

    [Fact]
    public void CustomRadiusTopRight_ShouldForwardTo_Renderable()
    {
        var sut = new RoundedRectangleRuntime();
        sut.CustomRadiusTopRight = 12f;
        var renderable = (RoundedRectangle)sut.RenderableComponent;
        renderable.CustomRadiusTopRight.ShouldBe(12f);
    }

    [Fact]
    public void Renderable_DefaultCustomRadii_ShouldAllBeNull()
    {
        var renderable = new RoundedRectangle();
        renderable.CustomRadiusTopLeft.ShouldBeNull();
        renderable.CustomRadiusTopRight.ShouldBeNull();
        renderable.CustomRadiusBottomRight.ShouldBeNull();
        renderable.CustomRadiusBottomLeft.ShouldBeNull();
    }
}
#pragma warning restore CS0618
