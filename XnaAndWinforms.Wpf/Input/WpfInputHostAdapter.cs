using System;
using Microsoft.Xna.Framework.Input;
using WpfFrameworkElement = System.Windows.FrameworkElement;
using WpfPoint = System.Windows.Point;

namespace InputLibrary
{
    /// <summary>
    /// Adapts a WPF <see cref="WpfFrameworkElement"/> to <see cref="IInputHostControl"/>, so
    /// <see cref="Cursor"/> and <see cref="Keyboard"/> can be initialized against a WPF-native
    /// rendering surface (a host built on <c>XnaAndWinforms.WpfGraphicsDeviceControl</c>) without
    /// depending on a concrete WPF element type. Pointer and key state come from the KNI DX11
    /// platform's polled input, converted into the element's own device-independent units.
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

        // IsKeyboardFocused (not IsFocused) is required here: IsFocused reflects WPF's logical
        // focus-scope state, which does not clear when the containing window loses OS activation,
        // so clicks would keep registering while another window sits on top. IsKeyboardFocused
        // tracks real OS keyboard focus. Requires _element to be connected to a live
        // PresentationSource (i.e. hosted in a shown window) - not unit-testable without spinning
        // up a real WPF window, so this is exercised by the manual/runtime check instead.
        public bool Focused => _element.IsKeyboardFocused;

        public int Width => (int)_element.ActualWidth;

        public int Height => (int)_element.ActualHeight;

        public CursorKind Cursor
        {
            get => WpfCursorKindConverter.ToCursorKind(_element.Cursor);
            set => _element.Cursor = WpfCursorKindConverter.ToWpfCursor(value);
        }

        // Requires _element to be connected to a live PresentationSource - see the Focused remark
        // above; same manual-check-only caveat applies here.
        public HostPointerState GetPointerState()
        {
            MouseState mouseState = Mouse.GetState();
            WpfPoint clientPoint = _element.PointFromScreen(new WpfPoint(mouseState.X, mouseState.Y));
            return new HostPointerState(
                (int)clientPoint.X,
                (int)clientPoint.Y,
                mouseState.LeftButton == ButtonState.Pressed,
                mouseState.RightButton == ButtonState.Pressed,
                mouseState.MiddleButton == ButtonState.Pressed);
        }

        public KeyboardState GetKeyboardState() => Microsoft.Xna.Framework.Input.Keyboard.GetState();
    }
}
