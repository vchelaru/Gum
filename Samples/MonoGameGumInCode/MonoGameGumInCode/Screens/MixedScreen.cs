using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;

namespace MonoGameGumInCode.Screens;
internal class MixedScreen : FrameworkElement
{
    public MixedScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        var container = new ContainerRuntime();
        container.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        container.Width = 0;
        container.Height = 0;
        container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        this.AddChild(container);

        AddText(container, "This is a colored rectangle:");

        var coloredRectangleInstance = new ColoredRectangleRuntime();
        coloredRectangleInstance.X = 10;
        coloredRectangleInstance.Y = 10;
        coloredRectangleInstance.Width = 120;
        coloredRectangleInstance.Height = 24;
        coloredRectangleInstance.Color = Color.White;
        container.Children.Add(coloredRectangleInstance);

        AddText(container, "This is a (line) rectangle:");

        var lineRectangle = new RectangleRuntime();
        lineRectangle.X = 10;
        lineRectangle.Y = 10;
        lineRectangle.Width = 120;
        lineRectangle.Height = 24;
        lineRectangle.LineWidth = 5;
        lineRectangle.Color = Color.Purple;
        container.Children.Add(lineRectangle);

        AddText(container, "This is a sprite:");

        var sprite = new SpriteRuntime();
        sprite.X = 10;
        sprite.Y = 10;
        sprite.SourceFileName = "BearTexture.png";
        container.Children.Add(sprite);

        AddText(container, "This is a NineSlice:");

        var nineSlice = new NineSliceRuntime();
        nineSlice.X = 10;
        nineSlice.Y = 10;
        nineSlice.SourceFileName = "Frame.png";
        nineSlice.Width = 256;
        nineSlice.Height = 48;
        container.Children.Add(nineSlice);

        var customText = new TextRuntime();
        customText.Width = 300;
        customText.UseCustomFont = true;
        customText.CustomFontFile = "WhitePeaberryOutline/WhitePeaberryOutline.fnt";
        customText.Text = "Hello, I am using a custom font.\nPretty cool huh?";
        container.Children.Add(customText);

        var layoutCountBefore = GraphicalUiElement.UpdateLayoutCallCount;

        AddText(container, "Stacking/wrapping container:");

        var stackingContainer = new ContainerRuntime();

        GraphicalUiElement.IsAllLayoutSuspended = true;
        stackingContainer.Width = 200;
        stackingContainer.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        stackingContainer.StackSpacing = 3;
        stackingContainer.WrapsChildren = true;
        stackingContainer.Y = 10;
        stackingContainer.Height = 10;
        stackingContainer.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        for (int i = 0; i < 70; i++)
        {
            var rectangle = new ColoredRectangleRuntime();
            stackingContainer.Children.Add(rectangle);
            rectangle.Width = 7;
            rectangle.Height = 7;
        }

        container.Children.Add(stackingContainer);
        GraphicalUiElement.IsAllLayoutSuspended = false;
        container.UpdateLayout();
        var layoutCountAfter = GraphicalUiElement.UpdateLayoutCallCount;
        System.Diagnostics.Debug.WriteLine($"Number of layout calls: {layoutCountAfter - layoutCountBefore}");

        AddText(container, "This is a polygon:");
        var polygon = new PolygonRuntime();
        polygon.Name = "PolygonRuntime";
        polygon.X = 10;
        polygon.Y = 10;
        polygon.Color = Color.Red;

        // width/heights are used for layout
        polygon.IsDotted = true;
        polygon.SetPoints(new System.Numerics.Vector2[]
        {
            new System.Numerics.Vector2(30, 0),
            new System.Numerics.Vector2(0, 30),
            new System.Numerics.Vector2(30, 30),
            new System.Numerics.Vector2(60, 0),
            new System.Numerics.Vector2(30, 0),
        });
        polygon.Width = 30;
        polygon.Height = 8;
        container.Children.Add(polygon);
    }

    internal static void AddText(ContainerRuntime container, string text)
    {
        var textInstance = new TextRuntime();

        // adds a gap between this text and the item above
        textInstance.Y = 8;

        textInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        textInstance.Width = 0;

        textInstance.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        // Makes it so the item below appears closer to the text:
        textInstance.Height = -8;

        textInstance.Text = text;
        container.Children.Add(textInstance);
    }
}
