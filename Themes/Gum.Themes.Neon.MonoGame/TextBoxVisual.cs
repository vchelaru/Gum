using BaseTextBoxVisual = Gum.Forms.DefaultVisuals.V3.TextBoxVisual;

namespace Gum.Themes.Neon;

/// <summary>
/// Neon-styled TextBox visual. Decoration logic lives in
/// <see cref="NeonTextInputDecoration"/> and is shared with
/// <see cref="PasswordBoxVisual"/>.
/// </summary>
public class TextBoxVisual : BaseTextBoxVisual
{
    private readonly NeonTextInputDecoration _decoration;

    public TextBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        _decoration = new NeonTextInputDecoration(this);
    }
}
