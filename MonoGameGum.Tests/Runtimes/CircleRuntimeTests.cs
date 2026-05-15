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
