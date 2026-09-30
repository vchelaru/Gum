using Stride.Input;
using System.Collections.Generic;
using GumKeys = Gum.Forms.Input.Keys;
using StrideKeys = Stride.Input.Keys;

namespace Gum.Input;

/// <summary>
/// Keyboard implementation for Stride. Down/pushed/released are read directly from
/// <see cref="IKeyboardDevice"/>'s per-frame sets, which Stride resets each
/// <c>InputManager.Update</c>, so a press and release within one frame still reports a push.
/// <see cref="PolledKeyboard"/> times held-key repeat (Stride has no repeat-rate poll) and latches
/// typed text, which arrives through a <see cref="TextInputEvent"/> listener.
/// </summary>
public class Keyboard : PolledKeyboard, IInputEventListener<TextInputEvent>
{
    private readonly IKeyboardDevice? _keyboard;

    /// <summary>
    /// Constructs a keyboard backed by the supplied Stride device (from
    /// <see cref="InputManager.Keyboards"/>). Registers with <paramref name="inputManager"/> for
    /// <see cref="TextInputEvent"/> typed-text capture, and enables text input on the device.
    /// </summary>
    public Keyboard(IKeyboardDevice keyboard, InputManager inputManager)
    {
        _keyboard = keyboard;
        inputManager.AddListener(this);
        inputManager.TextInput?.EnabledTextInput();
    }

    /// <summary>
    /// Device-less constructor. Used by <c>GumService.CreateKeyboard</c> for the degenerate case
    /// where the input manager exposes no keyboard (headless), so the Forms input pump still has a
    /// non-null keyboard to tick (all queries return false / empty text) rather than crashing on a
    /// null. Also used by unit tests to drive the translation table and edge detection through the
    /// <see cref="KeyDown"/>/<see cref="KeyPushed"/>/<see cref="KeyReleased"/> seams without a live
    /// Stride device.
    /// </summary>
    internal Keyboard()
    {
    }

    #region Key translation table

    /// <summary>
    /// Maps <see cref="GumKeys"/> (Gum/XNA key space) to <see cref="StrideKeys"/> (Stride key
    /// space). Keys present in Gum but not in Stride (media/browser keys map 1:1 actually; IME
    /// keys, Attn/Crsel/Exsel/Pa1, OemClear, F13-F24 exist in both -- omitted here are the handful
    /// Stride has no distinct member for) are intentionally omitted -- queries for those return
    /// <c>false</c>.
    /// </summary>
    private static readonly Dictionary<GumKeys, StrideKeys> _gumToStride = new()
    {
        { GumKeys.Back, StrideKeys.Back },
        { GumKeys.Tab, StrideKeys.Tab },
        { GumKeys.Enter, StrideKeys.Enter },
        { GumKeys.Pause, StrideKeys.Pause },
        { GumKeys.CapsLock, StrideKeys.CapsLock },
        { GumKeys.Escape, StrideKeys.Escape },
        { GumKeys.Space, StrideKeys.Space },
        { GumKeys.PageUp, StrideKeys.PageUp },
        { GumKeys.PageDown, StrideKeys.PageDown },
        { GumKeys.End, StrideKeys.End },
        { GumKeys.Home, StrideKeys.Home },
        { GumKeys.Left, StrideKeys.Left },
        { GumKeys.Up, StrideKeys.Up },
        { GumKeys.Right, StrideKeys.Right },
        { GumKeys.Down, StrideKeys.Down },
        { GumKeys.PrintScreen, StrideKeys.PrintScreen },
        { GumKeys.Insert, StrideKeys.Insert },
        { GumKeys.Delete, StrideKeys.Delete },

        { GumKeys.D0, StrideKeys.D0 },
        { GumKeys.D1, StrideKeys.D1 },
        { GumKeys.D2, StrideKeys.D2 },
        { GumKeys.D3, StrideKeys.D3 },
        { GumKeys.D4, StrideKeys.D4 },
        { GumKeys.D5, StrideKeys.D5 },
        { GumKeys.D6, StrideKeys.D6 },
        { GumKeys.D7, StrideKeys.D7 },
        { GumKeys.D8, StrideKeys.D8 },
        { GumKeys.D9, StrideKeys.D9 },

        { GumKeys.A, StrideKeys.A },
        { GumKeys.B, StrideKeys.B },
        { GumKeys.C, StrideKeys.C },
        { GumKeys.D, StrideKeys.D },
        { GumKeys.E, StrideKeys.E },
        { GumKeys.F, StrideKeys.F },
        { GumKeys.G, StrideKeys.G },
        { GumKeys.H, StrideKeys.H },
        { GumKeys.I, StrideKeys.I },
        { GumKeys.J, StrideKeys.J },
        { GumKeys.K, StrideKeys.K },
        { GumKeys.L, StrideKeys.L },
        { GumKeys.M, StrideKeys.M },
        { GumKeys.N, StrideKeys.N },
        { GumKeys.O, StrideKeys.O },
        { GumKeys.P, StrideKeys.P },
        { GumKeys.Q, StrideKeys.Q },
        { GumKeys.R, StrideKeys.R },
        { GumKeys.S, StrideKeys.S },
        { GumKeys.T, StrideKeys.T },
        { GumKeys.U, StrideKeys.U },
        { GumKeys.V, StrideKeys.V },
        { GumKeys.W, StrideKeys.W },
        { GumKeys.X, StrideKeys.X },
        { GumKeys.Y, StrideKeys.Y },
        { GumKeys.Z, StrideKeys.Z },

        { GumKeys.LeftWindows, StrideKeys.LeftWin },
        { GumKeys.RightWindows, StrideKeys.RightWin },
        { GumKeys.Apps, StrideKeys.Apps },

        { GumKeys.NumPad0, StrideKeys.NumPad0 },
        { GumKeys.NumPad1, StrideKeys.NumPad1 },
        { GumKeys.NumPad2, StrideKeys.NumPad2 },
        { GumKeys.NumPad3, StrideKeys.NumPad3 },
        { GumKeys.NumPad4, StrideKeys.NumPad4 },
        { GumKeys.NumPad5, StrideKeys.NumPad5 },
        { GumKeys.NumPad6, StrideKeys.NumPad6 },
        { GumKeys.NumPad7, StrideKeys.NumPad7 },
        { GumKeys.NumPad8, StrideKeys.NumPad8 },
        { GumKeys.NumPad9, StrideKeys.NumPad9 },
        { GumKeys.Multiply, StrideKeys.Multiply },
        { GumKeys.Add, StrideKeys.Add },
        { GumKeys.Subtract, StrideKeys.Subtract },
        { GumKeys.Decimal, StrideKeys.Decimal },
        { GumKeys.Divide, StrideKeys.Divide },

        { GumKeys.F1, StrideKeys.F1 },
        { GumKeys.F2, StrideKeys.F2 },
        { GumKeys.F3, StrideKeys.F3 },
        { GumKeys.F4, StrideKeys.F4 },
        { GumKeys.F5, StrideKeys.F5 },
        { GumKeys.F6, StrideKeys.F6 },
        { GumKeys.F7, StrideKeys.F7 },
        { GumKeys.F8, StrideKeys.F8 },
        { GumKeys.F9, StrideKeys.F9 },
        { GumKeys.F10, StrideKeys.F10 },
        { GumKeys.F11, StrideKeys.F11 },
        { GumKeys.F12, StrideKeys.F12 },

        { GumKeys.NumLock, StrideKeys.NumLock },
        { GumKeys.Scroll, StrideKeys.Scroll },

        { GumKeys.LeftShift, StrideKeys.LeftShift },
        { GumKeys.RightShift, StrideKeys.RightShift },
        { GumKeys.LeftControl, StrideKeys.LeftCtrl },
        { GumKeys.RightControl, StrideKeys.RightCtrl },
        { GumKeys.LeftAlt, StrideKeys.LeftAlt },
        { GumKeys.RightAlt, StrideKeys.RightAlt },

        { GumKeys.OemSemicolon, StrideKeys.OemSemicolon },
        { GumKeys.OemPlus, StrideKeys.OemPlus },
        { GumKeys.OemComma, StrideKeys.OemComma },
        { GumKeys.OemMinus, StrideKeys.OemMinus },
        { GumKeys.OemPeriod, StrideKeys.OemPeriod },
        { GumKeys.OemQuestion, StrideKeys.OemQuestion },
        { GumKeys.OemTilde, StrideKeys.OemTilde },
        { GumKeys.OemOpenBrackets, StrideKeys.OemOpenBrackets },
        { GumKeys.OemPipe, StrideKeys.OemPipe },
        { GumKeys.OemCloseBrackets, StrideKeys.OemCloseBrackets },
        { GumKeys.OemQuotes, StrideKeys.OemQuotes },
        { GumKeys.OemBackslash, StrideKeys.OemBackslash },
    };

    #endregion

    void IInputEventListener<TextInputEvent>.ProcessEvent(TextInputEvent inputEvent)
    {
        if (inputEvent.Type == TextInputEventType.Input)
        {
            AppendTypedText(inputEvent.Text);
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<GumKeys> SupportedKeys => _gumToStride.Keys;

    /// <inheritdoc/>
    protected override bool IsDeviceKeyDown(GumKeys key) => KeyDown(key);

    /// <inheritdoc/>
    public override bool KeyDown(GumKeys key) =>
        _gumToStride.TryGetValue(key, out var strideKey) && _keyboard?.DownKeys.Contains(strideKey) == true;

    /// <inheritdoc/>
    public override bool KeyPushed(GumKeys key) =>
        _gumToStride.TryGetValue(key, out var strideKey) && _keyboard?.PressedKeys.Contains(strideKey) == true;

    /// <inheritdoc/>
    public override bool KeyReleased(GumKeys key) =>
        _gumToStride.TryGetValue(key, out var strideKey) && _keyboard?.ReleasedKeys.Contains(strideKey) == true;
}
