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
        right.Children.Add(BuildSection("Centered radial gradient — DrawCircleGradient (#2757)", BuildRadialGradientRow()));
        right.Children.Add(BuildSection("FillColor + StrokeColor on same instance — both layers render (#2790 parity)", BuildBothColorsRow()));
        right.Children.Add(BuildSection("Inscribed in 64x64 frame — stroke stays inside the gray rectangle (#2790 visual contract)", BuildInscribedRow()));
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

    // raylib's DrawCircleGradient is centered-radial only. Offset center and inner radius are
    // both #2757 follow-ups (need a custom rlgl triangle fan). Showing four variations of the
    // same centered gradient keeps the section visually distinct from the solid-fill row.
    static ContainerRuntime BuildRadialGradientRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        (Color inner, Color outer)[] palette = new[]
        {
            (Color.White, new Color(70, 130, 180, 255)),     // steel blue
            (new Color(255, 215, 0, 255), new Color(220, 20, 60, 255)),  // gold → crimson
            (new Color(0, 255, 255, 255), new Color(255, 0, 255, 255)),  // cyan → magenta
            (Color.White, new Color(0, 100, 0, 255)),        // white → dark green
        };

        foreach ((Color inner, Color outer) in palette)
        {
            CircleRuntime circle = new();
            circle.Radius = 28;
            circle.FillColor = Color.White;
            circle.UseGradient = true;
            circle.GradientType = GradientType.Radial;
            circle.Color1 = inner;
            circle.Color2 = outer;
            row.Children.Add(circle);
        }
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
}
