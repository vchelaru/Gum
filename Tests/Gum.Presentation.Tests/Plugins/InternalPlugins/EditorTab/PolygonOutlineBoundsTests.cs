using Gum.Wireframe;
using Shouldly;
using System.Numerics;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.EditorTab;

public class PolygonOutlineBoundsTests
{
    [Fact]
    public void Compute_NoRotation_ReturnsPointExtentsFromOrigin()
    {
        Vector2[] points = { new(-20, -10), new(40, -10), new(40, 30) };

        (float X, float Y, float Width, float Height) result =
            PolygonOutlineBounds.Compute(points, 200, 200, 0);

        result.X.ShouldBe(180, 0.001f);
        result.Y.ShouldBe(190, 0.001f);
        result.Width.ShouldBe(60, 0.001f);
        result.Height.ShouldBe(40, 0.001f);
    }

    [Fact]
    public void Compute_Rotated_PivotIsTheRotatedTopLeftPoint()
    {
        // At 90 degrees (counterclockwise on screen) a local point (x, y) lands at (y, -x)
        // relative to the origin, so the local top-left (-20, -10) lands at (-10, 20).
        Vector2[] points = { new(-20, -10), new(40, -10), new(40, 30) };

        (float X, float Y, float Width, float Height) result =
            PolygonOutlineBounds.Compute(points, 200, 200, 90);

        result.X.ShouldBe(190, 0.001f);
        result.Y.ShouldBe(220, 0.001f);
        result.Width.ShouldBe(60, 0.001f);
        result.Height.ShouldBe(40, 0.001f);
    }
}
