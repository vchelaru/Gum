using Gum.Wireframe;
using System.Collections.Generic;
using System.Text;
using static Sokol.SApp;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Input;

/// <summary>
/// Keyboard implementation for SokolGum.
/// </summary>
/// <remarks>
/// Sokol delivers keyboard input as a stream of events through the host's
/// <c>sokol_app</c> event callback — there is no live poll API. Host apps
/// forward each <see cref="sapp_event"/> to <see cref="HandleSokolEvent"/>
/// (directly, or via <c>GumService.HandleSokolEvent</c>), which populates
/// static per-frame buffers. <see cref="Activity"/> rolls those buffers
/// forward each frame to derive push/release state.
///
/// Thread-safety: the static buffers are not synchronized. This matches
/// MonoGame/Raylib — all three assume event intake and <see cref="Activity"/>
/// run on the same thread (the main/game thread). sokol_app delivers events
/// synchronously on the main thread by default, so this holds under normal
/// use. If a host app ever pumps Sokol events from a background thread,
/// add locking or double-buffering around the statics here.
/// </remarks>
public class Keyboard : IInputReceiverKeyboard
{
    #region Key translation tables

    /// <summary>
    /// Maps <see cref="GumKeys"/> (Gum/XNA key space) to <see cref="sapp_keycode"/>
    /// (Sokol key space). Keys present in Gum but not in Sokol (e.g. media/browser
    /// keys, Kana/Kanji/IME keys, Attn, Crsel, Exsel, Pa1, OemClear, ChatPad,
    /// Sleep, Select, Execute, VolumeUp/Down) are intentionally omitted —
    /// queries for those keys return <c>false</c>.
    /// </summary>
    private static readonly Dictionary<GumKeys, sapp_keycode> _gumToSokol = new()
    {
        { GumKeys.Back, sapp_keycode.SAPP_KEYCODE_BACKSPACE },
        { GumKeys.Tab, sapp_keycode.SAPP_KEYCODE_TAB },
        { GumKeys.Enter, sapp_keycode.SAPP_KEYCODE_ENTER },
        { GumKeys.Pause, sapp_keycode.SAPP_KEYCODE_PAUSE },
        { GumKeys.CapsLock, sapp_keycode.SAPP_KEYCODE_CAPS_LOCK },
        { GumKeys.Escape, sapp_keycode.SAPP_KEYCODE_ESCAPE },
        { GumKeys.Space, sapp_keycode.SAPP_KEYCODE_SPACE },
        { GumKeys.PageUp, sapp_keycode.SAPP_KEYCODE_PAGE_UP },
        { GumKeys.PageDown, sapp_keycode.SAPP_KEYCODE_PAGE_DOWN },
        { GumKeys.End, sapp_keycode.SAPP_KEYCODE_END },
        { GumKeys.Home, sapp_keycode.SAPP_KEYCODE_HOME },
        { GumKeys.Left, sapp_keycode.SAPP_KEYCODE_LEFT },
        { GumKeys.Up, sapp_keycode.SAPP_KEYCODE_UP },
        { GumKeys.Right, sapp_keycode.SAPP_KEYCODE_RIGHT },
        { GumKeys.Down, sapp_keycode.SAPP_KEYCODE_DOWN },
        { GumKeys.PrintScreen, sapp_keycode.SAPP_KEYCODE_PRINT_SCREEN },
        { GumKeys.Insert, sapp_keycode.SAPP_KEYCODE_INSERT },
        { GumKeys.Delete, sapp_keycode.SAPP_KEYCODE_DELETE },

        { GumKeys.D0, sapp_keycode.SAPP_KEYCODE_0 },
        { GumKeys.D1, sapp_keycode.SAPP_KEYCODE_1 },
        { GumKeys.D2, sapp_keycode.SAPP_KEYCODE_2 },
        { GumKeys.D3, sapp_keycode.SAPP_KEYCODE_3 },
        { GumKeys.D4, sapp_keycode.SAPP_KEYCODE_4 },
        { GumKeys.D5, sapp_keycode.SAPP_KEYCODE_5 },
        { GumKeys.D6, sapp_keycode.SAPP_KEYCODE_6 },
        { GumKeys.D7, sapp_keycode.SAPP_KEYCODE_7 },
        { GumKeys.D8, sapp_keycode.SAPP_KEYCODE_8 },
        { GumKeys.D9, sapp_keycode.SAPP_KEYCODE_9 },

        { GumKeys.A, sapp_keycode.SAPP_KEYCODE_A },
        { GumKeys.B, sapp_keycode.SAPP_KEYCODE_B },
        { GumKeys.C, sapp_keycode.SAPP_KEYCODE_C },
        { GumKeys.D, sapp_keycode.SAPP_KEYCODE_D },
        { GumKeys.E, sapp_keycode.SAPP_KEYCODE_E },
        { GumKeys.F, sapp_keycode.SAPP_KEYCODE_F },
        { GumKeys.G, sapp_keycode.SAPP_KEYCODE_G },
        { GumKeys.H, sapp_keycode.SAPP_KEYCODE_H },
        { GumKeys.I, sapp_keycode.SAPP_KEYCODE_I },
        { GumKeys.J, sapp_keycode.SAPP_KEYCODE_J },
        { GumKeys.K, sapp_keycode.SAPP_KEYCODE_K },
        { GumKeys.L, sapp_keycode.SAPP_KEYCODE_L },
        { GumKeys.M, sapp_keycode.SAPP_KEYCODE_M },
        { GumKeys.N, sapp_keycode.SAPP_KEYCODE_N },
        { GumKeys.O, sapp_keycode.SAPP_KEYCODE_O },
        { GumKeys.P, sapp_keycode.SAPP_KEYCODE_P },
        { GumKeys.Q, sapp_keycode.SAPP_KEYCODE_Q },
        { GumKeys.R, sapp_keycode.SAPP_KEYCODE_R },
        { GumKeys.S, sapp_keycode.SAPP_KEYCODE_S },
        { GumKeys.T, sapp_keycode.SAPP_KEYCODE_T },
        { GumKeys.U, sapp_keycode.SAPP_KEYCODE_U },
        { GumKeys.V, sapp_keycode.SAPP_KEYCODE_V },
        { GumKeys.W, sapp_keycode.SAPP_KEYCODE_W },
        { GumKeys.X, sapp_keycode.SAPP_KEYCODE_X },
        { GumKeys.Y, sapp_keycode.SAPP_KEYCODE_Y },
        { GumKeys.Z, sapp_keycode.SAPP_KEYCODE_Z },

        { GumKeys.LeftWindows, sapp_keycode.SAPP_KEYCODE_LEFT_SUPER },
        { GumKeys.RightWindows, sapp_keycode.SAPP_KEYCODE_RIGHT_SUPER },
        { GumKeys.Apps, sapp_keycode.SAPP_KEYCODE_MENU },

        { GumKeys.NumPad0, sapp_keycode.SAPP_KEYCODE_KP_0 },
        { GumKeys.NumPad1, sapp_keycode.SAPP_KEYCODE_KP_1 },
        { GumKeys.NumPad2, sapp_keycode.SAPP_KEYCODE_KP_2 },
        { GumKeys.NumPad3, sapp_keycode.SAPP_KEYCODE_KP_3 },
        { GumKeys.NumPad4, sapp_keycode.SAPP_KEYCODE_KP_4 },
        { GumKeys.NumPad5, sapp_keycode.SAPP_KEYCODE_KP_5 },
        { GumKeys.NumPad6, sapp_keycode.SAPP_KEYCODE_KP_6 },
        { GumKeys.NumPad7, sapp_keycode.SAPP_KEYCODE_KP_7 },
        { GumKeys.NumPad8, sapp_keycode.SAPP_KEYCODE_KP_8 },
        { GumKeys.NumPad9, sapp_keycode.SAPP_KEYCODE_KP_9 },
        { GumKeys.Multiply, sapp_keycode.SAPP_KEYCODE_KP_MULTIPLY },
        { GumKeys.Add, sapp_keycode.SAPP_KEYCODE_KP_ADD },
        { GumKeys.Subtract, sapp_keycode.SAPP_KEYCODE_KP_SUBTRACT },
        { GumKeys.Decimal, sapp_keycode.SAPP_KEYCODE_KP_DECIMAL },
        { GumKeys.Divide, sapp_keycode.SAPP_KEYCODE_KP_DIVIDE },

        { GumKeys.F1, sapp_keycode.SAPP_KEYCODE_F1 },
        { GumKeys.F2, sapp_keycode.SAPP_KEYCODE_F2 },
        { GumKeys.F3, sapp_keycode.SAPP_KEYCODE_F3 },
        { GumKeys.F4, sapp_keycode.SAPP_KEYCODE_F4 },
        { GumKeys.F5, sapp_keycode.SAPP_KEYCODE_F5 },
        { GumKeys.F6, sapp_keycode.SAPP_KEYCODE_F6 },
        { GumKeys.F7, sapp_keycode.SAPP_KEYCODE_F7 },
        { GumKeys.F8, sapp_keycode.SAPP_KEYCODE_F8 },
        { GumKeys.F9, sapp_keycode.SAPP_KEYCODE_F9 },
        { GumKeys.F10, sapp_keycode.SAPP_KEYCODE_F10 },
        { GumKeys.F11, sapp_keycode.SAPP_KEYCODE_F11 },
        { GumKeys.F12, sapp_keycode.SAPP_KEYCODE_F12 },
        { GumKeys.F13, sapp_keycode.SAPP_KEYCODE_F13 },
        { GumKeys.F14, sapp_keycode.SAPP_KEYCODE_F14 },
        { GumKeys.F15, sapp_keycode.SAPP_KEYCODE_F15 },
        { GumKeys.F16, sapp_keycode.SAPP_KEYCODE_F16 },
        { GumKeys.F17, sapp_keycode.SAPP_KEYCODE_F17 },
        { GumKeys.F18, sapp_keycode.SAPP_KEYCODE_F18 },
        { GumKeys.F19, sapp_keycode.SAPP_KEYCODE_F19 },
        { GumKeys.F20, sapp_keycode.SAPP_KEYCODE_F20 },
        { GumKeys.F21, sapp_keycode.SAPP_KEYCODE_F21 },
        { GumKeys.F22, sapp_keycode.SAPP_KEYCODE_F22 },
        { GumKeys.F23, sapp_keycode.SAPP_KEYCODE_F23 },
        { GumKeys.F24, sapp_keycode.SAPP_KEYCODE_F24 },

        { GumKeys.NumLock, sapp_keycode.SAPP_KEYCODE_NUM_LOCK },
        { GumKeys.Scroll, sapp_keycode.SAPP_KEYCODE_SCROLL_LOCK },

        { GumKeys.LeftShift, sapp_keycode.SAPP_KEYCODE_LEFT_SHIFT },
        { GumKeys.RightShift, sapp_keycode.SAPP_KEYCODE_RIGHT_SHIFT },
        { GumKeys.LeftControl, sapp_keycode.SAPP_KEYCODE_LEFT_CONTROL },
        { GumKeys.RightControl, sapp_keycode.SAPP_KEYCODE_RIGHT_CONTROL },
        { GumKeys.LeftAlt, sapp_keycode.SAPP_KEYCODE_LEFT_ALT },
        { GumKeys.RightAlt, sapp_keycode.SAPP_KEYCODE_RIGHT_ALT },

        { GumKeys.OemSemicolon, sapp_keycode.SAPP_KEYCODE_SEMICOLON },
        { GumKeys.OemPlus, sapp_keycode.SAPP_KEYCODE_EQUAL },
        { GumKeys.OemComma, sapp_keycode.SAPP_KEYCODE_COMMA },
        { GumKeys.OemMinus, sapp_keycode.SAPP_KEYCODE_MINUS },
        { GumKeys.OemPeriod, sapp_keycode.SAPP_KEYCODE_PERIOD },
        { GumKeys.OemQuestion, sapp_keycode.SAPP_KEYCODE_SLASH },
        { GumKeys.OemTilde, sapp_keycode.SAPP_KEYCODE_GRAVE_ACCENT },
        { GumKeys.OemOpenBrackets, sapp_keycode.SAPP_KEYCODE_LEFT_BRACKET },
        { GumKeys.OemPipe, sapp_keycode.SAPP_KEYCODE_BACKSLASH },
        { GumKeys.OemCloseBrackets, sapp_keycode.SAPP_KEYCODE_RIGHT_BRACKET },
        { GumKeys.OemQuotes, sapp_keycode.SAPP_KEYCODE_APOSTROPHE },
        { GumKeys.OemBackslash, sapp_keycode.SAPP_KEYCODE_BACKSLASH },
    };

    /// <summary>
    /// Inverse of <see cref="_gumToSokol"/>, built for translating incoming
    /// Sokol key events to <see cref="GumKeys"/>. Entries where multiple Gum
    /// keys map to the same Sokol key (e.g. <see cref="GumKeys.OemPipe"/> and
    /// <see cref="GumKeys.OemBackslash"/> both map to
    /// <see cref="sapp_keycode.SAPP_KEYCODE_BACKSLASH"/>) resolve to the first
    /// Gum key encountered.
    /// </summary>
    private static readonly Dictionary<sapp_keycode, GumKeys> _sokolToGum = BuildSokolToGum();

    private static Dictionary<sapp_keycode, GumKeys> BuildSokolToGum()
    {
        Dictionary<sapp_keycode, GumKeys> result = new();
        foreach (KeyValuePair<GumKeys, sapp_keycode> pair in _gumToSokol)
        {
            if (!result.ContainsKey(pair.Value))
            {
                result[pair.Value] = pair.Key;
            }
        }
        return result;
    }

    #endregion

    #region Static event-driven state

    // Matches Cursor.Sokol.cs: all input state lives in statics populated by
    // HandleSokolEvent. The Keyboard instance is a thin interface-facing
    // veneer over these buffers.
    private static readonly HashSet<GumKeys> _keysDown = new();
    private static readonly HashSet<GumKeys> _lastFrameKeysDown = new();
    private static readonly HashSet<GumKeys> _keysPushedThisFrame = new();
    private static readonly List<GumKeys> _keysTypedThisFrame = new();
    private static readonly StringBuilder _charsTypedThisFrame = new();

    /// <summary>
    /// Forwards a Sokol app event into the Keyboard input buffer. Host apps should
    /// call this from their sokol_app event callback for every key / char event.
    /// </summary>
    public static void HandleSokolEvent(in sapp_event ev)
    {
        switch (ev.type)
        {
            case sapp_event_type.SAPP_EVENTTYPE_KEY_DOWN:
                if (_sokolToGum.TryGetValue(ev.key_code, out GumKeys downKey))
                {
                    if (!ev.key_repeat)
                    {
                        _keysDown.Add(downKey);
                        _keysPushedThisFrame.Add(downKey);
                        _keysTypedThisFrame.Add(downKey);
                    }
                    else
                    {
                        // Repeat events contribute to KeyTyped but not KeyPushed,
                        // matching the push-plus-repeat semantics other runtimes use.
                        _keysTypedThisFrame.Add(downKey);
                    }
                }
                break;
            case sapp_event_type.SAPP_EVENTTYPE_KEY_UP:
                if (_sokolToGum.TryGetValue(ev.key_code, out GumKeys upKey))
                {
                    _keysDown.Remove(upKey);
                }
                break;
            case sapp_event_type.SAPP_EVENTTYPE_CHAR:
                _charsTypedThisFrame.Append(char.ConvertFromUtf32((int)ev.char_code));
                break;
        }
    }

    #endregion

    /// <summary>
    /// Returns true if either the left or right shift key is currently pressed down.
    /// </summary>
    public bool IsShiftDown =>
        _keysDown.Contains(GumKeys.LeftShift) ||
        _keysDown.Contains(GumKeys.RightShift);

    /// <summary>
    /// Returns true if either the left or right control key is currently pressed down.
    /// </summary>
    public bool IsCtrlDown =>
        _keysDown.Contains(GumKeys.LeftControl) ||
        _keysDown.Contains(GumKeys.RightControl);

    /// <summary>
    /// Returns true if either the left or right alt key is currently pressed down.
    /// </summary>
    public bool IsAltDown =>
        _keysDown.Contains(GumKeys.LeftAlt) ||
        _keysDown.Contains(GumKeys.RightAlt);

    /// <inheritdoc/>
    IEnumerable<GumKeys> IInputReceiverKeyboard.KeysTyped => _keysTypedThisFrame;

    /// <inheritdoc/>
    public bool KeyDown(GumKeys key) => _keysDown.Contains(key);

    /// <inheritdoc/>
    public bool KeyPushed(GumKeys key) => _keysPushedThisFrame.Contains(key);

    /// <inheritdoc/>
    public bool KeyReleased(GumKeys key) =>
        _lastFrameKeysDown.Contains(key) && !_keysDown.Contains(key);

    /// <inheritdoc/>
    /// <remarks>
    /// True on initial press (non-repeat KEY_DOWN) and on OS-driven repeat
    /// (KEY_DOWN with <c>key_repeat == true</c>). Matches MonoGame/Raylib
    /// KeyTyped semantics closely enough for keyboard-driven Forms controls.
    /// </remarks>
    public bool KeyTyped(GumKeys key) => _keysTypedThisFrame.Contains(key);

    /// <summary>
    /// Performs every-frame activity for the keyboard. This is automatically called by Gum.
    /// </summary>
    /// <param name="gameTime">The number of seconds since the start of the game.</param>
    public void Activity(double gameTime)
    {
        _lastFrameKeysDown.Clear();
        foreach (GumKeys key in _keysDown)
        {
            _lastFrameKeysDown.Add(key);
        }
        _keysPushedThisFrame.Clear();
        _keysTypedThisFrame.Clear();
        _charsTypedThisFrame.Clear();
    }

    /// <summary>
    /// Retrieves a string containing all Unicode characters typed by the user since the last
    /// <see cref="Activity"/> call.
    /// </summary>
    /// <remarks>Characters are buffered from <see cref="sapp_event_type.SAPP_EVENTTYPE_CHAR"/>
    /// events forwarded through <see cref="HandleSokolEvent"/>. The buffer is cleared every
    /// frame in <see cref="Activity"/>, so callers should read this once per frame.</remarks>
    /// <returns>A string representing the sequence of characters typed by the user. Returns an
    /// empty string if no characters have been typed since the last frame boundary.</returns>
    public string GetStringTyped() => _charsTypedThisFrame.ToString();
}
