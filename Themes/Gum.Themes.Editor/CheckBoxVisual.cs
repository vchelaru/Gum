using BaseCheckBoxVisual = Gum.Forms.DefaultVisuals.V3.CheckBoxVisual;
using MonoGameGum.GueDeriving;

namespace Gum.Themes.Editor;

public class CheckBoxVisual : BaseCheckBoxVisual
{
    public CheckBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        var rectangle = new RectangleRuntime();
        CheckBoxBackground.Children.Add(rectangle);
        rectangle.Dock(Gum.Wireframe.Dock.Fill);

        this.States.EnabledOn.Apply += () =>
        {
            rectangle.Visible = false;
        };

        this.States.EnabledOff.Apply += () =>
        {
            rectangle.Visible = false;
        };

        this.States.HighlightedOn.Apply += () =>
        {
            rectangle.Visible = true;
            rectangle.Color = new Microsoft.Xna.Framework.Color(150, 150, 150);
        };
        this.States.HighlightedOff.Apply += () =>
        {
            rectangle.Visible = true;
            rectangle.Color = new Microsoft.Xna.Framework.Color(150, 150, 150);
        };

        this.States.PushedOn.Apply += () =>
        {
            rectangle.Visible = true;
            rectangle.Color = new Microsoft.Xna.Framework.Color(255, 255, 255);
        };
        this.States.PushedOff.Apply += () =>
        {
            rectangle.Visible = true;
            rectangle.Color = new Microsoft.Xna.Framework.Color(255, 255, 255);
        };

        this.States.DisabledOn.Apply += () =>
        {
            rectangle.Visible = false;
        };

        this.States.DisabledOff.Apply += () =>
        {
            rectangle.Visible = false;
        };
    }
}
