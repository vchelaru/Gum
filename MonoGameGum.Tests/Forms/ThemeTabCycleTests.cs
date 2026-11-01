using Gum.Forms.Controls;
using Gum.Forms.Controls.Games;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Tab / Shift+Tab through the DemoScreenGum of every packaged theme, loaded by the real file loader with a
/// scripted keyboard. Every real control is reached exactly once per lap, the lap wraps, and the parts a
/// control is built from (scrollbar buttons, slider thumb, ...) are not stops.
/// </summary>
public class ThemeTabCycleTests : FileThemeInputTestBase
{
    private static readonly string[] HoldingControls = { "Panel", "StackPanel", "UserControl", "Window", "DialogBox", "ScrollViewer", "Toast" };

    private static readonly string[] FocusableTypes =
        { "Button", "CheckBox", "RadioButton", "Slider", "ComboBox", "TextBox", "PasswordBox", "ListBox", "ToggleButton" };

    private static bool IsPart(FrameworkElement control)
    {
        for (GraphicalUiElement? current = control.Visual.Parent as GraphicalUiElement; current != null; current = current.Parent as GraphicalUiElement)
        {
            if (current is InteractiveGue gue && gue.FormsControlAsObject is FrameworkElement owner
                && !HoldingControls.Contains(owner.GetType().Name))
            {
                return true;
            }
        }
        return false;
    }

    // Scrollbar parts as tab stops are tracked separately (#7429).
    private static bool InsideScrollBar(FrameworkElement control)
    {
        for (GraphicalUiElement? current = control.Visual.Parent as GraphicalUiElement; current != null; current = current.Parent as GraphicalUiElement)
        {
            if (current is InteractiveGue { FormsControlAsObject: ScrollBar }) { return true; }
        }
        return false;
    }

    private List<FrameworkElement> ExpectedStops(GraphicalUiElement screen)
    {
        List<FrameworkElement> all = new();
        CollectControls(screen, all);
        return all
            .Where(c => FocusableTypes.Contains(c.GetType().Name) && c.IsEnabled && c.Visual.AbsoluteVisible && !IsPart(c))
            .ToList();
    }

    // A visible DialogBox keeps Tab for itself (its own test below), which would hide every later stop.
    private void HideDialogBoxes(GraphicalUiElement screen)
    {
        List<FrameworkElement> all = new();
        CollectControls(screen, all);
        foreach (FrameworkElement dialog in all.Where(c => c is DialogBox)) { dialog.Visual.Visible = false; }
        Driver.Step();
    }

    private FrameworkElement? Focused() =>
        (InteractiveGue.CurrentInputReceiver as FrameworkElement);

    private string Describe(FrameworkElement? c) => c == null ? "<none>" : $"{c.GetType().Name} {c.Visual.Name}";

    private List<FrameworkElement?> Lap(FrameworkElement start, Keys? modifier, int presses)
    {
        start.IsFocused = true;
        Driver.Step();
        List<FrameworkElement?> visited = new();
        for (int i = 0; i < presses; i++)
        {
            if (modifier != null) { Driver.SetKey(modifier.Value, true); }
            Driver.PressKey(Keys.Tab);
            if (modifier != null) { Driver.SetKey(modifier.Value, false); }
            visited.Add(Focused());
        }
        return visited;
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Tab_FullLapOverDemoScreen_ReachesEveryControlOnceAndWraps(string theme)
    {
        GraphicalUiElement screen = LoadDemoScreen(theme);
        HideDialogBoxes(screen);
        List<FrameworkElement> expected = ExpectedStops(screen);
        expected.Count.ShouldBeGreaterThan(8);

        List<FrameworkElement?> visited = Lap(expected[0], null, expected.Count + 6);

        // Walk until the lap returns to the starting control.
        int lapLength = visited.FindIndex(c => c == expected[0]) + 1;
        lapLength.ShouldBeGreaterThan(0, $"{Label(theme)}: Tab never wrapped back to {Describe(expected[0])}. Visited: {string.Join(" > ", visited.Select(Describe))}");
        List<FrameworkElement?> lap = visited.Take(lapLength).ToList();
        string path = string.Join(" > ", lap.Select(Describe));

        lap.ShouldAllBe(c => c != null, $"{Label(theme)}: focus lost during the lap: {path}");
        lap.Distinct().Count().ShouldBe(lap.Count, $"{Label(theme)}: a control was visited twice in one lap: {path}");
        lap.Where(c => IsPart(c!) && !InsideScrollBar(c!)).ShouldBeEmpty($"{Label(theme)}: composite parts are tab stops: {path}");
        expected.Where(c => !lap.Contains(c)).Select(Describe).ShouldBeEmpty($"{Label(theme)}: controls never reached: {path}");
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void ShiftTab_FullLapOverDemoScreen_IsTheReverseOfTab(string theme)
    {
        GraphicalUiElement screen = LoadDemoScreen(theme);
        HideDialogBoxes(screen);
        List<FrameworkElement> expected = ExpectedStops(screen);
        List<FrameworkElement?> forward = Lap(expected[0], null, expected.Count + 6);
        int lapLength = forward.FindIndex(c => c == expected[0]) + 1;
        lapLength.ShouldBeGreaterThan(0);
        List<FrameworkElement?> forwardLap = forward.Take(lapLength).ToList();

        List<FrameworkElement?> backward = Lap(expected[0], Keys.LeftShift, lapLength);

        // Forward lap is [s1, s2, ..., sN, s0]; Shift+Tab from s0 gives [sN, ..., s1, s0].
        List<FrameworkElement?> expectedBackward = forwardLap.Take(lapLength - 1).Reverse().Append(forwardLap.Last()).ToList();
        string.Join(" > ", backward.Select(Describe)).ShouldBe(string.Join(" > ", expectedBackward.Select(Describe)), $"{Label(theme)} Shift+Tab");
    }
}
