using BasePasswordBoxVisual = Gum.Forms.DefaultVisuals.V3.PasswordBoxVisual;

namespace Gum.Themes.Template;

/// <summary>
/// Template-styled PasswordBox visual. Visually identical to
/// <see cref="TextBoxVisual"/> - masking is a Forms-layer behavior, not a visual
/// concern - so the decoration is shared via <see cref="TemplateTextInputDecoration"/>.
/// </summary>
public class PasswordBoxVisual : BasePasswordBoxVisual
{
    private readonly TemplateTextInputDecoration _decoration;

    public PasswordBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        _decoration = new TemplateTextInputDecoration(this);
    }
}
