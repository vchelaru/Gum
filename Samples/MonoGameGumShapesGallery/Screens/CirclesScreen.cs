using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace MonoGameGumShapesGallery.Screens;

// CircleRuntime survey on the shapes-package side (issue #2761 / PR #2767). This sample
// references MonoGameGumShapes, so RenderableRegistry resolves ICircleRenderable to the
// Apos.Shapes Circle — every CircleRuntime here renders through Apos with full fill,
// stroke, and stroke-width support. The exact same code in MonoGameGumInCode (no shapes
// package) degrades to outline-only via DefaultCircleRenderable; compare both to see the
// construct-time-binding model in action.
//
// Deliberately uses CircleRuntime directly. ColoredCircleRuntime is on the obsolete path
// (separate follow-up to this issue) and will eventually become an [Obsolete] shim.
//
// Layout convention: every container that sets WidthUnits / HeightUnits to
// RelativeToChildren also sets Width / Height = 0. RelativeToChildren means the final
// size is children-extent + the explicit Width/Height; a non-zero value adds extra
// padding the layout almost never wants.
internal class CirclesScreen : FrameworkElement
{
    public CirclesScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        root.StackSpacing = 14;
        root.X = 10;
        root.Y = 10;
        AddChild(root);

        root.AddChild(BuildSection("Sizes (radius 16, 24, 32, 48) — default outline", BuildSizesRow()));
        root.AddChild(BuildSection("Alpha on StrokeColor (255, 192, 128, 64)", BuildAlphaRow()));
        root.AddChild(BuildSection("Modes: FillColor, StrokeColor, default", BuildModeRow()));
        root.AddChild(BuildSection("StrokeWidth (1, 2, 4, 8 px)", BuildStrokeWidthRow()));
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
        foreach (float radius in new[] { 16f, 24f, 32f, 48f })
        {
            CircleRuntime circle = new();
            circle.Radius = radius;
            circle.StrokeColor = Color.White;
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
        filled.Radius = 28;
        filled.FillColor = Color.Crimson;
        row.AddChild(filled);

        CircleRuntime stroked = new();
        stroked.Radius = 28;
        stroked.StrokeColor = Color.Cyan;
        stroked.StrokeWidth = 3;
        row.AddChild(stroked);

        CircleRuntime defaultCircle = new();
        defaultCircle.Radius = 28;
        row.AddChild(defaultCircle);

        return row;
    }

    static ContainerRuntime BuildStrokeWidthRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            CircleRuntime circle = new();
            circle.Radius = 24;
            circle.StrokeColor = Color.LightGreen;
            circle.StrokeWidth = strokeWidth;
            row.AddChild(circle);
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
        // ColoredRectangle is used as a visible frame so the alignment is obvious. Children
        // are positioned relative to it via YOrigin + PixelsFromSmall/Middle/Large.
        ColoredRectangleRuntime frame = new();
        frame.Width = 220;
        frame.Height = 100;
        frame.Color = new Color(50, 50, 70);

        CircleRuntime circle = new();
        circle.Radius = 22;
        circle.FillColor = Color.Orange;
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
