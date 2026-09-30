using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Input;

/// <summary>
/// Base for a keyboard whose host can report which keys are down but not "pressed this frame" or
/// key repeat. <see cref="Activity"/> snapshots the down state once per frame to derive push and
/// release edges, times held-key repeat for <see cref="KeyTyped"/> the way MonoGame's keyboard does,
/// and latches the text the host appended with <see cref="AppendTypedText(string)"/>. A host supplies its key
/// map through <see cref="SupportedKeys"/> and <see cref="IsDeviceKeyDown"/>.
/// </summary>
public abstract class PolledKeyboard : IInputReceiverKeyboard
{
    private readonly HashSet<GumKeys> _currentDown = new();
    private readonly HashSet<GumKeys> _previousDown = new();

    private readonly Dictionary<GumKeys, double> _keyDownSince = new();
    private readonly Dictionary<GumKeys, double> _lastRepeatTime = new();
    private double _currentGameTime;

    // Typed text accrues from host events as they arrive (before Update). Activity snapshots and
    // clears it so the frame's DoKeyboardAction reads exactly this frame's input.
    private readonly StringBuilder _charsTyped = new();
    private string _frameChars = "";

    /// <summary>
    /// Delay after the initial key press before repeat typing begins.
    /// </summary>
    public TimeSpan RepeatDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Interval between repeated key-typed events while a key is held down, once
    /// <see cref="RepeatDelay"/> has elapsed.
    /// </summary>
    public TimeSpan RepeatRate { get; set; } = TimeSpan.FromMilliseconds(70);

    /// <summary>
    /// The Gum keys this host can report. <see cref="Activity"/> polls only these, and
    /// <see cref="IInputReceiverKeyboard.KeysTyped"/> yields them in this order.
    /// </summary>
    protected abstract IEnumerable<GumKeys> SupportedKeys { get; }

    /// <summary>
    /// Returns whether the host's device currently reports <paramref name="key"/> as held.
    /// Called for each of <see cref="SupportedKeys"/> during <see cref="Activity"/>.
    /// </summary>
    protected abstract bool IsDeviceKeyDown(GumKeys key);

    /// <summary>
    /// Adds text the host received from its text-input event. It is returned by
    /// <see cref="GetStringTyped"/> after the next <see cref="Activity"/>.
    /// </summary>
    protected void AppendTypedText(string text) => _charsTyped.Append(text);

    /// <summary>
    /// Adds one typed character. See <see cref="AppendTypedText(string)"/>.
    /// </summary>
    protected void AppendTypedText(char character) => _charsTyped.Append(character);

    /// <summary>
    /// Returns true if either the left or right shift key is currently pressed down.
    /// </summary>
    public bool IsShiftDown => KeyDown(GumKeys.LeftShift) || KeyDown(GumKeys.RightShift);

    /// <summary>
    /// Returns true if either the left or right control key is currently pressed down.
    /// </summary>
    public bool IsCtrlDown => KeyDown(GumKeys.LeftControl) || KeyDown(GumKeys.RightControl);

    /// <summary>
    /// Returns true if either Command key is held on macOS, where it is reported as a Windows key. Always false
    /// elsewhere, so the Windows key never triggers text shortcuts.
    /// </summary>
    public bool IsCommandDown => IsMacOS && (KeyDown(GumKeys.LeftWindows) || KeyDown(GumKeys.RightWindows));

    /// <summary>
    /// Returns true if either the left or right alt key is currently pressed down.
    /// </summary>
    public bool IsAltDown => KeyDown(GumKeys.LeftAlt) || KeyDown(GumKeys.RightAlt);

#if NET5_0_OR_GREATER
    private static bool IsMacOS => OperatingSystem.IsMacOS();
#else
    private static bool IsMacOS =>
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
#endif

    /// <inheritdoc/>
    IEnumerable<GumKeys> IInputReceiverKeyboard.KeysTyped => SupportedKeys.Where(KeyTyped);

    /// <inheritdoc/>
    public virtual bool KeyDown(GumKeys key) => _currentDown.Contains(key);

    /// <inheritdoc/>
    public virtual bool KeyPushed(GumKeys key) => _currentDown.Contains(key) && !_previousDown.Contains(key);

    /// <inheritdoc/>
    public virtual bool KeyReleased(GumKeys key) => !_currentDown.Contains(key) && _previousDown.Contains(key);

    /// <inheritdoc/>
    /// <remarks>
    /// Returns true on the initial press and again at <see cref="RepeatDelay"/>/<see cref="RepeatRate"/>
    /// intervals while the key is held. Character-producing input (including OS repeat) flows through
    /// <see cref="GetStringTyped"/> instead, which is what TextBox text entry consumes.
    /// </remarks>
    public bool KeyTyped(GumKeys key)
    {
        if (KeyPushed(key))
        {
            return true;
        }

        if (!KeyDown(key) || !_keyDownSince.TryGetValue(key, out double downSince))
        {
            return false;
        }

        double elapsedSincePush = _currentGameTime - downSince;
        if (elapsedSincePush < RepeatDelay.TotalSeconds)
        {
            return false;
        }

        if (_lastRepeatTime.TryGetValue(key, out double lastRepeat) &&
            _currentGameTime - lastRepeat < RepeatRate.TotalSeconds)
        {
            return false;
        }

        _lastRepeatTime[key] = _currentGameTime;
        return true;
    }

    /// <summary>
    /// Performs every-frame activity: rolls the down-state snapshot forward and repolls the device,
    /// updates repeat timing, then latches this frame's typed text. Called by Gum via
    /// FormsUtilities.Update.
    /// </summary>
    /// <param name="gameTime">The number of seconds since the start of the game.</param>
    public void Activity(double gameTime)
    {
        _currentGameTime = gameTime;

        _previousDown.Clear();
        _previousDown.UnionWith(_currentDown);

        _currentDown.Clear();
        foreach (GumKeys key in SupportedKeys)
        {
            if (IsDeviceKeyDown(key))
            {
                _currentDown.Add(key);
            }
        }

        foreach (GumKeys key in _currentDown)
        {
            if (!_previousDown.Contains(key))
            {
                _keyDownSince[key] = gameTime;
                _lastRepeatTime.Remove(key);
            }
        }

        foreach (GumKeys key in _previousDown)
        {
            if (!_currentDown.Contains(key))
            {
                _keyDownSince.Remove(key);
                _lastRepeatTime.Remove(key);
            }
        }

        _frameChars = _charsTyped.ToString();
        _charsTyped.Clear();
    }

    /// <summary>
    /// Retrieves the text typed since the previous <see cref="Activity"/>.
    /// </summary>
    /// <returns>The characters typed this frame, or an empty string if none.</returns>
    public string GetStringTyped() => _frameChars;
}
