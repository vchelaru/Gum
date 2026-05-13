using BasePasswordBoxVisual = Gum.Forms.DefaultVisuals.V3.PasswordBoxVisual;

namespace Gum.Themes.ForestGlade;

/// <summary>
/// Forest Glade-styled PasswordBox visual. Visually identical to
/// <see cref="TextBoxVisual"/>; masking is a Forms-layer concern.
/// Decoration shared via <see cref="ForestGladeTextInputDecoration"/>.
/// </summary>
public class PasswordBoxVisual : BasePasswordBoxVisual
{
    private readonly ForestGladeTextInputDecoration _decoration;

    public PasswordBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        _decoration = new ForestGladeTextInputDecoration(this);
    }
}
