using Gum.Wireframe;
using Gum.Forms.Controls;

namespace Gum.Forms.DefaultFromFileVisuals;
public class DefaultFromFileTabPageRuntime : InteractiveGue
{
    public DefaultFromFileTabPageRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base()
    {
    }
    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        if (FormsControl == null)
        {
            FormsControlAsObject = new TabPage(this);
            BehaviorFormsPropertyApplier.ApplyToCreatedControl(this);
        }
    }
    public TabPage? FormsControl => FormsControlAsObject as TabPage;
}
