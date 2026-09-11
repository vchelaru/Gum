using Microsoft.Xna.Framework.Input;

namespace InputLibrary
{
    /// <summary>
    /// The subset of a rendering host's control surface that <see cref="Cursor"/> and
    /// <see cref="Keyboard"/> need in order to poll pointer and key state in window-relative
    /// coordinates and focus. Lets those classes be initialized against any host - a live WPF
    /// element, an Avalonia control, or a test double - rather than one concrete UI-framework
    /// control type. Each host samples its own framework's input and reports it in the host's
    /// client space, so the polling classes never touch a platform input API themselves.
    /// </summary>
    public interface IInputHostControl
    {
        /// <summary>
        /// Whether the host control currently has input focus.
        /// </summary>
        bool Focused { get; }

        /// <summary>
        /// The host control's width, in its own units (device-independent units for WPF and Avalonia).
        /// </summary>
        int Width { get; }

        /// <summary>
        /// The host control's height, in the same units as <see cref="Width"/>.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// The cursor icon currently displayed over the host control.
        /// </summary>
        CursorKind Cursor { get; set; }

        /// <summary>
        /// Samples the pointer: its position relative to the host's top-left corner, in the same
        /// units as <see cref="Width"/>, and the buttons currently held.
        /// </summary>
        HostPointerState GetPointerState();

        /// <summary>
        /// Samples the keys currently held, as the host sees them.
        /// </summary>
        KeyboardState GetKeyboardState();
    }
}
