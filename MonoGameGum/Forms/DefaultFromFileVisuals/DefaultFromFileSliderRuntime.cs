using Gum.Wireframe;
using MonoGameGum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Forms.DefaultFromFileVisuals;

public class DefaultFromFileSliderRuntime : InteractiveGue
{
    public DefaultFromFileSliderRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(){ }

    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        if (FormsControl == null)
        {
            FormsControlAsObject = new Slider(this);
        }
    }

    public Slider FormsControl => FormsControlAsObject as Slider;
}
