using System.Drawing;
using WinCursor = System.Windows.Forms.Cursor;

namespace InputLibrary
{
    /// <summary>
    /// The subset of <see cref="System.Windows.Forms.Control"/> that <see cref="Cursor"/> and
    /// <see cref="Keyboard"/> need in order to translate mouse/keyboard state into window-relative
    /// coordinates and focus. Lets those classes be initialized against a host that isn't a real
    /// WinForms control (e.g. a test double or a future non-WinForms rendering host).
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
        /// The cursor currently displayed over the host control.
        /// </summary>
        WinCursor Cursor { get; set; }

        /// <summary>
        /// Converts a point in screen coordinates to client (window-relative) coordinates.
        /// </summary>
        Point PointToClient(Point point);
    }
}
