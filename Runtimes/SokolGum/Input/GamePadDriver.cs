namespace Gum.Input;

/// <summary>
/// No-op gamepad driver for the Sokol platform. Unlike <see cref="Keyboard"/> and
/// <see cref="Cursor"/>, which read events the host forwards from its sokol_app callback,
/// sokol_app exposes no gamepad/controller input API at all -- there is no event stream to
/// forward. This driver exists so <c>FormsUtilities.UpdateGamepads</c> can dispatch through
/// the same <c>GamePadDriver.Apply(GamePad, int, double)</c> shape as MonoGame/Raylib without
/// a closed #if switch, and intentionally leaves every gamepad disconnected/neutral. Real
/// Sokol gamepad support needs a separate native input source (e.g. platform-specific
/// XInput/SDL2 bindings) -- not attempted here (issue #3559).
/// </summary>
internal static class GamePadDriver
{
    public static void Apply(GamePad gamepad, int index, double time)
    {
        gamepad.SetConnected(false);
        gamepad.Activity(time);
    }
}
