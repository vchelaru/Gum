using BasePasswordBoxVisual = Gum.Forms.DefaultVisuals.V3.PasswordBoxVisual;

namespace Gum.Themes.Neon;

/// <summary>
/// Neon-styled PasswordBox visual. Visually identical to <see cref="TextBoxVisual"/>;
/// masking is a Forms-layer concern. Decoration shared via
/// <see cref="NeonTextInputDecoration"/>.
/// </summary>
public class PasswordBoxVisual : BasePasswordBoxVisual
{
    private readonly NeonTextInputDecoration _decoration;

    public PasswordBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        _decoration = new NeonTextInputDecoration(this);
    }
}
