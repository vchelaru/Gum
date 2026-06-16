using BaseScrollBarVisual = Gum.Forms.DefaultVisuals.V3.ScrollBarVisual;
#if RAYLIB
using Raylib_cs;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Editor;

public class ScrollBarVisual : BaseScrollBarVisual
{
    public ScrollBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        this.TrackBackgroundColor = new Color(10, 10, 10);
        this.ThumbInstance.BackgroundColor = new Color(60, 60, 60);
    }
}
