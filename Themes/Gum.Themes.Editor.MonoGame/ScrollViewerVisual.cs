using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseScrollViewerVisual = Gum.Forms.DefaultVisuals.V3.ScrollViewerVisual;


namespace Gum.Themes.Editor;

public class ScrollViewerVisual : BaseScrollViewerVisual
{
    public ScrollViewerVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        this.BackgroundColor = new Microsoft.Xna.Framework.Color(27, 27, 27);
    }
}
