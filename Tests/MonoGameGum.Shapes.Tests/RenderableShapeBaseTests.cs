using MonoGameAndGum.Renderables;
using Microsoft.Xna.Framework;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #2925 — Circle/Arc Render compute the absolute center by rotating the (W/2, H/2)
// offset from the GUE's top-left origin around the top-left (Gum's default rotation pivot).
// These tests pin the math behind GetRotatedCenter independent of the rest of the Render
// pipeline, since the actual draw output (a call into Apos.Shapes ShapeBatch) is not
// inspectable from a unit test.
public class RenderableShapeBaseTests
{
    [Fact]
    public void GetRotatedCenter_ZeroRotation_ReturnsAxisAlignedCenter()
    {
        Vector2 result = RenderableShapeBase.GetRotatedCenter(100f, 200f, 40f, 60f, 0f);

        result.X.ShouldBe(120f);
        result.Y.ShouldBe(230f);
    }

    [Fact]
    public void GetRotatedCenter_NinetyDegrees_ShiftsCenterAlongRotatedAxes()
    {
        // 90° rotation: x' = x*cos - y*sin = -halfH ; y' = x*sin + y*cos = halfW
        // halfW = 20, halfH = 30  =>  offset = (-30, 20)  =>  center = (70, 220)
        var rotation = (float)System.Math.PI / 2f;

        Vector2 result = RenderableShapeBase.GetRotatedCenter(100f, 200f, 40f, 60f, rotation);

        result.X.ShouldBe(70f, tolerance: 0.001f);
        result.Y.ShouldBe(220f, tolerance: 0.001f);
    }

    [Fact]
    public void GetRotatedCenter_OneEightyDegrees_PutsCenterOppositeTopLeft()
    {
        // 180° rotation: center should be at top-left minus half-dimensions.
        var rotation = (float)System.Math.PI;

        Vector2 result = RenderableShapeBase.GetRotatedCenter(100f, 200f, 40f, 60f, rotation);

        result.X.ShouldBe(80f, tolerance: 0.001f);   // 100 - 20
        result.Y.ShouldBe(170f, tolerance: 0.001f);  // 200 - 30
    }

    [Fact]
    public void GetRotatedCenter_SquareBox_ZeroRotation_MatchesUnrotatedFormula()
    {
        // Sanity check: a square Circle's center at zero rotation is exactly (left + r, top + r).
        Vector2 result = RenderableShapeBase.GetRotatedCenter(0f, 0f, 32f, 32f, 0f);

        result.X.ShouldBe(16f);
        result.Y.ShouldBe(16f);
    }
}
