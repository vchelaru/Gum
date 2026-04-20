using static Sokol.SApp;

namespace Gum.Input;

public partial class Cursor
{
    private static MouseState _pendingState;

    private MouseState GetMouseState() => _pendingState;

    private TouchCollection GetTouchCollection() => new TouchCollection();

    private int? GetViewportLeft() => 0;

    private int? GetViewportTop() => 0;

    /// <summary>
    /// Forwards a Sokol app event into the Cursor input buffer. Host apps should
    /// call this from their sokol_app event callback for every mouse event.
    /// </summary>
    public static void HandleSokolEvent(in sapp_event ev)
    {
        switch (ev.type)
        {
            case sapp_event_type.SAPP_EVENTTYPE_MOUSE_MOVE:
                _pendingState.X = (int)ev.mouse_x;
                _pendingState.Y = (int)ev.mouse_y;
                break;
            case sapp_event_type.SAPP_EVENTTYPE_MOUSE_DOWN:
                _pendingState.X = (int)ev.mouse_x;
                _pendingState.Y = (int)ev.mouse_y;
                SetButton(ev.mouse_button, ButtonState.Pressed);
                break;
            case sapp_event_type.SAPP_EVENTTYPE_MOUSE_UP:
                _pendingState.X = (int)ev.mouse_x;
                _pendingState.Y = (int)ev.mouse_y;
                SetButton(ev.mouse_button, ButtonState.Released);
                break;
            case sapp_event_type.SAPP_EVENTTYPE_MOUSE_SCROLL:
                // Scale to match XNA detent convention (120 units per notch),
                // so the shared ScrollWheelChange math works unchanged.
                _pendingState.ScrollWheelValue += (int)(ev.scroll_y * 120);
                break;
        }
    }

    private static void SetButton(sapp_mousebutton button, ButtonState state)
    {
        switch (button)
        {
            case sapp_mousebutton.SAPP_MOUSEBUTTON_LEFT:
                _pendingState.LeftButton = state;
                break;
            case sapp_mousebutton.SAPP_MOUSEBUTTON_RIGHT:
                _pendingState.RightButton = state;
                break;
            case sapp_mousebutton.SAPP_MOUSEBUTTON_MIDDLE:
                _pendingState.MiddleButton = state;
                break;
        }
    }
}
