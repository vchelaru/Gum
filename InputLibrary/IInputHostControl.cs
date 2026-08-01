using System.Drawing;

namespace InputLibrary
{
    /// <summary>
    /// The subset of a rendering host's control surface that <see cref="Cursor"/> and
    /// <see cref="Keyboard"/> need in order to translate mouse/keyboard state into window-relative
    /// coordinates and focus. Lets those classes be initialized against any host - a live WPF
    /// element (via <see cref="WpfInputHostAdapter"/>) or a test double - rather than one concrete
    /// UI-framework control type.
    /// </summary>
    public interface IInputHostControl
    {
        /// <summary>
        /// Whether the host control currently has input focus.
        /// </summary>
        bool Focused { get; }

        /// <summary>
        /// The host control's width, in pixels.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// The host control's height, in pixels.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// The cursor icon currently displayed over the host control.
        /// </summary>
        CursorKind Cursor { get; set; }

        /// <summary>
        /// Converts a point in screen coordinates to client (window-relative) coordinates.
        /// </summary>
        Point PointToClient(Point point);
    }
}
