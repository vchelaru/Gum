using Gum.Wireframe;
using Gum.Forms.Controls;

namespace Gum.Forms.DefaultFromFileVisuals;
public class DefaultFromFileTooltipRuntime : InteractiveGue
{
    public DefaultFromFileTooltipRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base()
    {
    }
    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        if (FormsControl == null)
        {
            FormsControlAsObject = new Tooltip(this);
        }
    }
    public Tooltip FormsControl => FormsControlAsObject as Tooltip;
}
