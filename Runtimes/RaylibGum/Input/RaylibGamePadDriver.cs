using System;
using GumGamepadButton = Gum.Input.GamepadButton;
using RaylibGamepadButton = Raylib_cs.GamepadButton;
using RaylibGamepadAxis = Raylib_cs.GamepadAxis;

namespace Gum.Input;

/// <summary>
/// Reads native Raylib controller input and pushes it into the platform-neutral
/// <see cref="GamePad"/> holder (defined in GumCommon), mirroring the MonoGame input
/// driver (<c>MonoGameGamePadDriver</c>) used by <c>FormsUtilities.UpdateGamepads</c>.
/// Raylib exposes gamepad state through static query functions rather than a pre-fetched
/// state struct, so <see cref="Apply(GamePad, int, double)"/> reads by gamepad index
/// directly instead of taking a snapshot type like XNA's <c>GamePadState</c>.
/// </summary>
internal static class RaylibGamePadDriver
{
    /// <summary>
    /// Pushes the current Raylib input for gamepad <paramref name="index"/> into
    /// <paramref name="gamepad"/> via its driver-facing setters and commits the frame with
    /// <see cref="GamePad.Activity"/>. Raylib's IsGamepadButtonDown / GetGamepadAxisMovement
    /// return false / 0 for unavailable indices, so no connectivity guard is needed — a
    /// disconnected pad naturally reads as all-released and centered.
    /// </summary>
    public static void Apply(GamePad gamepad, int index, double time) =>
        Apply(
            gamepad,
            index,
            time,
            (i, button) => Raylib_cs.Raylib.IsGamepadButtonDown(i, button),
            Raylib_cs.Raylib.GetGamepadAxisMovement);

    /// <summary>
    /// Testing seam: identical mapping to <see cref="Apply(GamePad, int, double)"/>, but with the
    /// Raylib button/axis reads injected so the mapping can be pinned by a unit test — Raylib's
    /// static query functions read live hardware and can't be faked directly.
    /// </summary>
    internal static void Apply(
        GamePad gamepad,
        int index,
        double time,
        Func<int, RaylibGamepadButton, bool> isButtonDown,
        Func<int, RaylibGamepadAxis, float> getAxisMovement)
    {
        gamepad.SetButtonState(GumGamepadButton.DPadUp, isButtonDown(index, RaylibGamepadButton.LeftFaceUp));
        gamepad.SetButtonState(GumGamepadButton.DPadDown, isButtonDown(index, RaylibGamepadButton.LeftFaceDown));
        gamepad.SetButtonState(GumGamepadButton.DPadLeft, isButtonDown(index, RaylibGamepadButton.LeftFaceLeft));
        gamepad.SetButtonState(GumGamepadButton.DPadRight, isButtonDown(index, RaylibGamepadButton.LeftFaceRight));

        // Raylib's right-face buttons map to the XNA/Gum face buttons by position:
        // RightFaceDown = A, RightFaceRight = B, RightFaceLeft = X, RightFaceUp = Y.
        gamepad.SetButtonState(GumGamepadButton.A, isButtonDown(index, RaylibGamepadButton.RightFaceDown));
        gamepad.SetButtonState(GumGamepadButton.B, isButtonDown(index, RaylibGamepadButton.RightFaceRight));
        gamepad.SetButtonState(GumGamepadButton.X, isButtonDown(index, RaylibGamepadButton.RightFaceLeft));
        gamepad.SetButtonState(GumGamepadButton.Y, isButtonDown(index, RaylibGamepadButton.RightFaceUp));

        gamepad.SetButtonState(GumGamepadButton.LeftShoulder, isButtonDown(index, RaylibGamepadButton.LeftTrigger1));
        gamepad.SetButtonState(GumGamepadButton.RightShoulder, isButtonDown(index, RaylibGamepadButton.RightTrigger1));

        gamepad.SetButtonState(GumGamepadButton.Start, isButtonDown(index, RaylibGamepadButton.MiddleRight));
        gamepad.SetButtonState(GumGamepadButton.Back, isButtonDown(index, RaylibGamepadButton.MiddleLeft));

        gamepad.SetButtonState(GumGamepadButton.LeftStick, isButtonDown(index, RaylibGamepadButton.LeftThumb));
        gamepad.SetButtonState(GumGamepadButton.RightStick, isButtonDown(index, RaylibGamepadButton.RightThumb));

        // Gum's GamePad models triggers as a digital button only (no analog trigger value), so
        // read the digital trigger buttons LeftTrigger2/RightTrigger2 (LT/RT) directly — this is
        // exact parity with the MonoGame driver's thresholded triggers and avoids raylib's
        // trigger-axis rest-at-(-1) convention. (LeftTrigger1/RightTrigger1 are the shoulders above.)
        gamepad.SetButtonState(GumGamepadButton.LeftTrigger, isButtonDown(index, RaylibGamepadButton.LeftTrigger2));
        gamepad.SetButtonState(GumGamepadButton.RightTrigger, isButtonDown(index, RaylibGamepadButton.RightTrigger2));

        // Raylib reports stick Y as positive-down; flip it to the XNA/Gum convention (positive-up).
        float leftX = getAxisMovement(index, RaylibGamepadAxis.LeftX);
        float leftY = getAxisMovement(index, RaylibGamepadAxis.LeftY);
        gamepad.SetLeftStickPosition(leftX, -leftY);

        float rightX = getAxisMovement(index, RaylibGamepadAxis.RightX);
        float rightY = getAxisMovement(index, RaylibGamepadAxis.RightY);
        gamepad.SetRightStickPosition(rightX, -rightY);

        gamepad.Activity(time);
    }
}
