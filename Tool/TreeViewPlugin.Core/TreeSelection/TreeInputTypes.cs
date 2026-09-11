using System;

namespace Gum.Controls;

/// <summary>
/// Modifier keys held during a tree click or key press. The values match WPF's
/// <c>System.Windows.Input.ModifierKeys</c>, so the WPF tree casts its keyboard state straight across.
/// </summary>
[Flags]
public enum TreeModifierKeys
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8,
}

/// <summary>
/// The pointer button behind a tree click. The order matches WPF's
/// <c>System.Windows.Input.MouseButton</c>.
/// </summary>
public enum TreePointerButton
{
    Left,
    Middle,
    Right,
    XButton1,
    XButton2,
}

/// <summary>The keys the tree handles itself to move the selection.</summary>
public enum TreeNavigationKey
{
    Left,
    Right,
    Up,
    Down,
    Home,
    End,
    PageUp,
    PageDown,
}
