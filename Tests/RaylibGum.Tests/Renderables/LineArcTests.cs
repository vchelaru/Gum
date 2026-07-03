using Gum.Renderables;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Issue #3454 — verifies the gradient surface added to <see cref="LineArc"/> so the raylib arc
/// renderable can paint a linear/radial gradient across its stroked band, matching the Skia/Apos
/// <c>Arc</c>. Like <see cref="LineCircleTests"/>, these cover property round-trip and the paint
/// gate only; the actual triangle-strip render path calls native rlgl functions that require a GL
/// context and is validated visually in the raylib sample's arc screen.
/// </summary>
public class LineArcTests
{
    [Fact]
    public void Gradient_Defaults_Off()
    {
        LineArc arc = new LineArc();

        arc.UseGradient.ShouldBeFalse();
        arc.GradientType.ShouldBe(GradientType.Linear);
        arc.Color1.A.ShouldBe((byte)255);
        arc.Color2.A.ShouldBe((byte)255);
    }

    [Fact]
    public void Gradient_PropertiesRoundTrip()
    {
        LineArc arc = new LineArc();
        Color c1 = new Color(255, 0, 0, 255);
        Color c2 = new Color(0, 0, 255, 255);

        arc.UseGradient = true;
        arc.GradientType = GradientType.Radial;
        arc.Color1 = c1;
        arc.Color2 = c2;

        arc.UseGradient.ShouldBeTrue();
        arc.GradientType.ShouldBe(GradientType.Radial);
        arc.Color1.R.ShouldBe((byte)255);
        arc.Color2.B.ShouldBe((byte)255);
    }

    [Fact]
    public void GradientAxis_PropertiesRoundTrip()
    {
        LineArc arc = new LineArc();

        arc.GradientX1.ShouldBe(0f);
        arc.GradientY1.ShouldBe(0f);
        arc.GradientX2.ShouldBe(0f);
        arc.GradientY2.ShouldBe(0f);
        arc.GradientInnerRadius.ShouldBe(0f);
        arc.GradientOuterRadius.ShouldBe(0f);

        arc.GradientX1 = 4f;
        arc.GradientY1 = 8f;
        arc.GradientX2 = 56f;
        arc.GradientY2 = 28f;
        arc.GradientInnerRadius = 4f;
        arc.GradientOuterRadius = 28f;

        arc.GradientX1.ShouldBe(4f);
        arc.GradientY1.ShouldBe(8f);
        arc.GradientX2.ShouldBe(56f);
        arc.GradientY2.ShouldBe(28f);
        arc.GradientInnerRadius.ShouldBe(4f);
        arc.GradientOuterRadius.ShouldBe(28f);
    }

    // Issue #3454 — the arc is stroke-only (no fill slot), so the gradient paint gate is simpler
    // than LineCircle's ShouldPaintFillGradient: it keys on UseGradient plus at least one visible
    // gradient stop. No IsFilled / FillColor to enable.
    [Fact]
    public void ShouldPaintGradient_GradientOff_False()
    {
        LineArc arc = new()
        {
            UseGradient = false,
            Color1 = new Color(10, 20, 30, 255),
            Color2 = new Color(40, 50, 60, 255),
        };

        arc.ShouldPaintGradient.ShouldBeFalse();
    }

    [Fact]
    public void ShouldPaintGradient_BothStopsTransparent_False()
    {
        LineArc arc = new()
        {
            UseGradient = true,
            Color1 = new Color(1, 2, 3, 0),
            Color2 = new Color(4, 5, 6, 0),
        };

        arc.ShouldPaintGradient.ShouldBeFalse();
    }

    [Fact]
    public void ShouldPaintGradient_OneStopVisible_True()
    {
        LineArc arc = new()
        {
            UseGradient = true,
            Color1 = new Color(10, 20, 30, 255),
            Color2 = new Color(40, 50, 60, 0),
        };

        arc.ShouldPaintGradient.ShouldBeTrue();
    }

    // Issue #3454 — the gradient axis is defined in object-local bbox coords and must rotate
    // around the bbox center (Width/2, Height/2) to track the arc under self-rotation, matching
    // LineCircle.GetRotatedGradientEndpoints. Rotation convention mirrors LineRectangle's R helper
    // (visual CCW on screen: [cos sin; -sin cos]).
    [Fact]
    public void GetRotatedGradientEndpoints_ZeroRotation_ReturnsOriginalEndpoints()
    {
        LineArc arc = new()
        {
            Width = 100,
            Height = 100,
            GradientX1 = 0f,
            GradientY1 = 0f,
            GradientX2 = 100f,
            GradientY2 = 0f,
        };

        (float x1, float y1, float x2, float y2) = arc.GetRotatedGradientEndpoints(rotationDegrees: 0f);

        x1.ShouldBe(0f);
        y1.ShouldBe(0f);
        x2.ShouldBe(100f);
        y2.ShouldBe(0f);
    }

    [Fact]
    public void GetRotatedGradientEndpoints_OneHundredEightyDegrees_FlipsAcrossBboxCenter()
    {
        // bbox 100x100, center (50, 50). 180° flips both endpoints across the center:
        //   (0, 0)   → (100, 100)
        //   (100, 0) → (0, 100)
        LineArc arc = new()
        {
            Width = 100,
            Height = 100,
            GradientX1 = 0f,
            GradientY1 = 0f,
            GradientX2 = 100f,
            GradientY2 = 0f,
        };

        (float x1, float y1, float x2, float y2) = arc.GetRotatedGradientEndpoints(rotationDegrees: 180f);

        x1.ShouldBe(100f, tolerance: 0.001);
        y1.ShouldBe(100f, tolerance: 0.001);
        x2.ShouldBe(0f, tolerance: 0.001);
        y2.ShouldBe(100f, tolerance: 0.001);
    }
}
