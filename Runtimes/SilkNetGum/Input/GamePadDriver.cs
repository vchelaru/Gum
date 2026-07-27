using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.Input;
using GumGamepadButton = Gum.Input.GamepadButton;

namespace Gum.Input;

/// <summary>
/// Reads a Silk.NET.Input <see cref="IGamepad"/> and pushes it into the platform-neutral
/// <see cref="GamePad"/> holder (defined in GumCommon). Every platform (MonoGame, Raylib, Sokol)
/// exposes a same-named <c>GamePadDriver</c> class in its own namespace, so
/// <c>FormsUtilities.UpdateGamepads</c> can dispatch through one unqualified call resolved by the
/// file's per-platform <c>using</c> block, with no <c>#if</c> in the method body (issue #3559).
/// Unlike Raylib (static query functions reading live hardware), Silk.NET.Input already hands out
/// an <see cref="IGamepad"/> instance per connected controller, so that instance itself is the
/// testing seam -- no delegate-injection overload is needed here.
/// </summary>
internal static class GamePadDriver
{
    // Silk has no digital trigger buttons -- ButtonName has no LeftTrigger/RightTrigger member --
    // so triggers are only exposed as the analog Triggers[].Position (0..1). Threshold to the
    // digital LeftTrigger/RightTrigger semantics Gum's GamePad models, matching the MonoGame
    // driver's TriggerThreshold.
    const float TriggerThreshold = 0.5f;

    // Works around a Silk.NET.Input.Sdl bug: SdlGamepad's DoEvent handler writes its internal
    // _buttons/_triggers arrays as a side effect INSIDE the argument list of
    // `ButtonDown?.Invoke(this, _buttons[i] = new Button(...))` (and the equivalent for
    // ButtonUp/TriggerMoved). C#'s null-conditional operator short-circuits the ENTIRE call --
    // including evaluating its arguments -- when the event has zero subscribers, so with nobody
    // listening those arrays are never actually updated: every poll of Buttons/Triggers reads
    // permanently stale (false/0) state, even though the controller is reporting real presses.
    // Thumbsticks are unaffected (SdlGamepad assigns _thumbsticks unconditionally before invoking
    // ThumbstickMoved), which is why analog-stick navigation always worked while D-pad/face
    // buttons and analog triggers silently did nothing. Subscribing a no-op handler per gamepad
    // instance forces Silk to keep the arrays live; a reference-equality set keyed by instance
    // (not index) means a reconnect's new IGamepad gets re-subscribed automatically, and repeated
    // per-frame Apply calls don't pile up duplicate handlers.
    static readonly HashSet<IGamepad> _forceUpdatedGamepads = new(ReferenceEqualityComparer.Instance);

    static void EnsureButtonAndTriggerEventsAreLive(IGamepad silkGamepad)
    {
        if (_forceUpdatedGamepads.Add(silkGamepad))
        {
            silkGamepad.ButtonDown += static (_, _) => { };
            silkGamepad.ButtonUp += static (_, _) => { };
            silkGamepad.TriggerMoved += static (_, _) => { };
        }
    }

    /// <summary>
    /// Pushes the current state of <paramref name="silkGamepad"/> into <paramref name="gamepad"/>
    /// via its driver-facing setters and commits the frame with <see cref="GamePad.Activity"/>.
    /// </summary>
    public static void Apply(GamePad gamepad, IGamepad silkGamepad, double time)
    {
        EnsureButtonAndTriggerEventsAreLive(silkGamepad);

        gamepad.SetConnected(silkGamepad.IsConnected);

        gamepad.SetButtonState(GumGamepadButton.DPadUp, IsDown(silkGamepad, ButtonName.DPadUp));
        gamepad.SetButtonState(GumGamepadButton.DPadDown, IsDown(silkGamepad, ButtonName.DPadDown));
        gamepad.SetButtonState(GumGamepadButton.DPadLeft, IsDown(silkGamepad, ButtonName.DPadLeft));
        gamepad.SetButtonState(GumGamepadButton.DPadRight, IsDown(silkGamepad, ButtonName.DPadRight));

        gamepad.SetButtonState(GumGamepadButton.A, IsDown(silkGamepad, ButtonName.A));
        gamepad.SetButtonState(GumGamepadButton.B, IsDown(silkGamepad, ButtonName.B));
        gamepad.SetButtonState(GumGamepadButton.X, IsDown(silkGamepad, ButtonName.X));
        gamepad.SetButtonState(GumGamepadButton.Y, IsDown(silkGamepad, ButtonName.Y));

        gamepad.SetButtonState(GumGamepadButton.LeftShoulder, IsDown(silkGamepad, ButtonName.LeftBumper));
        gamepad.SetButtonState(GumGamepadButton.RightShoulder, IsDown(silkGamepad, ButtonName.RightBumper));

        gamepad.SetButtonState(GumGamepadButton.Start, IsDown(silkGamepad, ButtonName.Start));
        gamepad.SetButtonState(GumGamepadButton.Back, IsDown(silkGamepad, ButtonName.Back));

        gamepad.SetButtonState(GumGamepadButton.LeftStick, IsDown(silkGamepad, ButtonName.LeftStick));
        gamepad.SetButtonState(GumGamepadButton.RightStick, IsDown(silkGamepad, ButtonName.RightStick));

        // Silk.NET's SDL/GLFW backends both fix Triggers[0] = left, Triggers[1] = right.
        gamepad.SetButtonState(GumGamepadButton.LeftTrigger, TriggerPosition(silkGamepad, 0) >= TriggerThreshold);
        gamepad.SetButtonState(GumGamepadButton.RightTrigger, TriggerPosition(silkGamepad, 1) >= TriggerThreshold);

        // Silk.NET's SDL/GLFW backends both fix Thumbsticks[0] = left, Thumbsticks[1] = right, and
        // report Y as positive-down (SDL native convention); flip to the XNA/Gum convention
        // (positive-up), matching the Raylib driver.
        Thumbstick left = Stick(silkGamepad, 0);
        gamepad.SetLeftStickPosition(left.X, -left.Y);

        Thumbstick right = Stick(silkGamepad, 1);
        gamepad.SetRightStickPosition(right.X, -right.Y);

        gamepad.Activity(time);
    }

    static bool IsDown(IGamepad silkGamepad, ButtonName name) =>
        silkGamepad.Buttons.FirstOrDefault(b => b.Name == name).Pressed;

    static float TriggerPosition(IGamepad silkGamepad, int index) =>
        index < silkGamepad.Triggers.Count ? silkGamepad.Triggers[index].Position : 0f;

    static Thumbstick Stick(IGamepad silkGamepad, int index) =>
        index < silkGamepad.Thumbsticks.Count ? silkGamepad.Thumbsticks[index] : default;
}
