using BasePasswordBoxVisual = Gum.Forms.DefaultVisuals.V3.PasswordBoxVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled PasswordBox visual. Visually identical to <see cref="TextBoxVisual"/>;
/// masking is a Forms-layer concern. Decoration shared via
/// <see cref="BubblegumTextInputDecoration"/>.
/// </summary>
public class PasswordBoxVisual : BasePasswordBoxVisual
{
    private readonly BubblegumTextInputDecoration _decoration;

    public PasswordBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        _decoration = new BubblegumTextInputDecoration(this);
    }
}
