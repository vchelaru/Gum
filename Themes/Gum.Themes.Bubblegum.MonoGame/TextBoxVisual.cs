using BaseTextBoxVisual = Gum.Forms.DefaultVisuals.V3.TextBoxVisual;

namespace Gum.Themes.Bubblegum;

/// <summary>
/// Bubblegum-styled TextBox visual. Decoration logic lives in
/// <see cref="BubblegumTextInputDecoration"/> and is shared with
/// <see cref="PasswordBoxVisual"/>.
/// </summary>
public class TextBoxVisual : BaseTextBoxVisual
{
    private readonly BubblegumTextInputDecoration _decoration;

    public TextBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        _decoration = new BubblegumTextInputDecoration(this);
    }
}
