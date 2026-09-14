using Avalonia.Input;
using Gum.Managers;

namespace Gum.Avalonia.Services;

/// <summary>
/// Tracks the modifier keys from the main window's key events, since Avalonia has no
/// process-wide keyboard-state query. The window updates <see cref="Current"/> on every key press
/// and release.
/// </summary>
public class AvaloniaModifierKeyState : IModifierKeyState
{
    /// <summary>The modifiers as of the last key event the main window saw.</summary>
    public KeyModifiers Current { get; set; }

    /// <inheritdoc/>
    public bool IsCtrlDown => Current.HasFlag(KeyModifiers.Control);

    /// <inheritdoc/>
    public bool IsShiftDown => Current.HasFlag(KeyModifiers.Shift);

    /// <inheritdoc/>
    public bool IsAltDown => Current.HasFlag(KeyModifiers.Alt);
}
