using System;
using Avalonia.Input;
using AvaloniaDataUi;
using Gum.Input;
using Gum.Managers;

namespace Gum.Avalonia.Services;

/// <summary>
/// Translates Avalonia key events to Gum's framework-neutral <see cref="GumKeyEventArgs"/> at the
/// window boundary, mirroring the WPF head's <c>KeysExtensions</c>. The platform's command
/// modifier (<see cref="PlatformKeyModifiers.Command"/>: Cmd on macOS, Ctrl elsewhere) becomes
/// Gum's neutral "Ctrl" flag, so the Ctrl-based hotkey defaults fire from Cmd on a Mac.
/// </summary>
public static class AvaloniaKeyMapping
{
    /// <summary>
    /// Maps an Avalonia key to the matching <see cref="GumKey"/> by name. <see cref="GumKey"/>
    /// uses the classic Windows key names and Avalonia's <see cref="Key"/> follows the same
    /// naming, so a case-insensitive name match covers every key Gum binds. Others map to null.
    /// </summary>
    public static GumKey? ToGumKey(this Key key) =>
        Enum.TryParse(key.ToString(), ignoreCase: true, out GumKey gumKey) ? gumKey : null;

    /// <summary>The Avalonia key for a <see cref="GumKey"/>, by the same name match as <see cref="ToGumKey"/>.</summary>
    public static Key? ToAvaloniaKey(this GumKey key) =>
        Enum.TryParse(key.ToString(), ignoreCase: true, out Key avaloniaKey) ? avaloniaKey : null;

    /// <summary>Builds a neutral key event from an Avalonia one, using the running platform's command modifier.</summary>
    public static GumKeyEventArgs ToGumKeyEventArgs(this KeyEventArgs e) =>
        e.ToGumKeyEventArgs(PlatformKeyModifiers.Command);

    /// <summary>Builds a neutral key event from an Avalonia one, with <paramref name="commandModifiers"/> as the neutral Ctrl.</summary>
    public static GumKeyEventArgs ToGumKeyEventArgs(this KeyEventArgs e, KeyModifiers commandModifiers) => new GumKeyEventArgs
    {
        Key = e.Key.ToGumKey(),
        IsShiftDown = e.KeyModifiers.HasFlag(KeyModifiers.Shift),
        IsCtrlDown = e.KeyModifiers.HasFlag(commandModifiers),
        IsAltDown = e.KeyModifiers.HasFlag(KeyModifiers.Alt),
        Handled = e.Handled,
    };

    /// <summary>
    /// The Avalonia gesture for a binding, with <paramref name="commandModifiers"/> standing in for
    /// the neutral Ctrl. Null for a modifier-only combination or a key Avalonia has no name for.
    /// </summary>
    public static KeyGesture? ToKeyGesture(this KeyCombination combination, KeyModifiers commandModifiers)
    {
        if (combination.Key?.ToAvaloniaKey() is not { } key)
        {
            return null;
        }

        KeyModifiers modifiers = KeyModifiers.None;
        if (combination.IsCtrlDown)
        {
            modifiers |= commandModifiers;
        }
        if (combination.IsShiftDown)
        {
            modifiers |= KeyModifiers.Shift;
        }
        if (combination.IsAltDown)
        {
            modifiers |= KeyModifiers.Alt;
        }
        return new KeyGesture(key, modifiers);
    }
}
