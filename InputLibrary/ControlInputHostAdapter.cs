using System;
using System.Drawing;
using System.Windows.Forms;

namespace InputLibrary
{
    /// <summary>
    /// Adapts a real <see cref="System.Windows.Forms.Control"/> to <see cref="IInputHostControl"/>
    /// via 1:1 forwarding, so <see cref="Cursor"/> and <see cref="Keyboard"/> don't need to depend
    /// on the concrete WinForms type.
    /// </summary>
    public class ControlInputHostAdapter : IInputHostControl
    {
        private readonly Control _control;

        public ControlInputHostAdapter(Control control)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }
            _control = control;
        }

        public bool Focused => _control.Focused;

        public int Width => _control.Width;

        public int Height => _control.Height;

        public CursorKind Cursor
        {
            get => CursorKindConverter.ToCursorKind(_control.Cursor);
            set => _control.Cursor = CursorKindConverter.ToWinCursor(value);
        }

        public Point PointToClient(Point point) => _control.PointToClient(point);
    }
}
