using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace MonoGameGumInCode.Screens;

// Visual smoke test for CircleRuntime's construct-time binding (issue #2761 / PR #2767).
// This sample project does NOT reference MonoGameGumShapes, so every circle here is
// backed by the core DefaultCircleRenderable — outline-only. FillColor and StrokeColor
// assignments are retained but render as outline. That is the documented graceful
// degradation; load MonoGameGumShapes in a real project to get visual fill.
//
// Layout convention used throughout: every container that sets WidthUnits / HeightUnits
// to RelativeToChildren also sets Width / Height = 0. RelativeToChildren means the
// container's final size is children-extent + the explicit Width/Height — so a non-zero
// value here would add that many extra pixels of padding on top of the children's
// natural size, which is almost never what you want.
internal class CirclesScreen : FrameworkElement
{
    public CirclesScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        root.StackSpacing = 12;
        root.X = 8;
        root.Y = 8;
        AddChild(root);

        root.AddChild(BuildSection("Sizes (radius 16, 24, 32, 48)", BuildSizesRow()));
        root.AddChild(BuildSection("Alpha (255, 192, 128, 64)", BuildAlphaRow()));
        root.AddChild(BuildSection("FillColor / StrokeColor / default (no shapes pkg → outline)", BuildModeRow()));
        root.AddChild(BuildSection("Alignment inside a 200x100 container (Top / Center / Bottom)", BuildAlignmentRow()));
    }

    static ContainerRuntime BuildSection(string label, GraphicalUiElement body)
    {
        ContainerRuntime section = new();
        section.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        section.StackSpacing = 4;
        section.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        section.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        section.Width = 0;
        section.Height = 0;

        TextRuntime header = new();
        header.Text = label;
        section.AddChild(header);
        section.AddChild(body);
        return section;
    }

    static ContainerRuntime BuildSizesRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float radius in new[] { 16f, 24f, 32f, 48f })
        {
            CircleRuntime circle = new();
            circle.Radius = radius;
            row.AddChild(circle);
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
            row.AddChild(circle);
        }
        return row;
    }

    static ContainerRuntime BuildModeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        CircleRuntime filled = new();
        filled.Radius = 24;
        filled.FillColor = Color.Red;
        filled.IsFilled = true;
        row.AddChild(filled);

        CircleRuntime stroked = new();
        stroked.Radius = 24;
        stroked.StrokeColor = Color.Cyan;
        row.AddChild(stroked);

        CircleRuntime defaultCircle = new();
        defaultCircle.Radius = 24;
        row.AddChild(defaultCircle);

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
        row.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        return row;
    }

    static RectangleRuntime BuildAlignmentCell(VerticalAlignment alignment)
    {
        // RectangleRuntime is used as a visible frame so the alignment is obvious. Children
        // are positioned relative to it via YOrigin + PixelsFromSmall/Middle/Large.
        RectangleRuntime frame = new();
        frame.Width = 200;
        frame.Height = 100;
        frame.FillColor = new Color(40, 40, 60);
        frame.IsFilled = true;

        CircleRuntime circle = new();
        circle.Radius = 20;
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
