using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.Input;
public class GamePad
{
    // todo - this is needed to be implemented
    public bool ButtonPushed(GamepadButton button) => false;
    public bool ButtonReleased(GamepadButton button) => false;

    public bool ButtonRepeatRate(GamepadButton button) => false;

    public AnalogStick LeftStick { get; private set; } = new();
}

public class AnalogStick
{
    public bool AsDPadPushed(DPadDirection direction) => false;
    public bool AsDPadPushedRepeatRate(DPadDirection direction) => false;
}

public enum GamepadButton
{
    //
    // Summary:
    //     Directional pad up.
    DPadUp = 1,
    //
    // Summary:
    //     Directional pad down.
    DPadDown = 2,
    //
    // Summary:
    //     Directional pad left.
    DPadLeft = 4,
    //
    // Summary:
    //     Directional pad right.
    DPadRight = 8,
    //
    // Summary:
    //     START button.
    Start = 0x10,
    //
    // Summary:
    //     BACK button.
    Back = 0x20,
    //
    // Summary:
    //     Left stick button (pressing the left stick).
    LeftStick = 0x40,
    //
    // Summary:
    //     Right stick button (pressing the right stick).
    RightStick = 0x80,
    //
    // Summary:
    //     Left bumper (shoulder) button.
    LeftShoulder = 0x100,
    //
    // Summary:
    //     Right bumper (shoulder) button.
    RightShoulder = 0x200,
    //
    // Summary:
    //     Big button.
    BigButton = 0x800,
    //
    // Summary:
    //     A button.
    A = 0x1000,
    //
    // Summary:
    //     B button.
    B = 0x2000,
    //
    // Summary:
    //     X button.
    X = 0x4000,
    //
    // Summary:
    //     Y button.
    Y = 0x8000,
    //
    // Summary:
    //     Left grip.
    LeftGrip = 0x80000,
    //
    // Summary:
    //     Right grip.
    RightGrip = 0x100000,
    //
    // Summary:
    //     Left stick is towards the left.
    LeftThumbstickLeft = 0x200000,
    //
    // Summary:
    //     Right trigger.
    RightTrigger = 0x400000,
    //
    // Summary:
    //     Left trigger.
    LeftTrigger = 0x800000,
    //
    // Summary:
    //     Right stick is towards up.
    RightThumbstickUp = 0x1000000,
    //
    // Summary:
    //     Right stick is towards down.
    RightThumbstickDown = 0x2000000,
    //
    // Summary:
    //     Right stick is towards the right.
    RightThumbstickRight = 0x4000000,
    //
    // Summary:
    //     Right stick is towards the left.
    RightThumbstickLeft = 0x8000000,
    //
    // Summary:
    //     Left stick is towards up.
    LeftThumbstickUp = 0x10000000,
    //
    // Summary:
    //     Left stick is towards down.
    LeftThumbstickDown = 0x20000000,
    //
    // Summary:
    //     Left stick is towards the right.
    LeftThumbstickRight = 0x40000000
}


public enum DPadDirection
{
    Up,
    Down,
    Left,
    Right
}