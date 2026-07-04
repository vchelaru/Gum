using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;

namespace Examples.Shapes;

// Raylib mirror of SilkNetGum/Screens/CirclesScreen.cs (issue #2757). Section order, labels,
// and parameter sweeps are kept aligned with the Skia gallery so visual regressions in one
// backend are easy to spot against the other when both samples are run side-by-side. Skia is
// the authoritative reference (Skia and MonoGame shape rendering already match), so any visual
// divergence here points at a raylib renderer bug, not a sample drift.
//
// What's intentionally NOT mirrored: the Antialiasing section. raylib has no per-shape AA —
// framebuffer MSAA via SetConfigFlags(Msaa4xHint) in Program.Main is the only AA path, so
// toggling the runtime IsAntialiased flag would render identically. As of #3491 the flag DOES
// exist on the raylib CircleRuntime (round-trip parity so cross-backend code compiles), but it
// stays a no-op visually — hence still no Antialiasing row. The Blend section below IS new in
// #3491: unlike AA, raylib blend is real (LineCircle wraps its fill/stroke draws in the blend).
internal class CirclesScreen : FrameworkElement
{
    public CirclesScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        // Two-column root mirrors SilkNet/MG so the screen grows wide rather than tall as
        // rows accumulate — keeps the page legible without scrolling.
        ContainerRuntime root = new();
        root.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        root.StackSpacing = 24;
        root.X = 10;
        root.Y = 10;
        this.AddChild(root);

        ContainerRuntime left = BuildColumn();
        ContainerRuntime right = BuildColumn();
        root.Children.Add(left);
        root.Children.Add(right);

        // Section order mirrors SilkNetGum/Screens/CirclesScreen.cs exactly, minus the one
        // section raylib can't currently demo (Antialiasing). That keeps the remaining rows
        // top-aligned across both windows when run side-by-side.
        left.Children.Add(BuildSection("Sizes", BuildSizesRow()));
        left.Children.Add(BuildSection("Alpha", BuildAlphaRow()));
        left.Children.Add(BuildSection("Modes", BuildModeRow()));
        left.Children.Add(BuildSection("Stroke width", BuildStrokeWidthRow()));
        left.Children.Add(BuildSection("Alignment", BuildAlignmentRow()));
        left.Children.Add(BuildSection("Gradients", BuildGradientRow()));
        left.Children.Add(BuildSection("Rotation (filled)", BuildRotationFilledRow()));
        left.Children.Add(BuildSection("Rotation (outline)", BuildRotationOutlineUnsupportedRow()));

        right.Children.Add(BuildSection("Dropshadow", BuildDropshadowRow()));
        right.Children.Add(BuildSection("Dashed strokes", BuildDashedStrokeRow()));
        right.Children.Add(BuildSection("Fill + stroke", BuildBothColorsRow()));
        right.Children.Add(BuildSection("Hairline bleed (#2834)", BuildHairlineBleedRow()));
        right.Children.Add(BuildSection("Inscribed", BuildInscribedRow()));
        right.Children.Add(BuildSection("Non-square aspect", BuildNonSquareRow()));
        right.Children.Add(BuildSection("Blend (additive #3491)", BuildBlendRow()));
    }

    static ContainerRuntime BuildColumn()
    {
        ContainerRuntime column = new();
        column.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        column.StackSpacing = 14;
        column.WidthUnits = DimensionUnitType.RelativeToChildren;
        column.HeightUnits = DimensionUnitType.RelativeToChildren;
        column.Width = 0;
        column.Height = 0;
        return column;
    }

    static ContainerRuntime BuildSection(string label, GraphicalUiElement body)
    {
        ContainerRuntime section = new();
        section.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        section.StackSpacing = 4;
        section.WidthUnits = DimensionUnitType.RelativeToChildren;
        section.HeightUnits = DimensionUnitType.RelativeToChildren;
        section.Width = 0;
        section.Height = 0;

        TextRuntime header = new();
        header.Text = label;
        header.Red = 220;
        header.Green = 220;
        header.Blue = 220;
        section.Children.Add(header);
        section.Children.Add(body);
        return section;
    }

    static ContainerRuntime BuildHorizontalRow()
    {
        ContainerRuntime row = new();
        row.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 16;
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        return row;
    }

    static ContainerRuntime BuildSizesRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float radius in new[] { 16f, 24f, 32f, 48f })
        {
            CircleRuntime circle = new();
            circle.Radius = radius;
            circle.StrokeColor = Color.White;
            row.Children.Add(circle);
        }
        return row;
    }

    static ContainerRuntime BuildAlphaRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (byte alpha in new byte[] { 255, 192, 128, 64 })
        {
            CircleRuntime circle = new();
            circle.Radius = 24;
            circle.StrokeColor = new Color((byte)255, (byte)255, (byte)255, alpha);
            row.Children.Add(circle);
        }
        return row;
    }

    static ContainerRuntime BuildModeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        CircleRuntime filled = new();
        filled.Radius = 28;
        filled.FillColor = new Color(220, 20, 60, 255);
        filled.IsFilled = true;
        row.Children.Add(filled);

        CircleRuntime stroked = new();
        stroked.Radius = 28;
        stroked.StrokeColor = new Color(0, 255, 255, 255);
        stroked.StrokeWidth = 3;
        row.Children.Add(stroked);

        CircleRuntime defaultCircle = new();
        defaultCircle.Radius = 28;
        row.Children.Add(defaultCircle);

        return row;
    }

    // Issue #3491 — classic additive-blend demo: three filled RGB circles overlapping on a black
    // frame, each with Blend = Additive. Additive keeps the destination and adds the source, so the
    // pairwise overlaps read yellow (R+G) / cyan (G+B) / magenta (R+B) and the center reads white.
    // With the pre-#3491 behavior (Blend ignored) the circles would just paint over each other in
    // draw order — opaque, no color mixing — so this row makes the feature obvious at a glance.
    static RectangleRuntime BuildBlendRow()
    {
        RectangleRuntime frame = new();
        frame.Width = 130;
        frame.Height = 110;
        frame.FillColor = Color.Black;
        frame.IsFilled = true;

        (Color color, float x, float y)[] discs =
        {
            (new Color((byte)255, (byte)0, (byte)0, (byte)255), 0f, -14f),
            (new Color((byte)0, (byte)255, (byte)0, (byte)255), -14f, 10f),
            (new Color((byte)0, (byte)0, (byte)255, (byte)255), 14f, 10f),
        };

        foreach ((Color color, float x, float y) in discs)
        {
            CircleRuntime circle = new();
            circle.Radius = 26;
            circle.FillColor = color;
            circle.IsFilled = true;
            circle.Blend = Gum.RenderingLibrary.Blend.Additive;
            circle.XOrigin = HorizontalAlignment.Center;
            circle.XUnits = GeneralUnitType.PixelsFromMiddle;
            circle.X = x;
            circle.YOrigin = VerticalAlignment.Center;
            circle.YUnits = GeneralUnitType.PixelsFromMiddle;
            circle.Y = y;
            frame.Children.Add(circle);
        }

        return frame;
    }

    static ContainerRuntime BuildStrokeWidthRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            CircleRuntime circle = new();
            circle.Radius = 24;
            circle.StrokeColor = new Color(144, 238, 144, 255);
            circle.StrokeWidth = strokeWidth;
            row.Children.Add(circle);
        }
        return row;
    }

    static ContainerRuntime BuildAlignmentRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (VerticalAlignment alignment in new[] { VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom })
        {
            row.Children.Add(BuildAlignmentCell(alignment));
        }
        return row;
    }

    // Rotation row — black→white horizontal gradient on circles, rotated in 60° steps
    // (0/60/120/180). Plain circles are rotation-symmetric, so the gradient is what makes
    // the rotation visible. Endpoints are 0→20 px (less than the 56 px diameter) so the
    // transition is concentrated in a narrow band — the resulting hard light/dark edge
    // makes the rotation angle obvious. Cells use a fixed-size frame because Rotation
    // pushes content outside the natural bounding box, which breaks the
    // RelativeToChildren row sizing. Mirrors the same row on the MG and SilkNet sides.
    // Filled row only on raylib — see BuildRotationOutlineUnsupportedRow for why the outline
    // row is replaced with a label. The filled row exercises the post-#2956 contract on the
    // fill slot, plus the new LineCircle rotation handling added in the same PR.
    static ContainerRuntime BuildRotationFilledRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float rotation in new[] { 0f, 60f, 120f, 180f })
        {
            row.Children.Add(BuildRotatedGradientCircleCell(rotation));
        }
        return row;
    }

    // Issue #2956 — raylib's LineCircle stroke pass is solid-color only (no gradient-on-
    // stroke support); combined with default-transparent fill, an outline + gradient cell
    // would render either nothing (with the contract enforced) or a solid white outline on
    // a rotation-symmetric circle (no rotation signal). Label the row instead so the gallery
    // documents the limitation rather than presenting a misleading visual.
    static ContainerRuntime BuildRotationOutlineUnsupportedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        TextRuntime label = new();
        label.Text = "Not supported in raylib (LineCircle has no gradient-on-stroke path)";
        label.Red = 220;
        label.Green = 220;
        label.Blue = 220;
        row.Children.Add(label);
        return row;
    }

    static RectangleRuntime BuildRotatedGradientCircleCell(float rotation)
    {
        RectangleRuntime frame = new();
        frame.Width = 70;
        frame.Height = 70;
        frame.FillColor = new Color(60, 60, 80, 255);
        frame.IsFilled = true;

        CircleRuntime circle = new();
        circle.Radius = 28;
        circle.XOrigin = HorizontalAlignment.Center;
        circle.XUnits = GeneralUnitType.PixelsFromMiddle;
        circle.YOrigin = VerticalAlignment.Center;
        circle.YUnits = GeneralUnitType.PixelsFromMiddle;
        // Filled cell — light up the fill slot with an opaque color so the gradient renders
        // on the fill. raylib's outline-only case is handled by BuildRotationOutlineUnsupportedRow.
        circle.FillColor = Color.Black;
        circle.IsFilled = true;
        circle.UseGradient = true;
        circle.GradientType = GradientType.Linear;
        circle.Color2 = Color.White;
        circle.GradientX1 = 0; circle.GradientY1 = 0;
        circle.GradientX2 = 20; circle.GradientY2 = 0;
        circle.Rotation = rotation;
        frame.Children.Add(circle);
        return frame;
    }

    static RectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        RectangleRuntime frame = new();
        frame.Width = 128;
        frame.Height = 100;
        frame.FillColor = new Color(50, 50, 70, 255);
        frame.IsFilled = true;

        CircleRuntime circle = new();
        circle.Radius = 22;
        circle.FillColor = new Color(255, 165, 0, 255);
        circle.IsFilled = true;
        circle.XOrigin = HorizontalAlignment.Center;
        circle.XUnits = GeneralUnitType.PixelsFromMiddle;
        circle.YOrigin = alignment;
        circle.YUnits = alignment switch
        {
            VerticalAlignment.Top => GeneralUnitType.PixelsFromSmall,
            VerticalAlignment.Center => GeneralUnitType.PixelsFromMiddle,
            VerticalAlignment.Bottom => GeneralUnitType.PixelsFromLarge,
            _ => GeneralUnitType.PixelsFromMiddle,
        };
        frame.Children.Add(circle);
        return frame;
    }

    // #2757 follow-ups #8 + #9. Mirrors SilkNet's BuildGradientRow — same four cells, same
    // gradient axis coordinates. Coords are in pixels relative to the circle's bounding-box
    // top-left (0,0 = top-left, 2R,2R = bottom-right). raylib routes all four through an
    // rlgl triangle fan with per-vertex colors.
    static ContainerRuntime BuildGradientRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Linear horizontal: white → steel blue, left edge to right edge of the 56x56 bbox.
        CircleRuntime linearH = new();
        linearH.Radius = 28;
        linearH.FillColor = Color.White;
        linearH.IsFilled = true;
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color2 = new Color(70, 130, 180, 255);
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 56; linearH.GradientY2 = 0;
        row.Children.Add(linearH);

        // Linear vertical: gold → crimson, top to bottom.
        CircleRuntime linearV = new();
        linearV.Radius = 28;
        linearV.FillColor = new Color(255, 215, 0, 255);
        linearV.IsFilled = true;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color2 = new Color(220, 20, 60, 255);
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 56;
        row.Children.Add(linearV);

        // Linear diagonal: cyan → magenta, top-left to bottom-right.
        CircleRuntime linearD = new();
        linearD.Radius = 28;
        linearD.FillColor = new Color(0, 255, 255, 255);
        linearD.IsFilled = true;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color2 = new Color(255, 0, 255, 255);
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 56; linearD.GradientY2 = 56;
        row.Children.Add(linearD);

        // Radial centered: white → dark green from (28,28) with inner=0, outer=28.
        CircleRuntime radial = new();
        radial.Radius = 28;
        radial.FillColor = Color.White;
        radial.IsFilled = true;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
        radial.Color2 = new Color(0, 100, 0, 255);
        radial.GradientX1 = 28; radial.GradientY1 = 28;
        radial.GradientInnerRadius = 0;
        radial.GradientOuterRadius = 28;
        row.Children.Add(radial);

        return row;
    }

    // #2790 parity: two-slot composition. Setting both FillColor and StrokeColor lights up
    // both layers — order-independent. Each cell here should render a filled disk with a
    // contrasting ring around it.
    static ContainerRuntime BuildBothColorsRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        CircleRuntime strokeLast = new();
        strokeLast.Radius = 28;
        strokeLast.FillColor = new Color(220, 20, 60, 255);
        strokeLast.IsFilled = true;
        strokeLast.StrokeColor = new Color(0, 255, 255, 255);
        strokeLast.StrokeWidth = 4;
        row.Children.Add(strokeLast);

        CircleRuntime fillLast = new();
        fillLast.Radius = 28;
        fillLast.StrokeColor = new Color(255, 0, 255, 255);
        fillLast.StrokeWidth = 4;
        fillLast.FillColor = new Color(255, 215, 0, 255);
        fillLast.IsFilled = true;
        row.Children.Add(fillLast);

        return row;
    }

    // Visual repro for #2834 — hairline white stroke over a red fill. raylib's LineCircle
    // draws the fill (DrawCircleV) and the stroke (DrawRing arcs) as separate ops; whether
    // the same AA-bleed shows up on raylib depends on the renderer details (framebuffer
    // MSAA rather than per-shape AA). This row exists so the raylib output can be compared
    // directly against the Skia and MG-shapes outputs to confirm the bleed's scope.
    static ContainerRuntime BuildHairlineBleedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        Color red = new Color(255, 0, 0, 255);
        foreach (float strokeWidth in new[] { 1f, 2f, 3f, 4f })
        {
            CircleRuntime circle = new();
            circle.Radius = 28;
            circle.FillColor = red;
            circle.IsFilled = true;
            circle.StrokeColor = Color.White;
            circle.StrokeWidth = strokeWidth;
            row.Children.Add(circle);
        }
        return row;
    }

    static ContainerRuntime BuildInscribedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 4f, 8f, 12f })
        {
            row.Children.Add(BuildInscribedCell(strokeWidth));
        }
        return row;
    }

    // Visual acceptance for non-square circles — wide, tall, and square cells. The gray frame
    // is the circle's bounding box; the circle inside must use min(W,H) for its diameter and
    // sit centered. Mirrors the matching row in MonoGameGumInCode and SilkNetGum.
    static ContainerRuntime BuildNonSquareRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach ((float w, float h) in new[] { (200f, 50f), (50f, 120f), (100f, 100f) })
        {
            row.Children.Add(BuildNonSquareCell(w, h));
        }
        return row;
    }

    static RectangleRuntime BuildNonSquareCell(float width, float height)
    {
        RectangleRuntime frame = new();
        frame.Width = width;
        frame.Height = height;
        frame.FillColor = new Color(60, 60, 80, 255);
        frame.IsFilled = true;

        CircleRuntime circle = new();
        circle.Width = width;
        circle.Height = height;
        circle.WidthUnits = DimensionUnitType.Absolute;
        circle.HeightUnits = DimensionUnitType.Absolute;
        circle.FillColor = new Color(46, 139, 87, 255);
        circle.IsFilled = true;
        circle.StrokeColor = Color.Yellow;
        circle.StrokeWidth = 1;
        frame.Children.Add(circle);
        return frame;
    }

    static RectangleRuntime BuildInscribedCell(float strokeWidth)
    {
        RectangleRuntime frame = new();
        frame.Width = 64;
        frame.Height = 64;
        frame.FillColor = new Color(60, 60, 80, 255);
        frame.IsFilled = true;

        CircleRuntime circle = new();
        circle.Radius = 32;
        circle.FillColor = new Color(46, 139, 87, 255);
        circle.IsFilled = true;
        circle.StrokeColor = new Color(255, 255, 0, 255);
        circle.StrokeWidth = strokeWidth;
        circle.StrokeWidthUnits = DimensionUnitType.Absolute;
        frame.Children.Add(circle);
        return frame;
    }

    // #2757 follow-up #10 — raylib mirror of SilkNet's BuildDashedStrokeRow. raylib has no
    // built-in path-effect dash (Skia has SKPathEffect.CreateDash); LineCircle emits each
    // dash as a separate DrawRing arc with angles computed from circumference.
    static ContainerRuntime BuildDashedStrokeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Baseline: solid stroke (dash=0).
        CircleRuntime solid = new();
        solid.Radius = 28;
        solid.StrokeColor = Color.White;
        solid.StrokeWidth = 2;
        row.Children.Add(solid);

        // Short 6/4 dash.
        CircleRuntime short64 = new();
        short64.Radius = 28;
        short64.StrokeColor = Color.White;
        short64.StrokeWidth = 2;
        short64.StrokeDashLength = 6;
        short64.StrokeGapLength = 4;
        row.Children.Add(short64);

        // Tight 2/2 dotted at 1 px — framebuffer MSAA keeps edges smooth without per-shape AA.
        CircleRuntime dotted = new();
        dotted.Radius = 28;
        dotted.StrokeColor = Color.White;
        dotted.StrokeWidth = 1;
        dotted.StrokeDashLength = 2;
        dotted.StrokeGapLength = 2;
        row.Children.Add(dotted);

        // Long-dash motif: 12/6 with a thicker stroke.
        CircleRuntime longDash = new();
        longDash.Radius = 28;
        longDash.StrokeColor = new Color(144, 238, 144, 255);
        longDash.StrokeWidth = 3;
        longDash.StrokeDashLength = 12;
        longDash.StrokeGapLength = 6;
        row.Children.Add(longDash);

        return row;
    }

    // #2757 follow-up #12 — raylib mirror of SilkNet's BuildDropshadowRow. Same four cells
    // (baseline, soft, hard offset, colored). raylib has no SKImageFilter.CreateDropShadow
    // equivalent; the renderable approximates the blurred edge with concentric semi-transparent
    // rings of decreasing alpha.
    static ContainerRuntime BuildDropshadowRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        Color goldenrod = new Color(218, 165, 32, 255);

        // Baseline: no shadow.
        CircleRuntime baseline = new();
        baseline.Radius = 28;
        baseline.FillColor = goldenrod;
        baseline.IsFilled = true;
        row.Children.Add(baseline);

        // Soft shadow: noticeable offset, generous blur, default opaque black.
        CircleRuntime soft = new();
        soft.Radius = 28;
        soft.FillColor = goldenrod;
        soft.IsFilled = true;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 14;
        soft.DropshadowOffsetY = 14;
        soft.DropshadowBlur = 4;
        row.Children.Add(soft);

        // Hard offset: bigger offset, no blur, semi-transparent black. Per-channel setters
        // mirror SilkNet's example.
        CircleRuntime hard = new();
        hard.Radius = 28;
        hard.FillColor = goldenrod;
        hard.IsFilled = true;
        hard.HasDropshadow = true;
        hard.DropshadowRed = 0; hard.DropshadowGreen = 0; hard.DropshadowBlue = 0; hard.DropshadowAlpha = 160;
        hard.DropshadowOffsetX = 16;
        hard.DropshadowOffsetY = 16;
        hard.DropshadowBlur = 0;
        row.Children.Add(hard);

        // Colored shadow: magenta cast with real offset so the cast is visible against the
        // blue background.
        CircleRuntime colored = new();
        colored.Radius = 28;
        colored.FillColor = goldenrod;
        colored.IsFilled = true;
        colored.HasDropshadow = true;
        colored.DropshadowRed = 220; colored.DropshadowGreen = 40; colored.DropshadowBlue = 160; colored.DropshadowAlpha = 220;
        colored.DropshadowOffsetX = 16;
        colored.DropshadowOffsetY = 16;
        colored.DropshadowBlur = 6;
        row.Children.Add(colored);

        // Issue #2851 visual acceptance: same soft-shadow config as the second cell, but with
        // the body's alpha cut to 80. The shadow must fade alongside the body — matches Skia
        // (and therefore the Gum tool/viewport). Pre-fix the raylib LineCircle renderable
        // emitted DropshadowColor straight through and left an opaque shadow ghost behind.
        CircleRuntime fadedBody = new();
        fadedBody.Radius = 28;
        fadedBody.FillColor = new Color((byte)218, (byte)165, (byte)32, (byte)80);
        fadedBody.IsFilled = true;
        fadedBody.HasDropshadow = true;
        fadedBody.DropshadowOffsetX = 14;
        fadedBody.DropshadowOffsetY = 14;
        fadedBody.DropshadowBlur = 4;
        row.Children.Add(fadedBody);

        return row;
    }
}
