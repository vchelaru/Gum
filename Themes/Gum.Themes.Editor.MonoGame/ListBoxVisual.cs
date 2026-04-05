using BaseListBoxVisual = Gum.Forms.DefaultVisuals.V3.ListBoxVisual;
using MonoGameGum.GueDeriving;

namespace Gum.Themes.Editor;

public class ListBoxVisual : BaseListBoxVisual
{
    public ListBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        this.BackgroundColor = new Microsoft.Xna.Framework.Color(27, 27, 27);

        var rectangle = new RectangleRuntime();
        Background.Children.Add(rectangle);
        rectangle.Color = new Microsoft.Xna.Framework.Color(60, 60, 60);
        rectangle.Dock(Gum.Wireframe.Dock.Fill);
    }
}
