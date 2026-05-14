using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Math.Geometry;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Spike (#2758): proves the renderable-swap mechanism. A core CircleRuntime (in MonoGameGum,
// which does NOT reference Apos.Shapes) acquires a fill-capable Apos.Shapes renderable across
// the optional-dependency boundary via ShapeRenderableRegistry, which the optional
// MonoGameGumShapes package populates from its [ModuleInitializer].
public class CircleRuntimeFillColorTests
{
    public CircleRuntimeFillColorTests()
    {
        // CircleRuntime lives in core MonoGameGum, so newing one does not load the
        // MonoGameGumShapes assembly or fire its [ModuleInitializer]. Touch the registration
        // explicitly so the registry is populated. The call is idempotent.
        AposShapeRuntime.RegisterRuntimeTypes();
    }

    [Fact]
    public void FillColor_WhenClearedToNull_SwapsRenderableBackToLineCircle()
    {
        CircleRuntime sut = new();
        sut.FillColor = Color.Red;
        sut.FillColor = null;

        sut.RenderableComponent.ShouldBeOfType<LineCircle>();
    }

    [Fact]
    public void FillColor_WhenSet_ForwardsColorToFilledRenderable()
    {
        CircleRuntime sut = new();
        sut.FillColor = Color.Red;

        Circle renderable = sut.RenderableComponent.ShouldBeOfType<Circle>();
        renderable.IsFilled.ShouldBeTrue();
        renderable.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void FillColor_WhenSet_SwapsRenderableToFilledShape()
    {
        CircleRuntime sut = new();
        sut.RenderableComponent.ShouldBeOfType<LineCircle>();

        sut.FillColor = Color.Red;

        sut.RenderableComponent.ShouldBeOfType<Circle>();
    }
}
