using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using Xunit;

// These tests exercise the legacy Color/Red/Green/Blue/Alpha routing on CircleRuntime,
// which is intentionally [Obsolete] post-#2761 (soft deprecation pointing migrators at
// FillColor/StrokeColor). The routing still has to work, so we silence CS0618 here.
#pragma warning disable CS0618

namespace MonoGameGum.Tests.Runtimes;

// Phase 2 of #2761: CircleRuntime gains nullable FillColor / StrokeColor that swap the
// contained renderable via RenderableRegistry. This test project does NOT reference the
// optional MonoGameGumShapes package, so no IFilledShapeRenderable factory is ever
// registered — every test here validates the graceful-degradation path (no crash, stay on
// the outline LineCircle) plus the legacy-property routing through whichever renderable is
// currently contained. The swap matrix that exercises the fill-renderable path lives in
// Tests/MonoGameGum.Shapes.Tests/CircleRuntimeFillColorTests.cs.
public class CircleRuntimeTests : BaseTestClass
{
    [Fact]
    public void Alpha_ShouldRouteThroughLineCircle_WhenNoFillFactoryRegistered()
    {
        CircleRuntime sut = new();
        sut.Color = Color.White;

        sut.Alpha = 128;

        sut.Alpha.ShouldBe(128);
        LineCircle line = sut.RenderableComponent.ShouldBeOfType<LineCircle>();
        line.Color.A.ShouldBe((byte)128);
    }

    [Fact]
    public void Color_LegacyProperty_ShouldRouteThroughLineCircle_WhenNoFillFactoryRegistered()
    {
        CircleRuntime sut = new();

        sut.Color = Color.Red;

        sut.Color.ShouldBe(Color.Red);
    }

    [Fact]
    public void FillColor_WhenClearedToNull_StaysOnLineCircle_WhenNoFillFactoryRegistered()
    {
        CircleRuntime sut = new();
        sut.FillColor = Color.Red;
        sut.FillColor = null;

        sut.RenderableComponent.ShouldBeOfType<LineCircle>();
    }

    [Fact]
    public void FillColor_WhenSetWithoutShapesPackage_DoesNotThrowAndStaysOutline()
    {
        // Carried forward from spike (#2758): graceful degradation when the optional
        // MonoGameGumShapes package is NOT referenced. Setting FillColor must NOT crash and
        // the runtime stays on its outline LineCircle renderable.
        CircleRuntime sut = new();

        Should.NotThrow(() => sut.FillColor = Color.Red);

        sut.RenderableComponent.ShouldBeOfType<LineCircle>();
    }

    [Fact]
    public void Radius_ShouldRoundTrip_OnLineCircle()
    {
        CircleRuntime sut = new();

        sut.Radius = 42f;

        sut.Radius.ShouldBe(42f);
        sut.Width.ShouldBe(84f);
        sut.Height.ShouldBe(84f);
    }

    [Fact]
    public void StrokeColor_WhenSet_AppliesColorToLineCircle_WhenNoFillFactoryRegistered()
    {
        // No factory registered ⇒ degradation path: stroke-only also stays on the outline
        // LineCircle. The stroke color is pushed onto LineCircle.Color so the outline
        // visually adopts the requested stroke color.
        CircleRuntime sut = new();

        sut.StrokeColor = Color.Lime;

        LineCircle line = sut.RenderableComponent.ShouldBeOfType<LineCircle>();
        line.Color.R.ShouldBe(Color.Lime.R);
        line.Color.G.ShouldBe(Color.Lime.G);
        line.Color.B.ShouldBe(Color.Lime.B);
    }
}
