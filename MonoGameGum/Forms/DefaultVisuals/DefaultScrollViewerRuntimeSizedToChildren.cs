using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if FRB
namespace MonoGameGum.Forms.DefaultVisuals;
#else
namespace Gum.Forms.DefaultVisuals;
#endif
[System.Obsolete("Legacy V1 default visual. Use the V3 visuals via DefaultVisualsVersion.V3/.Newest; the V1 default visuals are slated for removal in a future release.")]
internal class DefaultScrollViewerRuntimeSizedToChildren : DefaultScrollViewerRuntime
{
    public DefaultScrollViewerRuntimeSizedToChildren(bool fullInstantiation = true, bool tryCreateFormsObject = true) : 
        base(fullInstantiation, tryCreateFormsObject)
    {
        this.HasEvents = true;

        base.MakeSizedToChildren();
    }
}
