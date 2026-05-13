using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace MonoGameGumInCode.Screens;
internal class InvisibleScreen : FrameworkElement
{
    public InvisibleScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        // Mutates global canvas size; left in place to preserve the original
        // CreateInvisibleLayout behavior. Switching screens does not reset this.
        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;

        GraphicalUiElement parentContainer = new(new InvisibleRenderable(), null!)
        {
            X = 5,
            Y = 5,
            Width = 40,
            Height = 0,

            HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren,
            ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack,
            WrapsChildren = true
        };

        for (int i = 0; i < 10; i++)
        {
            GraphicalUiElement buttonWrapper = new(new InvisibleRenderable(), null!)
            {
                WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute,
                Width = 20,
                XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Left,
                XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall,
                X = 0,

                HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute,
                Height = 1
            };

            parentContainer.Children.Add(buttonWrapper);
        }

        this.AddChild(parentContainer);
        parentContainer.UpdateLayout();
    }
}
