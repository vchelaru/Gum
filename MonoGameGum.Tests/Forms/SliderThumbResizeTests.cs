using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.Forms.DefaultVisuals;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Probe coverage for issue #2781 — the issue notes Slider is structurally similar to ScrollBar
/// and may also fail to recompute on Track resize. In practice Slider stores thumb X in
/// <c>Percentage</c> units and its <c>UpdateThumbPositionAccordingToValue</c> is pure Value/Min/Max
/// math (it never reads Track size), so Slider should already survive Track resize naturally.
/// These tests pin that behavior down so a future refactor that switches Slider to absolute
/// pixel math without re-running on resize gets caught.
/// </summary>
public class SliderThumbResizeTests : BaseTestClass
{
    private static GraphicalUiElement GetThumbVisual(Slider slider) =>
        (GraphicalUiElement)slider.Visual.GetGraphicalUiElementByName("ThumbInstance")!;

    // Default thumb has Center XOrigin so AbsoluteLeft is offset by half its width.
    // Compare centers, not left edges.
    private static float CenterX(GraphicalUiElement element) =>
        element.AbsoluteLeft + element.GetAbsoluteWidth() * 0.5f;

    private static float CenterX(InteractiveGue element) =>
        element.AbsoluteLeft + element.GetAbsoluteWidth() * 0.5f;

    [Fact]
    public void ThumbCenter_ShouldFollow_WhenSliderVisualResizes()
    {
        // At Value=midpoint the thumb center should equal the Track center regardless of width.
        DefaultSliderRuntime visual = new DefaultSliderRuntime();
        visual.Width = 128;
        visual.Height = 24;
        Slider slider = visual.FormsControl;
        slider.Minimum = 0;
        slider.Maximum = 100;
        slider.Value = 50;
        visual.UpdateLayout();

        CenterX(GetThumbVisual(slider)).ShouldBe(CenterX(slider.Track!), 0.5f,
            "Sanity: at Value=50 the thumb center should equal the Track center.");

        visual.Width = 256;
        visual.UpdateLayout();

        CenterX(GetThumbVisual(slider)).ShouldBe(CenterX(slider.Track!), 0.5f,
            "After Slider resize, thumb center must still equal the (now wider) Track center.");
    }

    [Fact]
    public void ThumbCenter_ShouldFollow_WhenTrackResizesDirectly()
    {
        // Direct Track resize — the case ScrollBar misses pre-fix. Slider uses Percentage X for
        // the thumb so this works without any explicit Track.SizeChanged subscription.
        DefaultSliderRuntime visual = new DefaultSliderRuntime();
        visual.Width = 128;
        visual.Height = 24;
        Slider slider = visual.FormsControl;
        slider.Minimum = 0;
        slider.Maximum = 100;
        slider.Value = 50;
        visual.UpdateLayout();

        slider.Track!.WidthUnits = DimensionUnitType.Absolute;
        slider.Track.Width = 200;
        visual.UpdateLayout();

        CenterX(GetThumbVisual(slider)).ShouldBe(CenterX(slider.Track), 0.5f,
            "After direct Track resize, thumb center must still equal the Track center.");
    }
}
