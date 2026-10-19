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
    public const float TriggerThreshold = GamePadSnapshot.TriggerThreshold;

    /// <summary>
    /// Gamepads fill Gum's slots in order; slots past the connected pads are disconnected and released.
    /// Call every frame, before <see cref="GumServiceSkiaInteractiveBase.Update"/>.
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
        // Gum uses Xbox face-button names; Unity names them by position. Unity's stick Y is
        // positive-up, the same convention as Gum's, so sticks pass through.
        gumGamepad.SetState(new GamePadSnapshot
        {
            IsConnected = isConnected,

            DPadUp = pad.DPadUp,
            DPadDown = pad.DPadDown,
            DPadLeft = pad.DPadLeft,
            DPadRight = pad.DPadRight,

            A = pad.ButtonSouth,
            B = pad.ButtonEast,
            X = pad.ButtonWest,
            Y = pad.ButtonNorth,

            LeftShoulder = pad.LeftShoulder,
            RightShoulder = pad.RightShoulder,
            Start = pad.Start,
            Back = pad.Select,
            LeftStick = pad.LeftStickButton,
            RightStick = pad.RightStickButton,

            LeftTrigger = pad.LeftTrigger,
            RightTrigger = pad.RightTrigger,

            LeftStickX = pad.LeftStickX,
            LeftStickY = pad.LeftStickY,
            RightStickX = pad.RightStickX,
            RightStickY = pad.RightStickY,
        });
    }
}
