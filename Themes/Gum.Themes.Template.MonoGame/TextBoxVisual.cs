using BaseTextBoxVisual = Gum.Forms.DefaultVisuals.V3.TextBoxVisual;

namespace Gum.Themes.Template;

/// <summary>
/// Template-styled TextBox visual. Decoration logic lives in
/// <see cref="TemplateTextInputDecoration"/> and is shared with
/// <see cref="PasswordBoxVisual"/>.
/// </summary>
public class TextBoxVisual : BaseTextBoxVisual
{
    private readonly TemplateTextInputDecoration _decoration;

    public TextBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        _decoration = new TemplateTextInputDecoration(this);
    }
}
