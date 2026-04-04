using BaseButtonVisual = Gum.Forms.DefaultVisuals.V3.ButtonVisual;
using MonoGameGum.GueDeriving;

namespace Gum.Themes.Editor;

public class ButtonVisual : BaseButtonVisual
{
    public ButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        // remove the old focused indicator, we don't use it:
        FocusedIndicator.Parent = null;

        this.Height = 2;
        this.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.Width = 18;
        this.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        this.TextInstance.Width = 0;
        this.TextInstance.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        var rectangle = new RectangleRuntime();
        this.Children.Add(rectangle);
        rectangle.Dock(Gum.Wireframe.Dock.Fill);

        this.States.Enabled.Apply += () =>
        {
            rectangle.Visible = false;
        };
        this.States.Highlighted.Apply += () =>
        {
            rectangle.Visible = true;
            rectangle.Color = new Microsoft.Xna.Framework.Color(150, 150, 150);
        };
        this.States.Pushed.Apply += () =>
        {
            rectangle.Visible = true;

            rectangle.Color = new Microsoft.Xna.Framework.Color(255, 255, 255);
        };
        this.States.Disabled.Apply += () =>
        {
            rectangle.Visible = false;

        };
    }
}
