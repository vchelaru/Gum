using System;
using System.Drawing;
using System.Windows.Media;
using WpfFrameworkElement = System.Windows.FrameworkElement;
using WpfPoint = System.Windows.Point;

namespace InputLibrary
{
    /// <summary>
    /// Adapts a WPF <see cref="WpfFrameworkElement"/> to <see cref="IInputHostControl"/>, so
    /// <see cref="Cursor"/> and <see cref="Keyboard"/> can be initialized against a WPF-native
    /// rendering surface (e.g. a host built on <c>XnaAndWinforms.WpfRenderSurfaceHost</c>) without
    /// depending on a concrete WPF element type.
    /// </summary>
    /// <remarks>
    /// <see cref="IInputHostControl"/>'s contract is physical pixels, matching every other backend's
    /// window/control (their client area has no separate DIU layer, so it's already physical). WPF
    /// reports everything in device-independent units (DIU), so this adapter converts by the current
    /// display's DPI scale - otherwise <see cref="Cursor"/>/<c>SelectionManager</c>/the drag handlers
    /// would read hit-testing coordinates in DIU while the render target they're compared against
    /// (<c>XnaAndWinforms.WpfGraphicsDeviceControl</c>'s) is sized in physical pixels, making dragging
    /// track at the display's DPI scale factor instead of 1:1 with the cursor (#4681).
    /// </remarks>
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

        private double DpiScale => VisualTreeHelper.GetDpi(_element).DpiScaleX;

        /// <summary>
        /// Converts a device-independent (DIU) value to its physical-pixel equivalent for the given
        /// DPI scale, rounding to the nearest pixel. No clamping - unlike a render target's size, a
        /// window dimension or point coordinate can legitimately be zero or negative (e.g. the cursor
        /// outside the control, to the left of or above its origin). Pure/static so it's unit-testable
        /// without a live WPF visual tree.
        /// </summary>
        public static int ToPhysicalPixels(double diuValue, double dpiScale) =>
            (int)Math.Round(diuValue * dpiScale);

        // IsKeyboardFocused (not IsFocused) is required here: IsFocused reflects WPF's logical
        // focus-scope state, which does not clear when the containing window loses OS activation,
        // so clicks would keep registering while another window sits on top. IsKeyboardFocused
        // tracks real OS keyboard focus. Requires _element to be connected to a live
        // PresentationSource (i.e. hosted in a shown window) - not unit-testable without spinning
        // up a real WPF window, so this is exercised by the manual/runtime check instead.
        public bool Focused => _element.IsKeyboardFocused;

        public int Width => ToPhysicalPixels(_element.ActualWidth, DpiScale);

        public int Height => ToPhysicalPixels(_element.ActualHeight, DpiScale);

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
            double dpiScale = DpiScale;
            return new Point(
                ToPhysicalPixels(clientPoint.X, dpiScale),
                ToPhysicalPixels(clientPoint.Y, dpiScale));
        }
    }
}
