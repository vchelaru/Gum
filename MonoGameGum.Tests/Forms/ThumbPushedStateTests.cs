using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A slider or scroll bar thumb keeps its Pushed visual state for as long as the primary button is
/// held after it was grabbed, even when the cursor is dragged off it. A plain Button does not.
/// </summary>
public class ThumbPushedStateTests : BaseTestClass
{
    [Fact]
    public void ThumbState_ShouldStayPushed_WhenSliderThumbDraggedOffThumb()
    {
        Slider slider = new();
        Button thumb = GetThumb(slider);
        StateRecorder recorder = new(thumb);

        PressAndDragOff(thumb);

        recorder.LastState.ShouldBe(FrameworkElement.PushedStateName);
    }

    [Fact]
    public void ThumbState_ShouldStayPushed_WhenVerticalScrollBarThumbDraggedOffThumb()
    {
        ScrollBar scrollBar = new();
        scrollBar.Visual.Width = 24;
        scrollBar.Visual.Height = 200;
        Button thumb = GetThumb(scrollBar);
        StateRecorder recorder = new(thumb);

        PressAndDragOff(thumb);

        recorder.LastState.ShouldBe(FrameworkElement.PushedStateName);
    }

    [Fact]
    public void ThumbState_ShouldStayPushed_WhenHorizontalScrollBarThumbDraggedOffThumb()
    {
        ScrollBar scrollBar = new();
        scrollBar.Visual.Width = 200;
        scrollBar.Visual.Height = 24;
        scrollBar.Orientation = Orientation.Horizontal;
        Button thumb = GetThumb(scrollBar);
        StateRecorder recorder = new(thumb);

        PressAndDragOff(thumb);

        recorder.LastState.ShouldBe(FrameworkElement.PushedStateName);
    }

    [Fact]
    public void ThumbState_ShouldReturnToEnabled_WhenPrimaryReleasedOffThumb()
    {
        Slider slider = new();
        Button thumb = GetThumb(slider);
        StateRecorder recorder = new(thumb);
        PressAndDragOff(thumb);

        Mock<ICursor> cursor = Mock.Get(FrameworkElement.MainCursor);
        cursor.Setup(x => x.PrimaryDown).Returns(false);
        // InteractiveGue raises this while WindowPushed is still the thumb, after PrimaryDown went false.
        thumb.Visual.TryCallRemoveAsPushed();

        recorder.LastState.ShouldBe(FrameworkElement.EnabledStateName);
    }

    [Fact]
    public void ButtonState_ShouldReturnToEnabled_WhenPushedButtonDraggedOffButton()
    {
        // Unchanged behavior pin: only slider and scroll bar thumbs hold Pushed after the cursor leaves.
        Button button = new();
        StateRecorder recorder = new(button);

        PressAndDragOff(button);

        recorder.LastState.ShouldBe(FrameworkElement.EnabledStateName);
    }

    private static Button GetThumb(Slider slider) =>
        (Button)((SliderVisual)slider.Visual).ThumbInstance!.FormsControlAsObject!;

    private static Button GetThumb(ScrollBar scrollBar) =>
        (Button)((ScrollBarVisual)scrollBar.Visual).ThumbInstance.FormsControlAsObject!;

    private static void PressAndDragOff(Button button)
    {
        Mock<ICursor> cursor = Mock.Get(FrameworkElement.MainCursor);
        cursor.Setup(x => x.PrimaryDown).Returns(true);

        // Press on the button.
        cursor.Object.VisualOver = button.Visual;
        cursor.Object.WindowPushed = button.Visual;
        button.Visual.TryCallPush();

        // Drag off it. The cursor stays the pushed window, but is no longer over the button.
        cursor.Object.VisualOver = null;
        button.Visual.TryCallRollOff(cursor.Object);
    }

    /// <summary>Records the name of the last visual state applied to a button.</summary>
    private class StateRecorder
    {
        public string? LastState { get; private set; }

        public StateRecorder(Button button)
        {
            string[] names =
            {
                FrameworkElement.EnabledStateName,
                FrameworkElement.HighlightedStateName,
                FrameworkElement.PushedStateName,
                FrameworkElement.DisabledStateName,
                FrameworkElement.FocusedStateName,
            };
            foreach (string name in names)
            {
                string captured = name;
                button.GetState(captured).Apply = () => LastState = captured;
            }
        }
    }
}
