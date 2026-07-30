namespace Gum.Input;

/// <summary>
/// Platform-neutral abstraction over a gamepad, implemented by the shared
/// <see cref="GamePad"/> holder in <c>GumCommon</c>. Each platform feeds that holder
/// through an input driver (the MonoGame and Raylib branches of
/// <c>FormsUtilities.UpdateGamepads</c>) so code in <c>GumCommon</c> can reference
/// gamepads without depending on platform-typed APIs.
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

    /// <summary>
    /// The right analog stick. Always non-null even if the gamepad has no physical analog stick.
    /// </summary>
    IAnalogStick RightStick { get; }
}

/// <summary>
/// Platform-neutral abstraction over an analog stick, implemented by the shared
/// <see cref="AnalogStick"/> in <c>GumCommon</c>, so code in <c>GumCommon</c> can read
/// DPad-like directional input without depending on platform-typed math types.
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

    /// <summary>
    /// Returns whether the stick's radial magnitude (sqrt(x²+y²)) was pushed past the on-threshold
    /// this frame, or has held there long enough to trigger key-repeat semantics. Unlike
    /// <see cref="AsDPadPushedRepeatRate(DPadDirection)"/>, this is direction-agnostic — it fires
    /// at any angle. Read <see cref="X"/>/<see cref="Y"/> after this returns true for the angle.
    /// </summary>
    bool RadialPushedRepeatRate();

    /// <summary>
    /// The stick's horizontal position after deadzone processing, from -1 (left) to +1 (right).
    /// </summary>
    float X { get; }

    /// <summary>
    /// The stick's vertical position after deadzone processing, from -1 (down) to +1 (up).
    /// </summary>
    float Y { get; }
}
