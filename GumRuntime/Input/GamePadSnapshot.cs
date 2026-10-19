namespace Gum.Input;

/// <summary>
/// One gamepad's state for a frame as plain values, in Gum's conventions. A host translates its
/// device into this and passes it to <see cref="GamePad.SetState"/>, which owns the mapping onto
/// Gum's buttons and sticks.
/// </summary>
public struct GamePadSnapshot
{
    /// <summary>
    /// Triggers at or past this value count as the digital <see cref="GamepadButton.LeftTrigger"/> /
    /// <see cref="GamepadButton.RightTrigger"/>.
    /// </summary>
    public const float TriggerThreshold = 0.5f;

    /// <summary>Whether the controller is connected.</summary>
    public bool IsConnected;

    public bool DPadUp;
    public bool DPadDown;
    public bool DPadLeft;
    public bool DPadRight;

    /// <summary>The bottom face button on an Xbox layout.</summary>
    public bool A;
    /// <summary>The right face button on an Xbox layout.</summary>
    public bool B;
    /// <summary>The left face button on an Xbox layout.</summary>
    public bool X;
    /// <summary>The top face button on an Xbox layout.</summary>
    public bool Y;

    public bool LeftShoulder;
    public bool RightShoulder;
    public bool Start;
    public bool Back;

    /// <summary>Whether the left stick is clicked in.</summary>
    public bool LeftStick;
    /// <summary>Whether the right stick is clicked in.</summary>
    public bool RightStick;

    /// <summary>Analog left trigger, 0 to 1.</summary>
    public float LeftTrigger;
    /// <summary>Analog right trigger, 0 to 1.</summary>
    public float RightTrigger;

    /// <summary>Left stick X, -1 left to +1 right.</summary>
    public float LeftStickX;
    /// <summary>Left stick Y, -1 down to +1 up.</summary>
    public float LeftStickY;
    /// <summary>Right stick X, -1 left to +1 right.</summary>
    public float RightStickX;
    /// <summary>Right stick Y, -1 down to +1 up.</summary>
    public float RightStickY;
}
