namespace Gum.Input;

/// <summary>
/// Platform-neutral gamepad data holder. This class holds input state only — it
/// contains no platform-specific reading code. A platform driver (for example the
/// <c>#if RAYLIB</c> branch of <c>FormsUtilities.UpdateGamepads</c>) reads native
/// controller input each frame, pushes it in via <see cref="SetButtonState"/> /
/// <see cref="SetLeftStickPosition"/>, then calls <see cref="Activity"/> to advance
/// a frame. Forms code consumes the result through the <see cref="IGamePad"/> query
/// API and never needs to know how the data was populated.
/// </summary>
public class GamePad : IGamePad
{
    // GamepadButton values are single-bit flags (powers of two), so each maps to a
    // unique array index via its trailing-zero count. The largest value
    // (LeftThumbstickRight = 0x40000000) is bit 30, so 31 slots cover every button.
    // This mirrors MonoGameGum.Input.GamePad's bitfield-to-index scheme.
    const int ButtonCount = 31;

    const double DefaultTimeAfterPush = 0.35;
    const double DefaultTimeBetweenRepeating = 0.12;

    // Buffer written by the setters during a frame, committed to _currentButtonDown
    // by Activity so the current/last transition (used for pushed/released) is atomic.
    bool[] _incomingButtonDown;
    bool[] _currentButtonDown;
    bool[] _lastButtonDown;

    double[] _lastButtonPush;
    double[] _lastButtonRepeatRate;

    double _currentTime;

    float _incomingLeftStickX;
    float _incomingLeftStickY;
    float _incomingRightStickX;
    float _incomingRightStickY;

    // Connection is pushed in by the driver each frame, then committed by Activity so
    // WasDisconnectedThisFrame can observe the current/last transition.
    bool _incomingConnected;
    bool _currentConnected;
    bool _lastConnected;

    AnalogStick _leftStick;
    AnalogStick _rightStick;

    /// <summary>
    /// The left analog stick. Always non-null even if the physical controller has no analog stick.
    /// </summary>
    public AnalogStick LeftStick => _leftStick;

    /// <summary>
    /// The right analog stick. Always non-null even if the physical controller has no analog stick.
    /// </summary>
    public AnalogStick RightStick => _rightStick;

    /// <inheritdoc/>
    IAnalogStick IGamePad.LeftStick => _leftStick;

    /// <inheritdoc/>
    IAnalogStick IGamePad.RightStick => _rightStick;

    /// <summary>
    /// Whether the controller was connected as of the last committed frame.
    /// </summary>
    public bool IsConnected => _currentConnected;

    /// <summary>
    /// Whether the controller was connected last frame but is not connected this frame.
    /// </summary>
    public bool WasDisconnectedThisFrame => _lastConnected && !_currentConnected;

    /// <summary>
    /// Creates a new gamepad with all inputs in their neutral (released / centered) state.
    /// </summary>
    public GamePad()
    {
        _incomingButtonDown = new bool[ButtonCount];
        _currentButtonDown = new bool[ButtonCount];
        _lastButtonDown = new bool[ButtonCount];
        _lastButtonPush = new double[ButtonCount];
        _lastButtonRepeatRate = new double[ButtonCount];
        _leftStick = new AnalogStick();
        _rightStick = new AnalogStick();
    }

    static int GetButtonIndex(GamepadButton button) =>
        System.Numerics.BitOperations.TrailingZeroCount((uint)button);

    #region Driver-facing setters

    /// <summary>
    /// Sets whether a button is held down for the current frame. Called by the platform
    /// input driver for every relevant button before <see cref="Activity"/> commits the frame.
    /// </summary>
    public void SetButtonState(GamepadButton button, bool isDown)
    {
        _incomingButtonDown[GetButtonIndex(button)] = isDown;
    }

    /// <summary>
    /// Sets the left analog stick position for the current frame, in the XNA/Gum convention
    /// (X: -1 left to +1 right, Y: -1 down to +1 up). Called by the platform input driver
    /// before <see cref="Activity"/> commits the frame.
    /// </summary>
    public void SetLeftStickPosition(float x, float y)
    {
        _incomingLeftStickX = x;
        _incomingLeftStickY = y;
    }

    /// <summary>
    /// Sets the right analog stick position for the current frame, in the XNA/Gum convention
    /// (X: -1 left to +1 right, Y: -1 down to +1 up). Called by the platform input driver
    /// before <see cref="Activity"/> commits the frame.
    /// </summary>
    public void SetRightStickPosition(float x, float y)
    {
        _incomingRightStickX = x;
        _incomingRightStickY = y;
    }

    /// <summary>
    /// Sets whether the controller is connected for the current frame. Called by the platform
    /// input driver before <see cref="Activity"/> commits the frame.
    /// </summary>
    public void SetConnected(bool isConnected)
    {
        _incomingConnected = isConnected;
    }

    /// <summary>
    /// Commits the state pushed in via the setters and advances repeat-rate timing.
    /// Called once per frame by the platform input driver after the setters.
    /// </summary>
    /// <param name="time">The total elapsed game time in seconds.</param>
    public void Activity(double time)
    {
        _currentTime = time;

        _lastConnected = _currentConnected;
        _currentConnected = _incomingConnected;

        for (int i = 0; i < ButtonCount; i++)
        {
            _lastButtonDown[i] = _currentButtonDown[i];
            _currentButtonDown[i] = _incomingButtonDown[i];
        }

        for (int i = 0; i < ButtonCount; i++)
        {
            if (_currentButtonDown[i] && !_lastButtonDown[i])
            {
                _lastButtonPush[i] = time;
            }
        }

        _leftStick.Update(_incomingLeftStickX, _incomingLeftStickY, time);
        _rightStick.Update(_incomingRightStickX, _incomingRightStickY, time);
    }

    /// <summary>
    /// Clears all transient input state (buttons, sticks, repeat timing) while preserving
    /// connection status and the <see cref="LeftStick"/>/<see cref="RightStick"/> references.
    /// Resets the current and last frames together so no spurious push/release is reported
    /// on the next <see cref="Activity"/>.
    /// </summary>
    public void Clear()
    {
        System.Array.Clear(_incomingButtonDown, 0, ButtonCount);
        System.Array.Clear(_currentButtonDown, 0, ButtonCount);
        System.Array.Clear(_lastButtonDown, 0, ButtonCount);
        System.Array.Clear(_lastButtonPush, 0, ButtonCount);
        System.Array.Clear(_lastButtonRepeatRate, 0, ButtonCount);

        _incomingLeftStickX = 0;
        _incomingLeftStickY = 0;
        _incomingRightStickX = 0;
        _incomingRightStickY = 0;

        // Preserve connection: collapse last/incoming onto current so IsConnected is
        // unchanged and WasDisconnectedThisFrame is false after the clear.
        _incomingConnected = _currentConnected;
        _lastConnected = _currentConnected;

        _currentTime = 0;

        _leftStick.Clear();
        _rightStick.Clear();
    }

    #endregion

    #region IGamePad query API

    /// <inheritdoc/>
    public bool ButtonDown(GamepadButton button)
    {
        if (TryGetLeftStickDirection(button, out DPadDirection direction))
        {
            return _leftStick.AsDPadDown(direction);
        }
        return _currentButtonDown[GetButtonIndex(button)];
    }

    /// <inheritdoc/>
    public bool ButtonPushed(GamepadButton button)
    {
        if (TryGetLeftStickDirection(button, out DPadDirection direction))
        {
            return _leftStick.AsDPadPushed(direction);
        }
        int index = GetButtonIndex(button);
        return _currentButtonDown[index] && !_lastButtonDown[index];
    }

    /// <inheritdoc/>
    public bool ButtonReleased(GamepadButton button)
    {
        int index = GetButtonIndex(button);
        return !_currentButtonDown[index] && _lastButtonDown[index];
    }

    /// <inheritdoc/>
    public bool ButtonRepeatRate(GamepadButton button) =>
        ButtonRepeatRate(button, DefaultTimeAfterPush, DefaultTimeBetweenRepeating);

    /// <summary>
    /// Returns whether the button was pushed this frame, or has been held long enough to
    /// trigger a key-repeat with the supplied timing.
    /// </summary>
    /// <param name="button">The button to test.</param>
    /// <param name="timeAfterPush">Seconds to wait after the initial push before the first repeat.</param>
    /// <param name="timeBetweenRepeating">Seconds between repeats once repeating has started.</param>
    public bool ButtonRepeatRate(GamepadButton button, double timeAfterPush, double timeBetweenRepeating)
    {
        if (ButtonPushed(button))
        {
            return true;
        }

        int index = GetButtonIndex(button);

        // If called multiple times in one frame, keep returning true for the rest of the frame.
        bool repeatedThisFrame = _currentTime > 0 && _lastButtonRepeatRate[index] == _currentTime;

        if (repeatedThisFrame ||
            (ButtonDown(button) &&
             _currentTime - _lastButtonPush[index] > timeAfterPush &&
             _currentTime - _lastButtonRepeatRate[index] > timeBetweenRepeating))
        {
            _lastButtonRepeatRate[index] = _currentTime;
            return true;
        }

        return false;
    }

    #endregion

    // The four LeftThumbstick* buttons report the analog stick as if it were a DPad,
    // matching MonoGameGum.Input.GamePad. All other buttons read the button array.
    static bool TryGetLeftStickDirection(GamepadButton button, out DPadDirection direction)
    {
        switch (button)
        {
            case GamepadButton.LeftThumbstickUp: direction = DPadDirection.Up; return true;
            case GamepadButton.LeftThumbstickDown: direction = DPadDirection.Down; return true;
            case GamepadButton.LeftThumbstickLeft: direction = DPadDirection.Left; return true;
            case GamepadButton.LeftThumbstickRight: direction = DPadDirection.Right; return true;
            default: direction = DPadDirection.Up; return false;
        }
    }
}

/// <summary>
/// Platform-neutral analog stick data holder. Stores the stick position pushed in by a
/// platform driver and emulates DPad-style directional presses (with hysteresis and
/// key-repeat) for UI navigation. Contains no platform-specific reading code.
/// </summary>
public class AnalogStick : IAnalogStick
{
    // The stick must pass DPadOnValue to register a direction, and must fall back under
    // DPadOffValue to release it. The gap prevents rapid on/off chatter when the user
    // holds the stick near the threshold. Mirrors MonoGameGum.Input.AnalogStick.
    const float DPadOnValue = 0.55f;
    const float DPadOffValue = 0.45f;

    const double DefaultTimeAfterPush = 0.35;
    const double DefaultTimeBetweenRepeating = 0.12;

    /// <summary>
    /// Below this magnitude the stick is treated as centered, to ignore resting drift.
    /// Defaults to 0.1; set to 0 to disable deadzone processing entirely.
    /// </summary>
    public float Deadzone { get; set; } = 0.1f;

    /// <summary>
    /// Whether the deadzone is applied to the stick's radial magnitude (<see cref="DeadzoneType.Radial"/>)
    /// or to each axis independently (<see cref="DeadzoneType.Cross"/>). Radial matches the behavior
    /// prior to the deadzone properties being introduced.
    /// </summary>
    public DeadzoneType DeadzoneType { get; set; } = DeadzoneType.Radial;

    /// <summary>
    /// How values past the deadzone are scaled back toward the edge. <see cref="DeadzoneInterpolationType.Instant"/>
    /// passes them through unchanged; Linear/Quadratic ramp from the deadzone edge for finer low-end control.
    /// </summary>
    public DeadzoneInterpolationType DeadzoneInterpolation { get; set; }

    /// <summary>
    /// Whether the post-deadzone position is clamped so its magnitude never exceeds 1.
    /// Recommended for top-down games.
    /// </summary>
    public bool IsMaxPositionNormalized { get; set; } = false;

    /// <inheritdoc/>
    public float X => _x;

    /// <inheritdoc/>
    public float Y => _y;

    float _x;
    float _y;
    double _currentTime;

    bool[] _lastDPadDown;
    double[] _lastDPadPush;
    double[] _lastDPadRepeatRate;

    /// <summary>
    /// Creates a new analog stick in its neutral (centered) state.
    /// </summary>
    public AnalogStick()
    {
        _lastDPadDown = new bool[4];
        // Start at -1 (not 0) so the first frame at time 0 is not mistaken for a repeat.
        _lastDPadPush = new double[4] { -1, -1, -1, -1 };
        _lastDPadRepeatRate = new double[4] { -1, -1, -1, -1 };
    }

    /// <summary>
    /// Returns whether the stick is currently pushed far enough toward the given direction
    /// to count as a DPad press (with on/off hysteresis).
    /// </summary>
    public bool AsDPadDown(DPadDirection direction)
    {
        switch (direction)
        {
            case DPadDirection.Left:
                return _lastDPadDown[(int)direction] ? _x < -DPadOffValue : _x < -DPadOnValue;
            case DPadDirection.Right:
                return _lastDPadDown[(int)direction] ? _x > DPadOffValue : _x > DPadOnValue;
            case DPadDirection.Up:
                return _lastDPadDown[(int)direction] ? _y > DPadOffValue : _y > DPadOnValue;
            case DPadDirection.Down:
                return _lastDPadDown[(int)direction] ? _y < -DPadOffValue : _y < -DPadOnValue;
            default:
                return false;
        }
    }

    /// <inheritdoc/>
    public bool AsDPadPushed(DPadDirection direction) =>
        !_lastDPadDown[(int)direction] && AsDPadDown(direction);

    /// <inheritdoc/>
    public bool AsDPadPushedRepeatRate(DPadDirection direction) =>
        AsDPadPushedRepeatRate(direction, DefaultTimeAfterPush, DefaultTimeBetweenRepeating);

    /// <summary>
    /// Returns whether the stick was pushed toward the direction this frame, or has been held
    /// there long enough to trigger a key-repeat with the supplied timing.
    /// </summary>
    public bool AsDPadPushedRepeatRate(DPadDirection direction, double timeAfterPush, double timeBetweenRepeating)
    {
        if (AsDPadPushed(direction))
        {
            return true;
        }

        bool repeatedThisFrame = _currentTime > 0 && _lastDPadPush[(int)direction] == _currentTime;

        if (repeatedThisFrame ||
            (AsDPadDown(direction) &&
             _currentTime - _lastDPadPush[(int)direction] > timeAfterPush &&
             _currentTime - _lastDPadRepeatRate[(int)direction] > timeBetweenRepeating))
        {
            _lastDPadRepeatRate[(int)direction] = _currentTime;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Commits the stick position for the current frame. Called by <see cref="GamePad.Activity"/>.
    /// Position is in the XNA/Gum convention (X: -1 left to +1 right, Y: -1 down to +1 up).
    /// </summary>
    public void Update(float x, float y, double time)
    {
        _currentTime = time;

        if (Deadzone > 0)
        {
            switch (DeadzoneType)
            {
                case DeadzoneType.Radial:
                    ApplyRadialDeadzone(ref x, ref y);
                    break;
                case DeadzoneType.Cross:
                    ApplyCrossDeadzone(ref x, ref y);
                    break;
            }
        }

        if (IsMaxPositionNormalized && (x * x) + (y * y) > 1)
        {
            Normalize(ref x, ref y);
        }

        // Capture the previous down-state (using the previous position) before moving to the
        // new position, so AsDPadPushed can detect the off-to-on transition this frame.
        _lastDPadDown[(int)DPadDirection.Up] = AsDPadDown(DPadDirection.Up);
        _lastDPadDown[(int)DPadDirection.Down] = AsDPadDown(DPadDirection.Down);
        _lastDPadDown[(int)DPadDirection.Left] = AsDPadDown(DPadDirection.Left);
        _lastDPadDown[(int)DPadDirection.Right] = AsDPadDown(DPadDirection.Right);

        _x = x;
        _y = y;

        for (int i = 0; i < 4; i++)
        {
            if (AsDPadPushed((DPadDirection)i))
            {
                _lastDPadPush[i] = time;
            }
        }
    }

    void ApplyRadialDeadzone(ref float x, ref float y)
    {
        float lengthSquared = (x * x) + (y * y);
        if (lengthSquared < Deadzone * Deadzone)
        {
            x = 0;
            y = 0;
            return;
        }

        if (DeadzoneInterpolation == DeadzoneInterpolationType.Instant)
        {
            return;
        }

        float length = (float)System.Math.Sqrt(lengthSquared);
        float range = 1 - Deadzone;
        float ratio = (length - Deadzone) / range;
        if (DeadzoneInterpolation == DeadzoneInterpolationType.Quadratic)
        {
            ratio = EaseIn(ratio);
        }

        NormalizeOrRight(ref x, ref y);
        x *= ratio;
        y *= ratio;
    }

    void ApplyCrossDeadzone(ref float x, ref float y)
    {
        x = ApplyCrossDeadzoneToAxis(x);
        y = ApplyCrossDeadzoneToAxis(y);
    }

    float ApplyCrossDeadzoneToAxis(float value)
    {
        if (value < Deadzone && value > -Deadzone)
        {
            return 0;
        }

        switch (DeadzoneInterpolation)
        {
            case DeadzoneInterpolationType.Linear:
                {
                    float range = 1 - Deadzone;
                    float distanceBeyondDeadzone = System.Math.Abs(value) - Deadzone;
                    return System.Math.Sign(value) * (distanceBeyondDeadzone / range);
                }
            case DeadzoneInterpolationType.Quadratic:
                {
                    float range = 1 - Deadzone;
                    float distanceBeyondDeadzone = System.Math.Abs(value) - Deadzone;
                    return System.Math.Sign(value) * EaseIn(distanceBeyondDeadzone / range);
                }
            default: // Instant: pass the value through unchanged.
                return value;
        }
    }

    static void Normalize(ref float x, ref float y)
    {
        float length = (float)System.Math.Sqrt((x * x) + (y * y));
        if (length != 0)
        {
            x /= length;
            y /= length;
        }
    }

    // Normalizes the (x, y) pair, falling back to a unit vector pointing right when the
    // input is zero (matching MonoGameGum.Input.AnalogStick's NormalizedOrRight).
    static void NormalizeOrRight(ref float x, ref float y)
    {
        if (x != 0 || y != 0)
        {
            Normalize(ref x, ref y);
        }
        else
        {
            x = 1;
            y = 0;
        }
    }

    // Quadratic ease-in over a unit duration: f(t) = t*t. Matches MonoGameGum.Input.AnalogStick.
    static float EaseIn(float ratio) => ratio * ratio;

    /// <summary>
    /// Resets the stick to its neutral (centered) state, clearing current and previous
    /// frames together so no spurious DPad push/release is reported on the next update.
    /// </summary>
    public void Clear()
    {
        _x = 0;
        _y = 0;
        _currentTime = 0;

        System.Array.Clear(_lastDPadDown, 0, _lastDPadDown.Length);
        for (int i = 0; i < _lastDPadPush.Length; i++)
        {
            _lastDPadPush[i] = -1;
            _lastDPadRepeatRate[i] = -1;
        }
    }
}

public enum GamepadButton
{
    //
    // Summary:
    //     Directional pad up.
    DPadUp = 1,
    //
    // Summary:
    //     Directional pad down.
    DPadDown = 2,
    //
    // Summary:
    //     Directional pad left.
    DPadLeft = 4,
    //
    // Summary:
    //     Directional pad right.
    DPadRight = 8,
    //
    // Summary:
    //     START button.
    Start = 0x10,
    //
    // Summary:
    //     BACK button.
    Back = 0x20,
    //
    // Summary:
    //     Left stick button (pressing the left stick).
    LeftStick = 0x40,
    //
    // Summary:
    //     Right stick button (pressing the right stick).
    RightStick = 0x80,
    //
    // Summary:
    //     Left bumper (shoulder) button.
    LeftShoulder = 0x100,
    //
    // Summary:
    //     Right bumper (shoulder) button.
    RightShoulder = 0x200,
    //
    // Summary:
    //     Big button.
    BigButton = 0x800,
    //
    // Summary:
    //     A button.
    A = 0x1000,
    //
    // Summary:
    //     B button.
    B = 0x2000,
    //
    // Summary:
    //     X button.
    X = 0x4000,
    //
    // Summary:
    //     Y button.
    Y = 0x8000,
    //
    // Summary:
    //     Left grip.
    LeftGrip = 0x80000,
    //
    // Summary:
    //     Right grip.
    RightGrip = 0x100000,
    //
    // Summary:
    //     Left stick is towards the left.
    LeftThumbstickLeft = 0x200000,
    //
    // Summary:
    //     Right trigger.
    RightTrigger = 0x400000,
    //
    // Summary:
    //     Left trigger.
    LeftTrigger = 0x800000,
    //
    // Summary:
    //     Right stick is towards up.
    RightThumbstickUp = 0x1000000,
    //
    // Summary:
    //     Right stick is towards down.
    RightThumbstickDown = 0x2000000,
    //
    // Summary:
    //     Right stick is towards the right.
    RightThumbstickRight = 0x4000000,
    //
    // Summary:
    //     Right stick is towards the left.
    RightThumbstickLeft = 0x8000000,
    //
    // Summary:
    //     Left stick is towards up.
    LeftThumbstickUp = 0x10000000,
    //
    // Summary:
    //     Left stick is towards down.
    LeftThumbstickDown = 0x20000000,
    //
    // Summary:
    //     Left stick is towards the right.
    LeftThumbstickRight = 0x40000000
}

public enum DPadDirection
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// How an <see cref="AnalogStick"/> applies its deadzone: against the stick's radial
/// magnitude, or to each axis independently.
/// </summary>
public enum DeadzoneType
{
    Radial = 0,
    //BoundingBox = 1, // Not currently supported
    Cross
}

/// <summary>
/// How an <see cref="AnalogStick"/> scales values that lie beyond the deadzone.
/// </summary>
public enum DeadzoneInterpolationType
{
    /// <summary>
    /// No interpolation is performed. Values less than the deadzone are set to 0; values past it pass through unchanged.
    /// </summary>
    Instant,
    /// <summary>
    /// Linear interpolation is performed for values greater than the deadzone.
    /// </summary>
    Linear,
    /// <summary>
    /// Quadratic (ease-in) interpolation is performed for values greater than the deadzone. This increases
    /// accuracy at lower values (closer to the deadzone) so small movements are easier to perform.
    /// </summary>
    Quadratic
}
