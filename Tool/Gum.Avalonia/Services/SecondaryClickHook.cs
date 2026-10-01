using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Gum.Services;

namespace Gum.Avalonia.Services;

/// <summary>
/// Turns macOS's Ctrl+left-click into a right-click in every window, so each control that opens a
/// menu or selects on a right-click (the trees, the canvas, the lists, a <see cref="Control.ContextMenu"/>)
/// does the same for it (#5555). It raises a right-button press and release on the element the
/// original press and release went to, and marks the originals handled. Does nothing on Windows and
/// Linux, where Ctrl+click keeps its own meaning.
/// </summary>
public static class SecondaryClickHook
{
    /// <summary>Starts translating secondary clicks; dispose the result to stop.</summary>
    public static IDisposable Install(IOperatingSystemInfo operatingSystem)
    {
        SecondaryClickTracker tracker = new SecondaryClickTracker(operatingSystem);
        // Tunnel routing starts at the TopLevel, so this sees every press before any control does.
        IDisposable pressed = InputElement.PointerPressedEvent.AddClassHandler<TopLevel>(
            (topLevel, e) => HandlePressed(topLevel, e, tracker), RoutingStrategies.Tunnel, handledEventsToo: true);
        IDisposable released = InputElement.PointerReleasedEvent.AddClassHandler<TopLevel>(
            (topLevel, e) => HandleReleased(topLevel, e, tracker), RoutingStrategies.Tunnel, handledEventsToo: true);
        return new HandlerPair(pressed, released);
    }

    private static void HandlePressed(TopLevel topLevel, PointerPressedEventArgs e, SecondaryClickTracker tracker)
    {
        PointerUpdateKind kind = e.GetCurrentPoint(topLevel).Properties.PointerUpdateKind;
        if (!tracker.Track(kind, e.KeyModifiers) || e.Source is not Interactive source)
        {
            return;
        }
        e.Handled = true;
        KeyModifiers modifiers = WithoutControl(e.KeyModifiers);
        source.RaiseEvent(new PointerPressedEventArgs(source, e.Pointer, topLevel, e.GetPosition(topLevel), e.Timestamp,
            new PointerPointProperties(ToRaw(modifiers) | RawInputModifiers.RightMouseButton, PointerUpdateKind.RightButtonPressed),
            modifiers, e.ClickCount));
    }

    private static void HandleReleased(TopLevel topLevel, PointerReleasedEventArgs e, SecondaryClickTracker tracker)
    {
        PointerUpdateKind kind = e.GetCurrentPoint(topLevel).Properties.PointerUpdateKind;
        if (!tracker.Track(kind, e.KeyModifiers) || e.Source is not Interactive source)
        {
            return;
        }
        e.Handled = true;
        KeyModifiers modifiers = WithoutControl(e.KeyModifiers);
        source.RaiseEvent(new PointerReleasedEventArgs(source, e.Pointer, topLevel, e.GetPosition(topLevel), e.Timestamp,
            new PointerPointProperties(ToRaw(modifiers), PointerUpdateKind.RightButtonReleased),
            modifiers, MouseButton.Right));
    }

    // Ctrl is what made the click secondary, so the right-click carries no Ctrl of its own.
    private static KeyModifiers WithoutControl(KeyModifiers modifiers) => modifiers & ~KeyModifiers.Control;

    // KeyModifiers and RawInputModifiers share the Alt, Control, Shift and Meta bits.
    private static RawInputModifiers ToRaw(KeyModifiers modifiers) => (RawInputModifiers)(int)modifiers;

    private sealed class HandlerPair : IDisposable
    {
        private readonly IDisposable _pressed;
        private readonly IDisposable _released;

        public HandlerPair(IDisposable pressed, IDisposable released)
        {
            _pressed = pressed;
            _released = released;
        }

        public void Dispose()
        {
            _pressed.Dispose();
            _released.Dispose();
        }
    }
}

/// <summary>
/// Follows one pointer's presses and releases to tell which left press is a macOS secondary click
/// (<see cref="AvaloniaMouseMapping.IsSecondaryClickPress"/>) and which release ends it. The press
/// decides: letting go of Ctrl before the button does not turn it back into a left click.
/// </summary>
public sealed class SecondaryClickTracker
{
    private readonly IOperatingSystemInfo _operatingSystem;

    /// <summary>Creates a tracker that applies the rule for <paramref name="operatingSystem"/>.</summary>
    public SecondaryClickTracker(IOperatingSystemInfo operatingSystem)
    {
        _operatingSystem = operatingSystem;
    }

    /// <summary>Whether the left button is down as part of a secondary click.</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Records a pointer event's button change and returns whether the event is a secondary click's
    /// press or the release that ends it.
    /// </summary>
    public bool Track(PointerUpdateKind kind, KeyModifiers modifiers)
    {
        if (kind == PointerUpdateKind.LeftButtonPressed)
        {
            IsActive = AvaloniaMouseMapping.IsSecondaryClickPress(kind, modifiers, _operatingSystem);
            return IsActive;
        }
        if (kind == PointerUpdateKind.LeftButtonReleased && IsActive)
        {
            IsActive = false;
            return true;
        }
        return false;
    }
}
