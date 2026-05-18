using Gum.Converters;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;
using System.Numerics;

namespace Examples.Shapes;

internal class RawVisualsScreen : FrameworkElement
{
    public RawVisualsScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        var page = NewSection(ChildrenLayout.TopToBottomStack, spacing: 16);
        page.X = 16;
        page.Y = 16;
        this.AddChild(page);

        BuildSpritesRow(page);

        BuildShapesRow(page);

        BuildNineSliceRow(page);
    }

    private static ContainerRuntime NewSection(ChildrenLayout layout, int spacing)
    {
        var section = new ContainerRuntime();
        section.Width = 0;
        section.Height = 0;
        section.WidthUnits = DimensionUnitType.RelativeToChildren;
        section.HeightUnits = DimensionUnitType.RelativeToChildren;
        section.ChildrenLayout = layout;
        section.StackSpacing = spacing;
        return section;
    }

    private static void AddSectionLabel(ContainerRuntime parent, string text)
    {
        var label = new TextRuntime();
        label.Text = text;
        label.WidthUnits = DimensionUnitType.RelativeToChildren;
        label.HeightUnits = DimensionUnitType.RelativeToChildren;
        label.Width = 0;
        label.Height = 0;
        parent.AddChild(label);
    }

    private static void BuildSpritesRow(ContainerRuntime page)
    {
        var section = NewSection(ChildrenLayout.TopToBottomStack, spacing: 4);
        page.AddChild(section);

        AddSectionLabel(section, "Sprites:");

        var row = NewSection(ChildrenLayout.LeftToRightStack, spacing: 8);
        section.AddChild(row);

        var sprite = new SpriteRuntime();
        row.AddChild(sprite);
        sprite.SourceFileName = "resources\\gum-logo-normal-64.png";

        var flippedH = new SpriteRuntime();
        row.AddChild(flippedH);
        flippedH.FlipHorizontal = true;
        flippedH.SourceFileName = "resources\\gum-logo-normal-64.png";

        var flippedV = new SpriteRuntime();
        row.AddChild(flippedV);
        flippedV.FlipVertical = true;
        flippedV.SourceFileName = "resources\\gum-logo-normal-64.png";
    }

    private static void BuildShapesRow(ContainerRuntime page)
    {
        var section = NewSection(ChildrenLayout.TopToBottomStack, spacing: 4);
        page.AddChild(section);

        AddSectionLabel(section, "Shapes:");

        var row = NewSection(ChildrenLayout.LeftToRightStack, spacing: 16);
        section.AddChild(row);

        var lineRectangle = new RectangleRuntime();
        lineRectangle.Width = 80;
        lineRectangle.Height = 80;
        lineRectangle.IsDotted = true;
        lineRectangle.Color = new Color(80, 160, 200, 255);
        lineRectangle.LineWidth = 3f;
        row.AddChild(lineRectangle);

        var rotatedLineRect = new RectangleRuntime();
        rotatedLineRect.Width = 80;
        rotatedLineRect.Height = 80;
        rotatedLineRect.IsDotted = true;
        rotatedLineRect.Color = new Color(80, 160, 200, 255);
        rotatedLineRect.LineWidth = 1f;
        rotatedLineRect.Rotation = -20;
        row.AddChild(rotatedLineRect);

        var polygon = new PolygonRuntime();
        polygon.Color = new Color(80, 160, 200, 255);
        polygon.IsDotted = true;
        polygon.SetPoints(new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(100, 0),
            new Vector2(100, 100),
            new Vector2(0, 100),
            new Vector2(0, 0),
        });
        polygon.Width = 100;
        polygon.Height = 100;
        row.AddChild(polygon);

        var rotatedPolygon = new PolygonRuntime();
        rotatedPolygon.Color = new Color(80, 160, 200, 255);
        rotatedPolygon.IsDotted = true;
        rotatedPolygon.SetPoints(new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(100, 0),
            new Vector2(100, 100),
            new Vector2(0, 100),
            new Vector2(0, 0),
        });
        rotatedPolygon.Width = 100;
        rotatedPolygon.Height = 100;
        rotatedPolygon.Rotation = -20;
        row.AddChild(rotatedPolygon);

        var rectangle = new RectangleRuntime();
        rectangle.Width = 80;
        rectangle.Height = 80;
        rectangle.FillColor = new Color(80, 160, 220, 255);
        row.AddChild(rectangle);

        var circle = new CircleRuntime();
        circle.Radius = 40;
        circle.FillColor = new Color(255, 100, 50, 255);
        row.AddChild(circle);

        var bigCircle = new CircleRuntime();
        bigCircle.Radius = 60;
        bigCircle.FillColor = new Color(50, 180, 80, 255);
        row.AddChild(bigCircle);
    }

    private static void BuildNineSliceRow(ContainerRuntime page)
    {
        var section = NewSection(ChildrenLayout.TopToBottomStack, spacing: 4);
        page.AddChild(section);

        AddSectionLabel(section, "NineSlice:");

        var row = NewSection(ChildrenLayout.LeftToRightStack, spacing: 16);
        section.AddChild(row);

        var nineSlice = new NineSliceRuntime();
        nineSlice.Width = 160;
        nineSlice.Height = 80;
        nineSlice.SourceFileName = "resources\\ExampleSpriteFrame.png";
        row.AddChild(nineSlice);

        var tinted = new NineSliceRuntime();
        tinted.Width = 120;
        tinted.Height = 120;
        tinted.SourceFileName = "resources\\ExampleSpriteFrame.png";
        tinted.Color = new Color(255, 200, 100, 255);
        row.AddChild(tinted);
    }
}
