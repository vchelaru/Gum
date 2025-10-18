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
public class DefaultFromFileStackPanelRuntime : ContainerRuntime
{
    public DefaultFromFileStackPanelRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base()
    { }

    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        if (FormsControl == null)
        {
            FormsControlAsObject = new StackPanel(this);
        }
    }
    public StackPanel FormsControl => FormsControlAsObject as StackPanel;

}
