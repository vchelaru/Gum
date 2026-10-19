using Stride.Input;

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
        GamePadState state = strideGamePad.State;

        // Stride's LeftThumb/RightThumb are already XNA/Gum-convention (Y positive-up), unlike
        // Silk's SDL/GLFW backends which report Y positive-down.
        gamepad.SetState(new GamePadSnapshot
        {
            IsConnected = true,

            DPadUp = (state.Buttons & GamePadButton.PadUp) != 0,
            DPadDown = (state.Buttons & GamePadButton.PadDown) != 0,
            DPadLeft = (state.Buttons & GamePadButton.PadLeft) != 0,
            DPadRight = (state.Buttons & GamePadButton.PadRight) != 0,

            A = (state.Buttons & GamePadButton.A) != 0,
            B = (state.Buttons & GamePadButton.B) != 0,
            X = (state.Buttons & GamePadButton.X) != 0,
            Y = (state.Buttons & GamePadButton.Y) != 0,

            LeftShoulder = (state.Buttons & GamePadButton.LeftShoulder) != 0,
            RightShoulder = (state.Buttons & GamePadButton.RightShoulder) != 0,
            Start = (state.Buttons & GamePadButton.Start) != 0,
            Back = (state.Buttons & GamePadButton.Back) != 0,
            LeftStick = (state.Buttons & GamePadButton.LeftThumb) != 0,
            RightStick = (state.Buttons & GamePadButton.RightThumb) != 0,

            LeftTrigger = state.LeftTrigger,
            RightTrigger = state.RightTrigger,

            LeftStickX = state.LeftThumb.X,
            LeftStickY = state.LeftThumb.Y,
            RightStickX = state.RightThumb.X,
            RightStickY = state.RightThumb.Y,
        });

        gamepad.Activity(time);
    }
}
