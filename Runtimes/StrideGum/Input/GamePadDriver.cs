using Stride.Input;
using GumGamepadButton = Gum.Input.GamepadButton;

namespace Gum.Input;

/// <summary>
/// Reads a Stride <see cref="IGamePadDevice"/> and pushes it into the platform-neutral
/// <see cref="GamePad"/> holder (defined in GumCommon). Every platform (MonoGame, Raylib, Silk.NET)
/// exposes a same-named <c>GamePadDriver</c> class in its own namespace, so
/// <c>FormsUtilities.UpdateGamepads</c> can dispatch through one unqualified call resolved by the
/// file's per-platform <c>using</c> block. Simpler than Silk's driver: Stride's
/// <see cref="GamePadState"/> is a single bitmask + axis struct, not a live per-button query.
/// </summary>
internal static class GamePadDriver
{
    /// <summary>
    /// Pushes the current state of <paramref name="strideGamePad"/> into <paramref name="gamepad"/>
    /// via its driver-facing setters and commits the frame with <see cref="GamePad.Activity"/>.
    /// </summary>
    public static void Apply(GamePad gamepad, IGamePadDevice strideGamePad, double time)
    {
        var state = strideGamePad.State;

        gamepad.SetConnected(true);

        gamepad.SetButtonState(GumGamepadButton.DPadUp, (state.Buttons & GamePadButton.PadUp) != 0);
        gamepad.SetButtonState(GumGamepadButton.DPadDown, (state.Buttons & GamePadButton.PadDown) != 0);
        gamepad.SetButtonState(GumGamepadButton.DPadLeft, (state.Buttons & GamePadButton.PadLeft) != 0);
        gamepad.SetButtonState(GumGamepadButton.DPadRight, (state.Buttons & GamePadButton.PadRight) != 0);

        gamepad.SetButtonState(GumGamepadButton.A, (state.Buttons & GamePadButton.A) != 0);
        gamepad.SetButtonState(GumGamepadButton.B, (state.Buttons & GamePadButton.B) != 0);
        gamepad.SetButtonState(GumGamepadButton.X, (state.Buttons & GamePadButton.X) != 0);
        gamepad.SetButtonState(GumGamepadButton.Y, (state.Buttons & GamePadButton.Y) != 0);

        gamepad.SetButtonState(GumGamepadButton.LeftShoulder, (state.Buttons & GamePadButton.LeftShoulder) != 0);
        gamepad.SetButtonState(GumGamepadButton.RightShoulder, (state.Buttons & GamePadButton.RightShoulder) != 0);

        gamepad.SetButtonState(GumGamepadButton.Start, (state.Buttons & GamePadButton.Start) != 0);
        gamepad.SetButtonState(GumGamepadButton.Back, (state.Buttons & GamePadButton.Back) != 0);

        gamepad.SetButtonState(GumGamepadButton.LeftStick, (state.Buttons & GamePadButton.LeftThumb) != 0);
        gamepad.SetButtonState(GumGamepadButton.RightStick, (state.Buttons & GamePadButton.RightThumb) != 0);

        // Threshold to the digital LeftTrigger/RightTrigger semantics Gum's GamePad models,
        // matching the MonoGame/Silk drivers' TriggerThreshold.
        const float TriggerThreshold = 0.5f;
        gamepad.SetButtonState(GumGamepadButton.LeftTrigger, state.LeftTrigger >= TriggerThreshold);
        gamepad.SetButtonState(GumGamepadButton.RightTrigger, state.RightTrigger >= TriggerThreshold);

        // Stride's LeftThumb/RightThumb are already XNA/Gum-convention (Y positive-up), unlike
        // Silk's SDL/GLFW backends which report Y positive-down.
        gamepad.SetLeftStickPosition(state.LeftThumb.X, state.LeftThumb.Y);
        gamepad.SetRightStickPosition(state.RightThumb.X, state.RightThumb.Y);

        gamepad.Activity(time);
    }
}
