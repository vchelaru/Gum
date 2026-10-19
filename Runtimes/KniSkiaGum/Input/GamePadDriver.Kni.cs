using System;
using XnaButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using XnaGamePad = Microsoft.Xna.Framework.Input.GamePad;
using XnaGamePadState = Microsoft.Xna.Framework.Input.GamePadState;

namespace Gum.Input;

/// <summary>
/// Reads a KNI <c>GamePadState</c> and pushes it into the platform-neutral <see cref="GamePad"/>
/// holder through <see cref="GamePadSnapshot"/>, which owns the trigger threshold. KNI already uses
/// Gum's stick convention (Y positive up), so the sticks pass through.
/// </summary>
internal static class GamePadDriver
{
    /// <summary>
    /// Reads a gamepad's state. Replaced in tests: KNI's <c>GamePad.GetState</c> needs a platform
    /// package, which a unit-test process does not have.
    /// </summary>
    internal static Func<int, XnaGamePadState> ReadState { get; set; } = index => XnaGamePad.GetState(index);

    /// <summary>
    /// Fetches the state of gamepad <paramref name="index"/> and pushes it into <paramref name="gamepad"/>.
    /// </summary>
    public static void Apply(GamePad gamepad, int index, double time) =>
        Apply(gamepad, ReadState(index), time);

    /// <summary>
    /// Pushes <paramref name="state"/> into <paramref name="gamepad"/> and commits the frame. Takes the
    /// state as a parameter so tests can drive it without a device.
    /// </summary>
    internal static void Apply(GamePad gamepad, XnaGamePadState state, double time)
    {
        gamepad.SetState(new GamePadSnapshot
        {
            IsConnected = state.IsConnected,

            DPadUp = IsDown(state.DPad.Up),
            DPadDown = IsDown(state.DPad.Down),
            DPadLeft = IsDown(state.DPad.Left),
            DPadRight = IsDown(state.DPad.Right),

            A = IsDown(state.Buttons.A),
            B = IsDown(state.Buttons.B),
            X = IsDown(state.Buttons.X),
            Y = IsDown(state.Buttons.Y),

            LeftShoulder = IsDown(state.Buttons.LeftShoulder),
            RightShoulder = IsDown(state.Buttons.RightShoulder),
            Start = IsDown(state.Buttons.Start),
            Back = IsDown(state.Buttons.Back),
            LeftStick = IsDown(state.Buttons.LeftStick),
            RightStick = IsDown(state.Buttons.RightStick),

            LeftTrigger = state.Triggers.Left,
            RightTrigger = state.Triggers.Right,

            LeftStickX = state.ThumbSticks.Left.X,
            LeftStickY = state.ThumbSticks.Left.Y,
            RightStickX = state.ThumbSticks.Right.X,
            RightStickY = state.ThumbSticks.Right.Y,
        });

        gamepad.Activity(time);
    }

    private static bool IsDown(XnaButtonState state) => state == XnaButtonState.Pressed;
}
