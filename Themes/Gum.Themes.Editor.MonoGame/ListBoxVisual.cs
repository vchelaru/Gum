using BaseListBoxVisual = Gum.Forms.DefaultVisuals.V3.ListBoxVisual;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Editor;

public class ListBoxVisual : BaseListBoxVisual
{
    public ListBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        this.BackgroundColor = new Color(27, 27, 27);

        var rectangle = new RectangleRuntime();
        Background.Children.Add(rectangle);
        rectangle.StrokeColor = new Color(60, 60, 60);
        rectangle.Dock(Gum.Wireframe.Dock.Fill);
    }
}
