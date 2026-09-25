using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Gum.Avalonia.Canvas;
using Gum.Input;

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
    /// Builds a neutral wheel event. On macOS a trackpad's two-finger scroll pans (unless Cmd is
    /// held) like native Mac canvas apps; a mouse wheel, and every event on other OSes, zooms (#4985).
    /// </summary>
    public static GumMouseEventArgs ToGumWheelEventArgs(this PointerWheelEventArgs e, Visual relativeTo)
    {
        GumMouseEventArgs args = e.ToGumMouseEventArgs(relativeTo, PointerUpdateKind.Other);
        WheelSource source = !OperatingSystem.IsMacOS() ? WheelSource.Wheel
            : MacScrollEvent.IsCurrentEventPrecise() ? WheelSource.MacTrackpad
            : WheelSource.MacMouseWheel;
        double dpiScale = TopLevel.GetTopLevel(relativeTo)?.RenderScaling ?? 1.0;
        ApplyWheelDelta(args, e.Delta, e.KeyModifiers, source, dpiScale);
        return args;
    }

    /// <summary>
    /// Fills in <paramref name="args"/>' zoom <see cref="GumMouseEventArgs.Delta"/> or, for a
    /// trackpad scroll without Cmd, its pan in physical pixels.
    /// </summary>
    public static void ApplyWheelDelta(GumMouseEventArgs args, Vector delta, KeyModifiers modifiers, WheelSource source, double dpiScale)
    {
        if (source == WheelSource.MacTrackpad && !modifiers.HasFlag(KeyModifiers.Meta))
        {
            args.IsPanScroll = true;
            args.PanX = (float)(delta.X * PrecisePointsPerDelta * dpiScale);
            args.PanY = (float)(delta.Y * PrecisePointsPerDelta * dpiScale);
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

    // WPF reports 120 per notch; Avalonia reports 1.
    private const int WheelNotchDelta = 120;

    // Avalonia's macOS backend divides a precise scroll's points by 50 (AvnView.mm).
    private const double PrecisePointsPerDelta = 50;

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
    /// <summary>A precise-delta device on macOS (trackpad, Magic Mouse).</summary>
    MacTrackpad,
}
