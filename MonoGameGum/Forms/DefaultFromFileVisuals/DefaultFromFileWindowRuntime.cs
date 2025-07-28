using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Forms.DefaultFromFileVisuals;
public class DefaultFromFileWindowRuntime : InteractiveGue
{
    public DefaultFromFileWindowRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base()
    { }

    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        if (FormsControl == null)
        {
            FormsControlAsObject = new Window(this);
        }
    }

    public Window FormsControl => (Window)FormsControlAsObject;
}
