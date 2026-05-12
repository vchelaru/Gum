using BaseTextBoxVisual = Gum.Forms.DefaultVisuals.V3.TextBoxVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade-styled TextBox visual. Decoration logic lives in
/// <see cref="ForestGladeTextInputDecoration"/> and is shared with
/// <see cref="PasswordBoxVisual"/>.
/// </summary>
public class TextBoxVisual : BaseTextBoxVisual
{
    private readonly ForestGladeTextInputDecoration _decoration;

    public TextBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        _decoration = new ForestGladeTextInputDecoration(this);
    }
}
