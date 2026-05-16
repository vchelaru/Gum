using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using MonoGameGum.Renderables;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math.Geometry;
using Shouldly;
using Xunit;

// These tests exercise the legacy Color/Red/Green/Blue/Alpha routing on CircleRuntime,
// which is intentionally [Obsolete] post-#2761 (soft deprecation pointing migrators at
// FillColor/StrokeColor). The routing still has to work, so we silence CS0618 here.
#pragma warning disable CS0618

namespace MonoGameGum.Tests.Runtimes;

// Issue #2768 two-slot model: CircleRuntime resolves IFilledCircleRenderable AND
// IStrokedCircleRenderable at construction. Core MonoGameGum has no IFilledCircleRenderable
// default — fill is genuinely unavailable without the optional MonoGameGumShapes (Apos.Shapes)
// package, and FillColor setters are no-ops here. The stroke slot resolves to
// DefaultStrokedCircleRenderable. The Apos-backed fill-and-stroke path is covered in
// Tests/MonoGameGum.Shapes.Tests/CircleRuntimeTests.cs.
public class CircleRuntimeTests : BaseTestClass
{
    [Fact]
    public void Constructor_BindsDefaultStrokedRenderable_AsContainedObject()
    {
        CircleRuntime sut = new();

        sut.RenderableComponent.ShouldBeOfType<DefaultStrokedCircleRenderable>();
    }

    [Fact]
    public void Constructor_LeavesFillSlotNull_WhenNoFactoryRegistered()
    {
        // Core MonoGameGum registers no IFilledCircleRenderable default. The runtime must
        // tolerate a null fill slot — see graceful-degradation contract in #2768.
        CircleRuntime sut = new();

        // Indirectly: setting FillColor must not throw, and the stroke renderable stays the
        // contained object (no fill instance to take its place).
        sut.FillColor = Color.Red;
        sut.RenderableComponent.ShouldBeOfType<DefaultStrokedCircleRenderable>();
    }

    [Fact]
    public void FillColor_RoundTripsBackingField_WhenFillSlotIsNull()
    {
        CircleRuntime sut = new();

        sut.FillColor = Color.Red;

        // Stored on the runtime so a later install of MonoGameGumShapes (which would
        // re-create the runtime) honors the user's color. Without the package, no visual
        // effect — see DefaultStrokedCircleRenderable remarks.
        sut.FillColor.ShouldBe(Color.Red);
    }

    // Issue #2791 graceful-degradation contract: with no MonoGameGumShapes package the stroke
    // slot is DefaultStrokedCircleRenderable (does not implement IGradientedRenderable). The
    // gradient setters must round-trip on the runtime so user code is forward-compatible with
    // a later install of the package, but produce no visual effect today.
    [Fact]
    public void Gradient_RoundTripsBackingFields_WhenNoGradientCapableSlot()
    {
        CircleRuntime sut = new();

        sut.UseGradient = true;
        sut.GradientType = GradientType.Radial;
        sut.Color1 = Color.Red;
        sut.Color2 = Color.Blue;
        sut.GradientX1 = 1;
        sut.GradientY1 = 2;
        sut.GradientX2 = 3;
        sut.GradientY2 = 4;
        sut.GradientX1Units = GeneralUnitType.PixelsFromMiddle;
        sut.GradientInnerRadius = 5;
        sut.GradientOuterRadius = 6;
        sut.GradientInnerRadiusUnits = DimensionUnitType.PercentageOfParent;
        sut.GradientOuterRadiusUnits = DimensionUnitType.RelativeToParent;

        sut.UseGradient.ShouldBeTrue();
        sut.GradientType.ShouldBe(GradientType.Radial);
        sut.Color1.ShouldBe(Color.Red);
        sut.Color2.ShouldBe(Color.Blue);
        sut.GradientX1.ShouldBe(1);
        sut.GradientY1.ShouldBe(2);
        sut.GradientX2.ShouldBe(3);
        sut.GradientY2.ShouldBe(4);
        sut.GradientX1Units.ShouldBe(GeneralUnitType.PixelsFromMiddle);
        sut.GradientInnerRadius.ShouldBe(5);
        sut.GradientOuterRadius.ShouldBe(6);
        sut.GradientInnerRadiusUnits.ShouldBe(DimensionUnitType.PercentageOfParent);
        sut.GradientOuterRadiusUnits.ShouldBe(DimensionUnitType.RelativeToParent);
    }

    // Issue #2798: IsAntialiased graceful-degradation contract. With no MonoGameGumShapes
    // package, neither slot implements IAntialiasedRenderable. The setter must round-trip on
    // the runtime so user code is forward-compatible with a later install of the package, but
    // produce no visual effect today (LineCircle has no AA concept).
    [Fact]
    public void IsAntialiased_RoundTripsBackingField_WhenNoAntialiasedCapableSlot()
    {
        CircleRuntime sut = new();

        sut.IsAntialiased.ShouldBeTrue("default matches Apos.Shapes' own default");

        sut.IsAntialiased = false;

        sut.IsAntialiased.ShouldBeFalse();
    }

    [Fact]
    public void LegacyColor_RoutesToStrokeRenderable()
    {
        CircleRuntime sut = new();

        sut.Color = Color.Yellow;

        IStrokedCircleRenderable stroke = sut.RenderableComponent.ShouldBeAssignableTo<IStrokedCircleRenderable>()!;
        stroke.Color.ShouldBe(Color.Yellow);
        sut.Color.ShouldBe(Color.Yellow);
    }

    [Fact]
    public void Radius_RoundTrips_ThroughStrokeRenderable()
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
    public void StrokeColor_WhenSet_WritesToStrokeRenderable()
    {
        CircleRuntime sut = new();

        sut.StrokeColor = Color.Lime;

        IStrokedCircleRenderable stroke = sut.RenderableComponent.ShouldBeAssignableTo<IStrokedCircleRenderable>()!;
        stroke.Color.ShouldBe(Color.Lime);
    }
}
