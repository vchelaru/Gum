using Gum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if RAYLIB
using Gum.GueDeriving;
#else
using MonoGameGum.GueDeriving;
#endif

namespace Gum.Forms.DefaultFromFileVisuals;
internal class DefaultFromFilePanelRuntime : ContainerRuntime
{
    public DefaultFromFilePanelRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base()
    { }

    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        if (FormsControl == null)
        {
            FormsControlAsObject = new Panel(this);
        }
    }
    public Panel FormsControl => FormsControlAsObject as Panel;

}
