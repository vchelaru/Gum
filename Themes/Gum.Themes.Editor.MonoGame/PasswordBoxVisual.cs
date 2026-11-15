using BaseTextBoxVisual = Gum.Forms.DefaultVisuals.V3.PasswordBoxVisual;

namespace Gum.Themes.Editor;

public class PasswordBoxVisual : BaseTextBoxVisual
{
    public PasswordBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        EditorTextInputDecoration.Apply(this);
    }
}
