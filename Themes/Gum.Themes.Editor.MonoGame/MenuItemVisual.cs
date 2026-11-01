using BaseMenuItemVisual = Gum.Forms.DefaultVisuals.V3.MenuItemVisual;

namespace Gum.Themes.Editor;

/// <summary>
/// The V3 MenuItem with dark text while highlighted: the Accent highlight is pale blue, and the
/// normal gray text on it is only 1.5:1 (#7366).
/// </summary>
public class MenuItemVisual : BaseMenuItemVisual
{
    public MenuItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        System.Action baseHighlighted = States.Highlighted.Apply!;
        States.Highlighted.Apply = () =>
        {
            baseHighlighted();
            TextInstance.Color = EditorStyling.ActiveStyle.Colors.TextOnAccent;
        };
    }
}
