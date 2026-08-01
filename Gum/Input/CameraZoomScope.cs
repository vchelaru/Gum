using System.Windows;

namespace Gum.Input;

/// <summary>
/// Marks a WPF element as owning the zoom hotkeys (Ctrl+= / Ctrl+-) for its own camera, so the
/// app-wide font zoom in <c>MainWindow</c>'s <c>PreviewKeyDown</c> tunnel doesn't claim them first.
/// </summary>
/// <remarks>
/// Needed because tunneling runs root-to-leaf: the window sees every key before the focused canvas
/// does, so a canvas cannot simply handle the key itself. The property inherits, so setting it on a
/// canvas also covers anything focusable inside it.
/// </remarks>
public static class CameraZoomScope
{
    public static readonly DependencyProperty OwnsCameraZoomProperty =
        DependencyProperty.RegisterAttached(
            "OwnsCameraZoom",
            typeof(bool),
            typeof(CameraZoomScope),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static void SetOwnsCameraZoom(DependencyObject element, bool value) =>
        element.SetValue(OwnsCameraZoomProperty, value);

    public static bool GetOwnsCameraZoom(DependencyObject element) =>
        (bool)element.GetValue(OwnsCameraZoomProperty);

    /// <summary>
    /// Whether app-wide font zoom should apply to a key event originating from
    /// <paramref name="source"/>. Anything that isn't a <see cref="DependencyObject"/> (or is null)
    /// counts as ordinary UI, so zoom stays enabled.
    /// </summary>
    public static bool IsEntireAppZoomEnabledFor(object? source) =>
        source is not DependencyObject element || !GetOwnsCameraZoom(element);
}
