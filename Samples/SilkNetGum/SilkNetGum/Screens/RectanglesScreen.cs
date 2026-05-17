using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SilkNetGum.Screens;

// Skia mirror of MonoGameGumShapesGallery/Screens/RectanglesScreen.cs (issue #2814).
// Same approach as CirclesScreen.
internal class RectanglesScreen : GraphicalUiElement
{
    public RectanglesScreen() : base(new InvisibleRenderable())
    {
        ContainerRuntime root = new();
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        root.StackSpacing = 14;
        root.X = 10;
        root.Y = 10;
        this.Children.Add(root);

        root.Children.Add(BuildSection("Sizes", BuildSizesRow()));
        root.Children.Add(BuildSection("Modes: FillColor, StrokeColor, Fill+Stroke", BuildModeRow()));
        root.Children.Add(BuildSection("CornerRadius (0, 6, 16, 28)", BuildRoundedRow()));
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

    static ContainerRuntime BuildSizesRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float width in new[] { 40f, 60f, 90f, 130f })
        {
            RectangleRuntime rect = new();
            rect.Width = width;
            rect.Height = 40;
            rect.FillColor = new SKColor(80, 80, 120);
            rect.StrokeColor = SKColors.White;
            rect.StrokeWidth = 2;
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
        row.Children.Add(filled);

        RectangleRuntime stroked = new();
        stroked.Width = 80; stroked.Height = 50;
        stroked.StrokeColor = SKColors.Cyan;
        stroked.StrokeWidth = 3;
        row.Children.Add(stroked);

        RectangleRuntime both = new();
        both.Width = 80; both.Height = 50;
        both.FillColor = SKColors.Gold;
        both.StrokeColor = SKColors.Magenta;
        both.StrokeWidth = 3;
        row.Children.Add(both);

        return row;
    }

    static ContainerRuntime BuildRoundedRow()
    {
        ContainerRuntime row = BuildHorizontalRow();
        foreach (float radius in new[] { 0f, 6f, 16f, 28f })
        {
            RoundedRectangleRuntime rect = new();
            rect.Width = 90;
            rect.Height = 60;
            rect.CornerRadius = radius;
            rect.FillColor = new SKColor(80, 80, 120);
            rect.StrokeColor = SKColors.White;
            rect.StrokeWidth = 2;
            row.Children.Add(rect);
        }
        return row;
    }
}
