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

// Raylib mirror of SilkNetGum/Screens/RectanglesScreen.cs (issue #2757). Structurally aligned
// with the Skia/MG gallery so visual regressions in one backend are easy to spot against the
// other — same section grouping, same parameter sweeps where features overlap.
//
// What's intentionally NOT mirrored on this pass: CornerRadius and per-corner radii sections.
// Raylib has no rounded-rectangle renderable yet; the corresponding RoundedRectangleRuntime
// only exists for Skia/Apos. Antialiasing toggle is also omitted — raylib relies on
// framebuffer MSAA (set in Program.Main via SetConfigFlags(Msaa4xHint)) and has no per-shape
// AA equivalent.
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

        left.Children.Add(BuildSection("Sizes (40, 60, 90, 130 wide) — default outline", BuildSizesRow()));
        left.Children.Add(BuildSection("Alpha on StrokeColor (255, 192, 128, 64)", BuildAlphaRow()));
        left.Children.Add(BuildSection("Modes: FillColor, StrokeColor, Fill+Stroke, default", BuildModeRow()));
        left.Children.Add(BuildSection("StrokeWidth (1, 2, 4, 8 px) — thick stroke via DrawRectangleLinesEx (#2757)", BuildStrokeWidthRow()));
        left.Children.Add(BuildSection("Alignment inside a 128x100 frame (Top / Center / Bottom)", BuildAlignmentRow()));
        left.Children.Add(BuildSection("CornerRadius (0, 6, 16, 28 px) — DrawRectangleRounded (#2757)", BuildCornerRadiusRow()));

        right.Children.Add(BuildSection("Gradients (linear H / V / diagonal / radial) — rlgl mesh (#2757)", BuildGradientRow()));
        right.Children.Add(BuildSection("FillColor + StrokeColor on same instance — both layers render (#2790 parity)", BuildBothColorsRow()));
        right.Children.Add(BuildSection("Inscribed in 64x64 frame — stroke stays inside the gray rectangle (#2790 visual contract)", BuildInscribedRow()));
        right.Children.Add(BuildSection("Dashed strokes (solid / 6/4 / 2/2 dotted / long-dash) — perimeter walk (#2757)", BuildDashedStrokeRow()));
        right.Children.Add(BuildSection("Dropshadow (off / soft / hard offset / colored) — concentric-rect blur approximation (#2757)", BuildDropshadowRow()));
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
        row.Children.Add(filled);

        RectangleRuntime stroked = new();
        stroked.Width = 80; stroked.Height = 50;
        stroked.StrokeColor = new Color(0, 255, 255, 255);
        stroked.StrokeWidth = 2;
        row.Children.Add(stroked);

        RectangleRuntime both = new();
        both.Width = 80; both.Height = 50;
        both.FillColor = new Color(40, 40, 80, 255);
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

    static ColoredRectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        ColoredRectangleRuntime frame = new();
        frame.Width = 128;
        frame.Height = 100;
        frame.Color = new Color(50, 50, 70, 255);

        RectangleRuntime rect = new();
        rect.Width = 50;
        rect.Height = 30;
        rect.FillColor = new Color(255, 165, 0, 255);
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
        linearH.UseGradient = true;
        linearH.GradientType = GradientType.Linear;
        linearH.Color1 = Color.White;
        linearH.Color2 = new Color(70, 130, 180, 255);
        linearH.GradientX1 = 0; linearH.GradientY1 = 0;
        linearH.GradientX2 = 70; linearH.GradientY2 = 0;
        row.Children.Add(linearH);

        // Linear vertical: gold → crimson, top to bottom.
        RectangleRuntime linearV = new();
        linearV.Width = 70; linearV.Height = 50;
        linearV.FillColor = Color.White;
        linearV.UseGradient = true;
        linearV.GradientType = GradientType.Linear;
        linearV.Color1 = new Color(255, 215, 0, 255);
        linearV.Color2 = new Color(220, 20, 60, 255);
        linearV.GradientX1 = 0; linearV.GradientY1 = 0;
        linearV.GradientX2 = 0; linearV.GradientY2 = 50;
        row.Children.Add(linearV);

        // Linear diagonal: cyan → magenta, top-left to bottom-right.
        RectangleRuntime linearD = new();
        linearD.Width = 70; linearD.Height = 50;
        linearD.FillColor = Color.White;
        linearD.UseGradient = true;
        linearD.GradientType = GradientType.Linear;
        linearD.Color1 = new Color(0, 255, 255, 255);
        linearD.Color2 = new Color(255, 0, 255, 255);
        linearD.GradientX1 = 0; linearD.GradientY1 = 0;
        linearD.GradientX2 = 70; linearD.GradientY2 = 50;
        row.Children.Add(linearD);

        // Radial centered: white → dark green from (35, 25) with inner=0, outer=35.
        RectangleRuntime radial = new();
        radial.Width = 70; radial.Height = 50;
        radial.FillColor = Color.White;
        radial.UseGradient = true;
        radial.GradientType = GradientType.Radial;
        radial.Color1 = Color.White;
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
        strokeLast.StrokeColor = new Color(0, 255, 255, 255);
        strokeLast.StrokeWidth = 4;
        row.Children.Add(strokeLast);

        RectangleRuntime fillLast = new();
        fillLast.Width = 80; fillLast.Height = 50;
        fillLast.StrokeColor = new Color(255, 0, 255, 255);
        fillLast.StrokeWidth = 4;
        fillLast.FillColor = new Color(255, 215, 0, 255);
        row.Children.Add(fillLast);

        return row;
    }

    // DrawRectangleLinesEx centers stroke on the rectangle edge, so a thick stroke spreads
    // half-inside / half-outside the nominal bounds. The inscribed test pins that the stroke
    // stays within the parent gray frame even at 12 px — same visual contract Skia uses.
    // (Note: raylib's center-on-edge behavior differs from Skia's inset-only behavior; visual
    // parity here is "stays inside the frame within reason", not pixel-exact alignment.)
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

        RectangleRuntime rect = new();
        rect.Width = 64;
        rect.Height = 64;
        rect.FillColor = new Color(46, 139, 87, 255);
        rect.StrokeColor = new Color(255, 255, 0, 255);
        rect.StrokeWidth = strokeWidth;
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
        row.Children.Add(baseline);

        RectangleRuntime soft = new();
        soft.Width = 60; soft.Height = 50;
        soft.FillColor = goldenrod;
        soft.HasDropshadow = true;
        soft.DropshadowOffsetX = 4;
        soft.DropshadowOffsetY = 4;
        soft.DropshadowBlurX = 4;
        soft.DropshadowBlurY = 4;
        row.Children.Add(soft);

        RectangleRuntime hard = new();
        hard.Width = 60; hard.Height = 50;
        hard.FillColor = goldenrod;
        hard.HasDropshadow = true;
        hard.DropshadowRed = 0; hard.DropshadowGreen = 0; hard.DropshadowBlue = 0; hard.DropshadowAlpha = 160;
        hard.DropshadowOffsetX = 6;
        hard.DropshadowOffsetY = 6;
        hard.DropshadowBlurX = 0;
        hard.DropshadowBlurY = 0;
        row.Children.Add(hard);

        RectangleRuntime colored = new();
        colored.Width = 60; colored.Height = 50;
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
