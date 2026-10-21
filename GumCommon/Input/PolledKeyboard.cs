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
public abstract class PolledKeyboard : IInputReceiverKeyboard, IDisposable
{
    private readonly HashSet<GumKeys> _currentDown = new();
    private readonly HashSet<GumKeys> _previousDown = new();

    private readonly KeyRepeatTimer _repeatTimer = new();
    private double _currentGameTime;

    // Typed text accrues from host events as they arrive (before Update). Activity snapshots and
    // clears it so the frame's DoKeyboardAction reads exactly this frame's input.
    private readonly StringBuilder _charsTyped = new();
    private string _frameChars = "";

    // Undoes the host event subscriptions registered with OnDispose.
    private Action? _unsubscribe;

    /// <summary>
    /// Delay after the initial key press before repeat typing begins.
    /// </summary>
    public TimeSpan RepeatDelay { get; set; } = KeyRepeatTimer.DefaultDelay;

    /// <summary>
    /// Interval between repeated key-typed events while a key is held down, once
    /// <see cref="RepeatDelay"/> has elapsed.
    /// </summary>
    public TimeSpan RepeatRate { get; set; } = KeyRepeatTimer.DefaultRate;

    /// <summary>
    /// The Gum keys this host can report. <see cref="Activity"/> polls only these, and
    /// <see cref="IInputReceiverKeyboard.KeysTyped"/> yields them in this order.
    /// </summary>
    protected abstract IEnumerable<GumKeys> SupportedKeys { get; }

    /// <summary>
    /// Called once at the start of each <see cref="Activity"/>, before any <see cref="IsDeviceKeyDown"/>
    /// query. A host whose device is read as one snapshot (a keyboard-state struct, say) reads it here
    /// instead of once per key.
    /// </summary>
    protected virtual void BeginPoll()
    {
    }

    /// <summary>
    /// Returns whether the host's device currently reports <paramref name="key"/> as held.
    /// Called for each of <see cref="SupportedKeys"/> during <see cref="Activity"/>.
    /// </summary>
    protected abstract bool IsDeviceKeyDown(GumKeys key);

    /// <summary>
    /// Registers what <see cref="Dispose"/> runs to undo a host event subscription, such as
    /// <c>window.TextInput -= handler</c>. A host that subscribes in its constructor registers the
    /// matching unsubscribe here, so the keyboard stops receiving text once it is replaced.
    /// </summary>
    protected void OnDispose(Action unsubscribe) => _unsubscribe += unsubscribe;

    /// <summary>
    /// Undoes the host event subscriptions. <c>GumService.Uninitialize</c> and a second
    /// <c>Initialize</c> call this on the keyboard the service created.
    /// </summary>
    public void Dispose()
    {
        _unsubscribe?.Invoke();
        _unsubscribe = null;
    }

    /// <summary>
    /// Adds text the host received from its text-input event. It is returned by
    /// <see cref="GetStringTyped"/> after the next <see cref="Activity"/>. Control characters 0-29 are
    /// dropped, as MonoGame's keyboard does: some hosts report Backspace, Enter and Ctrl+letter (Ctrl+A
    /// is 1) as characters, and TextBox handles those through the keys.
    /// </summary>
    protected void AppendTypedText(string text)
    {
        foreach (char character in text)
        {
            AppendTypedText(character);
        }
    }

    /// <summary>
    /// Adds one typed character. See <see cref="AppendTypedText(string)"/>.
    /// </summary>
    protected void AppendTypedText(char character)
    {
        if (character > LastIgnoredControlCharacter)
        {
            _charsTyped.Append(character);
        }
    }

    private const char LastIgnoredControlCharacter = (char)29;

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

        return KeyDown(key) && _repeatTimer.IsRepeatDue(key, _currentGameTime, RepeatDelay, RepeatRate);
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

        BeginPoll();

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
                _repeatTimer.OnKeyPushed(key, gameTime);
            }
        }

        foreach (GumKeys key in _previousDown)
        {
            if (!_currentDown.Contains(key))
            {
                _repeatTimer.OnKeyReleased(key);
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
