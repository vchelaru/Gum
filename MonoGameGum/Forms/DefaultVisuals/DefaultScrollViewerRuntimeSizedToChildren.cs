using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.DefaultVisuals;
internal class DefaultScrollViewerRuntimeSizedToChildren : DefaultScrollViewerRuntime
{
    public DefaultScrollViewerRuntimeSizedToChildren(bool fullInstantiation = true, bool tryCreateFormsObject = true) : 
        base(fullInstantiation, tryCreateFormsObject)
    {
        this.HasEvents = true;

        base.MakeSizedToChildren();
    }
}
