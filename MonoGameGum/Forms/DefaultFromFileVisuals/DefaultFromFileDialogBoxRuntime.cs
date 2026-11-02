using Gum.Wireframe;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Games;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Forms.DefaultFromFileVisuals;

public class DefaultFromFileDialogBoxRuntime : InteractiveGue
{
    public DefaultFromFileDialogBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
    base() { }

    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        if (FormsControl == null)
        {
            FormsControlAsObject = new DialogBox(this);
            BehaviorFormsPropertyApplier.ApplyToCreatedControl(this);
        }
    }

    public DialogBox? FormsControl => FormsControlAsObject as DialogBox;
}
