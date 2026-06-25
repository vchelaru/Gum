using System;
using System.Windows.Input;
using WinForms = System.Windows.Forms;

namespace Gum.Input;

/// <summary>
/// Translates WinForms and WPF key types to Gum's framework-neutral <see cref="GumKey"/> /
/// <see cref="GumKeyEventArgs"/> at the editor-host boundary, so <see cref="Gum.Managers.IHotkeyManager"/>
/// need not reference WinForms or WPF.
/// </summary>
public static class KeysExtensions
{
    /// <summary>
    /// Maps a WinForms <see cref="WinForms.Keys"/> value to the matching <see cref="GumKey"/>, ignoring any
    /// modifier flags. Returns null for any key code that is not a defined <see cref="GumKey"/>.
    /// </summary>
    public static GumKey? ToGumKey(this WinForms.Keys keys)
    {
        GumKey candidate = (GumKey)(int)(keys & WinForms.Keys.KeyCode);
        return Enum.IsDefined(candidate) ? candidate : null;
    }

    /// <summary>Builds a framework-neutral <see cref="GumKeyEventArgs"/> from a WinForms key event.</summary>
    public static GumKeyEventArgs ToGumKeyEventArgs(this WinForms.KeyEventArgs e)
    {
        return new GumKeyEventArgs
        {
            Key = e.KeyCode.ToGumKey(),
            IsShiftDown = (e.Modifiers & WinForms.Keys.Shift) == WinForms.Keys.Shift,
            IsCtrlDown = (e.Modifiers & WinForms.Keys.Control) == WinForms.Keys.Control,
            IsAltDown = (e.Modifiers & WinForms.Keys.Alt) == WinForms.Keys.Alt,
            Handled = e.Handled,
            SuppressKeyPress = e.SuppressKeyPress,
        };
    }

    /// <summary>Builds a framework-neutral <see cref="GumKeyEventArgs"/> from a WPF key event.</summary>
    public static GumKeyEventArgs ToGumKeyEventArgs(this KeyEventArgs e)
    {
        // When Alt is held, WPF reports Key == Key.System and carries the real key in SystemKey.
        Key effectiveKey = e.Key == Key.System ? e.SystemKey : e.Key;
        ModifierKeys modifiers = e.KeyboardDevice.Modifiers;
        return new GumKeyEventArgs
        {
            Key = ((WinForms.Keys)KeyInterop.VirtualKeyFromKey(effectiveKey)).ToGumKey(),
            IsShiftDown = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift,
            IsCtrlDown = (modifiers & ModifierKeys.Control) == ModifierKeys.Control,
            IsAltDown = (modifiers & ModifierKeys.Alt) == ModifierKeys.Alt,
            Handled = e.Handled,
        };
    }

    /// <summary>
    /// Rebuilds the WinForms <see cref="WinForms.KeyEventArgs"/> that the hotkey matching logic consumes.
    /// <see cref="GumKey"/> values equal Win32 virtual-key codes, so the key is a pure cast and the
    /// modifier bits are re-applied from the booleans.
    /// </summary>
    public static WinForms.KeyEventArgs ToWinFormsKeyEventArgs(this GumKeyEventArgs e)
    {
        WinForms.Keys keyData = e.Key.HasValue ? (WinForms.Keys)(int)e.Key.Value : WinForms.Keys.None;
        if (e.IsShiftDown) { keyData |= WinForms.Keys.Shift; }
        if (e.IsCtrlDown) { keyData |= WinForms.Keys.Control; }
        if (e.IsAltDown) { keyData |= WinForms.Keys.Alt; }
        return new WinForms.KeyEventArgs(keyData);
    }
}
