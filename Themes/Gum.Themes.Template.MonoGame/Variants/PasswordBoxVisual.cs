using BasePasswordBoxVisual = Gum.Forms.DefaultVisuals.V3.PasswordBoxVisual;

namespace Gum.Themes.Template.Variants;

/// <summary>
/// "Rich" variant of the Template PasswordBox. Visually identical to the Variants
/// <see cref="TextBoxVisual"/> - masking is a Forms-layer behavior, not a visual
/// concern - so the decoration is shared via the Variants
/// <see cref="TemplateTextInputDecoration"/> (rounder corners + soft focus-ring glow).
/// <para>
/// Part of the opt-in Variants gallery - NOT registered by default. See
/// <see cref="TemplateTheme.RegisterVisuals"/>.
/// </para>
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
