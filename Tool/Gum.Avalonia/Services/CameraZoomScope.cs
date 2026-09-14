using Avalonia;
using Avalonia.Controls;

namespace Gum.Avalonia.Services;

/// <summary>
/// Marks a control as owning the zoom hotkeys (Ctrl+= / Ctrl+-) for its own camera, so the
/// app-wide font zoom in the main window's tunneling key handler does not claim them first. The
/// property inherits, so setting it on a canvas also covers anything focusable inside it. The
/// Avalonia counterpart of the WPF head's attached property of the same name.
/// </summary>
public static class CameraZoomScope
{
    /// <summary>The attached property.</summary>
    public static readonly AttachedProperty<bool> OwnsCameraZoomProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("OwnsCameraZoom", typeof(CameraZoomScope), defaultValue: false, inherits: true);

    /// <summary>Sets whether <paramref name="element"/> owns the zoom hotkeys.</summary>
    public static void SetOwnsCameraZoom(Control element, bool value) => element.SetValue(OwnsCameraZoomProperty, value);

    /// <summary>Gets whether <paramref name="element"/> owns the zoom hotkeys.</summary>
    public static bool GetOwnsCameraZoom(Control element) => element.GetValue(OwnsCameraZoomProperty);

    /// <summary>
    /// Whether app-wide font zoom should apply to a key event originating from
    /// <paramref name="source"/>. Anything that is not a control (or is null) counts as ordinary
    /// UI, so zoom stays enabled.
    /// </summary>
    public static bool IsEntireAppZoomEnabledFor(object? source) =>
        source is not Control element || !GetOwnsCameraZoom(element);
}
