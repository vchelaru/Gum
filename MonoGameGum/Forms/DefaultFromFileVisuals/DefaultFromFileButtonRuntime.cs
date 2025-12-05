using Gum.Wireframe;
using Gum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Forms.DefaultFromFileVisuals;

public class DefaultFromFileButtonRuntime : InteractiveGue
{
    public DefaultFromFileButtonRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : 
        base() { }

    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        if (FormsControl == null)
        {
            FormsControlAsObject = new Button(this);
        }
    }

    public Button FormsControl => (Button)FormsControlAsObject;
}
