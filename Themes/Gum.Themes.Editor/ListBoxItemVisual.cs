using Gum.Forms.Controls;
using BaseListBoxItemVisual = Gum.Forms.DefaultVisuals.V3.ListBoxItemVisual;
using MonoGameGum.GueDeriving;

namespace Gum.Themes.Editor;

public class ListBoxItemVisual : BaseListBoxItemVisual
{
    public ListBoxItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        this.HighlightedBackgroundColor = new Microsoft.Xna.Framework.Color(70, 70, 70);
        this.SelectedBackgroundColor = new Microsoft.Xna.Framework.Color(0, 92, 128);

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
        };

        this.States.Disabled.Apply += () =>
        {
            rectangle.Visible = false;
        };

        this.States.Selected.Apply += () =>
        {
            rectangle.Visible = this.FormsControl.IsHighlighted;
        };
    }
}
