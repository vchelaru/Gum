using Gum.Forms.Controls;

namespace Gum.Themes.Neon;

/// <summary>
/// The slider and scroll bar thumb. It is a copy of this theme's <see cref="ButtonVisual"/>, so it
/// looks the same until it is restyled; restyle it here without touching buttons.
/// </summary>
public class ThumbVisual : ButtonVisual
{
    public ThumbVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject: false)
    {
        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new Thumb(this);
        }
    }
}