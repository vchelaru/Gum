using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;

namespace Examples.Shapes;

// Raylib mirror of SilkNetGum/Screens/CirclesScreen.cs (issue #2757). Structurally aligned
// with the Skia/MG gallery so visual regressions in one backend are easy to spot against the
// other — same section grouping, same parameter sweeps where features overlap.
//
// What's intentionally NOT mirrored on this pass: linear gradients, dashed strokes, per-shape
// antialiasing toggle, and dropshadow — each requires renderer work that's tracked as a
// #2757 follow-up. The runtime accepts the property values (round-trips) where wired, but the
// raylib LineCircle's Render() doesn't honor them yet, so showing those sections would just
// render the same as their non-decorated baselines and be misleading.
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

        left.Children.Add(BuildSection("Sizes (radius 16, 24, 32, 48) — default outline", BuildSizesRow()));
        left.Children.Add(BuildSection("Alpha on StrokeColor (255, 192, 128, 64)", BuildAlphaRow()));
        left.Children.Add(BuildSection("Modes: FillColor, StrokeColor, default", BuildModeRow()));
        left.Children.Add(BuildSection("StrokeWidth (1, 2, 4, 8 px) — thick strokes via DrawRing (#2757)", BuildStrokeWidthRow()));

        right.Children.Add(BuildSection("Alignment inside a 128x100 frame (Top / Center / Bottom)", BuildAlignmentRow()));
        right.Children.Add(BuildSection("Gradients (linear H / V / diagonal / radial) — rlgl triangle fan (#2757)", BuildGradientRow()));
        right.Children.Add(BuildSection("FillColor + StrokeColor on same instance — both layers render (#2790 parity)", BuildBothColorsRow()));
        right.Children.Add(BuildSection("Inscribed in 64x64 frame — stroke stays inside the gray rectangle (#2790 visual contract)", BuildInscribedRow()));
        right.Children.Add(BuildSection("Dashed strokes (solid / 6/4 / 2/2 dotted / long-dash) — DrawRing arc loop (#2757)", BuildDashedStrokeRow()));
        right.Children.Add(BuildSection("Dropshadow (off / soft / hard offset / colored) — concentric-ring blur approximation (#2757)", BuildDropshadowRow()));
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

    static ColoredRectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        ColoredRectangleRuntime frame = new();
        frame.Width = 128;
        frame.Height = 100;
        frame.Color = new Color(50, 50, 70, 255);

        CircleRuntime circle = new();
        circle.Radius = 22;
        circle.FillColor = new Color(255, 165, 0, 255);
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
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color1 = Color.White;
        linearH.Color2 = new Color(70, 130, 180, 255);
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 56; linearH.GradientY2 = 0;
        row.Children.Add(linearH);

        // Linear vertical: gold → crimson, top to bottom.
        CircleRuntime linearV = new();
        linearV.Radius = 28;
        linearV.FillColor = Color.White;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color1 = new Color(255, 215, 0, 255);
        linearV.Color2 = new Color(220, 20, 60, 255);
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 56;
        row.Children.Add(linearV);

        // Linear diagonal: cyan → magenta, top-left to bottom-right.
        CircleRuntime linearD = new();
        linearD.Radius = 28;
        linearD.FillColor = Color.White;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color1 = new Color(0, 255, 255, 255);
        linearD.Color2 = new Color(255, 0, 255, 255);
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 56; linearD.GradientY2 = 56;
        row.Children.Add(linearD);

        // Radial centered: white → dark green from (28,28) with inner=0, outer=28.
        CircleRuntime radial = new();
        radial.Radius = 28;
        radial.FillColor = Color.White;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
        radial.Color1 = Color.White;
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
        strokeLast.StrokeColor = new Color(0, 255, 255, 255);
        strokeLast.StrokeWidth = 4;
        row.Children.Add(strokeLast);

        CircleRuntime fillLast = new();
        fillLast.Radius = 28;
        fillLast.StrokeColor = new Color(255, 0, 255, 255);
        fillLast.StrokeWidth = 4;
        fillLast.FillColor = new Color(255, 215, 0, 255);
        row.Children.Add(fillLast);

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

    static ColoredRectangleRuntime BuildInscribedCell(float strokeWidth)
    {
        ColoredRectangleRuntime frame = new();
        frame.Width = 64;
        frame.Height = 64;
        frame.Color = new Color(60, 60, 80, 255);

        CircleRuntime circle = new();
        circle.Radius = 32;
        circle.FillColor = new Color(46, 139, 87, 255);
        circle.StrokeColor = new Color(255, 255, 0, 255);
        circle.StrokeWidth = strokeWidth;
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
        row.Children.Add(baseline);

        // Soft shadow: noticeable offset, generous blur, default opaque black.
        CircleRuntime soft = new();
        soft.Radius = 28;
        soft.FillColor = goldenrod;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 4;
        soft.DropshadowOffsetY = 4;
        soft.DropshadowBlurX = 4;
        soft.DropshadowBlurY = 4;
        row.Children.Add(soft);

        // Hard offset: bigger offset, no blur, semi-transparent black. Per-channel setters
        // mirror SilkNet's example.
        CircleRuntime hard = new();
        hard.Radius = 28;
        hard.FillColor = goldenrod;
        hard.HasDropshadow = true;
        hard.DropshadowRed = 0; hard.DropshadowGreen = 0; hard.DropshadowBlue = 0; hard.DropshadowAlpha = 160;
        hard.DropshadowOffsetX = 6;
        hard.DropshadowOffsetY = 6;
        hard.DropshadowBlurX = 0;
        hard.DropshadowBlurY = 0;
        row.Children.Add(hard);

        // Colored shadow: magenta cast with real offset so the cast is visible against the
        // blue background.
        CircleRuntime colored = new();
        colored.Radius = 28;
        colored.FillColor = goldenrod;
        colored.HasDropshadow = true;
        colored.DropshadowRed = 220; colored.DropshadowGreen = 40; colored.DropshadowBlue = 160; colored.DropshadowAlpha = 220;
        colored.DropshadowOffsetX = 6;
        colored.DropshadowOffsetY = 6;
        colored.DropshadowBlurX = 6;
        colored.DropshadowBlurY = 6;
        row.Children.Add(colored);

        return row;
    }
}
