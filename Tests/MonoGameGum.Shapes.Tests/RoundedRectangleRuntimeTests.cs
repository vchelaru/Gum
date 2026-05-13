using Gum.GueDeriving;
using MonoGameAndGum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Per-corner radii were Skia-only until Apos.Shapes 0.6.9 exposed CornerRadii. These tests guard
// the Apos-side parity wiring: the runtime exposes the four nullable Custom* properties and
// forwards each to the underlying RoundedRectangle renderable.
//
// Clone parity is intentionally not covered — the Apos RoundedRectangle renderable doesn't
// implement ICloneable, so any runtime Clone call throws before reaching the new properties.
// That's a pre-existing gap and unrelated to per-corner radii; themes instantiate visuals via
// `new`, not Clone, so this doesn't block.
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
