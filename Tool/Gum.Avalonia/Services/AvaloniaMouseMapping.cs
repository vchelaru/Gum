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
