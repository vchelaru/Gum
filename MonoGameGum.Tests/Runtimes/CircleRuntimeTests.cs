using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Gum.Renderables;
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
    public void FillChannelSetters_ComposeFillColor()
    {
        CircleRuntime sut = new();

        sut.FillRed = 11;
        sut.FillGreen = 22;
        sut.FillBlue = 33;
        sut.FillAlpha = 44;

        sut.FillColor.ShouldBe(new Color(11, 22, 33, 44));
    }

    // FillColor defaults to opaque white and IsFilled defaults to false, so a freshly-
    // constructed runtime renders as a stroke-only outline (the white fill is gated off). This
    // is the "pit of success" default: flipping IsFilled = true fills the shape white without
    // needing to also assign FillColor.
    [Fact]
    public void FillColor_DefaultsToWhite()
    {
        CircleRuntime sut = new();

        sut.FillColor.ShouldBe(Color.White);
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

    [Fact]
    public void IsFilled_DefaultsToFalse()
    {
        CircleRuntime sut = new();

        sut.IsFilled.ShouldBeFalse();
    }

    [Fact]
    public void IsFilled_RoundTripsBackingField_WhenFillSlotNull()
    {
        CircleRuntime sut = new();

        sut.IsFilled = false;
        sut.IsFilled.ShouldBeFalse();
        // Renderable stays the stroke default — there's no fill slot to hide visually.
        sut.RenderableComponent.ShouldBeOfType<DefaultStrokedCircleRenderable>();

        sut.IsFilled = true;
        sut.IsFilled.ShouldBeTrue();
    }

    [Fact]
    public void StrokeChannelSetters_ComposeStrokeColor()
    {
        CircleRuntime sut = new();

        sut.StrokeRed = 11;
        sut.StrokeGreen = 22;
        sut.StrokeBlue = 33;
        sut.StrokeAlpha = 44;

        sut.StrokeColor.ShouldBe(new Color(11, 22, 33, 44));
    }

    [Fact]
    public void StrokeColor_DefaultsToWhite()
    {
        CircleRuntime sut = new();

        sut.StrokeColor.ShouldBe(Color.White);

        sut.StrokeColor = Color.Lime;
        sut.StrokeColor.ShouldBe(Color.Lime);
    }

    // Issue #2791 graceful-degradation contract: with no MonoGameGumShapes package the stroke
    // slot is DefaultStrokedCircleRenderable (does not implement IGradientedRenderable). The
    // gradient setters must round-trip on the runtime so user code is forward-compatible with
    // a later install of the package, but produce no visual effect today.
    // Issue #3009 — Color1 is no longer a standalone runtime value (the gradient start is the
    // active body color), so it's not part of this round-trip set; Color2 and the coordinate/
    // radius fields still round-trip.
    [Fact]
    public void Gradient_RoundTripsBackingFields_WhenNoGradientCapableSlot()
    {
        CircleRuntime sut = new();

        sut.UseGradient = true;
        sut.GradientType = GradientType.Radial;
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

    // Issue #2797: dropshadow graceful-degradation contract. With no MonoGameGumShapes
    // package, neither slot implements IDropshadowRenderable. The setters must round-trip on
    // the runtime so user code is forward-compatible with a later install of the package, but
    // produce no visual effect today.
    [Fact]
    public void Dropshadow_RoundTripsBackingFields_WhenNoDropshadowCapableSlot()
    {
        CircleRuntime sut = new();

        // Constructor seeds the same defaults as SkiaShapeRuntime so HasDropshadow = true
        // immediately produces a visible shadow without further setup.
        sut.DropshadowAlpha.ShouldBe(255);
        sut.DropshadowOffsetY.ShouldBe(3);
        sut.DropshadowBlur.ShouldBe(3);
        sut.HasDropshadow.ShouldBeFalse();

        sut.HasDropshadow = true;
        sut.DropshadowColor = new Color(10, 20, 30, 40);
        sut.DropshadowOffsetX = 5;
        sut.DropshadowOffsetY = 7;
        sut.DropshadowBlur = 2;

        sut.HasDropshadow.ShouldBeTrue();
        sut.DropshadowColor.ShouldBe(new Color(10, 20, 30, 40));
        sut.DropshadowRed.ShouldBe(10);
        sut.DropshadowGreen.ShouldBe(20);
        sut.DropshadowBlue.ShouldBe(30);
        sut.DropshadowAlpha.ShouldBe(40);
        sut.DropshadowOffsetX.ShouldBe(5);
        sut.DropshadowOffsetY.ShouldBe(7);
        sut.DropshadowBlur.ShouldBe(2);
    }

    [Fact]
    public void DropshadowChannelSetters_ComposeDropshadowColor()
    {
        CircleRuntime sut = new();

        sut.DropshadowRed = 11;
        sut.DropshadowGreen = 22;
        sut.DropshadowBlue = 33;
        sut.DropshadowAlpha = 44;

        sut.DropshadowColor.ShouldBe(new Color(11, 22, 33, 44));
    }

    // Issue #2798: IsAntialiased graceful-degradation contract. With no MonoGameGumShapes
    // package, neither slot implements IAntialiasedRenderable. The setter must round-trip on
    // the runtime so user code is forward-compatible with a later install of the package, but
    // produce no visual effect today (LineCircle has no AA concept).
    // Issue #2796: dashed-stroke graceful-degradation contract. With no MonoGameGumShapes
    // package, the stroke slot is DefaultStrokedCircleRenderable (wraps LineCircle, no dash
    // concept) and does not implement IDashedStrokeRenderable. The setters must round-trip
    // on the runtime so user code is forward-compatible with a later install of the package,
    // but produce no visual effect today.
    [Fact]
    public void DashedStroke_RoundTripsBackingFields_WhenNoDashCapableSlot()
    {
        CircleRuntime sut = new();

        sut.StrokeDashLength.ShouldBe(0f, "default 0 means solid stroke");
        sut.StrokeGapLength.ShouldBe(0f);

        sut.StrokeDashLength = 6;
        sut.StrokeGapLength = 4;

        sut.StrokeDashLength.ShouldBe(6f);
        sut.StrokeGapLength.ShouldBe(4f);
    }

    [Fact]
    public void IsAntialiased_RoundTripsBackingField_WhenNoAntialiasedCapableSlot()
    {
        CircleRuntime sut = new();

        sut.IsAntialiased.ShouldBeTrue("default matches Apos.Shapes' own default");

        sut.IsAntialiased = false;

        sut.IsAntialiased.ShouldBeFalse();
    }

    [Fact]
    public void LegacyChannelSetters_ComposeColor()
    {
        CircleRuntime sut = new();

        sut.Red = 11;
        sut.Green = 22;
        sut.Blue = 33;
        sut.Alpha = 44;

        sut.Color.ShouldBe(new Color(11, 22, 33, 44));

        IStrokedCircleRenderable stroke = sut.RenderableComponent.ShouldBeAssignableTo<IStrokedCircleRenderable>()!;
        stroke.Color.ShouldBe(new Color(11, 22, 33, 44));
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
        sut.IsFilled = false;
        sut.StrokeWidth = 0;

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
