using Microsoft.Xna.Framework.Input;
using MonoGameGum.Input;
using GamePad = Gum.Input.GamePad;

namespace MonoGameGum.Tests.Input;

/// <summary>
/// Test convenience that drives the platform-neutral <see cref="Gum.Input.GamePad"/> from an XNA
/// <see cref="GamePadState"/> through the production <see cref="GamePadDriver"/>, so the
/// gamepad characterization tests read like the retired <c>GamePad.Activity(GamePadState, time)</c>
/// API while actually exercising the real MonoGame input-driver mapping.
/// </summary>
internal static class GamePadTestDriverExtensions
{
    public static void Activity(this GamePad pad, GamePadState state, double time)
        => GamePadDriver.Apply(pad, state, time);
}
