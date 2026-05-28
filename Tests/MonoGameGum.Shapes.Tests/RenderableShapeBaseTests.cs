using MonoGameAndGum.Renderables;
using Microsoft.Xna.Framework;
using Shouldly;
using Apos.Shapes;

namespace MonoGameGum.Shapes.Tests;

// Issue #2925 — Circle/Arc Render compute the absolute center by rotating the (W/2, H/2)
// offset from the GUE's top-left origin around the top-left (Gum's default rotation pivot).
// These tests pin the math behind GetRotatedCenter independent of the rest of the Render
// pipeline, since the actual draw output (a call into Apos.Shapes ShapeBatch) is not
// inspectable from a unit test.
public class RenderableShapeBaseTests
{
    // Issue #2937 — Blend had no visual effect on Apos shapes because StartBatch never passed
    // a BlendState to ShapeBatch.Begin (which then fell back to its AlphaBlend default) and the
    // Blend variable had nowhere to land on the Apos renderable. GetEffectiveXnaBlendState is the
    // device-free seam StartBatch consults; pinning it here avoids needing a GraphicsDevice.
    [Fact]
    public void GetEffectiveXnaBlendState_AdditiveBlend_ReturnsXnaAdditive()
    {
        TestShape shape = new() { Blend = Gum.RenderingLibrary.Blend.Additive };

        shape.GetEffectiveXnaBlendState().ShouldBe(Microsoft.Xna.Framework.Graphics.BlendState.Additive);
    }

    [Fact]
    public void GetEffectiveXnaBlendState_NormalBlend_ReturnsNullSoBeginKeepsItsAlphaBlendDefault()
    {
        // Normal is the default, and Apos shapes have always rendered with Begin's AlphaBlend
        // default. Returning null (rather than NonPremultiplied) preserves that exact behavior
        // so no existing content changes appearance.
        TestShape shape = new();

        shape.Blend.ShouldBe(Gum.RenderingLibrary.Blend.Normal);
        shape.GetEffectiveXnaBlendState().ShouldBeNull();
    }

    // Issue #2937 — BatchKey names the rendering tech, NOT internal state like blend (mirroring
    // how all SpriteBatch renderables share one key and SpriteBatchStack handles blend changes
    // internally). Blend differences are resolved by ShapeRenderer.EnsureBlend re-opening the
    // batch, not by splitting the key. This guards against regressing to a blend-keyed batch.
    [Fact]
    public void BatchKey_IsTechOnly_AndDoesNotVaryWithBlend()
    {
        TestShape normal = new();
        TestShape additive = new() { Blend = Gum.RenderingLibrary.Blend.Additive };

        additive.BatchKey.ShouldBe(normal.BatchKey);
    }

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

    // RotateAround — rotates a point around an arbitrary pivot. Shared math used both for the
    // dashed-stroke perimeter walk and for rotating gradient endpoints with the shape so the
    // gradient stays anchored to object-local space rather than the unrotated bounding box.
    [Fact]
    public void RotateAround_ZeroRotation_ReturnsPointUnchanged()
    {
        Vector2 result = RenderableShapeBase.RotateAround(new Vector2(10f, 20f), new Vector2(5f, 5f), 0f);
        result.X.ShouldBe(10f, tolerance: 0.001f);
        result.Y.ShouldBe(20f, tolerance: 0.001f);
    }

    [Fact]
    public void RotateAround_PointAtPivot_StaysAtPivot()
    {
        Vector2 pivot = new(100f, 200f);
        Vector2 result = RenderableShapeBase.RotateAround(pivot, pivot, (float)System.Math.PI / 4f);
        result.X.ShouldBe(100f, tolerance: 0.001f);
        result.Y.ShouldBe(200f, tolerance: 0.001f);
    }

    [Fact]
    public void RotateAround_NinetyDegrees_RotatesCorrectly()
    {
        // 90° CCW around origin: (1, 0) -> (0, 1).  Around pivot (100, 100), the offset (10, 0)
        // becomes (0, 10), so the point (110, 100) lands at (100, 110).
        Vector2 result = RenderableShapeBase.RotateAround(new Vector2(110f, 100f), new Vector2(100f, 100f), (float)System.Math.PI / 2f);
        result.X.ShouldBe(100f, tolerance: 0.001f);
        result.Y.ShouldBe(110f, tolerance: 0.001f);
    }

    // GetGradient — object-space anchoring fix. Before this fix, the gradient endpoints were
    // computed in unrotated bounding-box coordinates and passed unchanged to Apos.Shapes, so
    // the gradient pattern stayed axis-aligned while the rendered shape rotated underneath
    // (visible repro: rotation row in the gradient gallery samples). After the fix, endpoints
    // are rotated around the shape's rotation pivot (the GUE top-left origin, per Gum
    // convention) so the gradient travels with the shape.
    [Fact]
    public void GetGradient_ZeroRotation_PreservesPreRotationEndpoints()
    {
        // Regression guard: with rotation = 0, the rotation-aware call must return the same
        // endpoints as the legacy axis-aligned computation. Shape at absolute (100, 200),
        // gradient from local (0,0) to local (20, 0).
        TestShape shape = new()
        {
            Width = 70f,
            Height = 50f,
            UseGradient = true,
            GradientX1 = 0f,
            GradientY1 = 0f,
            GradientX2 = 20f,
            GradientY2 = 0f,
            Red1 = 0, Green1 = 0, Blue1 = 0, Alpha1 = 255,
            Red2 = 255, Green2 = 255, Blue2 = 255, Alpha2 = 255,
        };

        Gradient gradient = shape.CallGetGradient(absoluteLeft: 100f, absoluteTop: 200f, rotationRadians: 0f);

        gradient.AXY.X.ShouldBe(100f, tolerance: 0.001f);
        gradient.AXY.Y.ShouldBe(200f, tolerance: 0.001f);
        gradient.BXY.X.ShouldBe(120f, tolerance: 0.001f);
        gradient.BXY.Y.ShouldBe(200f, tolerance: 0.001f);
    }

    [Fact]
    public void GetGradient_NinetyDegreeRotation_RotatesEndpointsAroundTopLeft()
    {
        // Shape at absolute (100, 200), gradient from local (0, 0) → (20, 0). The unrotated
        // world endpoints are (100, 200) and (120, 200). Rotating 90° CCW around the pivot
        // (100, 200) leaves endpoint A at the pivot and moves endpoint B to (100, 220).
        TestShape shape = new()
        {
            Width = 70f,
            Height = 50f,
            UseGradient = true,
            GradientX1 = 0f,
            GradientY1 = 0f,
            GradientX2 = 20f,
            GradientY2 = 0f,
            Red1 = 0, Green1 = 0, Blue1 = 0, Alpha1 = 255,
            Red2 = 255, Green2 = 255, Blue2 = 255, Alpha2 = 255,
        };

        Gradient gradient = shape.CallGetGradient(absoluteLeft: 100f, absoluteTop: 200f, rotationRadians: (float)System.Math.PI / 2f);

        gradient.AXY.X.ShouldBe(100f, tolerance: 0.001f);
        gradient.AXY.Y.ShouldBe(200f, tolerance: 0.001f);
        gradient.BXY.X.ShouldBe(100f, tolerance: 0.001f);
        gradient.BXY.Y.ShouldBe(220f, tolerance: 0.001f);
    }

    [Fact]
    public void GetGradient_RadialGradient_RotatesCenterAndKeepsRadiusVectorAligned()
    {
        // Radial gradient: A = center, B = (center + outerRadius along +X). Rotating 180°
        // around the top-left pivot should swing the center to the opposite side and keep
        // the radius extent vector reflected accordingly.
        TestShape shape = new()
        {
            Width = 56f,
            Height = 56f,
            UseGradient = true,
            GradientType = global::RenderingLibrary.Graphics.GradientType.Radial,
            GradientX1 = 28f,
            GradientY1 = 28f,
            // Radius set via GradientOuterRadius (absolute units default)
            Red1 = 0, Green1 = 0, Blue1 = 0, Alpha1 = 255,
            Red2 = 255, Green2 = 255, Blue2 = 255, Alpha2 = 255,
        };
        shape.SetOuterRadiusAbsolute(28f);

        // Shape at (0, 0): center is at world (28, 28); B = center + (28, 0) = (56, 28).
        // 180° around (0, 0): A → (-28, -28), B → (-56, -28).
        Gradient gradient = shape.CallGetGradient(absoluteLeft: 0f, absoluteTop: 0f, rotationRadians: (float)System.Math.PI);

        gradient.AXY.X.ShouldBe(-28f, tolerance: 0.001f);
        gradient.AXY.Y.ShouldBe(-28f, tolerance: 0.001f);
        gradient.BXY.X.ShouldBe(-56f, tolerance: 0.001f);
        gradient.BXY.Y.ShouldBe(-28f, tolerance: 0.001f);
    }

    // Test-only subclass exposing the protected GetGradient so a unit test can drive endpoint
    // math without going through the runtime/layout system. Width/Height are already public
    // on RenderableBase, so they're settable via object initializer directly.
    private class TestShape : RenderableShapeBase
    {
        public override void Render(global::RenderingLibrary.ISystemManagers managers) { }

        public void SetOuterRadiusAbsolute(float radius)
        {
            GradientOuterRadius = radius;
            GradientOuterRadiusUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        }

        public Gradient CallGetGradient(float absoluteLeft, float absoluteTop, float rotationRadians)
            => GetGradient(absoluteLeft, absoluteTop, rotationRadians);
    }
}
