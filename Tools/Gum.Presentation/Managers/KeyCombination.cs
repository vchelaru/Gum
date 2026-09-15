using Gum.Input;

namespace Gum.Managers;

/// <summary>
/// A framework-neutral keyboard shortcut binding: a <see cref="GumKey"/> plus modifier flags. Headless
/// (ADR-0005) — the WinForms/WPF matching (<c>IsPressed</c>) lives in <c>KeyCombinationExtensions</c> in
/// the tool layer, and the live-modifier query is <see cref="IHotkeyManager.IsPressedInControl"/>.
/// Display text comes from <see cref="IKeyCombinationFormatter"/>; <see cref="ToString"/> is the
/// Windows spelling.
/// </summary>
public class KeyCombination
{
    public GumKey? Key { get; set; }

    /// <summary>
    /// Whether the platform's primary shortcut modifier is part of the combination: Ctrl on Windows
    /// and Linux, Cmd on macOS. Each head maps its own modifier onto this flag at the window boundary.
    /// </summary>
    public bool IsCtrlDown { get; set; }
    public bool IsShiftDown { get; set; }
    public bool IsAltDown { get; set; }

    public static KeyCombination Pressed(GumKey key) => new KeyCombination { Key = key };
    public static KeyCombination Ctrl(GumKey key) => new KeyCombination { Key = key, IsCtrlDown = true };
    public static KeyCombination Alt(GumKey? key = null) => new KeyCombination { Key = key, IsAltDown = true };
    public static KeyCombination Shift(GumKey? key = null) => new KeyCombination { Key = key, IsShiftDown = true };

    /// <summary>
    /// Whether the held modifiers satisfy this combination. Shared by every framework-specific
    /// <c>IsPressed</c> overload, which adds only the key comparison on top.
    /// </summary>
    /// <remarks>
    /// A combination naming a <see cref="Key"/> requires the modifiers to match exactly, so a
    /// modifier it did not ask for blocks it. That keeps AltGr - which Windows reports as Ctrl+Alt -
    /// from triggering a Ctrl shortcut and swallowing the character the user meant to type.
    /// A combination with no <see cref="Key"/> qualifies a mouse gesture (hold Shift to constrain a
    /// drag) rather than naming a shortcut, so it asks only about the modifiers it lists.
    /// </remarks>
    public bool ModifiersMatch(bool isCtrlDown, bool isShiftDown, bool isAltDown)
    {
        if (Key == null)
        {
            return (!IsCtrlDown || isCtrlDown)
                && (!IsShiftDown || isShiftDown)
                && (!IsAltDown || isAltDown);
        }

        return IsCtrlDown == isCtrlDown
            && IsShiftDown == isShiftDown
            && IsAltDown == isAltDown;
    }

    /// <inheritdoc/>
    public override string ToString() => new KeyCombinationFormatter(KeyDisplayStyle.Windows).Format(this);
}
