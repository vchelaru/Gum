using Gum.GueDeriving;
using Raylib_cs;
using Shouldly;
using System.Numerics;

namespace RaylibGum.Tests.Runtimes;

public class PolygonRuntimeTests : BaseTestClass
{
    [Fact]
    public void Alpha_ShouldDefaultTo255()
    {
        PolygonRuntime sut = new();
        sut.Alpha.ShouldBe(255);
    }

    [Fact]
    public void Alpha_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.Alpha = 128;
        sut.Alpha.ShouldBe(128);
    }

    [Fact]
    public void Blue_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.Blue = 64;
        sut.Blue.ShouldBe(64);
    }

    [Fact]
    public void Color_ShouldDefaultToWhite()
    {
        PolygonRuntime sut = new();
        sut.Color.ShouldBe(Color.White);
    }

    [Fact]
    public void Color_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        Color expected = new Color(10, 20, 30, 40);
        sut.Color = expected;
        sut.Color.ShouldBe(expected);
    }

    [Fact]
    public void Green_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.Green = 32;
        sut.Green.ShouldBe(32);
    }

    // IsDotted was obsoleted in #2757 in favor of StrokeDashLength + StrokeGapLength for
    // cross-backend naming parity. The legacy property is preserved on MG/Raylib (these
    // tests pin that it still works); the new properties are tested below.
#pragma warning disable CS0618 // Type or member is obsolete
    [Fact]
    public void IsDotted_ShouldBeFalse_ByDefault()
    {
        PolygonRuntime sut = new();
        sut.IsDotted.ShouldBeFalse();
    }

    [Fact]
    public void IsDotted_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.IsDotted = true;
        sut.IsDotted.ShouldBeTrue();
    }
#pragma warning restore CS0618

    // StrokeDashLength + StrokeGapLength on raylib push the actual lengths through to the
    // contained LinePolygon for per-segment dashed rendering. Both must be positive for
    // dashing to engage, matching Skia's SKPathEffect.CreateDash guard in
    // RenderableShapeBase.
    [Fact]
    public void StrokeDashLength_ShouldPushLengthsThroughToContained_WhenPairedWithGap()
    {
        PolygonRuntime sut = new();
        sut.StrokeDashLength = 4;
        sut.StrokeGapLength = 2;
        Gum.Renderables.LinePolygon inner = (Gum.Renderables.LinePolygon)sut.RenderableComponent;
        inner.StrokeDashLength.ShouldBe(4f);
        inner.StrokeGapLength.ShouldBe(2f);
    }

    [Fact]
    public void StrokeDashLength_ShouldNotPushNonZeroLengthAlone_WhenGapIsZero()
    {
        PolygonRuntime sut = new();
        sut.StrokeDashLength = 4;
        Gum.Renderables.LinePolygon inner = (Gum.Renderables.LinePolygon)sut.RenderableComponent;
        // Both must be > 0 to engage — gap is still 0, so the renderable's dash length stays
        // un-set (renderable then falls through to its solid-stroke path).
        inner.StrokeGapLength.ShouldBe(0f);
    }

    [Fact]
    public void StrokeColor_RoundTrips_AndPushesToContainedRenderable()
    {
        // #2757 follow-up — raylib branch surfaces StrokeColor for cross-backend naming parity
        // with the SilkNet/Skia PolygonsScreen sample (which sets polygon.StrokeColor = ...).
        PolygonRuntime sut = new();
        Color expected = new Color(40, 50, 60, 255);

        sut.StrokeColor = expected;

        sut.StrokeColor.ShouldNotBeNull();
        Gum.Renderables.LinePolygon inner = (Gum.Renderables.LinePolygon)sut.RenderableComponent;
        inner.StrokeColor.ShouldNotBeNull();
        inner.StrokeColor!.Value.G.ShouldBe((byte)50);
    }

    [Fact]
    public void IsClosed_DefaultsToTrue_MatchingSkia()
    {
        // Skia's Polygon defaults IsClosed = true; raylib matches so the cross-backend
        // PolygonsScreen sample reads identically without per-cell wiring on closed cells.
        PolygonRuntime sut = new();
        sut.IsClosed.ShouldBeTrue();
    }

    [Fact]
    public void IsClosed_RoundTrips_AndPushesToContainedRenderable()
    {
        PolygonRuntime sut = new();
        sut.IsClosed = false;
        sut.IsClosed.ShouldBeFalse();
        ((Gum.Renderables.LinePolygon)sut.RenderableComponent).IsClosed.ShouldBeFalse();
    }

    [Fact]
    public void IsPointInside_ShouldReturnFalse_WhenPointIsOutsideDefaultPolygon()
    {
        PolygonRuntime sut = new();
        sut.IsPointInside(50, 50).ShouldBeFalse();
    }

    [Fact]
    public void IsPointInside_ShouldReturnFalse_WhenPointWasInsideBeforeRotation()
    {
        // Default polygon is a 0..32 square. After 90° CCW rotation around (0,0) the
        // occupied region moves to x∈[0,32], y∈[-32,0]. A point that was inside the
        // un-rotated square at (16,16) should now be outside.
        PolygonRuntime sut = new();
        sut.Rotation = 90;
        sut.IsPointInside(16, 16).ShouldBeFalse();
    }

    [Fact]
    public void IsPointInside_ShouldReturnTrue_WhenPointIsInsideDefaultPolygon()
    {
        PolygonRuntime sut = new();
        sut.IsPointInside(16, 16).ShouldBeTrue();
    }

    [Fact]
    public void IsPointInside_ShouldReturnTrue_WhenPointIsInsideRotatedPolygon()
    {
        // After 90° CCW, world (16,-16) maps to local (16,16) which is inside the default square.
        PolygonRuntime sut = new();
        sut.Rotation = 90;
        sut.IsPointInside(16, -16).ShouldBeTrue();
    }

    // LineWidth was obsoleted in #2757 in favor of StrokeWidth + StrokeWidthUnits, but the
    // legacy property is preserved on MG/Raylib for back-compat.
#pragma warning disable CS0618 // Type or member is obsolete
    [Fact]
    public void LineWidth_ShouldBe1_ByDefault()
    {
        PolygonRuntime sut = new();
        sut.LineWidth.ShouldBe(1);
    }

    [Fact]
    public void LineWidth_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.LineWidth = 3f;
        sut.LineWidth.ShouldBe(3f);
    }
#pragma warning restore CS0618

    [Fact]
    public void StrokeWidth_ShouldBe1_ByDefault()
    {
        PolygonRuntime sut = new();
        sut.StrokeWidth.ShouldBe(1);
    }

    [Fact]
    public void StrokeWidth_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.StrokeWidth = 4f;
        sut.StrokeWidth.ShouldBe(4f);
    }

    [Fact]
    public void PreRender_ShouldPushStrokeWidthToContainedRenderable()
    {
        // Canonical resolve path for StrokeWidth is PreRender (handles StrokeWidthUnits
        // ScreenPixel ↔ camera zoom). The setter intentionally does NOT push — raylib's
        // Renderer.Draw walks the tree calling PreRender before render so this lands in time
        // for the first frame.
        PolygonRuntime sut = new();
        sut.StrokeWidth = 5f;
        sut.PreRender();
        ((Gum.Renderables.LinePolygon)sut.RenderableComponent).LinePixelWidth.ShouldBe(5f);
    }

    [Fact]
    public void Red_ShouldRoundTrip()
    {
        PolygonRuntime sut = new();
        sut.Red = 200;
        sut.Red.ShouldBe(200);
    }

    [Fact]
    public void SetPoints_ShouldUpdatePoints()
    {
        PolygonRuntime sut = new();
        Vector2[] points = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(0, 16),
            new Vector2(16, 16),
            new Vector2(16, 0),
            new Vector2(0, 0),
        };
        sut.SetPoints(points);
        sut.IsPointInside(8, 8).ShouldBeTrue();
        sut.IsPointInside(20, 20).ShouldBeFalse();
    }

    [Fact]
    public void Visible_ShouldBeTrue_ByDefault()
    {
        PolygonRuntime sut = new();
        sut.Visible.ShouldBeTrue();
    }
}
