using Avalonia.Input;
using AvaloniaDataUi;
using Gum.Managers;

namespace Gum.Avalonia.Services;

/// <summary>
/// Tracks the modifier keys from the main window's key events, since Avalonia has no
/// process-wide keyboard-state query. The window updates <see cref="Current"/> on every key press
/// and release.
/// </summary>
public class AvaloniaModifierKeyState : IModifierKeyState
{
    private readonly KeyModifiers _commandModifiers;

    /// <summary>Tracks against the running platform's command modifier.</summary>
    public AvaloniaModifierKeyState() : this(PlatformKeyModifiers.Command)
    {
    }

    /// <summary>Tracks with <paramref name="commandModifiers"/> as the neutral Ctrl.</summary>
    public AvaloniaModifierKeyState(KeyModifiers commandModifiers)
    {
        _commandModifiers = commandModifiers;
    }

    /// <summary>The modifiers as of the last key event the main window saw.</summary>
    public KeyModifiers Current { get; set; }

    /// <inheritdoc/>
    public bool IsCtrlDown => Current.HasFlag(_commandModifiers);

    /// <inheritdoc/>
    public bool IsShiftDown => Current.HasFlag(KeyModifiers.Shift);

    /// <inheritdoc/>
    public bool IsAltDown => Current.HasFlag(KeyModifiers.Alt);
}
