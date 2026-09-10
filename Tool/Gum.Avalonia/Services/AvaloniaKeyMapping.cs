using System;
using Avalonia.Input;
using Gum.Input;

namespace Gum.Avalonia.Services;

/// <summary>
/// Translates Avalonia key events to Gum's framework-neutral <see cref="GumKeyEventArgs"/> at the
/// window boundary, mirroring the WPF head's <c>KeysExtensions</c>.
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

    /// <summary>Builds a neutral key event from an Avalonia one.</summary>
    public static GumKeyEventArgs ToGumKeyEventArgs(this KeyEventArgs e) => new GumKeyEventArgs
    {
        Key = e.Key.ToGumKey(),
        IsShiftDown = e.KeyModifiers.HasFlag(KeyModifiers.Shift),
        IsCtrlDown = e.KeyModifiers.HasFlag(KeyModifiers.Control),
        IsAltDown = e.KeyModifiers.HasFlag(KeyModifiers.Alt),
        Handled = e.Handled,
    };
}
