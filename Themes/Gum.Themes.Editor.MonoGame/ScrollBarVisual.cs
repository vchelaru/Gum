using BaseScrollBarVisual = Gum.Forms.DefaultVisuals.V3.ScrollBarVisual;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Editor;

public class ScrollBarVisual : BaseScrollBarVisual
{
    public ScrollBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        this.TrackBackgroundColor = EditorStyling.ActiveStyle.Colors.RecessedBackground;
        this.ThumbInstance.BackgroundColor = EditorStyling.ActiveStyle.Colors.Primary;
    }
}
