using BaseTextBoxVisual = Gum.Forms.DefaultVisuals.V3.TextBoxVisual;

namespace Gum.Themes.DarkPro;

/// <summary>
/// Dark Pro styled TextBox visual. Decoration logic lives in
/// <see cref="DarkProTextInputDecoration"/> and is shared with
/// <see cref="PasswordBoxVisual"/>.
/// </summary>
public class TextBoxVisual : BaseTextBoxVisual
{
    private readonly DarkProTextInputDecoration _decoration;

    public TextBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        _decoration = new DarkProTextInputDecoration(this);
    }
}
