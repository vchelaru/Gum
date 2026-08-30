using Gum.Wireframe;
using Gum.Forms.Controls;

namespace Gum.Forms.DefaultFromFileVisuals;
public class DefaultFromFileColorPickerRuntime : InteractiveGue
{
    public DefaultFromFileColorPickerRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base()
    {
    }
    public override void AfterFullCreation()
    {
        base.AfterFullCreation();
        if (FormsControl == null)
        {
            FormsControlAsObject = new ColorPicker(this);
        }
    }
    public ColorPicker FormsControl => FormsControlAsObject as ColorPicker;
}
