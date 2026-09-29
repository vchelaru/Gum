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

    /// <summary>The modifiers as of the last key or pointer event the main window saw.</summary>
    public KeyModifiers Current { get; set; }

    /// <summary>
    /// Records a key press or release. When <paramref name="key"/> is itself a modifier, its own flag
    /// comes from <paramref name="isDown"/>, since X11 reports the modifiers from before the event.
    /// </summary>
    public void HandleKey(Key key, KeyModifiers reported, bool isDown)
    {
        KeyModifiers own = key switch
        {
            Key.LeftShift or Key.RightShift => KeyModifiers.Shift,
            Key.LeftCtrl or Key.RightCtrl => KeyModifiers.Control,
            Key.LeftAlt or Key.RightAlt => KeyModifiers.Alt,
            Key.LWin or Key.RWin => KeyModifiers.Meta,
            _ => KeyModifiers.None,
        };
        Current = isDown ? reported | own : reported & ~own;
    }

    /// <inheritdoc/>
    public bool IsCtrlDown => Current.HasFlag(_commandModifiers);

    /// <inheritdoc/>
    public bool IsShiftDown => Current.HasFlag(KeyModifiers.Shift);

    /// <inheritdoc/>
    public bool IsAltDown => Current.HasFlag(KeyModifiers.Alt);
}
