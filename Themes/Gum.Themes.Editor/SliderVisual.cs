using BaseSliderVisual = Gum.Forms.DefaultVisuals.V3.SliderVisual;

namespace Gum.Themes.Editor;

public class SliderVisual : BaseSliderVisual
{
    public SliderVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true) :
        base(fullInstantiation, tryCreateFormsObject)
    {
        float sliderButtonWidth = 12f;

        TrackInstance.Width = -sliderButtonWidth;
        ThumbInstance.Width = sliderButtonWidth;
        FocusedIndicator.Width = -sliderButtonWidth;
    }
}
