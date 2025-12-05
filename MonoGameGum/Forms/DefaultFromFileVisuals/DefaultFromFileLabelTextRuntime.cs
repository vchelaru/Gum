using Gum.Wireframe;
using Gum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if MONOGAME || FNA || KNI
using MonoGameGum.GueDeriving;
#elif RAYLIB
using Gum.GueDeriving;
#endif

namespace Gum.Forms.DefaultFromFileVisuals;
public class DefaultFromFileLabelTextRuntime : TextRuntime
{
    public DefaultFromFileLabelTextRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base()
    { }

    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        if (FormsControl == null)
        {
            FormsControlAsObject = new Label(this);
        }
    }

    public Label FormsControl => FormsControlAsObject as Label;
}
