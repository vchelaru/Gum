using System;
using System.Drawing;
using WpfFrameworkElement = System.Windows.FrameworkElement;
using WpfPoint = System.Windows.Point;

namespace InputLibrary
{
    /// <summary>
    /// Adapts a WPF <see cref="WpfFrameworkElement"/> to <see cref="IInputHostControl"/>, so
    /// <see cref="Cursor"/> and <see cref="Keyboard"/> can be initialized against a WPF-native
    /// rendering surface (e.g. a host built on <c>XnaAndWinforms.WpfRenderSurfaceHost</c>) instead of
    /// a WinForms <see cref="System.Windows.Forms.Control"/>. The WPF counterpart to
    /// <see cref="ControlInputHostAdapter"/>.
    /// </summary>
    public class WpfInputHostAdapter : IInputHostControl
    {
        private readonly WpfFrameworkElement _element;

        public WpfInputHostAdapter(WpfFrameworkElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
            _element = element;
        }

        // Requires _element to be connected to a live PresentationSource (i.e. hosted in a shown
        // window) to reflect real keyboard focus - not unit-testable without spinning up a real WPF
        // window, so this is exercised by the manual/runtime check instead.
        public bool Focused => _element.IsFocused;

        public int Width => (int)_element.ActualWidth;

        public int Height => (int)_element.ActualHeight;

        public CursorKind Cursor
        {
            get => WpfCursorKindConverter.ToCursorKind(_element.Cursor);
            set => _element.Cursor = WpfCursorKindConverter.ToWpfCursor(value);
        }

        // Requires _element to be connected to a live PresentationSource - see the Focused remark
        // above; same manual-check-only caveat applies here.
        public Point PointToClient(Point point)
        {
            WpfPoint screenPoint = new WpfPoint(point.X, point.Y);
            WpfPoint clientPoint = _element.PointFromScreen(screenPoint);
            return new Point((int)clientPoint.X, (int)clientPoint.Y);
        }
    }
}
