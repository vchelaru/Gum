using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
