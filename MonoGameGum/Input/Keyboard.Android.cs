#if ANDROID
using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace MonoGameGum.Input;

// The [SupportedOSPlatform("android21.0")] attributes on individual members below tell
// the CA1416 analyzer those methods are Android-only. The file is already inside #if
// ANDROID so at compile time this is redundant, but CA1416 does not track #if directives.
// The attribute is applied per-member rather than on the partial class because a
// class-level attribute would propagate to the platform-agnostic members defined in
// Keyboard.cs and make callers think the entire Keyboard type is Android-only.
//
// Ports FlatRedBall's Keyboard.Android.cs pattern to MonoGameGum. The FRB codepath
// (FlatRedBall/Engines/FlatRedBallXNA/FlatRedBall/Input/Keyboard.Android.cs) is the
// reference implementation — it has shipped on Android since the XNA/MonoGame transition.
//
// High level:
//   ShowKeyboard() pulls the Android View from Game.Services (MonoGame registers it),
//   requests focus, asks the InputMethodManager to pop the soft keyboard, and subscribes
//   View.KeyPress on first use. HandleAndroidKeyPress runs on the UI thread and pushes
//   characters + key actions into a buffer guarded by a lock. Activity() (game loop
//   thread) calls ProcessAndroidKeys() once per frame to swap the buffer into the
//   per-frame state that KeyDown/KeyPushed/GetStringTyped read from.
public partial class Keyboard
{
    struct AndroidKeyboardAction
    {
        public Keys Key;
        public KeyEventActions Action;
        public Keycode AndroidKeycode;
    }

    bool _hasAddedKeyPressEvent;

    readonly List<AndroidKeyboardAction> _androidActionsToProcess = new();
    readonly List<AndroidKeyboardAction> _lastFrameActions = new();
    readonly List<AndroidKeyboardAction> _downKeys = new();

    string? _stringToProcess;
    string _processedString = string.Empty;

    readonly object _androidActionListLock = new();

    /// <summary>
    /// Requests the Android soft keyboard to appear. Safe to call every frame — the IME
    /// subscription is established only on the first call. The game view is fetched from
    /// <c>Game.Services.GetService&lt;View&gt;()</c>; if no Game reference was provided at
    /// Keyboard construction time, this method is a no-op.
    /// </summary>
    [SupportedOSPlatform("android21.0")]
    public void ShowKeyboard()
    {
        var view = _game?.Services.GetService(typeof(View)) as View;
        if (view == null) return;

        view.RequestFocus();

        if (view.Context.GetSystemService(Context.InputMethodService) is InputMethodManager imm)
        {
            imm.ShowSoftInput(view, ShowFlags.Forced);
            imm.ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.ImplicitOnly);
        }

        if (!_hasAddedKeyPressEvent)
        {
            view.KeyPress += HandleAndroidKeyPress;
            _hasAddedKeyPressEvent = true;
        }
    }

    /// <summary>
    /// Hides the Android soft keyboard. Safe to call even if the keyboard is not currently
    /// visible. No-op if no Game reference is available.
    /// </summary>
    [SupportedOSPlatform("android21.0")]
    public void HideKeyboard()
    {
        var view = _game?.Services.GetService(typeof(View)) as View;
        if (view == null) return;

        if (view.Context.GetSystemService(Context.InputMethodService) is InputMethodManager imm)
        {
            imm.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
        }
    }

    [SupportedOSPlatform("android21.0")]
    void HandleAndroidKeyPress(object? sender, View.KeyEventArgs e)
    {
        if (e.Event == null) return;

        if (e.Event.Action == KeyEventActions.Multiple)
        {
            lock (_androidActionListLock)
            {
                _stringToProcess += e.Event.Characters;
            }
        }

        if (e.Event.Action == KeyEventActions.Down || e.Event.Action == KeyEventActions.Up)
        {
            var newAction = new AndroidKeyboardAction
            {
                Action = e.Event.Action,
                AndroidKeycode = e.KeyCode,
                Key = AndroidKeycodeToXnaKeys(e.KeyCode),
            };

            lock (_androidActionListLock)
            {
                var unicodeChar = e.Event.UnicodeChar;
                if (unicodeChar > 0 && e.Event.Action == KeyEventActions.Down)
                {
                    _stringToProcess += (char)unicodeChar;
                }

                _androidActionsToProcess.Add(newAction);
            }
        }
    }

    void ProcessAndroidKeys()
    {
        lock (_androidActionListLock)
        {
            for (int i = 0; i < _androidActionsToProcess.Count; i++)
            {
                var item = _androidActionsToProcess[i];

                if (item.Action == KeyEventActions.Down)
                {
                    _downKeys.Add(item);
                }
                else if (item.Action == KeyEventActions.Up)
                {
                    for (int j = _downKeys.Count - 1; j > -1; j--)
                    {
                        if (_downKeys[j].AndroidKeycode == item.AndroidKeycode)
                        {
                            _downKeys.RemoveAt(j);
                        }
                    }
                }
            }

            _processedString = _stringToProcess ?? string.Empty;
            _stringToProcess = null;

            _lastFrameActions.Clear();
            _lastFrameActions.AddRange(_androidActionsToProcess);

            _androidActionsToProcess.Clear();
        }
    }

    // Exposed so the shared Keyboard.cs #if ANDROID block in GetStringTyped can return it.
    string processedString => _processedString;

    bool KeyDownAndroid(Keys key)
    {
        lock (_androidActionListLock)
        {
            for (int i = 0; i < _downKeys.Count; i++)
            {
                if (_downKeys[i].Key == key) return true;
            }
            return false;
        }
    }

    bool KeyPushedAndroid(Keys key)
    {
        lock (_androidActionListLock)
        {
            for (int i = 0; i < _lastFrameActions.Count; i++)
            {
                var item = _lastFrameActions[i];
                if (item.Key == key && item.Action == KeyEventActions.Down) return true;
            }
            return false;
        }
    }

    // Ported from FlatRedBall.Input.Keyboard.Android.cs. Covers the keys that
    // meaningfully map between Android Keycode and XNA Keys. Anything unmapped
    // returns Keys.None, matching FRB behavior.
    [SupportedOSPlatform("android21.0")]
    static Keys AndroidKeycodeToXnaKeys(Keycode keycode)
    {
        switch (keycode)
        {
            case Keycode.Num0: return Keys.D0;
            case Keycode.Num1: return Keys.D1;
            case Keycode.Num2: return Keys.D2;
            case Keycode.Num3: return Keys.D3;
            case Keycode.Num4: return Keys.D4;
            case Keycode.Num5: return Keys.D5;
            case Keycode.Num6: return Keys.D6;
            case Keycode.Num7: return Keys.D7;
            case Keycode.Num8: return Keys.D8;
            case Keycode.Num9: return Keys.D9;
            case Keycode.A: return Keys.A;
            case Keycode.B: return Keys.B;
            case Keycode.C: return Keys.C;
            case Keycode.D: return Keys.D;
            case Keycode.E: return Keys.E;
            case Keycode.F: return Keys.F;
            case Keycode.G: return Keys.G;
            case Keycode.H: return Keys.H;
            case Keycode.I: return Keys.I;
            case Keycode.J: return Keys.J;
            case Keycode.K: return Keys.K;
            case Keycode.L: return Keys.L;
            case Keycode.M: return Keys.M;
            case Keycode.N: return Keys.N;
            case Keycode.O: return Keys.O;
            case Keycode.P: return Keys.P;
            case Keycode.Q: return Keys.Q;
            case Keycode.R: return Keys.R;
            case Keycode.S: return Keys.S;
            case Keycode.T: return Keys.T;
            case Keycode.U: return Keys.U;
            case Keycode.V: return Keys.V;
            case Keycode.W: return Keys.W;
            case Keycode.X: return Keys.X;
            case Keycode.Y: return Keys.Y;
            case Keycode.Z: return Keys.Z;
            case Keycode.AltLeft: return Keys.LeftAlt;
            case Keycode.AltRight: return Keys.RightAlt;
            case Keycode.Back: return Keys.Back;
            case Keycode.Backslash: return Keys.OemBackslash;
            case Keycode.ButtonSelect: return Keys.Select;
            case Keycode.Clear: return Keys.OemClear;
            case Keycode.Comma: return Keys.OemComma;
            // Del on Android's soft keyboard is backspace, matching FRB's mapping.
            case Keycode.Del: return Keys.Back;
            case Keycode.Enter: return Keys.Enter;
            case Keycode.Home: return Keys.Home;
            case Keycode.LeftBracket: return Keys.OemOpenBrackets;
            case Keycode.MediaNext: return Keys.MediaNextTrack;
            case Keycode.MediaPlayPause: return Keys.MediaPlayPause;
            case Keycode.MediaPrevious: return Keys.MediaPreviousTrack;
            case Keycode.Minus: return Keys.OemMinus;
            case Keycode.Mute: return Keys.VolumeMute;
            case Keycode.PageDown: return Keys.PageDown;
            case Keycode.PageUp: return Keys.PageUp;
            case Keycode.Period: return Keys.OemPeriod;
            case Keycode.Plus: return Keys.OemPlus;
            case Keycode.RightBracket: return Keys.OemCloseBrackets;
            case Keycode.Search: return Keys.BrowserSearch;
            case Keycode.Semicolon: return Keys.OemSemicolon;
            case Keycode.ShiftLeft: return Keys.LeftShift;
            case Keycode.ShiftRight: return Keys.RightShift;
            case Keycode.Space: return Keys.Space;
            case Keycode.Star: return Keys.Multiply;
            case Keycode.Tab: return Keys.Tab;
            case Keycode.VolumeUp: return Keys.VolumeUp;
            case Keycode.VolumeDown: return Keys.VolumeDown;
        }
        return Keys.None;
    }
}
#endif
