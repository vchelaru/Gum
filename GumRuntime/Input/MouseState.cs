namespace Gum.Input;

public struct MouseState
{
    public int X { get; set; }
    public int Y { get; set; }
    public ButtonState LeftButton { get; set; }
    public ButtonState MiddleButton { get; set; }
    public ButtonState RightButton { get; set; }
    public int ScrollWheelValue { get; set; }
}
