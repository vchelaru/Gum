using System.Collections.Generic;
using Gum.Input;

namespace Gum.Unity;

/// <summary>
/// One Unity gamepad's state for a frame, as plain values. The Unity package fills it from
/// <c>UnityEngine.InputSystem.Gamepad</c> (buttons named by position, as Unity names them) so the
/// mapping into Gum's <see cref="GamePad"/> lives here, where it can run without Unity.
/// </summary>
public struct UnityGamepadState
{
    public bool DPadUp;
    public bool DPadDown;
    public bool DPadLeft;
    public bool DPadRight;

    public bool ButtonSouth;
    public bool ButtonEast;
    public bool ButtonWest;
    public bool ButtonNorth;

    public bool LeftShoulder;
    public bool RightShoulder;
    public bool Start;
    public bool Select;
    public bool LeftStickButton;
    public bool RightStickButton;

    /// <summary>0 to 1.</summary>
    public float LeftTrigger;
    /// <summary>0 to 1.</summary>
    public float RightTrigger;

    public float LeftStickX;
    public float LeftStickY;
    public float RightStickX;
    public float RightStickY;
}

/// <summary>
/// Maps Unity gamepad state onto Gum's <see cref="GamePad"/> slots.
/// </summary>
public static class UnityGamepadMapper
{
    /// <summary>Triggers at or past this value count as pressed, matching the other runtimes.</summary>
    public const float TriggerThreshold = 0.5f;

    /// <summary>
    /// Gamepads fill Gum's slots in order; slots past the connected pads are disconnected and released.
    /// Call every frame, before <see cref="GumService.Update"/>.
    /// </summary>
    public static void Push(GamePad[] gumGamepads, IReadOnlyList<UnityGamepadState> unityGamepads)
    {
        for (int i = 0; i < gumGamepads.Length; i++)
        {
            bool isConnected = i < unityGamepads.Count;
            // Pushed state persists until overwritten, so a vanished pad is pushed as all-released
            // rather than left holding whatever it held when it was unplugged.
            Apply(gumGamepads[i], isConnected ? unityGamepads[i] : default, isConnected);
        }
    }

    static void Apply(GamePad gumGamepad, UnityGamepadState pad, bool isConnected)
    {
        gumGamepad.SetConnected(isConnected);

        gumGamepad.SetButtonState(GamepadButton.DPadUp, pad.DPadUp);
        gumGamepad.SetButtonState(GamepadButton.DPadDown, pad.DPadDown);
        gumGamepad.SetButtonState(GamepadButton.DPadLeft, pad.DPadLeft);
        gumGamepad.SetButtonState(GamepadButton.DPadRight, pad.DPadRight);

        // Gum uses Xbox face-button names; Unity names them by position.
        gumGamepad.SetButtonState(GamepadButton.A, pad.ButtonSouth);
        gumGamepad.SetButtonState(GamepadButton.B, pad.ButtonEast);
        gumGamepad.SetButtonState(GamepadButton.X, pad.ButtonWest);
        gumGamepad.SetButtonState(GamepadButton.Y, pad.ButtonNorth);

        gumGamepad.SetButtonState(GamepadButton.LeftShoulder, pad.LeftShoulder);
        gumGamepad.SetButtonState(GamepadButton.RightShoulder, pad.RightShoulder);
        gumGamepad.SetButtonState(GamepadButton.Start, pad.Start);
        gumGamepad.SetButtonState(GamepadButton.Back, pad.Select);
        gumGamepad.SetButtonState(GamepadButton.LeftStick, pad.LeftStickButton);
        gumGamepad.SetButtonState(GamepadButton.RightStick, pad.RightStickButton);

        gumGamepad.SetButtonState(GamepadButton.LeftTrigger, pad.LeftTrigger >= TriggerThreshold);
        gumGamepad.SetButtonState(GamepadButton.RightTrigger, pad.RightTrigger >= TriggerThreshold);

        // Unity's stick Y is positive-up, the same convention as Gum's.
        gumGamepad.SetLeftStickPosition(pad.LeftStickX, pad.LeftStickY);
        gumGamepad.SetRightStickPosition(pad.RightStickX, pad.RightStickY);
    }
}
