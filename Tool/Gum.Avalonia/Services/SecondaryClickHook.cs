using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Gum.Services;

namespace Gum.Avalonia.Services;

/// <summary>
/// Turns macOS's Ctrl+left-click into a right-click in every window, so each control that opens a
/// menu or selects on a right-click (the trees, the canvas, the lists, a <see cref="Control.ContextMenu"/>)
/// does the same for it (#5555). It raises a right-button press and release on the element the
/// original press and release went to, and marks the originals handled. Avalonia's tap gestures
/// still read the originals as left clicks, so the Tapped and DoubleTapped they raise are handled
/// before any control sees them. Does nothing on Windows and Linux, where Ctrl+click keeps its own
/// meaning.
/// </summary>
public static class SecondaryClickHook
{
    /// <summary>Starts translating secondary clicks; dispose the result to stop.</summary>
    public static IDisposable Install(IOperatingSystemInfo operatingSystem) => new Installation(operatingSystem);

    private sealed class Installation : IDisposable
    {
        private readonly SecondaryClickTracker _tracker;
        private readonly List<IDisposable> _handlers;
        // Whether the last press or release was a secondary click's original. Avalonia raises Tapped
        // and DoubleTapped right after that event's route, before the next pointer event.
        private bool _isLastButtonEventReplaced;

        public Installation(IOperatingSystemInfo operatingSystem)
        {
            _tracker = new SecondaryClickTracker(operatingSystem);
            // Tunnel routing starts at the TopLevel, so these see every press before any control does.
            _handlers = new List<IDisposable>
            {
                InputElement.PointerPressedEvent.AddClassHandler<TopLevel>(HandlePressed, RoutingStrategies.Tunnel, handledEventsToo: true),
                InputElement.PointerReleasedEvent.AddClassHandler<TopLevel>(HandleReleased, RoutingStrategies.Tunnel, handledEventsToo: true),
                // Class handlers run on each element before its own handlers, so the gesture's
                // source handles it first.
                InputElement.TappedEvent.AddClassHandler<Interactive>(HandleTap),
                InputElement.DoubleTappedEvent.AddClassHandler<Interactive>(HandleTap),
            };
        }

        public void Dispose()
        {
            foreach (IDisposable handler in _handlers)
            {
                handler.Dispose();
            }
            _handlers.Clear();
        }

        private void HandlePressed(TopLevel topLevel, PointerPressedEventArgs e)
        {
            PointerUpdateKind kind = e.GetCurrentPoint(topLevel).Properties.PointerUpdateKind;
            if (!_tracker.Track(kind, e.KeyModifiers) || e.Source is not Interactive source)
            {
                // The replacement right press comes through here too.
                _isLastButtonEventReplaced = false;
                return;
            }
            e.Handled = true;
            KeyModifiers modifiers = WithoutControl(e.KeyModifiers);
            source.RaiseEvent(new PointerPressedEventArgs(source, e.Pointer, topLevel, e.GetPosition(topLevel), e.Timestamp,
                new PointerPointProperties(ToRaw(modifiers) | RawInputModifiers.RightMouseButton, PointerUpdateKind.RightButtonPressed),
                modifiers, e.ClickCount));
            _isLastButtonEventReplaced = true;
        }

        private void HandleReleased(TopLevel topLevel, PointerReleasedEventArgs e)
        {
            PointerUpdateKind kind = e.GetCurrentPoint(topLevel).Properties.PointerUpdateKind;
            if (!_tracker.Track(kind, e.KeyModifiers) || e.Source is not Interactive source)
            {
                _isLastButtonEventReplaced = false;
                return;
            }
            e.Handled = true;
            KeyModifiers modifiers = WithoutControl(e.KeyModifiers);
            source.RaiseEvent(new PointerReleasedEventArgs(source, e.Pointer, topLevel, e.GetPosition(topLevel), e.Timestamp,
                new PointerPointProperties(ToRaw(modifiers), PointerUpdateKind.RightButtonReleased),
                modifiers, MouseButton.Right));
            _isLastButtonEventReplaced = true;
        }

        private void HandleTap(Interactive target, TappedEventArgs e)
        {
            if (_isLastButtonEventReplaced)
            {
                e.Handled = true;
            }
        }
    }

    // Ctrl is what made the click secondary, so the right-click carries no Ctrl of its own.
    private static KeyModifiers WithoutControl(KeyModifiers modifiers) => modifiers & ~KeyModifiers.Control;

    // KeyModifiers and RawInputModifiers share the Alt, Control, Shift and Meta bits.
    private static RawInputModifiers ToRaw(KeyModifiers modifiers) => (RawInputModifiers)(int)modifiers;
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
