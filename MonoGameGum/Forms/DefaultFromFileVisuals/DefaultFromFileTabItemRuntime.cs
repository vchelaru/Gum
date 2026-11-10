using Gum.Wireframe;
using Gum.Forms.Controls;

namespace Gum.Forms.DefaultFromFileVisuals;
public class DefaultFromFileTabItemRuntime : InteractiveGue
{
    public DefaultFromFileTabItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base()
    {
    }
    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        if (FormsControl == null)
        {
            FormsControlAsObject = new TabItem(this);
            BehaviorFormsPropertyApplier.ApplyToCreatedControl(this);
        }
    }
    public TabItem? FormsControl => FormsControlAsObject as TabItem;
}
