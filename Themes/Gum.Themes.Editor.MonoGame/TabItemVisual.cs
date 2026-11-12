using BaseTabItemVisual = Gum.Forms.DefaultVisuals.V3.TabItemVisual;

namespace Gum.Themes.Editor;

/// <summary>
/// Editor styled TabItem visual: the selected tab uses the Editor selection color with light text. The
/// stock V3 selected tab uses the theme's Primary with dark text, which reads as a flat gray here.
/// </summary>
public class TabItemVisual : BaseTabItemVisual
{
    public TabItemVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        // The base state callbacks read these colors each time a state is applied.
        SelectedBackgroundColor = EditorStyling.ActiveStyle.Colors.Selection;
        SelectedForegroundColor = EditorStyling.ActiveStyle.Colors.TextPrimary;
        ForegroundColor = EditorStyling.ActiveStyle.Colors.TextPrimary;
    }
}
