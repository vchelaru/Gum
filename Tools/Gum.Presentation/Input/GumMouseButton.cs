namespace Gum.Input;

/// <summary>
/// Gum's framework-neutral mouse button identity, used by <see cref="GumMouseEventArgs"/> so mouse
/// handling code (e.g. camera panning) does not need to reference WinForms or WPF button types.
/// </summary>
public enum GumMouseButton
{
    None,
    Left,
    Right,
    Middle,
}
