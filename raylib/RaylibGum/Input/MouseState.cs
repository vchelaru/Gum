using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.Input;

public enum ButtonState
{
    Released,
    Pressed
}

public struct MouseState
{
    public int X { get; set; }
    public int Y { get; set; }
    public ButtonState LeftButton { get; set; }

    public ButtonState MiddleButton { get; set; }

    public ButtonState RightButton { get; set; }

}
