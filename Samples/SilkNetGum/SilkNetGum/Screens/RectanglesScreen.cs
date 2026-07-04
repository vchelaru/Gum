using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SilkNetGum.Screens;

// Skia mirror of MonoGameGumInCode/Screens/RectanglesScreen.cs (issue #2814). The two
// files should stay in lock-step structurally — same sections, same parameter sweeps — so
// visual regressions in one backend are easy to spot against the other.
//
// What forces the two files apart:
//   - Color type. XNA Microsoft.Xna.Framework.Color becomes SKColor; named colors come from
//     SkiaSharp.SKColors instead of Color.X.
//   - CornerRadius row uses RectangleRuntime with CornerRadius on both sides as of #2771
//     (RoundedRectangleRuntime is now [Obsolete] pointing at the same replacement). The Skia
//     RectangleRuntime contains a RoundedRectangle renderable internally (#2818) so radii
//     render correctly without the Apos shapes package.
//
// Sections the MG side can't currently mirror (gated by missing API on MG RectangleRuntime
// rather than a design difference): Gradients, Antialiasing, Dropshadow, DashedStrokes. The
// MG header for those sections will land alongside follow-up plumbing.
internal class RectanglesScreen : FrameworkElement
{
    public RectanglesScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        // Two-column root so the screen grows wide rather than tall as rows accumulate. No
        // ScrollViewer in SkiaGum yet, so this is the cheapest layout that works on both
        // backends (mirrored in MonoGameGumInCode/Screens/RectanglesScreen).
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

        left.Children.Add(BuildSection("Sizes (40, 60, 90, 130 wide) — default outline", BuildSizesRow()));
        left.Children.Add(BuildSection("Alpha on StrokeColor (255, 192, 128, 64)", BuildAlphaRow()));
        left.Children.Add(BuildSection("Modes: FillColor, StrokeColor, Fill+Stroke, default", BuildModeRow()));
        left.Children.Add(BuildSection("StrokeWidth (1, 2, 4, 8 px)", BuildStrokeWidthRow()));
        left.Children.Add(BuildSection("Alignment inside a 128x100 frame (Top / Center / Bottom)", BuildAlignmentRow()));
        left.Children.Add(BuildSection("CornerRadius (0, 6, 16, 28) — RectangleRuntime + CornerRadius (#2814, #2771)", BuildCornerRadiusRow()));
        left.Children.Add(BuildSection("Per-corner radii on RectangleRuntime (TL=20, TR=2, BR=20, BL=2 — #2818)", BuildPerCornerRow()));
        left.Children.Add(BuildSection("Gradients (linear / radial / diagonal / centered)", BuildGradientRow()));

        right.Children.Add(BuildSection("Antialiasing (default ON, then OFF) — 1 px stroke makes the bloom obvious (#2798)", BuildAntialiasingRow()));
        right.Children.Add(BuildSection("Dropshadow (off / soft / hard offset / colored) — Skia draws the shadow on the single contained renderable (#2797)", BuildDropshadowRow()));
        right.Children.Add(BuildSection("Dashed strokes (solid / 6/4 / 2/2 dotted / long-dash) — Skia routes through SkiaShapeRuntime.StrokeDashLength (#2796)", BuildDashedStrokeRow()));
        right.Children.Add(BuildSection("FillColor + StrokeColor on the same instance — both layers render simultaneously (#2814)", BuildBothColorsRow()));
        right.Children.Add(BuildSection("Inscribed in a 64x64 frame — stroke must stay inside the gray rectangle's bounds at every StrokeWidth (#2814 visual contract)", BuildInscribedRow()));
        right.Children.Add(BuildSection("Rotation (filled)", BuildRotationRow(filled: true)));
        right.Children.Add(BuildSection("Rotation (outline)", BuildRotationRow(filled: false)));
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
        foreach (float width in new[] { 40f, 60f, 90f, 130f })
        {
            RectangleRuntime rect = new();
            rect.Width = width;
            rect.Height = 40;
            rect.StrokeColor = SKColors.White;
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
            rect.StrokeColor = new SKColor(255, 255, 255, alpha);
            row.Children.Add(rect);
        }
        return row;
    }

    static ContainerRuntime BuildModeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime filled = new();
        filled.Width = 80; filled.Height = 50;
        filled.FillColor = SKColors.Crimson;
        filled.IsFilled = true;
        row.Children.Add(filled);

        RectangleRuntime stroked = new();
        stroked.Width = 80; stroked.Height = 50;
        stroked.StrokeColor = SKColors.Cyan;
        stroked.StrokeWidth = 2;
        row.Children.Add(stroked);

        RectangleRuntime both = new();
        both.Width = 80; both.Height = 50;
        both.FillColor = new SKColor(40, 40, 80);
        both.IsFilled = true;
        both.StrokeColor = SKColors.Yellow;
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
            rect.StrokeColor = SKColors.LightGreen;
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

    // CornerRadius row exercises RectangleRuntime with CornerRadius (#2771 migration target;
    // previously RoundedRectangleRuntime). Same fill+stroke composition, just with rounded
    // corners — RectangleRuntime on Skia contains a RoundedRectangle internally (#2818) so
    // radii reach the renderer the same way.
    static ContainerRuntime BuildCornerRadiusRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float cornerRadius in new[] { 0f, 6f, 16f, 28f })
        {
            RectangleRuntime rect = new();
            rect.Width = 80;
            rect.Height = 60;
            rect.FillColor = new SKColor(40, 40, 80);
            rect.IsFilled = true;
            rect.StrokeColor = SKColors.Orange;
            rect.StrokeWidth = 2;
            rect.CornerRadius = cornerRadius;
            row.Children.Add(rect);
        }
        return row;
    }

    // Issue #2818 — RectangleRuntime now exposes per-corner radii on Skia (the contained type
    // was swapped from LineRectangle to RoundedRectangle to make this possible). Pushed to
    // both fill and stroke slots in PreRender so the outline matches the fill.
    static ContainerRuntime BuildPerCornerRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        RectangleRuntime rect = new();
        rect.Width = 120; rect.Height = 70;
        rect.FillColor = new SKColor(40, 40, 80);
        rect.IsFilled = true;
        rect.StrokeColor = SKColors.Orange;
        rect.StrokeWidth = 2;
        rect.CustomRadiusTopLeft = 20;
        rect.CustomRadiusTopRight = 2;
        rect.CustomRadiusBottomRight = 20;
        rect.CustomRadiusBottomLeft = 2;
        row.Children.Add(rect);
        return row;
    }

    static ContainerRuntime BuildGradientRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Linear horizontal: white → blue
        RectangleRuntime linearH = new();
        linearH.Width = 70; linearH.Height = 50;
        linearH.FillColor = SKColors.White; // gradient start stop is the fill color
        linearH.IsFilled = true;
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color2 = SKColors.SteelBlue;
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 70; linearH.GradientY2 = 0;
        row.Children.Add(linearH);

        // Linear vertical: gold → crimson
        RectangleRuntime linearV = new();
        linearV.Width = 70; linearV.Height = 50;
        linearV.FillColor = SKColors.Gold;
        linearV.IsFilled = true;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color2 = SKColors.Crimson;
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 50;
        row.Children.Add(linearV);

        // Linear diagonal: cyan → magenta
        RectangleRuntime linearD = new();
        linearD.Width = 70; linearD.Height = 50;
        linearD.FillColor = SKColors.Cyan;
        linearD.IsFilled = true;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color2 = SKColors.Magenta;
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 70; linearD.GradientY2 = 50;
        row.Children.Add(linearD);

        // Radial centered: white → dark green
        RectangleRuntime radial = new();
        radial.Width = 70; radial.Height = 50;
        radial.FillColor = SKColors.White;
        radial.IsFilled = true;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
        radial.Color2 = SKColors.DarkGreen;
        radial.GradientX1 = 35; radial.GradientY1 = 25;
        radial.GradientInnerRadius = 0;
        radial.GradientOuterRadius = 35;
        row.Children.Add(radial);

        return row;
    }

    // Visual acceptance for #2814. Two-slot composition means setting both FillColor and
    // StrokeColor lights up both layers simultaneously — order-independent. Each cell here
    // should render a filled card with a contrasting frame around it.
    static ContainerRuntime BuildBothColorsRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime strokeLast = new();
        strokeLast.Width = 80; strokeLast.Height = 50;
        strokeLast.FillColor = SKColors.Crimson;
        strokeLast.IsFilled = true;
        strokeLast.StrokeColor = SKColors.Cyan;
        strokeLast.StrokeWidth = 4;
        row.Children.Add(strokeLast);

        RectangleRuntime fillLast = new();
        fillLast.Width = 80; fillLast.Height = 50;
        fillLast.StrokeColor = SKColors.Magenta;
        fillLast.StrokeWidth = 4;
        fillLast.FillColor = SKColors.Gold;
        fillLast.IsFilled = true;
        row.Children.Add(fillLast);

        return row;
    }

    // Visual contract for #2814: the stroke slot mirrors the runtime's Width/Height (pushed
    // each frame in SkiaShapeRuntime.PreRender) and RenderableShapeBase.IsOffsetAppliedForStroke
    // insets the rendered frame by half the stroke width. Cells get progressively thicker
    // strokes (1, 4, 8, 12) — every frame must stay inside the gray rectangle. If a stroke
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

    // Rotation row — black→white horizontal gradient on rectangles, rotated in 60° steps
    // (0/60/120/180). Gradient endpoints are 0→20 px (less than the 70 px width) so the
    // transition is concentrated in a narrow band; the resulting hard light/dark edge
    // makes the rotation angle obvious. Cells wrap each rotated rectangle in a fixed-size
    // frame because Rotation pushes content outside the natural bounding box, which
    // breaks RelativeToChildren row sizing. 100x100 frame is sized to contain a 70x50
    // rectangle at any rotation. Mirrors the same row on the MG and raylib sides.
    // Two rows: see the MG CirclesScreen counterpart for the #2956 rationale.
    static ContainerRuntime BuildRotationRow(bool filled)
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float rotation in new[] { 0f, 60f, 120f, 180f })
        {
            row.Children.Add(BuildRotatedGradientRectCell(rotation, filled));
        }
        return row;
    }

    static RectangleRuntime BuildRotatedGradientRectCell(float rotation, bool filled)
    {
        RectangleRuntime frame = new();
        frame.Width = 100;
        frame.Height = 100;
        frame.FillColor = new SKColor(60, 60, 80);
        frame.IsFilled = true;

        RectangleRuntime rect = new();
        rect.Width = 70;
        rect.Height = 50;
        rect.XOrigin = HorizontalAlignment.Center;
        rect.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        rect.YOrigin = VerticalAlignment.Center;
        rect.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        // The gradient start stop is the active body color: FillColor when filled,
        // StrokeColor when only stroked. Set the appropriate slot to Black so the
        // gradient starts dark in both variants.
        if (filled)
        {
            rect.FillColor = SKColors.Black;
            rect.IsFilled = true;
        }
        else
        {
            rect.IsFilled = false;
            rect.StrokeColor = SKColors.Black;
        }
        rect.UseGradient = true;
        rect.GradientType = GradientType.Linear;
        rect.Color2 = SKColors.White;
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
        frame.FillColor = new SKColor(60, 60, 80);
        frame.IsFilled = true;

        RectangleRuntime rect = new();
        rect.Width = 64;
        rect.Height = 64;
        rect.FillColor = SKColors.SeaGreen;
        rect.IsFilled = true;
        rect.StrokeColor = SKColors.Yellow;
        rect.StrokeWidth = strokeWidth;
        rect.StrokeWidthUnits = DimensionUnitType.Absolute;
        frame.Children.Add(rect);
        return frame;
    }

    // Issue #2798 visual acceptance: two pairs (filled card + 1 px outline frame), once with
    // IsAntialiased = true (the default — soft edges) and once false (crisp pixels). On Skia
    // this flips SKPaint.IsAntialias on the contained renderable.
    static ContainerRuntime BuildAntialiasingRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        foreach (bool aa in new[] { true, false })
        {
            RectangleRuntime filled = new();
            filled.Width = 60; filled.Height = 50;
            filled.FillColor = SKColors.Goldenrod;
            filled.IsFilled = true;
            filled.IsAntialiased = aa;
            row.Children.Add(filled);

            RectangleRuntime frame = new();
            frame.Width = 60; frame.Height = 50;
            frame.StrokeColor = SKColors.White;
            frame.StrokeWidth = 1;
            frame.IsAntialiased = aa;
            row.Children.Add(frame);
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
        RectangleRuntime baseline = new();
        baseline.Width = 60; baseline.Height = 50;
        baseline.FillColor = SKColors.Goldenrod;
        baseline.IsFilled = true;
        row.Children.Add(baseline);

        // Soft shadow: noticeable offset, generous blur, default opaque black.
        RectangleRuntime soft = new();
        soft.Width = 60; soft.Height = 50;
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
        RectangleRuntime hard = new();
        hard.Width = 60; hard.Height = 50;
        hard.FillColor = SKColors.Goldenrod;
        hard.IsFilled = true;
        hard.HasDropshadow = true;
        hard.DropshadowRed = 0; hard.DropshadowGreen = 0; hard.DropshadowBlue = 0; hard.DropshadowAlpha = 160;
        hard.DropshadowOffsetX = 16;
        hard.DropshadowOffsetY = 16;
        hard.DropshadowBlur = 0;
        row.Children.Add(hard);

        // Colored shadow: magenta cast, real offset so the cast is visible against the blue
        // background (offset = 0 would tuck the entire shadow under the opaque card and leave
        // only a thin halo, which on a blue page reads as nothing).
        RectangleRuntime colored = new();
        colored.Width = 60; colored.Height = 50;
        colored.FillColor = SKColors.Goldenrod;
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
        RectangleRuntime solid = new();
        solid.Width = 60; solid.Height = 50;
        solid.StrokeColor = SKColors.White;
        solid.StrokeWidth = 2;
        row.Children.Add(solid);

        // Short 6/4 dash.
        RectangleRuntime short64 = new();
        short64.Width = 60; short64.Height = 50;
        short64.StrokeColor = SKColors.White;
        short64.StrokeWidth = 2;
        short64.StrokeDashLength = 6;
        short64.StrokeGapLength = 4;
        row.Children.Add(short64);

        // Tight 2/2 dotted. AA stays ON for visual parity with the MG side (which uses the
        // runtime's AA-bloom compensation, #2790, to keep dashes crisp without disabling AA).
        // Skia's 1 px stroke with AA on already reads as ~1 px, so no compensation is needed
        // here — just don't override IsAntialiased and the dashes stay smooth.
        RectangleRuntime dotted = new();
        dotted.Width = 60; dotted.Height = 50;
        dotted.StrokeColor = SKColors.White;
        dotted.StrokeWidth = 1;
        dotted.StrokeDashLength = 2;
        dotted.StrokeGapLength = 2;
        row.Children.Add(dotted);

        // Long-dash motif: 12/6 with a thicker stroke.
        RectangleRuntime longDash = new();
        longDash.Width = 60; longDash.Height = 50;
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

    static RectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        // RectangleRuntime is used as a visible frame so the alignment is obvious. Children
        // are positioned relative to it via YOrigin + PixelsFromSmall/Middle/Large.
        RectangleRuntime frame = new();
        frame.Width = 128;
        frame.Height = 100;
        frame.FillColor = new SKColor(50, 50, 70);
        frame.IsFilled = true;

        RectangleRuntime rect = new();
        rect.Width = 50;
        rect.Height = 30;
        rect.FillColor = SKColors.Orange;
        rect.IsFilled = true;
        rect.XOrigin = HorizontalAlignment.Center;
        rect.XUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        rect.YOrigin = alignment;
        rect.YUnits = alignment switch
        {
            VerticalAlignment.Top => Gum.Converters.GeneralUnitType.PixelsFromSmall,
            VerticalAlignment.Center => Gum.Converters.GeneralUnitType.PixelsFromMiddle,
            VerticalAlignment.Bottom => Gum.Converters.GeneralUnitType.PixelsFromLarge,
            _ => Gum.Converters.GeneralUnitType.PixelsFromMiddle,
        };
        frame.Children.Add(rect);
        return frame;
    }
}
