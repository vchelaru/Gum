namespace Gum.Input;

/// <summary>
/// Platform-neutral abstraction over a gamepad. Implementations live in each runtime
/// (MonoGame's <c>MonoGameGum.Input.GamePad</c>, the headless stub in
/// <see cref="GamePad"/>) so code in <c>GumCommon</c> can reference gamepads
/// without depending on platform-typed APIs.
/// </summary>
public interface IGamePad
{
    /// <summary>
    /// Returns whether the specified button is currently held down.
    /// </summary>
    bool ButtonDown(GamepadButton button);

    /// <summary>
    /// Returns whether the specified button was pushed this frame (down this frame, not down last frame).
    /// </summary>
    bool ButtonPushed(GamepadButton button);

    /// <summary>
    /// Returns whether the specified button was released this frame (down last frame, not down this frame).
    /// </summary>
    bool ButtonReleased(GamepadButton button);

    /// <summary>
    /// Returns whether the specified button was pushed this frame, or has been held
    /// long enough to trigger key-repeat semantics.
    /// </summary>
    bool ButtonRepeatRate(GamepadButton button);

    /// <summary>
    /// The left analog stick. Always non-null even if the gamepad has no physical analog stick.
    /// </summary>
    IAnalogStick LeftStick { get; }
}

/// <summary>
/// Platform-neutral abstraction over an analog stick. Implementations live in each
/// runtime (<c>MonoGameGum.Input.AnalogStick</c>, the headless stub in
/// <see cref="AnalogStick"/>) so code in <c>GumCommon</c> can read DPad-like
/// directional input without depending on platform-typed math types.
/// </summary>
public interface IAnalogStick
{
    /// <summary>
    /// Returns whether the user has pushed the stick toward the specified direction this frame
    /// (similar to a DPad push — true once per push, not continuously while held).
    /// </summary>
    bool AsDPadPushed(DPadDirection direction);

    /// <summary>
    /// Returns whether the user pushed the stick toward the specified direction this frame,
    /// or has held it long enough to trigger key-repeat semantics.
    /// </summary>
    bool AsDPadPushedRepeatRate(DPadDirection direction);
}
