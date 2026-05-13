using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;

namespace MonoGameGumShapesGallery.Screens;

// Four rows of five cells each, all using runtime types (no raw Apos.Shapes API):
//   1. ColoredCircleRuntime - solid / gradient / stroked variants.
//   2. RoundedRectangleRuntime - uniform CornerRadius, gradient, per-corner radii
//      (Apos.Shapes 0.6.9's CornerRadii overload, including the Forest Glade leaf shape).
//   3. LineRuntime - varied thickness, end caps, gradient, dropshadow.
//   4. ArcRuntime - the unified Apos<->Skia runtime from issue #2728. First cell is a bare
//      `new ArcRuntime()` to visually verify the locked-in defaults (flat caps,
//      SweepAngle = 90, Thickness = 10). The other cells exercise IsEndRounded, full-ring
//      sweep, thick stroke, and gradient.
internal class ShapeSurveyScreen : FrameworkElement
{
    private const int RowCount = 4;
    private const int ColCount = 5;

    private readonly float _canvasWidth;
    private readonly float _canvasHeight;

    public ShapeSurveyScreen(float canvasWidth, float canvasHeight)
        : base(new ContainerRuntime())
    {
        _canvasWidth = canvasWidth;
        _canvasHeight = canvasHeight;

        Dock(Gum.Wireframe.Dock.Fill);

        float cellW = _canvasWidth / ColCount;
        float rowH = _canvasHeight / RowCount;

        BuildCircleRow(0, cellW, rowH);
        BuildRoundedRectangleRow(1, cellW, rowH);
        BuildLineRow(2, cellW, rowH);
        BuildArcRow(3, cellW, rowH);
    }

    private void Place(GraphicalUiElement shape, int row, int col,
        float cellW, float rowH, float shapeW, float shapeH)
    {
        float cellCenterX = col * cellW + cellW / 2f;
        float cellCenterY = row * rowH + rowH / 2f;
        shape.X = cellCenterX - shapeW / 2f;
        shape.Y = cellCenterY - shapeH / 2f;
        shape.Width = shapeW;
        shape.Height = shapeH;
        this.AddChild(shape);
    }

    private void BuildCircleRow(int row, float cellW, float rowH)
    {
        const float size = 130f;
        Color teal = new Color(80, 180, 220);
        Color green = new Color(60, 180, 130);
        Color orange = new Color(220, 160, 60);
        Color purple = new Color(180, 80, 220);

        ColoredCircleRuntime filled = new ColoredCircleRuntime();
        filled.Color = teal;
        Place(filled, row, 0, cellW, rowH, size, size);

        ColoredCircleRuntime filledAlt = new ColoredCircleRuntime();
        filledAlt.Color = green;
        Place(filledAlt, row, 1, cellW, rowH, size, size);

        // Gradient: the ctor seeds Red1/Green1/Blue1 = 255 (white) and Red2 = 255, Green2 =
        // 255, Blue2 = 0 (yellow) along the gradient axis. UseGradient = true picks it up.
        ColoredCircleRuntime gradient = new ColoredCircleRuntime();
        gradient.UseGradient = true;
        Place(gradient, row, 2, cellW, rowH, size, size);

        ColoredCircleRuntime strokedThin = new ColoredCircleRuntime();
        strokedThin.IsFilled = false;
        strokedThin.Color = orange;
        strokedThin.StrokeWidth = 4;
        Place(strokedThin, row, 3, cellW, rowH, size, size);

        ColoredCircleRuntime strokedThick = new ColoredCircleRuntime();
        strokedThick.IsFilled = false;
        strokedThick.Color = purple;
        strokedThick.StrokeWidth = 12;
        Place(strokedThick, row, 4, cellW, rowH, size, size);
    }

    private void BuildRoundedRectangleRow(int row, float cellW, float rowH)
    {
        const float width = 200f;
        const float height = 70f;
        Color teal = new Color(80, 180, 220);
        Color green = new Color(112, 220, 80);
        Color orange = new Color(220, 160, 60);
        Color magenta = new Color(200, 120, 220);

        RoundedRectangleRuntime uniformSmall = new RoundedRectangleRuntime();
        uniformSmall.Color = teal;
        uniformSmall.CornerRadius = 8;
        Place(uniformSmall, row, 0, cellW, rowH, width, height);

        RoundedRectangleRuntime uniformLarge = new RoundedRectangleRuntime();
        uniformLarge.Color = orange;
        uniformLarge.CornerRadius = 22;
        Place(uniformLarge, row, 1, cellW, rowH, width, height);

        RoundedRectangleRuntime gradient = new RoundedRectangleRuntime();
        gradient.UseGradient = true;
        gradient.CornerRadius = 14;
        Place(gradient, row, 2, cellW, rowH, width, height);

        // Forest Glade signature leaf shape - opposite-corner asymmetric per-corner radii.
        // Uses the Apos.Shapes 0.6.9 CornerRadii overload exposed via CustomRadiusTopLeft etc.
        // CornerRadius = 0 because the per-corner values take over when any Custom* is set.
        RoundedRectangleRuntime leaf = new RoundedRectangleRuntime();
        leaf.Color = green;
        leaf.CornerRadius = 0;
        leaf.CustomRadiusTopLeft = 2;
        leaf.CustomRadiusTopRight = 18;
        leaf.CustomRadiusBottomRight = 2;
        leaf.CustomRadiusBottomLeft = 18;
        Place(leaf, row, 3, cellW, rowH, width, height);

        // Fully-asymmetric per-corner radii - sanity check that the four corners are wired
        // to the right Custom* property (a swap would be visible immediately here).
        RoundedRectangleRuntime asym = new RoundedRectangleRuntime();
        asym.Color = magenta;
        asym.CornerRadius = 0;
        asym.CustomRadiusTopLeft = 0;
        asym.CustomRadiusTopRight = 30;
        asym.CustomRadiusBottomRight = 12;
        asym.CustomRadiusBottomLeft = 4;
        Place(asym, row, 4, cellW, rowH, width, height);
    }

    private void BuildLineRow(int row, float cellW, float rowH)
    {
        const float width = 200f;
        const float height = 80f;
        Color teal = new Color(80, 180, 220);
        Color orange = new Color(220, 160, 60);
        Color purple = new Color(180, 80, 220);

        LineRuntime thin = new LineRuntime();
        thin.Color = teal;
        thin.StrokeWidth = 2;
        Place(thin, row, 0, cellW, rowH, width, height);

        LineRuntime thick = new LineRuntime();
        thick.Color = orange;
        thick.StrokeWidth = 10;
        Place(thick, row, 1, cellW, rowH, width, height);

        LineRuntime roundedEnds = new LineRuntime();
        roundedEnds.Color = purple;
        roundedEnds.StrokeWidth = 14;
        roundedEnds.IsRounded = true;
        Place(roundedEnds, row, 2, cellW, rowH, width, height);

        LineRuntime gradient = new LineRuntime();
        gradient.StrokeWidth = 12;
        gradient.IsRounded = true;
        gradient.UseGradient = true;
        Place(gradient, row, 3, cellW, rowH, width, height);

        LineRuntime dropshadow = new LineRuntime();
        dropshadow.Color = teal;
        dropshadow.StrokeWidth = 10;
        dropshadow.IsRounded = true;
        dropshadow.HasDropshadow = true;
        Place(dropshadow, row, 4, cellW, rowH, width, height);
    }

    private void BuildArcRow(int row, float cellW, float rowH)
    {
        const float size = 130f;

        // Bare default - verifies the locked-in defaults from issue #2728. A regression in
        // any of the unified ctor defaults shows up as a visibly-different first cell:
        //   IsEndRounded = false (the breaking-change default on Apos; was true pre-#2728)
        //   SweepAngle = 90
        //   Thickness = 10
        //   Color = white
        ArcRuntime defaultArc = new ArcRuntime();
        Place(defaultArc, row, 0, cellW, rowH, size, size);

        // Same as the default but with IsEndRounded explicitly set - this is the toggle
        // existing Apos consumers must apply if they relied on the previous rounded default.
        // Documented in docs/gum-tool/upgrading/migrating-to-2026-may.md.
        ArcRuntime rounded = new ArcRuntime();
        rounded.IsEndRounded = true;
        Place(rounded, row, 1, cellW, rowH, size, size);

        // Full ring - exercises the SweepAngle = 360 path (DrawRing under the hood when
        // ends are flat).
        ArcRuntime fullRing = new ArcRuntime();
        fullRing.SweepAngle = 360;
        fullRing.Thickness = 8;
        Place(fullRing, row, 2, cellW, rowH, size, size);

        // Thick rounded half-circle - bigger stroke + rounded caps stress the radius math
        // (the renderable computes inner/outer radii from Width/Height minus Thickness).
        ArcRuntime thickHalf = new ArcRuntime();
        thickHalf.SweepAngle = 180;
        thickHalf.Thickness = 18;
        thickHalf.IsEndRounded = true;
        thickHalf.Color = new Color(220, 160, 60);
        Place(thickHalf, row, 3, cellW, rowH, size, size);

        // Gradient three-quarter arc - exercises the UseGradient branch in Arc.Render. The
        // gradient stops are pre-seeded in the ctor (white to yellow); UseGradient = true
        // picks them up.
        ArcRuntime gradient = new ArcRuntime();
        gradient.SweepAngle = 270;
        gradient.Thickness = 14;
        gradient.IsEndRounded = true;
        gradient.UseGradient = true;
        Place(gradient, row, 4, cellW, rowH, size, size);
    }
}
