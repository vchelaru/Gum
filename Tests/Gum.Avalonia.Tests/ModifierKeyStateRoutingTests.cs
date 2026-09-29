using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.Managers;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// #5485: on Linux (X11) a modifier key's own press and release report the modifiers from before
/// the event, which left the tool thinking Shift was held after it was released.
/// </summary>
public class ModifierKeyStateRoutingTests
{
    [AvaloniaFact]
    public void ModifierKey_ReportingThePreviousState_IsTrackedByItsOwnPressAndRelease()
    {
        AvaloniaModifierKeyState state = new AvaloniaModifierKeyState(KeyModifiers.Control);
        Window window = CreateRoutedWindow(state);

        // X11 order: the press reports no Shift yet, the release still reports Shift.
        window.KeyPress(Key.LeftShift, RawInputModifiers.None, PhysicalKey.ShiftLeft, null);
        state.IsShiftDown.ShouldBeTrue();

        window.KeyRelease(Key.LeftShift, RawInputModifiers.Shift, PhysicalKey.ShiftLeft, null);
        state.IsShiftDown.ShouldBeFalse();

        window.Close();
    }

    [AvaloniaFact]
    public void PointerEvent_ReplacesStaleModifiers()
    {
        AvaloniaModifierKeyState state = new AvaloniaModifierKeyState(KeyModifiers.Control);
        Window window = CreateRoutedWindow(state);
        state.Current = KeyModifiers.Shift;

        window.MouseMove(new Point(10, 10), RawInputModifiers.None);
        state.IsShiftDown.ShouldBeFalse();

        window.MouseDown(new Point(10, 10), MouseButton.Left, RawInputModifiers.Shift);
        state.IsShiftDown.ShouldBeTrue();

        window.Close();
    }

    private static Window CreateRoutedWindow(AvaloniaModifierKeyState state)
    {
        Window window = new Window { Content = new Border(), Width = 100, Height = 100 };
        AppWideWindowInput.RouteHotkeys(window, Mock.Of<IHotkeyManager>(), state);
        window.Show();
        return window;
    }
}
