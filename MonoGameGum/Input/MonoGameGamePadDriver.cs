using Microsoft.Xna.Framework.Input;
using GamepadButton = Gum.Input.GamepadButton;

namespace MonoGameGum.Input;

/// <summary>
/// Reads a MonoGame/XNA <see cref="GamePadState"/> and pushes it into the platform-neutral
/// <see cref="Gum.Input.GamePad"/> holder (defined in GumCommon), mirroring the Raylib input
/// driver branch of <c>FormsUtilities.UpdateGamepads</c>. Buttons map by name; triggers are
/// thresholded to the digital <c>ButtonDown(LeftTrigger/RightTrigger)</c> semantics of the
/// retired XNA <c>MonoGameGum.Input.GamePad</c>.
/// </summary>
internal static class MonoGameGamePadDriver
{
    // Matches the retired MonoGameGum.Input.GamePad.AnalogOnThreshold (0.5, compared with >=).
    const float TriggerThreshold = 0.5f;

    /// <summary>
    /// Pushes the supplied <paramref name="state"/> into <paramref name="gamepad"/> via its
    /// driver-facing setters and commits the frame with <see cref="Gum.Input.GamePad.Activity"/>.
    /// </summary>
    public static void Apply(Gum.Input.GamePad gamepad, GamePadState state, double time)
    {
        gamepad.SetConnected(state.IsConnected);

        GamePadButtons buttons = state.Buttons;
        GamePadDPad dpad = state.DPad;

        gamepad.SetButtonState(GamepadButton.A, IsDown(buttons.A));
        gamepad.SetButtonState(GamepadButton.B, IsDown(buttons.B));
        gamepad.SetButtonState(GamepadButton.X, IsDown(buttons.X));
        gamepad.SetButtonState(GamepadButton.Y, IsDown(buttons.Y));

        gamepad.SetButtonState(GamepadButton.LeftShoulder, IsDown(buttons.LeftShoulder));
        gamepad.SetButtonState(GamepadButton.RightShoulder, IsDown(buttons.RightShoulder));

        gamepad.SetButtonState(GamepadButton.Start, IsDown(buttons.Start));
        gamepad.SetButtonState(GamepadButton.Back, IsDown(buttons.Back));
        gamepad.SetButtonState(GamepadButton.BigButton, IsDown(buttons.BigButton));

        gamepad.SetButtonState(GamepadButton.LeftStick, IsDown(buttons.LeftStick));
        gamepad.SetButtonState(GamepadButton.RightStick, IsDown(buttons.RightStick));

        gamepad.SetButtonState(GamepadButton.DPadUp, IsDown(dpad.Up));
        gamepad.SetButtonState(GamepadButton.DPadDown, IsDown(dpad.Down));
        gamepad.SetButtonState(GamepadButton.DPadLeft, IsDown(dpad.Left));
        gamepad.SetButtonState(GamepadButton.DPadRight, IsDown(dpad.Right));

        gamepad.SetButtonState(GamepadButton.LeftTrigger, state.Triggers.Left >= TriggerThreshold);
        gamepad.SetButtonState(GamepadButton.RightTrigger, state.Triggers.Right >= TriggerThreshold);

        // XNA ThumbSticks already use the Gum convention (X: -1 left..+1 right, Y: -1 down..+1 up).
        Microsoft.Xna.Framework.Vector2 left = state.ThumbSticks.Left;
        Microsoft.Xna.Framework.Vector2 right = state.ThumbSticks.Right;
        gamepad.SetLeftStickPosition(left.X, left.Y);
        gamepad.SetRightStickPosition(right.X, right.Y);

        gamepad.Activity(time);
    }

    static bool IsDown(ButtonState state) => state == ButtonState.Pressed;
}
