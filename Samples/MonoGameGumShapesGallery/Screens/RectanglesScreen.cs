using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary.Graphics;

namespace MonoGameGumShapesGallery.Screens;

// RectangleRuntime survey on the shapes-package side (issue #2768). MonoGameGumShapes
// overrides both core rectangle slots with Apos RoundedRectangle — IsFilled=true for fill,
// IsFilled=false for stroke — so CornerRadius is rendered, anti-aliased, and the same
// runtime can draw fill + stroke simultaneously (the design's headline use case). Compare
// against the no-package version in MonoGameGumInCode to see corners go from flat to round.
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

        root.AddChild(BuildSection("Modes: FillColor only, StrokeColor only, Fill+Stroke, default", BuildModeRow()));
        root.AddChild(BuildSection("StrokeWidth (1, 2, 4, 8 px on a filled card)", BuildStrokeWidthRow()));
        root.AddChild(BuildSection("CornerRadius (0, 4, 12, 24 — visibly rounded on Apos)", BuildCornerRadiusRow()));
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

    static ContainerRuntime BuildModeRow()
    {
        ContainerRuntime row = BuildHorizontalRow();

        RectangleRuntime filled = new();
        filled.Width = 90; filled.Height = 60;
        filled.FillColor = Color.Crimson;
        filled.CornerRadius = 8;
        row.AddChild(filled);

        RectangleRuntime stroked = new();
        stroked.Width = 90; stroked.Height = 60;
        stroked.StrokeColor = Color.Cyan;
        stroked.StrokeWidth = 2;
        stroked.CornerRadius = 8;
        row.AddChild(stroked);

        RectangleRuntime both = new();
        both.Width = 90; both.Height = 60;
        both.FillColor = new Color(40, 40, 80);
        both.StrokeColor = Color.Yellow;
        both.StrokeWidth = 2;
        both.CornerRadius = 8;
        row.AddChild(both);

        RectangleRuntime defaultRect = new();
        defaultRect.Width = 90; defaultRect.Height = 60;
        row.AddChild(defaultRect);

        return row;
    }

    static ContainerRuntime BuildStrokeWidthRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float strokeWidth in new[] { 1f, 2f, 4f, 8f })
        {
            RectangleRuntime rect = new();
            rect.Width = 90; rect.Height = 60;
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
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float cornerRadius in new[] { 0f, 4f, 12f, 24f })
        {
            RectangleRuntime rect = new();
            rect.Width = 90; rect.Height = 60;
            rect.FillColor = new Color(40, 40, 80);
            rect.StrokeColor = Color.Orange;
            rect.StrokeWidth = 2;
            rect.CornerRadius = cornerRadius;
            row.AddChild(rect);
        }
        return row;
    }
}
