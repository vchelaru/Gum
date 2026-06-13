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
internal class DefaultScrollViewerRuntimeSizedToChildren : DefaultScrollViewerRuntime
{
    public DefaultScrollViewerRuntimeSizedToChildren(bool fullInstantiation = true, bool tryCreateFormsObject = true) : 
        base(fullInstantiation, tryCreateFormsObject)
    {
        this.HasEvents = true;

        base.MakeSizedToChildren();
    }
}
