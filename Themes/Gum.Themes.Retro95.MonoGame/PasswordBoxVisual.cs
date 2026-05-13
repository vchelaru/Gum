using BasePasswordBoxVisual = Gum.Forms.DefaultVisuals.V3.PasswordBoxVisual;

namespace Gum.Themes.Retro95;

/// <summary>Retro95-styled PasswordBox visual. Visually identical to <see cref="TextBoxVisual"/>;
/// masking is a Forms-layer concern. Decoration shared via <see cref="Retro95TextInputDecoration"/>.</summary>
public class PasswordBoxVisual : BasePasswordBoxVisual
{
    private readonly Retro95TextInputDecoration _decoration;

    public PasswordBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        _decoration = new Retro95TextInputDecoration(this);
    }
}
