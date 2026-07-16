using BaseListBoxVisual = Gum.Forms.DefaultVisuals.V3.ListBoxVisual;
using Gum.GueDeriving;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Editor;

public class ListBoxVisual : BaseListBoxVisual
{
    public ListBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        this.BackgroundColor = EditorStyling.ActiveStyle.Colors.PanelBackground;

        var rectangle = new RectangleRuntime();
        Background.Children.Add(rectangle);
        rectangle.StrokeColor = EditorStyling.ActiveStyle.Colors.Primary;
        rectangle.Dock(Gum.Wireframe.Dock.Fill);
    }
}
