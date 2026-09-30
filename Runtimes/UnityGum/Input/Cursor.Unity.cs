namespace Gum.Input;

/// <summary>
/// Unity half of the shared <see cref="Cursor"/> partial. The host reads Unity's input and pushes it
/// here once per frame, before <c>GumService.Update</c>, in canvas pixels with the origin at the top
/// left (<see cref="ScreenToCanvasMapper"/> converts from Unity's bottom-left screen space). Set
/// <see cref="IsMobile"/> from <c>Application.isMobilePlatform</c>; netstandard2.1 can't detect it.
/// </summary>
public partial class Cursor
{
    private MouseState _pushedMouseState = new MouseState();
    private TouchCollection _pushedTouches = new TouchCollection();

    // Running scroll total in XNA units (120 per notch), which the shared ScrollWheelChange math expects.
    private float _scrollWheelValue;

    /// <summary>
    /// Sets the mouse state the next <see cref="Activity(double)"/> reads.
    /// </summary>
    /// <param name="x">X in canvas pixels from the left edge.</param>
    /// <param name="y">Y in canvas pixels from the top edge.</param>
    /// <param name="leftDown">Whether the left button is held.</param>
    /// <param name="middleDown">Whether the middle button is held.</param>
    /// <param name="rightDown">Whether the right button is held.</param>
    public void SetMouseState(float x, float y, bool leftDown, bool middleDown, bool rightDown)
    {
        _pushedMouseState = new MouseState
        {
            X = (int)x,
            Y = (int)y,
            LeftButton = leftDown ? ButtonState.Pressed : ButtonState.Released,
            MiddleButton = middleDown ? ButtonState.Pressed : ButtonState.Released,
            RightButton = rightDown ? ButtonState.Pressed : ButtonState.Released,
        };
    }

    /// <summary>
    /// Adds scroll wheel movement, in notches (Unity's Input System reports one per notch). Positive
    /// scrolls up.
    /// </summary>
    public void AddScrollNotches(float notches) => _scrollWheelValue += notches * 120;

    /// <summary>
    /// Sets the touches the next <see cref="Activity(double)"/> reads when <see cref="IsMobile"/> is true,
    /// in canvas pixels from the top left.
    /// </summary>
    public void SetTouches(TouchCollection touches) => _pushedTouches = touches;

    private MouseState GetMouseState()
    {
        MouseState state = _pushedMouseState;
        state.ScrollWheelValue = (int)_scrollWheelValue;
        return state;
    }

    private TouchCollection GetTouchCollection() => _pushedTouches;

    private int? GetViewportLeft() => 0;

    private int? GetViewportTop() => 0;
}
