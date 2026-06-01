using BaseTextBoxVisual = Gum.Forms.DefaultVisuals.V3.TextBoxVisual;

namespace Gum.Themes.Template.Variants;

/// <summary>
/// "Rich" variant of the Template TextBox. Identical to the flat
/// <see cref="Gum.Themes.Template.TextBoxVisual"/> except the decoration comes from
/// the Variants <see cref="TemplateTextInputDecoration"/> (rounder corners + soft
/// focus-ring glow). Shared with <see cref="PasswordBoxVisual"/>.
/// <para>
/// Part of the opt-in Variants gallery - NOT registered by default. See
/// <see cref="TemplateTheme.RegisterVisuals"/>.
/// </para>
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
