using Brutal.GlfwApi;
using System;

namespace Gum.Input;

/// <summary>
/// Translates a GLFW gamepad reading into the platform-neutral <see cref="GamePadSnapshot"/>.
/// </summary>
internal static class GamePadDriver
{
    // GLFW's gamepad layout: 15 buttons (GlfwGamepadButton) and 6 axes (GlfwGamepadAxis).
    public const int ButtonCount = 15;
    public const int AxisCount = 6;

    /// <summary>Translates a GLFW gamepad reading into Gum's conventions.</summary>
    public static GamePadSnapshot ToSnapshot(ReadOnlySpan<byte> buttons, ReadOnlySpan<float> axes)
    {
        // GLFW's triggers rest at -1 and reach +1; Gum's are 0 to 1.
        return new GamePadSnapshot
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
        };
    }

    private static bool Down(ReadOnlySpan<byte> buttons, GlfwGamepadButton button) =>
        (int)button < buttons.Length && buttons[(int)button] != 0;

    private static float Axis(ReadOnlySpan<float> axes, GlfwGamepadAxis axis) =>
        (int)axis < axes.Length ? axes[(int)axis] : 0f;
}
