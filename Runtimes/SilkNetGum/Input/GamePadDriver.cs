using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.Input;

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

        // Silk has no digital trigger buttons (ButtonName has no LeftTrigger/RightTrigger), so the
        // triggers go in as analog positions and GamePad.SetState thresholds them. Silk.NET's
        // SDL/GLFW backends both fix Triggers[0] = left, Triggers[1] = right, and
        // Thumbsticks[0] = left, Thumbsticks[1] = right.
        Thumbstick left = Stick(silkGamepad, 0);
        Thumbstick right = Stick(silkGamepad, 1);

        gamepad.SetState(new GamePadSnapshot
        {
            IsConnected = silkGamepad.IsConnected,

            DPadUp = IsDown(silkGamepad, ButtonName.DPadUp),
            DPadDown = IsDown(silkGamepad, ButtonName.DPadDown),
            DPadLeft = IsDown(silkGamepad, ButtonName.DPadLeft),
            DPadRight = IsDown(silkGamepad, ButtonName.DPadRight),

            A = IsDown(silkGamepad, ButtonName.A),
            B = IsDown(silkGamepad, ButtonName.B),
            X = IsDown(silkGamepad, ButtonName.X),
            Y = IsDown(silkGamepad, ButtonName.Y),

            LeftShoulder = IsDown(silkGamepad, ButtonName.LeftBumper),
            RightShoulder = IsDown(silkGamepad, ButtonName.RightBumper),
            Start = IsDown(silkGamepad, ButtonName.Start),
            Back = IsDown(silkGamepad, ButtonName.Back),
            LeftStick = IsDown(silkGamepad, ButtonName.LeftStick),
            RightStick = IsDown(silkGamepad, ButtonName.RightStick),

            LeftTrigger = TriggerPosition(silkGamepad, 0),
            RightTrigger = TriggerPosition(silkGamepad, 1),

            // Silk reports stick Y as positive-down (SDL native); flip to the XNA/Gum convention
            // (positive-up), matching the Raylib driver.
            LeftStickX = left.X,
            LeftStickY = -left.Y,
            RightStickX = right.X,
            RightStickY = -right.Y,
        });

        gamepad.Activity(time);
    }

    static bool IsDown(IGamepad silkGamepad, ButtonName name) =>
        silkGamepad.Buttons.FirstOrDefault(b => b.Name == name).Pressed;

    static float TriggerPosition(IGamepad silkGamepad, int index) =>
        index < silkGamepad.Triggers.Count ? silkGamepad.Triggers[index].Position : 0f;

    static Thumbstick Stick(IGamepad silkGamepad, int index) =>
        index < silkGamepad.Thumbsticks.Count ? silkGamepad.Thumbsticks[index] : default;
}
