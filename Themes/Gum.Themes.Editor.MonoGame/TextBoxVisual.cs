using BaseTextBoxVisual = Gum.Forms.DefaultVisuals.V3.TextBoxVisual;

namespace Gum.Themes.Editor;

public class TextBoxVisual : BaseTextBoxVisual
{
    public TextBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        EditorTextInputDecoration.Apply(this);
    }
}
