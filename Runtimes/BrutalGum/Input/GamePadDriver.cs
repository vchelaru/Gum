using Brutal.GlfwApi;
using System;

namespace Gum.Input;

/// <summary>
/// Copies a GLFW gamepad reading into the platform-neutral <see cref="GamePad"/> holder.
/// </summary>
internal static class GamePadDriver
{
    // GLFW's gamepad layout: 15 buttons (GlfwGamepadButton) and 6 axes (GlfwGamepadAxis).
    public const int ButtonCount = 15;
    public const int AxisCount = 6;

    /// <summary>
    /// Pushes a reading into <paramref name="gamepad"/> and commits the frame with
    /// <see cref="GamePad.Activity"/>. <paramref name="buttons"/> hold 1 when pressed.
    /// </summary>
    public static void Apply(GamePad gamepad, ReadOnlySpan<byte> buttons, ReadOnlySpan<float> axes, double time)
    {
        // GLFW's triggers rest at -1 and reach +1; Gum's are 0 to 1.
        gamepad.SetState(new GamePadSnapshot
        {
            IsConnected = true,

            DPadUp = Down(buttons, GlfwGamepadButton.DpadUp),
            DPadDown = Down(buttons, GlfwGamepadButton.DpadDown),
            DPadLeft = Down(buttons, GlfwGamepadButton.DpadLeft),
            DPadRight = Down(buttons, GlfwGamepadButton.DpadRight),

            A = Down(buttons, GlfwGamepadButton.A),
            B = Down(buttons, GlfwGamepadButton.B),
            X = Down(buttons, GlfwGamepadButton.X),
            Y = Down(buttons, GlfwGamepadButton.Y),

            LeftShoulder = Down(buttons, GlfwGamepadButton.LeftBumper),
            RightShoulder = Down(buttons, GlfwGamepadButton.RightBumper),
            Start = Down(buttons, GlfwGamepadButton.Start),
            Back = Down(buttons, GlfwGamepadButton.Back),
            LeftStick = Down(buttons, GlfwGamepadButton.LeftThumb),
            RightStick = Down(buttons, GlfwGamepadButton.RightThumb),

            LeftTrigger = (Axis(axes, GlfwGamepadAxis.LeftTrigger) + 1f) / 2f,
            RightTrigger = (Axis(axes, GlfwGamepadAxis.RightTrigger) + 1f) / 2f,

            // GLFW reports stick Y positive-down; Gum is positive-up.
            LeftStickX = Axis(axes, GlfwGamepadAxis.LeftX),
            LeftStickY = -Axis(axes, GlfwGamepadAxis.LeftY),
            RightStickX = Axis(axes, GlfwGamepadAxis.RightX),
            RightStickY = -Axis(axes, GlfwGamepadAxis.RightY),
        });

        gamepad.Activity(time);
    }

    private static bool Down(ReadOnlySpan<byte> buttons, GlfwGamepadButton button) =>
        (int)button < buttons.Length && buttons[(int)button] != 0;

    private static float Axis(ReadOnlySpan<float> axes, GlfwGamepadAxis axis) =>
        (int)axis < axes.Length ? axes[(int)axis] : 0f;
}
