using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SilkNetGum.Screens;

// Skia mirror of MonoGameGumInCode/Screens/CirclesScreen.cs (issue #2785). The two
// files should stay in lock-step structurally — same sections, same parameter sweeps — so
// visual regressions in one backend are easy to spot against the other.
//
// What forces the two files apart:
//   - Color type. XNA Microsoft.Xna.Framework.Color becomes SKColor; named colors come from
//     SkiaSharp.SKColors instead of Color.X.
//   - The MonoGame CirclesScreen also uses VerticalAlignment from the FRB Forms types in its
//     alignment switch; here we use the same RenderingLibrary.Graphics.VerticalAlignment.
//
// Everything else — section layout, sweep values, property names (Radius, StrokeColor,
// FillColor, StrokeWidth, gradient props) — is identical to the MonoGame version.
internal class CirclesScreen : FrameworkElement
{
    public CirclesScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        // Two-column root so the screen grows wide rather than tall as rows accumulate. No
        // ScrollViewer in SkiaGum yet, so this is the cheapest layout that works on both
        // backends (mirrored in MonoGameGumInCode/Screens/CirclesScreen).
        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        root.StackSpacing = 24;
        root.X = 10;
        root.Y = 10;
        this.AddChild(root);

        ContainerRuntime left = BuildColumn();
        ContainerRuntime right = BuildColumn();
        root.Children.Add(left);
        root.Children.Add(right);

        left.Children.Add(BuildSection("Sizes", BuildSizesRow()));
        left.Children.Add(BuildSection("Alpha", BuildAlphaRow()));
        left.Children.Add(BuildSection("Modes", BuildModeRow()));
        left.Children.Add(BuildSection("Stroke width", BuildStrokeWidthRow()));
        left.Children.Add(BuildSection("Alignment", BuildAlignmentRow()));
        left.Children.Add(BuildSection("Gradients", BuildGradientRow()));
        left.Children.Add(BuildSection("Rotation (filled)", BuildRotationRow(filled: true)));
        left.Children.Add(BuildSection("Rotation (outline)", BuildRotationRow(filled: false)));

        right.Children.Add(BuildSection("Antialiasing", BuildAntialiasingRow()));
        right.Children.Add(BuildSection("Dropshadow", BuildDropshadowRow()));
        right.Children.Add(BuildSection("Dashed strokes", BuildDashedStrokeRow()));
        right.Children.Add(BuildSection("Fill + stroke", BuildBothColorsRow()));
        right.Children.Add(BuildSection("Hairline bleed (#2834)", BuildHairlineBleedRow()));
        right.Children.Add(BuildSection("Inscribed", BuildInscribedRow()));
        right.Children.Add(BuildSection("Non-square aspect", BuildNonSquareRow()));
    }

    static ContainerRuntime BuildColumn()
    {
        ContainerRuntime column = new();
        column.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
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
        section.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
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

    static ContainerRuntime BuildSizesRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float radius in new[] { 16f, 24f, 32f, 48f })
        {
            CircleRuntime circle = new();
            circle.Radius = radius;
            circle.StrokeColor = SKColors.White;
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
            circle.StrokeColor = new SKColor(255, 255, 255, alpha);
            row.Children.Add(circle);
        }
        return row;
    }

    static ContainerRuntime BuildModeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        CircleRuntime filled = new();
        filled.Radius = 28;
        filled.FillColor = SKColors.Crimson;
        filled.IsFilled = true;
        row.Children.Add(filled);

        CircleRuntime stroked = new();
        stroked.Radius = 28;
        stroked.StrokeColor = SKColors.Cyan;
        stroked.StrokeWidth = 3;
        row.Children.Add(stroked);

        CircleRuntime defaultCircle = new();
        defaultCircle.Radius = 28;
        row.Children.Add(defaultCircle);

        return row;
    }

    static ContainerRuntime BuildStrokeWidthRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            CircleRuntime circle = new();
            circle.Radius = 24;
            circle.StrokeColor = SKColors.LightGreen;
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

    static ContainerRuntime BuildGradientRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Linear horizontal: white → blue
        CircleRuntime linearH = new();
        linearH.Radius = 28;
        linearH.FillColor = SKColors.White; // gradient start stop is the fill color
        linearH.IsFilled = true;
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color2 = SKColors.SteelBlue;
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 56; linearH.GradientY2 = 0;
        row.Children.Add(linearH);

        // Linear vertical: yellow → red
        CircleRuntime linearV = new();
        linearV.Radius = 28;
        linearV.FillColor = SKColors.Gold;
        linearV.IsFilled = true;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color2 = SKColors.Crimson;
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 56;
        row.Children.Add(linearV);

        // Linear diagonal: cyan → magenta
        CircleRuntime linearD = new();
        linearD.Radius = 28;
        linearD.FillColor = SKColors.Cyan;
        linearD.IsFilled = true;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color2 = SKColors.Magenta;
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 56; linearD.GradientY2 = 56;
        row.Children.Add(linearD);

        // Radial centered: white → dark green
        CircleRuntime radial = new();
        radial.Radius = 28;
        radial.FillColor = SKColors.White;
        radial.IsFilled = true;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
        radial.Color2 = SKColors.DarkGreen;
        radial.GradientX1 = 28; radial.GradientY1 = 28;
        radial.GradientInnerRadius = 0;
        radial.GradientOuterRadius = 28;
        row.Children.Add(radial);

        return row;
    }

    // Visual acceptance for #2790. Two-slot composition means setting both FillColor and
    // StrokeColor lights up both layers simultaneously — order-independent. Each cell here
    // should render a filled disk with a contrasting ring around it.
    static ContainerRuntime BuildBothColorsRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        CircleRuntime strokeLast = new();
        strokeLast.Radius = 28;
        strokeLast.FillColor = SKColors.Crimson;
        strokeLast.IsFilled = true;
        strokeLast.StrokeColor = SKColors.Cyan;
        strokeLast.StrokeWidth = 4;
        row.Children.Add(strokeLast);

        CircleRuntime fillLast = new();
        fillLast.Radius = 28;
        fillLast.StrokeColor = SKColors.Magenta;
        fillLast.StrokeWidth = 4;
        fillLast.FillColor = SKColors.Gold;
        fillLast.IsFilled = true;
        row.Children.Add(fillLast);

        return row;
    }

    // Visual repro for #2834 — hairline white stroke over a red fill, the primary surface
    // for the issue. The fill and stroke are separate SkPaint draws, each antialiased; the
    // semi-transparent AA pixels at the fill's outer edge composite under the
    // semi-transparent AA pixels at the stroke's inner edge, producing a ~1 px pink halo
    // on the inside of the ring. Most visible at 1–2 px stroke; fades at 3–4 px.
    static ContainerRuntime BuildHairlineBleedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 3f, 4f })
        {
            CircleRuntime circle = new();
            circle.Radius = 28;
            circle.FillColor = SKColors.Red;
            circle.IsFilled = true;
            circle.StrokeColor = SKColors.White;
            circle.StrokeWidth = strokeWidth;
            row.Children.Add(circle);
        }
        return row;
    }

    // Visual contract for #2790: the stroke slot mirrors the runtime's Width/Height (pushed
    // each frame in SkiaShapeRuntime.PreRender) and RenderableShapeBase.IsOffsetAppliedForStroke
    // insets the rendered ring by half the stroke width. Cells get progressively thicker
    // strokes (1, 4, 8, 12) — every ring must stay inside the gray rectangle. If a stroke
    // bleeds past the frame, layout or PreRender mirroring is wrong.
    static ContainerRuntime BuildInscribedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 4f, 8f, 12f })
        {
            row.Children.Add(BuildInscribedCell(strokeWidth));
        }
        return row;
    }

    // Visual acceptance for #2852 — wide, tall, and square cells sharing the same render code.
    // Skia already exhibited the canonical behavior (radius = min(W,H)/2, centered); this row
    // pairs with the matching row in MonoGameGumInCode so the two backends can be
    // compared side-by-side after the Apos.Shapes fix.
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
        frame.FillColor = new SKColor(60, 60, 80);
        frame.IsFilled = true;

        CircleRuntime circle = new();
        circle.Width = width;
        circle.Height = height;
        circle.WidthUnits = DimensionUnitType.Absolute;
        circle.HeightUnits = DimensionUnitType.Absolute;
        circle.FillColor = SKColors.SeaGreen;
        circle.IsFilled = true;
        circle.StrokeColor = SKColors.Yellow;
        circle.StrokeWidth = 1;
        frame.Children.Add(circle);
        return frame;
    }

    static RectangleRuntime BuildInscribedCell(float strokeWidth)
    {
        RectangleRuntime frame = new();
        frame.Width = 64;
        frame.Height = 64;
        frame.FillColor = new SKColor(60, 60, 80);
        frame.IsFilled = true;

        CircleRuntime circle = new();
        circle.Radius = 32;
        circle.FillColor = SKColors.SeaGreen;
        circle.IsFilled = true;
        circle.StrokeColor = SKColors.Yellow;
        circle.StrokeWidth = strokeWidth;
        circle.StrokeWidthUnits = DimensionUnitType.Absolute;
        frame.Children.Add(circle);
        return frame;
    }

    // Issue #2798 visual acceptance: two pairs (filled disk + 1 px outline ring), once with
    // IsAntialiased = true (the default — soft edges) and once false (crisp pixels). On Skia
    // this flips SKPaint.IsAntialias on the contained renderable.
    static ContainerRuntime BuildAntialiasingRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        foreach (bool aa in new[] { true, false })
        {
            CircleRuntime filled = new();
            filled.Radius = 28;
            filled.FillColor = SKColors.Goldenrod;
            filled.IsFilled = true;
            filled.IsAntialiased = aa;
            row.Children.Add(filled);

            CircleRuntime ring = new();
            ring.Radius = 28;
            ring.StrokeColor = SKColors.White;
            ring.StrokeWidth = 1;
            ring.IsAntialiased = aa;
            row.Children.Add(ring);
        }

        return row;
    }

    // Issue #2797 visual acceptance: mirror of the MonoGameGumInCode dropshadow row.
    // Same four cells — baseline, soft, hard offset, colored — so visual regressions in one
    // backend are easy to spot against the other.
    static ContainerRuntime BuildDropshadowRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Baseline: no shadow.
        CircleRuntime baseline = new();
        baseline.Radius = 28;
        baseline.FillColor = SKColors.Goldenrod;
        baseline.IsFilled = true;
        row.Children.Add(baseline);

        // Soft shadow: noticeable offset, generous blur, default opaque black.
        CircleRuntime soft = new();
        soft.Radius = 28;
        soft.FillColor = SKColors.Goldenrod;
        soft.IsFilled = true;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 14;
        soft.DropshadowOffsetY = 14;
        soft.DropshadowBlur = 4;
        row.Children.Add(soft);

        // Hard offset: bigger offset, no blur, semi-transparent black. Skia exposes only
        // per-channel ints on the SkiaShapeRuntime dropshadow surface (no DropshadowColor
        // composite), so set the channels individually here.
        CircleRuntime hard = new();
        hard.Radius = 28;
        hard.FillColor = SKColors.Goldenrod;
        hard.IsFilled = true;
        hard.HasDropshadow = true;
        hard.DropshadowRed = 0; hard.DropshadowGreen = 0; hard.DropshadowBlue = 0; hard.DropshadowAlpha = 160;
        hard.DropshadowOffsetX = 16;
        hard.DropshadowOffsetY = 16;
        hard.DropshadowBlur = 0;
        row.Children.Add(hard);

        // Colored shadow: magenta cast, real offset so the cast is visible against the blue
        // background (offset = 0 would tuck the entire shadow under the opaque disk and leave
        // only a thin halo, which on a blue page reads as nothing).
        CircleRuntime colored = new();
        colored.Radius = 28;
        colored.FillColor = SKColors.Goldenrod;
        colored.IsFilled = true;
        colored.HasDropshadow = true;
        colored.DropshadowRed = 220; colored.DropshadowGreen = 40; colored.DropshadowBlue = 160; colored.DropshadowAlpha = 220;
        colored.DropshadowOffsetX = 16;
        colored.DropshadowOffsetY = 16;
        colored.DropshadowBlur = 6;
        row.Children.Add(colored);

        // Issue #2851 visual acceptance: same soft-shadow config as the second cell, but with
        // the body's alpha cut to 80. SkiaGum has always faded the shadow with the body (this
        // is the canonical behavior); the cell exists here so the gallery matches the
        // post-#2851 MonoGameGumShapes side cell-for-cell.
        CircleRuntime fadedBody = new();
        fadedBody.Radius = 28;
        fadedBody.FillColor = new SKColor(218, 165, 32, 80);
        fadedBody.IsFilled = true;
        fadedBody.HasDropshadow = true;
        fadedBody.DropshadowOffsetX = 14;
        fadedBody.DropshadowOffsetY = 14;
        fadedBody.DropshadowBlur = 4;
        row.Children.Add(fadedBody);

        return row;
    }

    // Issue #2796 mirror of the MG dashed-stroke row. Skia inherits StrokeDashLength /
    // StrokeGapLength from SkiaShapeRuntime, so no per-runtime plumbing was added on this
    // side — the dashing flows through the single contained renderable.
    static ContainerRuntime BuildDashedStrokeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Baseline: solid stroke (dash=0).
        CircleRuntime solid = new();
        solid.Radius = 28;
        solid.StrokeColor = SKColors.White;
        solid.StrokeWidth = 2;
        row.Children.Add(solid);

        // Short 6/4 dash.
        CircleRuntime short64 = new();
        short64.Radius = 28;
        short64.StrokeColor = SKColors.White;
        short64.StrokeWidth = 2;
        short64.StrokeDashLength = 6;
        short64.StrokeGapLength = 4;
        row.Children.Add(short64);

        // Tight 2/2 dotted. AA stays ON for visual parity with the MG side (which uses the
        // runtime's AA-bloom compensation, #2790, to keep dashes crisp without disabling AA).
        // Skia's 1 px stroke with AA on already reads as ~1 px, so no compensation is needed
        // here — just don't override IsAntialiased and the dashes stay smooth.
        CircleRuntime dotted = new();
        dotted.Radius = 28;
        dotted.StrokeColor = SKColors.White;
        dotted.StrokeWidth = 1;
        dotted.StrokeDashLength = 2;
        dotted.StrokeGapLength = 2;
        row.Children.Add(dotted);

        // Long-dash motif: 12/6 with a thicker stroke.
        CircleRuntime longDash = new();
        longDash.Radius = 28;
        longDash.StrokeColor = SKColors.LightGreen;
        longDash.StrokeWidth = 3;
        longDash.StrokeDashLength = 12;
        longDash.StrokeGapLength = 6;
        row.Children.Add(longDash);

        return row;
    }

    static ContainerRuntime BuildHorizontalRow()
    {
        ContainerRuntime row = new();
        row.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 16;
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        return row;
    }

    // Rotation row — black→white horizontal gradient on circles, rotated in 60° steps
    // (0/60/120/180). Plain circles are rotation-symmetric, so the gradient is what makes
    // the rotation visible. Endpoints are 0→20 px (less than the 56 px diameter) so the
    // transition is concentrated in a narrow band — the resulting hard light/dark edge
    // makes the rotation angle obvious. Cells use a fixed-size frame because Rotation
    // pushes content outside the natural bounding box, which breaks the
    // RelativeToChildren row sizing. Mirrors the same row on the MG and raylib sides.
    // Two rows: see the MG CirclesScreen counterpart for the #2956 rationale.
    static ContainerRuntime BuildRotationRow(bool filled)
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float rotation in new[] { 0f, 60f, 120f, 180f })
        {
            row.Children.Add(BuildRotatedGradientCircleCell(rotation, filled));
        }
        return row;
    }

    static RectangleRuntime BuildRotatedGradientCircleCell(float rotation, bool filled)
    {
        RectangleRuntime frame = new();
        frame.Width = 70;
        frame.Height = 70;
        frame.FillColor = new SKColor(60, 60, 80);
        frame.IsFilled = true;

        CircleRuntime circle = new();
        circle.Radius = 28;
        circle.XOrigin = HorizontalAlignment.Center;
        circle.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        circle.YOrigin = VerticalAlignment.Center;
        circle.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        // The gradient start stop is the active body color: FillColor when filled,
        // StrokeColor when only stroked. Set the appropriate slot to Black so the
        // gradient starts dark in both variants.
        if (filled)
        {
            circle.FillColor = SKColors.Black;
            circle.IsFilled = true;
        }
        else
        {
            circle.IsFilled = false;
            circle.StrokeColor = SKColors.Black;
        }
        circle.UseGradient = true;
        circle.GradientType = GradientType.Linear;
        circle.Color2 = SKColors.White;
        circle.GradientX1 = 0; circle.GradientY1 = 0;
        circle.GradientX2 = 20; circle.GradientY2 = 0;
        circle.Rotation = rotation;
        frame.Children.Add(circle);
        return frame;
    }

    static RectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        // RectangleRuntime is used as a visible frame so the alignment is obvious. Children
        // are positioned relative to it via YOrigin + PixelsFromSmall/Middle/Large.
        RectangleRuntime frame = new();
        // Narrowed from 220 to 128 to keep the left column from forcing the page wider than
        // the right column needs (the long section labels in the right column would otherwise
        // get clipped or push the overall layout). 128 still gives 3 cells * 60 px clearance
        // for the three alignment circles plus row spacing.
        frame.Width = 128;
        frame.Height = 100;
        frame.FillColor = new SKColor(50, 50, 70);
        frame.IsFilled = true;

        CircleRuntime circle = new();
        circle.Radius = 22;
        circle.FillColor = SKColors.Orange;
        circle.IsFilled = true;
        circle.XOrigin = HorizontalAlignment.Center;
        circle.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        circle.YOrigin = alignment;
        circle.YUnits = alignment switch
        {
            VerticalAlignment.Top => Gum.Converters.GeneralUnitType.PixelsFromSmall,
            VerticalAlignment.Center => Gum.Converters.GeneralUnitType.PixelsFromMiddle,
            VerticalAlignment.Bottom => Gum.Converters.GeneralUnitType.PixelsFromLarge,
            _ => Gum.Converters.GeneralUnitType.PixelsFromMiddle,
        };
        frame.Children.Add(circle);
        return frame;
    }
}
