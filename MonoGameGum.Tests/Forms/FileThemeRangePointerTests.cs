using Gum.Automation;
using Gum.Forms.Controls;
using Gum.Forms.Controls.Primitives;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Pointer behavior of Slider and ScrollBar parts (thumb drag across the range, track click) built
/// from each packaged theme's files.
/// </summary>
public class FileThemeRangePointerTests : FileThemeInputTestBase
{
    private void DragThumb(RangeBase range, int fromOffset, int toX, int toY)
    {
        toX = System.Math.Clamp(toX, 1, (int)GraphicalUiElement.CanvasWidth - 1);
        toY = System.Math.Clamp(toY, 1, (int)GraphicalUiElement.CanvasHeight - 1);
        GraphicalUiElement thumb = range.Visual.GetGraphicalUiElementByName("ThumbInstance")!;
        int x = CenterX(thumb);
        int y = CenterY(thumb);
        Driver.MoveTo(x, y);
        Driver.SetButton(MouseButton.Primary, held: true);
        for (int i = 1; i <= 6; i++)
        {
            Driver.MoveTo(x + (toX - x) * i / 6, y + (toY - y) * i / 6);
        }
        Driver.SetButton(MouseButton.Primary, held: false);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void DragThumb_HorizontalSlider_ReachesMaximumAndMinimum(string theme)
    {
        Slider slider = Place<Slider>(LoadTheme(theme), "Slider");
        slider.Minimum = 0;
        slider.Maximum = 100;
        slider.Value = 50;
        Driver.Step(2);
        GraphicalUiElement track = slider.Visual.GetGraphicalUiElementByName("TrackInstance")!;

        DragThumb(slider, 0, (int)(track.AbsoluteLeft + track.AbsoluteWidth + 200), CenterY(track));
        slider.Value.ShouldBe(100, 1.0, theme + " drag far right");

        DragThumb(slider, 0, (int)(track.AbsoluteLeft - 200), CenterY(track));
        slider.Value.ShouldBe(0, 1.0, theme + " drag far left");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void DragThumb_Slider_FollowsCursorToTheMidpoint(string theme)
    {
        Slider slider = Place<Slider>(LoadTheme(theme), "Slider");
        slider.Minimum = 0;
        slider.Maximum = 100;
        slider.Value = 0;
        Driver.Step(2);
        GraphicalUiElement track = slider.Visual.GetGraphicalUiElementByName("TrackInstance")!;

        DragThumb(slider, 0, CenterX(track), CenterY(track));

        slider.Value.ShouldBe(50, 12.0, theme);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickTrack_Slider_MovesTowardTheClick(string theme)
    {
        Slider slider = Place<Slider>(LoadTheme(theme), "Slider");
        slider.Minimum = 0;
        slider.Maximum = 100;
        slider.Value = 0;
        Driver.Step(2);
        GraphicalUiElement track = slider.Visual.GetGraphicalUiElementByName("TrackInstance")!;

        Driver.Click((int)(track.AbsoluteLeft + track.AbsoluteWidth * 0.9f), CenterY(track));

        slider.Value.ShouldBeGreaterThan(0, theme + " track click did not move the value");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Disabled_Slider_IgnoresThumbDrag(string theme)
    {
        Slider slider = Place<Slider>(LoadTheme(theme), "Slider");
        slider.Value = 0;
        slider.IsEnabled = false;
        Driver.Step(2);
        GraphicalUiElement track = slider.Visual.GetGraphicalUiElementByName("TrackInstance")!;

        DragThumb(slider, 0, CenterX(track), CenterY(track));

        slider.Value.ShouldBe(0, theme);
    }

    private ScrollBar PlaceVerticalScrollBar(string theme)
    {
        ScrollBar bar = Place<ScrollBar>(LoadTheme(theme), "ScrollBar");
        bar.Orientation = Orientation.Horizontal;
        bar.Orientation = Orientation.Vertical;
        bar.Height = 200;
        bar.Minimum = 0;
        bar.Maximum = 100;
        bar.ViewportSize = 20;
        bar.LargeChange = 20;
        bar.Value = 0;
        Driver.Step(3);
        return bar;
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void DragThumb_VerticalScrollBar_ReachesMaximumAndMinimum(string theme)
    {
        if (theme == "" || theme == "Editor")
        {
            return; // Not covered: Standard and Editor scroll bars lay out differently headless (#7439).
        }
        ScrollBar bar = PlaceVerticalScrollBar(theme);
        GraphicalUiElement track = bar.Visual.GetGraphicalUiElementByName("TrackInstance")!;

        DragThumb(bar, 0, CenterX(track), (int)(track.AbsoluteTop + track.AbsoluteHeight + 300));
        bar.Value.ShouldBe(bar.Maximum, 1.0, theme + " drag to bottom");

        DragThumb(bar, 0, CenterX(track), (int)(track.AbsoluteTop - 300));
        bar.Value.ShouldBe(bar.Minimum, 1.0, theme + " drag to top");
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void ClickTrackBelowThumb_VerticalScrollBar_PagesByLargeChange(string theme)
    {
        ScrollBar bar = PlaceVerticalScrollBar(theme);
        GraphicalUiElement track = bar.Visual.GetGraphicalUiElementByName("TrackInstance")!;
        GraphicalUiElement thumb = bar.Visual.GetGraphicalUiElementByName("ThumbInstance")!;

        Driver.Click(CenterX(track), (int)(thumb.AbsoluteTop + thumb.AbsoluteHeight + 20));

        bar.Value.ShouldBeGreaterThan(0, $"{theme} page click did not scroll; hit {Describe(Driver.Cursor.VisualOver)}");
    }
}
