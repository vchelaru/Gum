using Gum.Forms.Controls;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Forms.DefaultFromFileVisuals;

public class DefaultFromFileItemsControlRuntime : InteractiveGue
{
    public DefaultFromFileItemsControlRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : 
        base()
    {

    }

    public override void AfterFullCreation()
    {
        base.AfterFullCreation();

        if(FormsControl == null)
        {
            FormsControlAsObject = new ItemsControl(this);
        }
    }

    public ItemsControl FormsControl => FormsControlAsObject as ItemsControl;

}
