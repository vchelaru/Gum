using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Gum.Avalonia.Canvas;
using Gum.Input;
using Gum.Services;

namespace Gum.Avalonia.Services;

/// <summary>
/// Translates Avalonia pointer events to Gum's framework-neutral <see cref="GumMouseEventArgs"/>
/// at a canvas control's boundary, mirroring the WPF head's <c>MouseExtensions</c>.
/// </summary>
public static class AvaloniaMouseMapping
{
    /// <summary>
    /// Builds a neutral mouse event positioned relative to <paramref name="relativeTo"/>, converted
    /// from Avalonia's device-independent units (DIU) to physical pixels - matching the render
    /// target's physical-pixel sizing (#4811, parity with the WPF head's #4681/#4682 fix) so camera
    /// panning/zoom-at-cursor stay in sync with what's actually drawn on a scaled display. Pass the
    /// event's <see cref="PointerPointProperties.PointerUpdateKind"/> for a press or release so the
    /// button that changed is reported; pass <see cref="PointerUpdateKind.Other"/> for a move or
    /// wheel event, which reports whichever button is currently held - what a drag needs.
    /// </summary>
    public static GumMouseEventArgs ToGumMouseEventArgs(this PointerEventArgs e, Visual relativeTo, PointerUpdateKind updateKind)
    {
        Point position = e.GetPosition(relativeTo);
        double dpiScale = TopLevel.GetTopLevel(relativeTo)?.RenderScaling ?? 1.0;
        PointerPointProperties properties = e.GetCurrentPoint(relativeTo).Properties;
        return new GumMouseEventArgs
        {
            X = AvaloniaInputHostAdapter.ToPhysicalPixels(position.X, dpiScale),
            Y = AvaloniaInputHostAdapter.ToPhysicalPixels(position.Y, dpiScale),
            Button = ToGumMouseButton(updateKind, properties),
            Handled = e.Handled,
        };
    }

    /// <summary>
    /// Whether a press is macOS's secondary click: the left button with Ctrl, which Mac apps treat as
    /// a right-click. Avalonia's macOS backend reports it as a plain left press with Ctrl held
    /// (<c>AvnView.mm</c> sends every <c>mouseDown:</c> as <c>LeftButtonDown</c>), so the head maps it
    /// itself; see <see cref="SecondaryClickHook"/>.
    /// </summary>
    public static bool IsSecondaryClickPress(PointerUpdateKind kind, KeyModifiers modifiers, IOperatingSystemInfo operatingSystem) =>
        operatingSystem.IsMacOS && kind == PointerUpdateKind.LeftButtonPressed && modifiers.HasFlag(KeyModifiers.Control);

    /// <summary>
    /// Builds a neutral wheel event. A touchpad's two-finger scroll pans like native canvas apps,
    /// unless Cmd (macOS, #4985) or Ctrl (Windows #5477, Linux #5489) is held; a mouse wheel zooms.
    /// </summary>
    public static GumMouseEventArgs ToGumWheelEventArgs(this PointerWheelEventArgs e, Visual relativeTo)
    {
        GumMouseEventArgs args = e.ToGumMouseEventArgs(relativeTo, PointerUpdateKind.Other);
        WheelSource source = WheelSource.Wheel;
        if (OperatingSystem.IsMacOS())
        {
            MacScrollEvent.ReadCurrentEvent(out bool isPrecise, out bool hasGesturePhase);
            source = GetMacWheelSource(isPrecise, hasGesturePhase);
        }
        else if (OperatingSystem.IsWindows())
        {
            source = GetWindowsWheelSource(Environment.TickCount64, WindowsTouchpadContacts.LastReportMs);
        }
        else if (OperatingSystem.IsLinux())
        {
            long now = Environment.TickCount64;
            source = GetLinuxWheelSource(e.Delta, now, _lastLinuxTouchpadMs);
            if (source == WheelSource.LinuxTouchpad)
            {
                _lastLinuxTouchpadMs = now;
            }
        }
        ScrollLog.Write(e.Delta, e.KeyModifiers, source);
        Vector? touchpadPan = null;
        if (source == WheelSource.WindowsTouchpad && OperatingSystem.IsWindows())
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                WindowsTouchpadContacts.PanTracker.Discard();
            }
            else
            {
                touchpadPan = WindowsTouchpadContacts.PanTracker.TakePan(e.Delta);
            }
        }
        double dpiScale = TopLevel.GetTopLevel(relativeTo)?.RenderScaling ?? 1.0;
        ApplyWheelDelta(args, e.Delta, e.KeyModifiers, source, dpiScale, touchpadPan);
        return args;
    }

    /// <summary>
    /// Classifies a macOS scroll event. Only a trackpad or Magic Mouse gesture carries a phase;
    /// remote-desktop tools inject a mouse wheel's clicks as precise scrolls with none.
    /// </summary>
    public static WheelSource GetMacWheelSource(bool isPrecise, bool hasGesturePhase) =>
        isPrecise && hasGesturePhase ? WheelSource.MacTrackpad : WheelSource.MacMouseWheel;

    /// <summary>
    /// Starts telling touchpad scrolls from wheel clicks for the window hosting <paramref name="canvas"/>.
    /// Only Windows needs this; call it when the canvas attaches to the visual tree.
    /// </summary>
    public static void EnableTouchpadDetection(Visual canvas)
    {
        if (OperatingSystem.IsWindows() && TopLevel.GetTopLevel(canvas) is { } topLevel)
        {
            WindowsTouchpadContacts.EnsureListening(topLevel);
        }
    }

    /// <summary>
    /// Classifies a Windows wheel event: it came from a precision touchpad if the touchpad sent a
    /// contact report within the last <see cref="TouchpadReportWindowMs"/>.
    /// </summary>
    public static WheelSource GetWindowsWheelSource(long nowMs, long? lastTouchpadReportMs) =>
        lastTouchpadReportMs is { } last && nowMs - last <= TouchpadReportWindowMs
            ? WheelSource.WindowsTouchpad
            : WheelSource.Wheel;

    /// <summary>
    /// Classifies a Linux wheel event from its delta alone, since Avalonia's X11 backend doesn't say
    /// which device sent it: a notched wheel scrolls whole steps, a touchpad fractions. A whole step
    /// within <see cref="LinuxTouchpadGestureGapMs"/> of the last touchpad event is still the touchpad.
    /// </summary>
    public static WheelSource GetLinuxWheelSource(Vector delta, long nowMs, long? lastTouchpadMs)
    {
        if (!IsWhole(delta.X) || !IsWhole(delta.Y))
        {
            return WheelSource.LinuxTouchpad;
        }
        return lastTouchpadMs is { } last && nowMs - last <= LinuxTouchpadGestureGapMs
            ? WheelSource.LinuxTouchpad
            : WheelSource.Wheel;
    }

    private static bool IsWhole(double value) => Math.Abs(value - Math.Round(value)) < 1e-6;

    /// <summary>
    /// Fills in <paramref name="args"/>' zoom <see cref="GumMouseEventArgs.Delta"/> or, for a
    /// touchpad scroll without Cmd (macOS) or Ctrl (Windows), its pan in physical pixels. A Windows
    /// touchpad pans by <paramref name="touchpadPan"/>, the fingers' travel in device-independent
    /// pixels, when it's known, since the wheel delta Windows sends is locked to one axis at first.
    /// </summary>
    public static void ApplyWheelDelta(GumMouseEventArgs args, Vector delta, KeyModifiers modifiers, WheelSource source, double dpiScale,
        Vector? touchpadPan = null)
    {
        if (source == WheelSource.MacTrackpad && !modifiers.HasFlag(KeyModifiers.Meta))
        {
            args.IsPanScroll = true;
            args.PanX = (float)(delta.X * PrecisePointsPerDelta * dpiScale);
            args.PanY = (float)(delta.Y * PrecisePointsPerDelta * dpiScale);
            return;
        }

        // Ctrl+scroll zooms, and so does a pinch, which Windows reports as Ctrl+wheel.
        if (source == WheelSource.WindowsTouchpad && !modifiers.HasFlag(KeyModifiers.Control))
        {
            Vector pan = touchpadPan ?? delta * WindowsTouchpadPixelsPerDelta;
            args.IsPanScroll = true;
            args.PanX = (float)(pan.X * dpiScale);
            args.PanY = (float)(pan.Y * dpiScale);
            return;
        }

        if (source == WheelSource.LinuxTouchpad && !modifiers.HasFlag(KeyModifiers.Control))
        {
            args.IsPanScroll = true;
            args.PanX = (float)(delta.X * LinuxTouchpadPixelsPerDelta * dpiScale);
            args.PanY = (float)(delta.Y * LinuxTouchpadPixelsPerDelta * dpiScale);
            return;
        }

        if (source == WheelSource.MacMouseWheel)
        {
            // macOS sends one event per click but scales its delta by scroll acceleration (about
            // 0.02 for a slow click), so only the direction counts (#5010).
            args.Delta = Math.Sign(delta.Y) * WheelNotchDelta;
            return;
        }

        args.Delta = (int)(delta.Y * WheelNotchDelta);
    }

    /// <summary>
    /// Converts a trackpad pinch's magnification into wheel delta, one zoom step per 15% of
    /// pinch, which is about the ratio between neighboring zoom levels.
    /// </summary>
    public static int PinchToWheelDelta(double magnification) =>
        (int)Math.Round(magnification / PinchPerZoomStep * WheelNotchDelta);

    private static long? _lastLinuxTouchpadMs;

    // WPF reports 120 per notch; Avalonia reports 1.
    private const int WheelNotchDelta = 120;

    // Avalonia's macOS backend divides a precise scroll's points by 50 (AvnView.mm).
    private const double PrecisePointsPerDelta = 50;

    // Browsers scroll 100 pixels per wheel notch; Avalonia reports a notch as 1.
    private const double WindowsTouchpadPixelsPerDelta = 100;

    // A precision touchpad reports at 100+ Hz while touched, so a gap this long means no fingers.
    private const long TouchpadReportWindowMs = 100;

    // xf86-input-libinput's default scroll distance: one unit of touchpad delta is 15 pixels of finger travel.
    private const double LinuxTouchpadPixelsPerDelta = 15;

    private const long LinuxTouchpadGestureGapMs = 200;

    private const double PinchPerZoomStep = 0.15;

    private static GumMouseButton ToGumMouseButton(PointerUpdateKind updateKind, PointerPointProperties properties) => updateKind switch
    {
        PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased => GumMouseButton.Left,
        PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => GumMouseButton.Right,
        PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => GumMouseButton.Middle,
        _ when properties.IsLeftButtonPressed => GumMouseButton.Left,
        _ when properties.IsRightButtonPressed => GumMouseButton.Right,
        _ when properties.IsMiddleButtonPressed => GumMouseButton.Middle,
        _ => GumMouseButton.None,
    };
}

/// <summary>Where a wheel event came from, which decides whether it zooms or pans and how far.</summary>
public enum WheelSource
{
    /// <summary>A mouse wheel on Windows or Linux, reporting 1 per notch.</summary>
    Wheel,
    /// <summary>A mouse wheel on macOS: one event per click, delta scaled by acceleration.</summary>
    MacMouseWheel,
    /// <summary>A gesture on a precise-delta device on macOS (trackpad, Magic Mouse).</summary>
    MacTrackpad,
    /// <summary>A scroll or pinch on a Windows precision touchpad, reporting 1 per notch.</summary>
    WindowsTouchpad,
    /// <summary>A two-finger scroll on a Linux touchpad, reporting fractional steps.</summary>
    LinuxTouchpad,
}
