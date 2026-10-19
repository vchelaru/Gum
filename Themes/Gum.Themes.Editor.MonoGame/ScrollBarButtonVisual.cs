using Gum.Forms.Controls;

namespace Gum.Themes.Editor;

/// <summary>
/// The scroll bar's up and down arrow button. It is a copy of this theme's <see cref="ButtonVisual"/>,
/// so it looks the same until it is restyled; restyle it here without touching other buttons.
/// </summary>
public class ScrollBarButtonVisual : ButtonVisual
{
    public ScrollBarButtonVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject: false)
    {
        if (tryCreateFormsObject)
        {
            FormsControlAsObject = new ScrollBarButton(this);
        }
    }
}