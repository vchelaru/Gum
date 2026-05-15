using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameGum.Renderables;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using Xunit;

// These tests exercise the legacy Color/Red/Green/Blue/Alpha routing on CircleRuntime,
// which is intentionally [Obsolete] post-#2761 (soft deprecation pointing migrators at
// FillColor/StrokeColor). The routing still has to work, so we silence CS0618 here.
#pragma warning disable CS0618

namespace MonoGameGum.Tests.Runtimes;

// Phase 2 (rewrite) of #2761: CircleRuntime binds a single ICircleRenderable at construction
// from RenderableRegistry and keeps it for life. This test project does NOT reference the
// optional MonoGameGumShapes package, so the renderable is always the core default
// (DefaultCircleRenderable, an outline). FillColor / StrokeColor still write into the
// renderable's color slot — they just don't visually fill on the default. The Apos-backed
// swap-to-fill path is covered in Tests/MonoGameGum.Shapes.Tests/CircleRuntimeTests.cs.
public class CircleRuntimeTests : BaseTestClass
{
    [Fact]
    public void Constructor_BindsDefaultCircleRenderable()
    {
        CircleRuntime sut = new();

        sut.RenderableComponent.ShouldBeOfType<DefaultCircleRenderable>();
    }

    [Fact]
    public void FillColor_WhenSet_WritesToRenderableColorAndSetsIsFilledTrue()
    {
        CircleRuntime sut = new();

        sut.FillColor = Color.Red;

        ICircleRenderable renderable = sut.RenderableComponent.ShouldBeAssignableTo<ICircleRenderable>()!;
        renderable.IsFilled.ShouldBeTrue();
        renderable.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void LegacyColor_RoutesThroughRenderableColor()
    {
        CircleRuntime sut = new();

        sut.Color = Color.Yellow;

        ICircleRenderable renderable = sut.RenderableComponent.ShouldBeAssignableTo<ICircleRenderable>()!;
        renderable.Color.ShouldBe(Color.Yellow);
        sut.Color.ShouldBe(Color.Yellow);
    }

    [Fact]
    public void Radius_RoundTrips_ThroughRenderable()
    {
        CircleRuntime sut = new();

        sut.Radius = 42f;

        sut.Radius.ShouldBe(42f);
        sut.Width.ShouldBe(84f);
        sut.Height.ShouldBe(84f);
    }

    [Fact]
    public void Renderable_IsStableAcrossPropertyChanges()
    {
        CircleRuntime sut = new();
        object original = sut.RenderableComponent;

        sut.FillColor = Color.Red;
        sut.StrokeColor = Color.Blue;
        sut.Color = Color.Lime;
        sut.Radius = 25f;
        sut.FillColor = null;
        sut.StrokeColor = null;

        sut.RenderableComponent.ShouldBeSameAs(original);
    }

    [Fact]
    public void StrokeColor_WhenSet_WritesToRenderableColorAndSetsIsFilledFalse()
    {
        CircleRuntime sut = new();

        sut.StrokeColor = Color.Lime;

        ICircleRenderable renderable = sut.RenderableComponent.ShouldBeAssignableTo<ICircleRenderable>()!;
        renderable.IsFilled.ShouldBeFalse();
        renderable.Color.ShouldBe(Color.Lime);
    }
}
