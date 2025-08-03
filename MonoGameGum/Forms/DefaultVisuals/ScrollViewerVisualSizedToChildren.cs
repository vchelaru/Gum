using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Forms.DefaultVisuals;
internal class ScrollViewerVisualSizedToChildren : ScrollViewerVisual
{
    public ScrollViewerVisualSizedToChildren(bool fullInstantiation = true, bool tryCreateFormsObject = true) : 
        base(fullInstantiation, tryCreateFormsObject)
    {
        base.MakeSizedToChildren();
    }
}
