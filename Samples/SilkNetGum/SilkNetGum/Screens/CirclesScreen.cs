using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SilkNetGum.Screens;

// Skia mirror of MonoGameGumShapesGallery/Screens/CirclesScreen.cs (issue #2785). The two
// files should stay in lock-step structurally — same sections, same parameter sweeps — so
// visual regressions in one backend are easy to spot against the other.
//
// What forces the two files apart:
//   - Base class. Forms (FrameworkElement / Dock / AddChild) hasn't reached the Skia runtime
//     yet, so this screen derives from GraphicalUiElement and uses Children.Add directly.
//   - Color type. XNA Microsoft.Xna.Framework.Color becomes SKColor; named colors come from
//     SkiaSharp.SKColors instead of Color.X.
//   - The MonoGame CirclesScreen also uses VerticalAlignment from the FRB Forms types in its
//     alignment switch; here we use the same RenderingLibrary.Graphics.VerticalAlignment.
//
// Everything else — section layout, sweep values, property names (Radius, StrokeColor,
// FillColor, StrokeWidth, gradient props) — is identical to the MonoGame version.
internal class CirclesScreen : GraphicalUiElement
{
    public CirclesScreen() : base(new InvisibleRenderable())
    {
        // Two-column root so the screen grows wide rather than tall as rows accumulate. No
        // ScrollViewer in SkiaGum yet, so this is the cheapest layout that works on both
        // backends (mirrored in MonoGameGumShapesGallery/Screens/CirclesScreen).
        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        root.StackSpacing = 24;
        root.X = 10;
        root.Y = 10;
        this.Children.Add(root);

        ContainerRuntime left = BuildColumn();
        ContainerRuntime right = BuildColumn();
        root.Children.Add(left);
        root.Children.Add(right);

        left.Children.Add(BuildSection("Sizes (radius 16, 24, 32, 48) — default outline", BuildSizesRow()));
        left.Children.Add(BuildSection("Alpha on StrokeColor (255, 192, 128, 64)", BuildAlphaRow()));
        left.Children.Add(BuildSection("Modes: FillColor, StrokeColor, default", BuildModeRow()));
        left.Children.Add(BuildSection("StrokeWidth (1, 2, 4, 8 px)", BuildStrokeWidthRow()));
        left.Children.Add(BuildSection("Alignment inside a 128x100 frame (Top / Center / Bottom)", BuildAlignmentRow()));
        left.Children.Add(BuildSection("Gradients (linear / radial / diagonal / centered)", BuildGradientRow()));

        right.Children.Add(BuildSection("Antialiasing (default ON, then OFF) — 1 px stroke makes the bloom obvious (#2798)", BuildAntialiasingRow()));
        right.Children.Add(BuildSection("Dropshadow (off / soft / hard offset / colored) — Skia draws the shadow on the single contained renderable (#2797)", BuildDropshadowRow()));
        right.Children.Add(BuildSection("Dashed strokes (solid / 6/4 / 2/2 dotted / long-dash) — Skia routes through SkiaShapeRuntime.StrokeDashLength (#2796)", BuildDashedStrokeRow()));
        right.Children.Add(BuildSection("FillColor + StrokeColor on the same instance — both layers render simultaneously (#2790)", BuildBothColorsRow()));
        right.Children.Add(BuildSection("Inscribed in a 64x64 frame — stroke must stay inside the gray rectangle's bounds at every StrokeWidth (#2790 visual contract)", BuildInscribedRow()));
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
        linearH.FillColor = SKColors.White; // fill mode; gradient overrides solid color
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color1 = SKColors.White;
        linearH.Color2 = SKColors.SteelBlue;
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 56; linearH.GradientY2 = 0;
        row.Children.Add(linearH);

        // Linear vertical: yellow → red
        CircleRuntime linearV = new();
        linearV.Radius = 28;
        linearV.FillColor = SKColors.White;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color1 = SKColors.Gold;
        linearV.Color2 = SKColors.Crimson;
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 56;
        row.Children.Add(linearV);

        // Linear diagonal: cyan → magenta
        CircleRuntime linearD = new();
        linearD.Radius = 28;
        linearD.FillColor = SKColors.White;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color1 = SKColors.Cyan;
        linearD.Color2 = SKColors.Magenta;
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 56; linearD.GradientY2 = 56;
        row.Children.Add(linearD);

        // Radial centered: white → dark green
        CircleRuntime radial = new();
        radial.Radius = 28;
        radial.FillColor = SKColors.White;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
        radial.Color1 = SKColors.White;
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
        strokeLast.StrokeColor = SKColors.Cyan;
        strokeLast.StrokeWidth = 4;
        row.Children.Add(strokeLast);

        CircleRuntime fillLast = new();
        fillLast.Radius = 28;
        fillLast.StrokeColor = SKColors.Magenta;
        fillLast.StrokeWidth = 4;
        fillLast.FillColor = SKColors.Gold;
        row.Children.Add(fillLast);

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

    static RectangleRuntime BuildInscribedCell(float strokeWidth)
    {
        RectangleRuntime frame = new();
        frame.Width = 64;
        frame.Height = 64;
        frame.FillColor = new SKColor(60, 60, 80);

        CircleRuntime circle = new();
        circle.Radius = 32;
        circle.FillColor = SKColors.SeaGreen;
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

    // Issue #2797 visual acceptance: mirror of the MonoGameGumShapesGallery dropshadow row.
    // Same four cells — baseline, soft, hard offset, colored — so visual regressions in one
    // backend are easy to spot against the other.
    static ContainerRuntime BuildDropshadowRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        // Baseline: no shadow.
        CircleRuntime baseline = new();
        baseline.Radius = 28;
        baseline.FillColor = SKColors.Goldenrod;
        row.Children.Add(baseline);

        // Soft shadow: noticeable offset, generous blur, default opaque black.
        CircleRuntime soft = new();
        soft.Radius = 28;
        soft.FillColor = SKColors.Goldenrod;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 4;
        soft.DropshadowOffsetY = 4;
        soft.DropshadowBlurX = 4;
        soft.DropshadowBlurY = 4;
        row.Children.Add(soft);

        // Hard offset: bigger offset, no blur, semi-transparent black. Skia exposes only
        // per-channel ints on the SkiaShapeRuntime dropshadow surface (no DropshadowColor
        // composite), so set the channels individually here.
        CircleRuntime hard = new();
        hard.Radius = 28;
        hard.FillColor = SKColors.Goldenrod;
        hard.HasDropshadow = true;
        hard.DropshadowRed = 0; hard.DropshadowGreen = 0; hard.DropshadowBlue = 0; hard.DropshadowAlpha = 160;
        hard.DropshadowOffsetX = 6;
        hard.DropshadowOffsetY = 6;
        hard.DropshadowBlurX = 0;
        hard.DropshadowBlurY = 0;
        row.Children.Add(hard);

        // Colored shadow: magenta cast, real offset so the cast is visible against the blue
        // background (offset = 0 would tuck the entire shadow under the opaque disk and leave
        // only a thin halo, which on a blue page reads as nothing).
        CircleRuntime colored = new();
        colored.Radius = 28;
        colored.FillColor = SKColors.Goldenrod;
        colored.HasDropshadow = true;
        colored.DropshadowRed = 220; colored.DropshadowGreen = 40; colored.DropshadowBlue = 160; colored.DropshadowAlpha = 220;
        colored.DropshadowOffsetX = 6;
        colored.DropshadowOffsetY = 6;
        colored.DropshadowBlurX = 6;
        colored.DropshadowBlurY = 6;
        row.Children.Add(colored);

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

        CircleRuntime circle = new();
        circle.Radius = 22;
        circle.FillColor = SKColors.Orange;
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
