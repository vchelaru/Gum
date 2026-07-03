using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Renderables;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;

namespace Examples.Shapes;

// Raylib mirror of SilkNetGum/Screens/RectanglesScreen.cs (issue #2757). Section order, labels,
// and parameter sweeps are kept aligned with the Skia/MG gallery so visual regressions in one
// backend are easy to spot against the other when both samples are run side-by-side. Background
// clear color matches too (Program.Main sets Color(51,76,204,255) — same as SilkNet's
// ClearColor(0.2,0.3,0.8,1.0)).
//
// What's intentionally NOT mirrored: per-corner radii section (raylib's DrawRectangleRounded
// only takes one uniform roundness; per-corner support requires writing an rlgl triangle mesh
// that stitches four quarter-circle arcs onto the corners) and antialiasing section (raylib
// has no per-shape AA — framebuffer MSAA via SetConfigFlags(Msaa4xHint) in Program.Main is the
// only AA path, so toggling the runtime IsAntialiased flag would render identically). Both are
// tracked as #2757 follow-ups.
internal class RectanglesScreen : FrameworkElement
{
    public RectanglesScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

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

        // Section order mirrors SilkNetGum/Screens/RectanglesScreen.cs exactly, minus the two
        // sections raylib can't currently demo (per-corner radii, antialiasing). That keeps the
        // remaining rows top-aligned across both windows when run side-by-side.
        left.Children.Add(BuildSection("Sizes (40, 60, 90, 130 wide) — default outline", BuildSizesRow()));
        left.Children.Add(BuildSection("Alpha on StrokeColor (255, 192, 128, 64)", BuildAlphaRow()));
        left.Children.Add(BuildSection("Modes: FillColor, StrokeColor, Fill+Stroke, default", BuildModeRow()));
        left.Children.Add(BuildSection("StrokeWidth (1, 2, 4, 8 px)", BuildStrokeWidthRow()));
        left.Children.Add(BuildSection("Alignment inside a 128x100 frame (Top / Center / Bottom)", BuildAlignmentRow()));
        left.Children.Add(BuildSection("CornerRadius (0, 6, 16, 28) — raylib DrawRectangleRounded via RectangleRuntime (#2757)", BuildCornerRadiusRow()));
        left.Children.Add(BuildSection("Gradients (linear / radial / diagonal / centered)", BuildGradientRow()));

        right.Children.Add(BuildSection("Dropshadow (off / soft / hard offset / colored) — raylib approximates the blur via concentric rectangles (#2757)", BuildDropshadowRow()));
        right.Children.Add(BuildSection("Dashed strokes (solid / 6/4 / 2/2 dotted / long-dash) — raylib walks the perimeter, one DrawLineEx per dash (#2757)", BuildDashedStrokeRow()));
        right.Children.Add(BuildSection("FillColor + StrokeColor on the same instance — both layers render simultaneously (#2757)", BuildBothColorsRow()));
        right.Children.Add(BuildSection("Blend (Additive) — overlapping R/G/B rectangles brighten toward white (#3458). Only Additive is a non-alpha mode on raylib today; the middle cell shows Normal for contrast.", BuildBlendRow()));
        right.Children.Add(BuildSection("Inscribed in a 64x64 frame — stroke must stay inside the gray rectangle's bounds at every StrokeWidth (#2757)", BuildInscribedRow()));
        right.Children.Add(BuildSection("Rotation (filled)", BuildRotationFilledRow()));
        right.Children.Add(BuildSection("Rotation (outline)", BuildRotationOutlineUnsupportedRow()));
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
        foreach (float width in new[] { 40f, 60f, 90f, 130f })
        {
            RectangleRuntime rect = new();
            rect.Width = width;
            rect.Height = 40;
            rect.StrokeColor = Color.White;
            row.Children.Add(rect);
        }
        return row;
    }

    static ContainerRuntime BuildAlphaRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (byte alpha in new byte[] { 255, 192, 128, 64 })
        {
            RectangleRuntime rect = new();
            rect.Width = 60;
            rect.Height = 40;
            rect.StrokeColor = new Color((byte)255, (byte)255, (byte)255, alpha);
            row.Children.Add(rect);
        }
        return row;
    }

    static ContainerRuntime BuildModeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime filled = new();
        filled.Width = 80; filled.Height = 50;
        filled.FillColor = new Color(220, 20, 60, 255);
        filled.IsFilled = true;
        row.Children.Add(filled);

        RectangleRuntime stroked = new();
        stroked.Width = 80; stroked.Height = 50;
        stroked.StrokeColor = new Color(0, 255, 255, 255);
        stroked.StrokeWidth = 2;
        row.Children.Add(stroked);

        RectangleRuntime both = new();
        both.Width = 80; both.Height = 50;
        both.FillColor = new Color(40, 40, 80, 255);
        both.IsFilled = true;
        both.StrokeColor = new Color(255, 255, 0, 255);
        both.StrokeWidth = 2;
        row.Children.Add(both);

        RectangleRuntime defaultRect = new();
        defaultRect.Width = 80; defaultRect.Height = 50;
        row.Children.Add(defaultRect);

        return row;
    }

    static ContainerRuntime BuildStrokeWidthRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            RectangleRuntime rect = new();
            rect.Width = 70;
            rect.Height = 50;
            rect.StrokeColor = new Color(144, 238, 144, 255);
            rect.StrokeWidth = strokeWidth;
            row.Children.Add(rect);
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

    static RectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        RectangleRuntime frame = new();
        frame.Width = 128;
        frame.Height = 100;
        frame.FillColor = new Color(50, 50, 70, 255);
        frame.IsFilled = true;

        RectangleRuntime rect = new();
        rect.Width = 50;
        rect.Height = 30;
        rect.FillColor = new Color(255, 165, 0, 255);
        rect.IsFilled = true;
        rect.XOrigin = HorizontalAlignment.Center;
        rect.XUnits = GeneralUnitType.PixelsFromMiddle;
        rect.YOrigin = alignment;
        rect.YUnits = alignment switch
        {
            VerticalAlignment.Top => GeneralUnitType.PixelsFromSmall,
            VerticalAlignment.Center => GeneralUnitType.PixelsFromMiddle,
            VerticalAlignment.Bottom => GeneralUnitType.PixelsFromLarge,
            _ => GeneralUnitType.PixelsFromMiddle,
        };
        frame.Children.Add(rect);
        return frame;
    }

    // #2757 — uniform CornerRadius in pixels (same unit as Skia's RectangleRuntime.CornerRadius).
    // The renderable converts to raylib's 0..1 roundness fraction at draw time. Per-corner
    // overrides are tracked separately — DrawRectangleRounded only takes a single roundness.
    static ContainerRuntime BuildCornerRadiusRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float cornerRadius in new[] { 0f, 6f, 16f, 28f })
        {
            RectangleRuntime rect = new();
            rect.Width = 80;
            rect.Height = 60;
            rect.FillColor = new Color(40, 40, 80, 255);
            rect.IsFilled = true;
            rect.StrokeColor = new Color(255, 165, 0, 255);
            rect.StrokeWidth = 2;
            rect.CornerRadius = cornerRadius;
            row.Children.Add(rect);
        }
        return row;
    }

    // Mirrors SilkNet's BuildGradientRow — same four cells, same gradient axis coordinates.
    // Coords are in pixels relative to the rectangle's top-left.
    static ContainerRuntime BuildGradientRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Linear horizontal: white → steel blue, left edge to right edge.
        RectangleRuntime linearH = new();
        linearH.Width = 70; linearH.Height = 50;
        linearH.FillColor = Color.White;
        linearH.IsFilled = true;
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color2 = new Color(70, 130, 180, 255);
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 70; linearH.GradientY2 = 0;
        row.Children.Add(linearH);

        // Linear vertical: gold → crimson, top to bottom.
        RectangleRuntime linearV = new();
        linearV.Width = 70; linearV.Height = 50;
        linearV.FillColor = new Color(255, 215, 0, 255);
        linearV.IsFilled = true;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color2 = new Color(220, 20, 60, 255);
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 50;
        row.Children.Add(linearV);

        // Linear diagonal: cyan → magenta, top-left to bottom-right.
        RectangleRuntime linearD = new();
        linearD.Width = 70; linearD.Height = 50;
        linearD.FillColor = new Color(0, 255, 255, 255);
        linearD.IsFilled = true;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color2 = new Color(255, 0, 255, 255);
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 70; linearD.GradientY2 = 50;
        row.Children.Add(linearD);

        // Radial centered: white → dark green from (35, 25) with inner=0, outer=35.
        RectangleRuntime radial = new();
        radial.Width = 70; radial.Height = 50;
        radial.FillColor = Color.White;
        radial.IsFilled = true;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
        radial.Color2 = new Color(0, 100, 0, 255);
        radial.GradientX1 = 35; radial.GradientY1 = 25;
        radial.GradientInnerRadius = 0;
        radial.GradientOuterRadius = 35;
        row.Children.Add(radial);

        return row;
    }

    // Two-slot composition: setting both FillColor and StrokeColor lights up both layers —
    // order-independent. Each cell renders a filled card with a contrasting frame around it.
    static ContainerRuntime BuildBothColorsRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime strokeLast = new();
        strokeLast.Width = 80; strokeLast.Height = 50;
        strokeLast.FillColor = new Color(220, 20, 60, 255);
        strokeLast.IsFilled = true;
        strokeLast.StrokeColor = new Color(0, 255, 255, 255);
        strokeLast.StrokeWidth = 4;
        row.Children.Add(strokeLast);

        RectangleRuntime fillLast = new();
        fillLast.Width = 80; fillLast.Height = 50;
        fillLast.StrokeColor = new Color(255, 0, 255, 255);
        fillLast.StrokeWidth = 4;
        fillLast.FillColor = new Color(255, 215, 0, 255);
        fillLast.IsFilled = true;
        row.Children.Add(fillLast);

        return row;
    }

    // Issue #3458 — raylib RectangleRuntime.Blend. Two overlapping-triad cells sit side by side:
    // the left cell uses Blend.Additive on all three rectangles so where red/green/blue overlap the
    // channels sum and the intersection brightens (R+G = yellow, R+G+B ≈ white). The middle-right
    // cell is the SAME geometry with Blend left at the default Normal, so the topmost rectangle
    // simply occludes the others with no brightening — a side-by-side control that makes the
    // additive effect obvious. raylib only maps Additive to a non-alpha blend today (Normal and the
    // other Gum blend modes fall through to standard alpha in ToRaylibBlendMode), so those aren't
    // demoed here.
    static ContainerRuntime BuildBlendRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        row.Children.Add(BuildBlendTriadCell(Gum.RenderingLibrary.Blend.Additive));
        row.Children.Add(BuildBlendTriadCell(Gum.RenderingLibrary.Blend.Normal));
        return row;
    }

    // A dark 150x120 frame with three 70x70 primary-color rectangles arranged so all three
    // overlap in the middle. Under Additive the overlaps sum toward white; under Normal the last
    // rectangle drawn just covers the earlier ones.
    static ContainerRuntime BuildBlendTriadCell(Gum.RenderingLibrary.Blend blend)
    {
        ContainerRuntime frame = new();
        frame.Width = 150;
        frame.Height = 120;

        RectangleRuntime backdrop = new();
        backdrop.Width = 0;
        backdrop.Height = 0;
        backdrop.WidthUnits = DimensionUnitType.RelativeToParent;
        backdrop.HeightUnits = DimensionUnitType.RelativeToParent;
        backdrop.FillColor = new Color(20, 20, 30, 255);
        backdrop.IsFilled = true;
        frame.Children.Add(backdrop);

        AddBlendRect(frame, blend, new Color(255, 0, 0, 255), x: 10, y: 10);
        AddBlendRect(frame, blend, new Color(0, 255, 0, 255), x: 45, y: 10);
        AddBlendRect(frame, blend, new Color(0, 0, 255, 255), x: 27, y: 42);

        return frame;
    }

    static void AddBlendRect(ContainerRuntime frame, Gum.RenderingLibrary.Blend blend, Color color, float x, float y)
    {
        RectangleRuntime rect = new();
        rect.Width = 70;
        rect.Height = 70;
        rect.X = x;
        rect.Y = y;
        rect.FillColor = color;
        rect.IsFilled = true;
        rect.Blend = blend;
        frame.Children.Add(rect);
    }

    // Mirror of SilkNet's BuildInscribedRow. As of #2757, LineRectangle insets the rendered
    // stroke entirely inside the nominal bounds (outer edge sits at the rect's nominal extent,
    // not on it) — same contract as Skia's RenderableShapeBase.IsOffsetAppliedForStroke
    // (#2814). With that in place, the 12px stroke cell stays inside its 64x64 gray frame here
    // exactly like on Skia.
    static ContainerRuntime BuildInscribedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 4f, 8f, 12f })
        {
            row.Children.Add(BuildInscribedCell(strokeWidth));
        }
        return row;
    }

    // Rotation row — black→white horizontal gradient on rectangles, rotated in 60° steps
    // (0/60/120/180). Gradient endpoints are 0→20 px (less than the 70 px width) so the
    // transition is concentrated in a narrow band; the resulting hard light/dark edge
    // makes the rotation angle obvious. Cells wrap each rotated rectangle in a fixed-size
    // frame because Rotation pushes content outside the natural bounding box, which
    // breaks RelativeToChildren row sizing. 100x100 frame is sized to contain a 70x50
    // rectangle at any rotation. Mirrors the same row on the MG and SilkNet sides.
    // Filled row only on raylib — see BuildRotationOutlineUnsupportedRow for why the outline
    // row is replaced with a label.
    static ContainerRuntime BuildRotationFilledRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float rotation in new[] { 0f, 60f, 120f, 180f })
        {
            row.Children.Add(BuildRotatedGradientRectCell(rotation));
        }
        return row;
    }

    // Issue #2956 — raylib's LineRectangle stroke pass is solid-color only (no gradient-on-
    // stroke support). The outline row's contract (gradient on stroke) can't be exercised
    // there, so label the row to document the limitation instead of presenting a misleading
    // visual. Kept consistent with the matching label on raylib's CirclesScreen.
    static ContainerRuntime BuildRotationOutlineUnsupportedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        TextRuntime label = new();
        label.Text = "Not supported in raylib (LineRectangle has no gradient-on-stroke path)";
        label.Red = 220;
        label.Green = 220;
        label.Blue = 220;
        row.Children.Add(label);
        return row;
    }

    static RectangleRuntime BuildRotatedGradientRectCell(float rotation)
    {
        RectangleRuntime frame = new();
        frame.Width = 100;
        frame.Height = 100;
        frame.FillColor = new Color(60, 60, 80, 255);
        frame.IsFilled = true;

        RectangleRuntime rect = new();
        rect.Width = 70;
        rect.Height = 50;
        rect.XOrigin = HorizontalAlignment.Center;
        rect.XUnits = GeneralUnitType.PixelsFromMiddle;
        rect.YOrigin = VerticalAlignment.Center;
        rect.YUnits = GeneralUnitType.PixelsFromMiddle;
        // Filled cell — light up the fill slot so the gradient renders on the fill. raylib's
        // outline-only case is handled by BuildRotationOutlineUnsupportedRow.
        rect.FillColor = Color.Black;
        rect.IsFilled = true;
        rect.UseGradient = true;
        rect.GradientType = GradientType.Linear;
        rect.Color2 = Color.White;
        rect.GradientX1 = 0; rect.GradientY1 = 0;
        rect.GradientX2 = 20; rect.GradientY2 = 0;
        rect.Rotation = rotation;
        frame.Children.Add(rect);
        return frame;
    }

    static RectangleRuntime BuildInscribedCell(float strokeWidth)
    {
        RectangleRuntime frame = new();
        frame.Width = 64;
        frame.Height = 64;
        frame.FillColor = new Color(60, 60, 80, 255);
        frame.IsFilled = true;

        RectangleRuntime rect = new();
        rect.Width = 64;
        rect.Height = 64;
        rect.FillColor = new Color(46, 139, 87, 255);
        rect.IsFilled = true;
        rect.StrokeColor = new Color(255, 255, 0, 255);
        rect.StrokeWidth = strokeWidth;
        rect.StrokeWidthUnits = DimensionUnitType.Absolute;
        frame.Children.Add(rect);
        return frame;
    }

    // Raylib mirror of SilkNet's BuildDashedStrokeRow. Raylib has no built-in path-effect dash
    // (Skia has SKPathEffect.CreateDash); LineRectangle emits each dash as a separate
    // DrawLineEx call along the perimeter.
    static ContainerRuntime BuildDashedStrokeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime solid = new();
        solid.Width = 60; solid.Height = 50;
        solid.StrokeColor = Color.White;
        solid.StrokeWidth = 2;
        row.Children.Add(solid);

        RectangleRuntime short64 = new();
        short64.Width = 60; short64.Height = 50;
        short64.StrokeColor = Color.White;
        short64.StrokeWidth = 2;
        short64.StrokeDashLength = 6;
        short64.StrokeGapLength = 4;
        row.Children.Add(short64);

        RectangleRuntime dotted = new();
        dotted.Width = 60; dotted.Height = 50;
        dotted.StrokeColor = Color.White;
        dotted.StrokeWidth = 1;
        dotted.StrokeDashLength = 2;
        dotted.StrokeGapLength = 2;
        row.Children.Add(dotted);

        RectangleRuntime longDash = new();
        longDash.Width = 60; longDash.Height = 50;
        longDash.StrokeColor = new Color(144, 238, 144, 255);
        longDash.StrokeWidth = 3;
        longDash.StrokeDashLength = 12;
        longDash.StrokeGapLength = 6;
        row.Children.Add(longDash);

        return row;
    }

    // Raylib mirror of SilkNet's BuildDropshadowRow. Same four cells (baseline, soft, hard
    // offset, colored). raylib has no SKImageFilter.CreateDropShadow equivalent; the renderable
    // approximates the blurred edge with concentric semi-transparent rectangles.
    static ContainerRuntime BuildDropshadowRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        Color goldenrod = new Color(218, 165, 32, 255);

        RectangleRuntime baseline = new();
        baseline.Width = 60; baseline.Height = 50;
        baseline.FillColor = goldenrod;
        baseline.IsFilled = true;
        row.Children.Add(baseline);

        RectangleRuntime soft = new();
        soft.Width = 60; soft.Height = 50;
        soft.FillColor = goldenrod;
        soft.IsFilled = true;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 14;
        soft.DropshadowOffsetY = 14;
        soft.DropshadowBlur = 4;
        row.Children.Add(soft);

        RectangleRuntime hard = new();
        hard.Width = 60; hard.Height = 50;
        hard.FillColor = goldenrod;
        hard.IsFilled = true;
        hard.HasDropshadow = true;
        hard.DropshadowRed = 0; hard.DropshadowGreen = 0; hard.DropshadowBlue = 0; hard.DropshadowAlpha = 160;
        hard.DropshadowOffsetX = 16;
        hard.DropshadowOffsetY = 16;
        hard.DropshadowBlur = 0;
        row.Children.Add(hard);

        RectangleRuntime colored = new();
        colored.Width = 60; colored.Height = 50;
        colored.FillColor = goldenrod;
        colored.IsFilled = true;
        colored.HasDropshadow = true;
        colored.DropshadowRed = 220; colored.DropshadowGreen = 40; colored.DropshadowBlue = 160; colored.DropshadowAlpha = 220;
        colored.DropshadowOffsetX = 16;
        colored.DropshadowOffsetY = 16;
        colored.DropshadowBlur = 6;
        row.Children.Add(colored);

        // Issue #2851 visual acceptance — body alpha multiplies into the shadow alpha.
        RectangleRuntime fadedBody = new();
        fadedBody.Width = 60; fadedBody.Height = 50;
        // This seems wrong:
        //fadedBody.FillColor = new Color((byte)218, (byte)165, (byte)32, (byte)80);
        fadedBody.FillColor = new Color((byte)218, (byte)165, (byte)32, (byte)32);
        fadedBody.IsFilled = true;
        fadedBody.HasDropshadow = true;
        fadedBody.DropshadowOffsetX = 14;
        fadedBody.DropshadowOffsetY = 14;
        fadedBody.DropshadowBlur = 4;
        row.Children.Add(fadedBody);

        RectangleRuntime shadowTest = new();
        shadowTest.Width = 60; shadowTest.Height = 50;
        // This seems wrong:
        //fadedBody.FillColor = new Color((byte)218, (byte)165, (byte)32, (byte)80);
        shadowTest.FillColor = new Color((byte)218, (byte)165, (byte)32, (byte)32);
        shadowTest.IsFilled = true;
        shadowTest.HasDropshadow = true;
        shadowTest.DropshadowOffsetX = 14;
        shadowTest.DropshadowOffsetY = 14;
        shadowTest.DropshadowBlur = 0;
        row.Children.Add(shadowTest);

        return row;
    }

}
