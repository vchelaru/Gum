namespace Gum.Input;

/// <summary>
/// Gum's framework-neutral keyboard key identity, used by <see cref="Gum.Managers.KeyCombination"/>
/// to describe hotkey bindings without referencing WinForms, WPF, or any rendering framework.
/// </summary>
/// <remarks>
/// Each member's integer value is defined equal to its Win32 virtual-key code — the same integers
/// WinForms <c>System.Windows.Forms.Keys</c> and XNA <c>Microsoft.Xna.Framework.Input.Keys</c> use —
/// so converting to a framework key type is a pure cast. This invariant is pinned by
/// <c>GumKeyTests.GumKey_Values_MatchWinFormsVirtualKeyCodes</c>; if a value ever drifts from the
/// VK code, that test fails. Add members here as Gum binds new keys.
/// </remarks>
public enum GumKey
{
    Delete = 0x2E,

    Left = 0x25,
    Up = 0x26,
    Right = 0x27,
    Down = 0x28,

    C = 0x43,
    F = 0x46,
    V = 0x56,
    X = 0x58,
    Y = 0x59,
    Z = 0x5A,

    Add = 0x6B,
    Subtract = 0x6D,

    F2 = 0x71,
    F12 = 0x7B,

    Oemplus = 0xBB,
    OemMinus = 0xBD,
}
