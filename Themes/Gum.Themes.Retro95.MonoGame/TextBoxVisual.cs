using BaseTextBoxVisual = Gum.Forms.DefaultVisuals.V3.TextBoxVisual;

namespace Gum.Themes.Retro95;

/// <summary>Retro95-styled TextBox visual. Decoration shared via <see cref="Retro95TextInputDecoration"/>.</summary>
public class TextBoxVisual : BaseTextBoxVisual
{
    private readonly Retro95TextInputDecoration _decoration;

    public TextBoxVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        _decoration = new Retro95TextInputDecoration(this);
    }
}
