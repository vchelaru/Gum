using Gum.Forms.Controls;
using Gum.Wireframe;

namespace Gum.Forms.DefaultFromFileVisuals;

public class DefaultFromFileProgressBarRuntime : InteractiveGue
{
    public DefaultFromFileProgressBarRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base() { }

    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        if (FormsControl == null)
        {
            FormsControlAsObject = new ProgressBar(this);
            BehaviorFormsPropertyApplier.ApplyToCreatedControl(this);
        }
    }

    public ProgressBar? FormsControl => FormsControlAsObject as ProgressBar;
}
