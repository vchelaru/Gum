using Gum.Input;
using System.Windows.Forms;

namespace EditorTabPlugin_XNA.ExtensionMethods;

/// <summary>
/// Converts WinForms key codes to Gum's framework-neutral <see cref="GumKey"/> at the editor-host
/// (WinForms) boundary, so interfaces such as <c>IHotkeyManager</c> need not reference
/// <c>System.Windows.Forms</c>.
/// </summary>
public static class KeysExtensions
{
    /// <summary>
    /// Maps a WinForms <see cref="Keys"/> value to the matching <see cref="GumKey"/>, ignoring any
    /// modifier flags (Shift/Ctrl/Alt) present on <paramref name="keys"/>. Only the keys the wireframe
    /// hotkey path actually binds are mapped; any other key returns <c>null</c>.
    /// </summary>
    public static GumKey? ToGumKey(this Keys keys)
    {
        Keys keyCode = keys & Keys.KeyCode;
        return keyCode switch
        {
            Keys.Up => GumKey.Up,
            Keys.Down => GumKey.Down,
            Keys.Left => GumKey.Left,
            Keys.Right => GumKey.Right,
            _ => null,
        };
    }
}
