using Gum.Renderables;
using Raylib_cs;
using Shouldly;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Issue #2757 — pins the property surface added to <see cref="LinePolygon"/> so the raylib
/// PolygonRuntime can render closed/open paths with explicit stroke color and per-segment
/// dash/gap lengths in addition to its original 1 px outline. Render-path correctness lives
/// in the gallery's PolygonsScreen (requires a GL context to validate visually).
/// </summary>
public class LinePolygonTests
{
    [Fact]
    public void Defaults_PreserveLegacyBehavior()
    {
        LinePolygon polygon = new LinePolygon();

        polygon.LinePixelWidth.ShouldBe(1f);
        polygon.IsDotted.ShouldBeFalse();
        polygon.StrokeColor.ShouldBeNull();
        polygon.StrokeDashLength.ShouldBe(0f);
        polygon.StrokeGapLength.ShouldBe(0f);
        polygon.Color.R.ShouldBe((byte)255);
        polygon.Color.A.ShouldBe((byte)255);
    }

    [Fact]
    public void IsClosed_DefaultsToTrue_MatchingSkia()
    {
        // Skia's Polygon auto-closes by default; raylib now matches so the cross-backend
        // PolygonsScreen sample reads identically without per-cell IsClosed wiring.
        LinePolygon polygon = new LinePolygon();
        polygon.IsClosed.ShouldBeTrue();
    }

    [Fact]
    public void IsClosed_RoundTrips()
    {
        LinePolygon polygon = new LinePolygon();
        polygon.IsClosed = false;
        polygon.IsClosed.ShouldBeFalse();
    }

    [Fact]
    public void StrokeColor_RoundTripsNullableColor()
    {
        LinePolygon polygon = new LinePolygon();
        Color expected = new Color(50, 60, 70, 80);

        polygon.StrokeColor = expected;

        polygon.StrokeColor.ShouldNotBeNull();
        polygon.StrokeColor!.Value.G.ShouldBe((byte)60);

        polygon.StrokeColor = null;
        polygon.StrokeColor.ShouldBeNull();
    }

    [Fact]
    public void JoinStyle_DefaultsToRound()
    {
        // Round line-joins close the wedge gap on the outside of each vertex where two
        // adjacent DrawLineEx rectangles otherwise leave a triangular notch (visible at thick
        // strokes — see #2831 hexagon screenshot). Round is the default because it handles
        // convex / concave / acute angles uniformly with no miter-spike degenerate case.
        LinePolygon polygon = new LinePolygon();
        polygon.JoinStyle.ShouldBe(LineJoinStyle.Round);
    }

    [Fact]
    public void JoinStyle_RoundTrips()
    {
        LinePolygon polygon = new LinePolygon();
        polygon.JoinStyle = LineJoinStyle.None;
        polygon.JoinStyle.ShouldBe(LineJoinStyle.None);
    }

    [Fact]
    public void DashedStroke_PropertiesRoundTrip()
    {
        // Both lengths must be > 0 for the render path to engage; preferred over the binary
        // IsDotted flag for cross-backend parity with Skia's SKPathEffect.CreateDash.
        LinePolygon polygon = new LinePolygon();

        polygon.StrokeDashLength = 6f;
        polygon.StrokeGapLength = 4f;

        polygon.StrokeDashLength.ShouldBe(6f);
        polygon.StrokeGapLength.ShouldBe(4f);
    }
}
