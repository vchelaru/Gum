using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Forms.DefaultVisuals;
[System.Obsolete("Legacy V2 default visual. Use the V3 visuals via DefaultVisualsVersion.V3/.Newest; the V2 default visuals are slated for removal in a future release.")]
internal class ScrollViewerVisualSizedToChildren : ScrollViewerVisual
{
    public ScrollViewerVisualSizedToChildren(bool fullInstantiation = true, bool tryCreateFormsObject = true) : 
        base(fullInstantiation, tryCreateFormsObject)
    {
        this.HasEvents = true;
        base.MakeSizedToChildren();
    }
}
