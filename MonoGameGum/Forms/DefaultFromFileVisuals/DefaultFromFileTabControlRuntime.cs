using Gum.Wireframe;
using Gum.Forms.Controls;

namespace Gum.Forms.DefaultFromFileVisuals;
public class DefaultFromFileTabControlRuntime : InteractiveGue
{
    public DefaultFromFileTabControlRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base()
    {
    }
    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        if (FormsControl == null)
        {
            FormsControlAsObject = new TabControl(this);
            BehaviorFormsPropertyApplier.ApplyToCreatedControl(this);
        }
    }
    public TabControl? FormsControl => FormsControlAsObject as TabControl;
}
