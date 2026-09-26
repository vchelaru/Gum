using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>
/// The timeline's time box and scrubber driven end to end with real key and pointer input, keys
/// going through the same app-wide hotkey routing the main window uses. The canvas is not built
/// headlessly, so "the canvas changed" is read from the state the canvas shows
/// (<c>ISelectedState.CustomCurrentStateSave</c>, which the animation sets for each time).
/// </summary>
public class TimelineEndToEndTests
{
    private const string Category = "AnimationCategory";
    private static readonly Color NowLine = Color.Parse("#c3e3ed");

    [AvaloniaFact]
    public void TypingATime_KeyByKey_MovesTheTimeLineAndTheShownState_AndCtrlZUndoesTheLastRealEditOnly()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        RouteAppWideHotkeys(editor.Window);
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        AnimatedKeyframeViewModel pressed = editor.AddStateKeyframe($"{Category}/Pressed");
        AnimatedKeyframeViewModel released = editor.AddStateKeyframe($"{Category}/Released");
        editor.Hover(new Point(5, 5));
        Point start = editor.KeyframeMarkerCenter(pressed);
        Point end = editor.KeyframeMarkerCenter(released);
        // Off the markers' centers, so a marker never hides the line.
        double lineY = start.Y + 7;
        editor.ViewModel.DisplayedAnimationTime = 0.12345;
        editor.Layout();
        editor.ViewModel.DisplayedAnimationTime.ShouldBe(0.12345, "the box showing a rounded time must not write it back");
        editor.ViewModel.DisplayedAnimationTime = 1;
        editor.Click(editor.TimelineTimeBox);
        editor.TimelineTimeBox.SelectAll();

        // "0." parses to the same time as "0", so that keystroke has nothing to move.
        (string Key, double Time)[] keystrokes = { ("0", 0), (".", 0), ("2", 0.2), ("5", 0.25) };
        double previousTime = 1;
        foreach ((string key, double time) in keystrokes)
        {
            TypeKey(editor.Window, key);
            editor.Layout();

            editor.ViewModel.DisplayedAnimationTime.ShouldBe(time, tolerance: 0.0001, $"after typing '{key}'");
            Point expectedLine = new Point(start.X + (end.X - start.X) * time, lineY);
            editor.AnyPixelNear(expectedLine, 1, NowLine).ShouldBeTrue($"the time line is at {time} after typing '{key}'");
            if (time != previousTime)
            {
                editor.AnyPixelNear(new Point(start.X + (end.X - start.X) * previousTime, lineY), 1, NowLine)
                    .ShouldBeFalse($"the time line left {previousTime} after typing '{key}'");
            }
            StateSave shown = editor.SelectedState.CustomCurrentStateSave.ShouldNotBeNull();
            ((float)shown.GetValue("X")!).ShouldBe((float)(100 * time), tolerance: 0.5f, $"the shown state after typing '{key}'");
            previousTime = time;
        }
        editor.TimelineTimeBox.Text.ShouldBe("0.25");
        editor.SaveFrame("step1-typed-0.25");

        // Past the end: the time stops at the length, but the box keeps what is being typed until Enter.
        editor.TimelineTimeBox.SelectAll();
        TypeKey(editor.Window, "3");
        editor.Layout();
        editor.ViewModel.DisplayedAnimationTime.ShouldBe(1);
        editor.TimelineTimeBox.Text.ShouldBe("3", "typing is not overwritten mid-edit");
        editor.Press(Key.Enter, PhysicalKey.Enter);
        editor.TimelineTimeBox.Text.ShouldBe("1", "Enter shows the time the typed value became");

        // A real edit: move the Released keyframe to 2 seconds in the detail column.
        editor.Click(editor.RowFor(editor.KeyframeList, released));
        editor.TypeAndEnter(editor.DetailTimeBox, "2");
        released.Time.ShouldBe(2f);
        int actionsAfterEdit = editor.UndoManager.CurrentElementHistory.ShouldNotBeNull().Actions.Count;

        editor.Click(editor.TimelineTimeBox);
        editor.TimelineTimeBox.SelectAll();
        foreach (char key in "0.5")
        {
            TypeKey(editor.Window, key.ToString());
        }
        editor.Layout();
        editor.ViewModel.DisplayedAnimationTime.ShouldBe(0.5, tolerance: 0.0001);
        editor.UndoManager.CurrentElementHistory!.Actions.Count.ShouldBe(actionsAfterEdit, "typing a time records no undo");

        editor.Press(Key.Z, PhysicalKey.Z, RawInputModifiers.Control);

        editor.ThrowIfPluginFailed();
        editor.ViewModel.SelectedAnimation.ShouldNotBeNull().Keyframes
            .Single(keyframe => keyframe.StateName == $"{Category}/Released").Time.ShouldBe(1f, "Ctrl+Z undid the keyframe move");
        editor.ReadSavedAnimations(component).ShouldNotBeNull().Animations.Single().States
            .Single(keyframe => keyframe.StateName == $"{Category}/Released").Time.ShouldBe(1f);
        editor.TimelineTimeBox.Text.ShouldBe(editor.ViewModel.DisplayedAnimationTime.ToString("0.###", System.Globalization.CultureInfo.CurrentCulture),
            "the focused time box shows the time the undo left, not the text typed before it");
        editor.SaveFrame("step1-after-ctrl-z");
    }

    [AvaloniaFact]
    public void DraggingTheScrubberThumb_ShowsTheTimeTipUntilRelease_OverATickedTrack()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave component = editor.AddComponent("Button", Category, "Pressed", "Released");
        editor.Select(component);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe($"{Category}/Pressed");
        editor.AddStateKeyframe($"{Category}/Released");
        Slider scrubber = editor.Scrubber;
        scrubber.TickFrequency.ShouldBe(0.1);
        scrubber.TickPlacement.ShouldBe(global::Avalonia.Controls.TickPlacement.BottomRight);
        Point thumb = editor.CenterOf(editor.ScrubberThumb);
        double trackWidth = scrubber.Bounds.Width - editor.ScrubberThumb.Bounds.Width;
        Point threeQuarters = new Point(thumb.X + trackWidth * 0.75, thumb.Y);

        editor.Window.MouseMove(thumb, RawInputModifiers.None);
        editor.Window.MouseDown(thumb, MouseButton.Left, RawInputModifiers.None);
        editor.Window.MouseMove(new Point(thumb.X + trackWidth * 0.4, thumb.Y), RawInputModifiers.LeftMouseButton);
        editor.Window.MouseMove(threeQuarters, RawInputModifiers.LeftMouseButton);
        editor.Layout();

        editor.ViewModel.DisplayedAnimationTime.ShouldBe(0.75, tolerance: 0.05);
        ToolTip.GetIsOpen(scrubber).ShouldBeTrue();
        ToolTip.GetTip(scrubber).ShouldBe(editor.ViewModel.DisplayedAnimationTime.ToString("0.00", System.Globalization.CultureInfo.CurrentCulture));
        editor.SaveFrame("step2-dragging");
        // Just above the thumb's left edge: the tip is drawn there while dragging, nothing after.
        Point thumbTopLeft = editor.ScrubberThumb.TranslatePoint(default, editor.Window)!.Value;
        Point aboveThumb = new Point(thumbTopLeft.X + 4, thumbTopLeft.Y - 14);
        Color aboveThumbWhileDragging = editor.Input.PixelAt(aboveThumb);

        editor.Window.MouseUp(threeQuarters, MouseButton.Left, RawInputModifiers.None);
        editor.Layout();

        ToolTip.GetIsOpen(scrubber).ShouldBeFalse();
        ToolTip.GetTip(scrubber).ShouldBeNull("hovering the scrubber later must not show a stale time");
        editor.SaveFrame("step2-released");
        editor.Input.PixelAt(aboveThumb).ShouldNotBe(aboveThumbWhileDragging, "the tip was drawn above the thumb, where the thumb had moved to");
    }

    internal static void RouteAppWideHotkeys(Window window) =>
        AppWideWindowInput.RouteHotkeys(window,
            TestAppBuilder.Services.GetRequiredService<IHotkeyManager>(),
            TestAppBuilder.Services.GetRequiredService<AvaloniaModifierKeyState>());

    // A key press as a keyboard sends it: key down, the text it types, key up.
    private static void TypeKey(Window window, string text)
    {
        (Key key, PhysicalKey physicalKey) = text switch
        {
            "." => (Key.OemPeriod, PhysicalKey.Period),
            _ => (Key.D0 + (text[0] - '0'), PhysicalKey.Digit0 + (text[0] - '0')),
        };
        window.KeyPress(key, RawInputModifiers.None, physicalKey, text);
        window.KeyTextInput(text);
        window.KeyRelease(key, RawInputModifiers.None, physicalKey, text);
    }
}
