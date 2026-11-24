using BaseProgressBarVisual = Gum.Forms.DefaultVisuals.V3.ProgressBarVisual;

namespace Gum.Themes.Editor;

/// <summary>
/// Editor-styled ProgressBar visual. Keeps the V3 NineSlice track, like the Editor Slider, and draws
/// the indicator in the theme's Accent: the V3 default (Primary) is a gray darker than the track.
/// </summary>
public class ProgressBarVisual : BaseProgressBarVisual
{
    public ProgressBarVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        IndicatorColor = EditorStyling.ActiveStyle.Colors.Accent;
    }
}
