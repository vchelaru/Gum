using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseScrollViewerVisual = Gum.Forms.DefaultVisuals.V3.ScrollViewerVisual;
#if RAYLIB
using Raylib_cs;
#elif SKIA
using Color = SkiaSharp.SKColor;
#else
using Microsoft.Xna.Framework;
#endif

namespace Gum.Themes.Editor;

public class ScrollViewerVisual : BaseScrollViewerVisual
{
    public ScrollViewerVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        this.BackgroundColor = EditorStyling.ActiveStyle.Colors.PanelBackground;
    }
}
