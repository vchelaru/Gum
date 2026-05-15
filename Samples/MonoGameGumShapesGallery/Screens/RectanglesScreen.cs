using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace MonoGameGumShapesGallery.Screens;

// RectangleRuntime survey on the shapes-package side (issue #2768). MonoGameGumShapes
// overrides both core rectangle slots with Apos RoundedRectangle — IsFilled=true for
// fill, IsFilled=false for stroke — so CornerRadius renders, strokes are anti-aliased,
// and the same runtime draws fill + stroke simultaneously (the design's headline use
// case). Mirrors CirclesScreen exactly except for the extra CornerRadius row that's
// only visible when MonoGameGumShapes is loaded. Compare against the no-package version
// in MonoGameGumInCode to see corners go from flat to round.
//
// Layout convention: every container that sets WidthUnits / HeightUnits to
// RelativeToChildren also sets Width / Height = 0.
internal class RectanglesScreen : FrameworkElement
{
    public RectanglesScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        root.StackSpacing = 14;
        root.X = 10;
        root.Y = 10;
        AddChild(root);

        root.AddChild(BuildSection("Sizes (40, 60, 90, 130 wide)", BuildSizesRow()));
        root.AddChild(BuildSection("Alpha on FillColor (255, 192, 128, 64)", BuildAlphaRow()));
        root.AddChild(BuildSection("Modes: FillColor, StrokeColor, Fill+Stroke, default", BuildModeRow()));
        root.AddChild(BuildSection("StrokeWidth (1, 2, 4, 8 px on a filled card)", BuildStrokeWidthRow()));
        root.AddChild(BuildSection("CornerRadius (0, 6, 16, 28 — visibly rounded on Apos)", BuildCornerRadiusRow()));
        root.AddChild(BuildSection("Alignment inside a 220x100 frame (Top / Center / Bottom)", BuildAlignmentRow()));
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
        section.AddChild(header);
        section.AddChild(body);
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
            rect.FillColor = new Color(80, 80, 120);
            rect.StrokeColor = Color.White;
            rect.StrokeWidth = 2;
            rect.CornerRadius = 6;
            row.AddChild(rect);
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
            rect.FillColor = new Color((byte)255, (byte)255, (byte)255, alpha);
            rect.CornerRadius = 6;
            row.AddChild(rect);
        }
        return row;
    }

    static ContainerRuntime BuildModeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime filled = new();
        filled.Width = 80; filled.Height = 50;
        filled.FillColor = Color.Crimson;
        filled.CornerRadius = 8;
        row.AddChild(filled);

        RectangleRuntime stroked = new();
        stroked.Width = 80; stroked.Height = 50;
        stroked.StrokeColor = Color.Cyan;
        stroked.StrokeWidth = 2;
        stroked.CornerRadius = 8;
        row.AddChild(stroked);

        RectangleRuntime both = new();
        both.Width = 80; both.Height = 50;
        both.FillColor = new Color(40, 40, 80);
        both.StrokeColor = Color.Yellow;
        both.StrokeWidth = 2;
        both.CornerRadius = 8;
        row.AddChild(both);

        RectangleRuntime defaultRect = new();
        defaultRect.Width = 80; defaultRect.Height = 50;
        row.AddChild(defaultRect);

        return row;
    }

    static ContainerRuntime BuildStrokeWidthRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            RectangleRuntime rect = new();
            rect.Width = 70; rect.Height = 50;
            rect.FillColor = new Color(30, 30, 50);
            rect.StrokeColor = Color.LightGreen;
            rect.StrokeWidth = strokeWidth;
            rect.CornerRadius = 6;
            row.AddChild(rect);
        }
        return row;
    }

    static ContainerRuntime BuildCornerRadiusRow()
    {
        // Showcase the visual payoff of installing MonoGameGumShapes — only the Apos
        // backend honors CornerRadius. The no-package version of this screen omits this
        // row entirely.
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float cornerRadius in new[] { 0f, 6f, 16f, 28f })
        {
            RectangleRuntime rect = new();
            rect.Width = 80; rect.Height = 60;
            rect.FillColor = new Color(40, 40, 80);
            rect.StrokeColor = Color.Orange;
            rect.StrokeWidth = 2;
            rect.CornerRadius = cornerRadius;
            row.AddChild(rect);
        }
        return row;
    }

    static ContainerRuntime BuildAlignmentRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (VerticalAlignment alignment in new[] { VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom })
        {
            row.AddChild(BuildAlignmentCell(alignment));
        }
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

    static ColoredRectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        // ColoredRectangle frame (hard-cornered on purpose so the inner Apos rectangle's
        // rounded corners are visually distinct from the frame's). The inner RectangleRuntime
        // is positioned relative to it via YOrigin + PixelsFromSmall/Middle/Large.
        ColoredRectangleRuntime frame = new();
        frame.Width = 220;
        frame.Height = 100;
        frame.Color = new Color(50, 50, 70);

        RectangleRuntime rect = new();
        rect.Width = 60;
        rect.Height = 30;
        rect.FillColor = Color.Orange;
        rect.CornerRadius = 6;
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
