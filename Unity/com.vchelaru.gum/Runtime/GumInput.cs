#nullable enable
using System.Collections.Generic;
using Gum.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using GumCursor = Gum.Input.Cursor;
using GumGamePad = Gum.Input.GamePad;
using GumKeys = Gum.Forms.Input.Keys;
using GumKeyboard = Gum.Input.Keyboard;
using TouchLocation = Gum.Input.TouchLocation;
using UnityKey = UnityEngine.InputSystem.Key;
using UnityGamepad = UnityEngine.InputSystem.Gamepad;
using UnityKeyboard = UnityEngine.InputSystem.Keyboard;

namespace Gum.Unity
{
    /// <summary>
    /// Reads Unity's Input System every frame and pushes the mouse, touches, keyboard and gamepads into
    /// <see cref="GumService.Default"/>'s cursor, keyboard and gamepads, before <see cref="GumRenderer"/> runs
    /// Gum's update. Put it on the same GameObject as the <see cref="GumRenderer"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GumInput : MonoBehaviour
    {
        // Gum keys follow XNA's key space. Several Unity keys can map to one Gum key (both Enters);
        // Gum keys with no Unity counterpart (media, browser, IME, F13-F24) are left out.
        static readonly (GumKeys gum, UnityKey unity)[] KeyMap =
        {
            (GumKeys.Back, UnityKey.Backspace),
            (GumKeys.Tab, UnityKey.Tab),
            (GumKeys.Enter, UnityKey.Enter),
            (GumKeys.Enter, UnityKey.NumpadEnter),
            (GumKeys.Pause, UnityKey.Pause),
            (GumKeys.CapsLock, UnityKey.CapsLock),
            (GumKeys.Escape, UnityKey.Escape),
            (GumKeys.Space, UnityKey.Space),
            (GumKeys.PageUp, UnityKey.PageUp),
            (GumKeys.PageDown, UnityKey.PageDown),
            (GumKeys.End, UnityKey.End),
            (GumKeys.Home, UnityKey.Home),
            (GumKeys.Left, UnityKey.LeftArrow),
            (GumKeys.Up, UnityKey.UpArrow),
            (GumKeys.Right, UnityKey.RightArrow),
            (GumKeys.Down, UnityKey.DownArrow),
            (GumKeys.PrintScreen, UnityKey.PrintScreen),
            (GumKeys.Insert, UnityKey.Insert),
            (GumKeys.Delete, UnityKey.Delete),

            (GumKeys.D0, UnityKey.Digit0),
            (GumKeys.D1, UnityKey.Digit1),
            (GumKeys.D2, UnityKey.Digit2),
            (GumKeys.D3, UnityKey.Digit3),
            (GumKeys.D4, UnityKey.Digit4),
            (GumKeys.D5, UnityKey.Digit5),
            (GumKeys.D6, UnityKey.Digit6),
            (GumKeys.D7, UnityKey.Digit7),
            (GumKeys.D8, UnityKey.Digit8),
            (GumKeys.D9, UnityKey.Digit9),

            (GumKeys.A, UnityKey.A),
            (GumKeys.B, UnityKey.B),
            (GumKeys.C, UnityKey.C),
            (GumKeys.D, UnityKey.D),
            (GumKeys.E, UnityKey.E),
            (GumKeys.F, UnityKey.F),
            (GumKeys.G, UnityKey.G),
            (GumKeys.H, UnityKey.H),
            (GumKeys.I, UnityKey.I),
            (GumKeys.J, UnityKey.J),
            (GumKeys.K, UnityKey.K),
            (GumKeys.L, UnityKey.L),
            (GumKeys.M, UnityKey.M),
            (GumKeys.N, UnityKey.N),
            (GumKeys.O, UnityKey.O),
            (GumKeys.P, UnityKey.P),
            (GumKeys.Q, UnityKey.Q),
            (GumKeys.R, UnityKey.R),
            (GumKeys.S, UnityKey.S),
            (GumKeys.T, UnityKey.T),
            (GumKeys.U, UnityKey.U),
            (GumKeys.V, UnityKey.V),
            (GumKeys.W, UnityKey.W),
            (GumKeys.X, UnityKey.X),
            (GumKeys.Y, UnityKey.Y),
            (GumKeys.Z, UnityKey.Z),

            (GumKeys.LeftWindows, UnityKey.LeftMeta),
            (GumKeys.RightWindows, UnityKey.RightMeta),
            (GumKeys.Apps, UnityKey.ContextMenu),

            (GumKeys.NumPad0, UnityKey.Numpad0),
            (GumKeys.NumPad1, UnityKey.Numpad1),
            (GumKeys.NumPad2, UnityKey.Numpad2),
            (GumKeys.NumPad3, UnityKey.Numpad3),
            (GumKeys.NumPad4, UnityKey.Numpad4),
            (GumKeys.NumPad5, UnityKey.Numpad5),
            (GumKeys.NumPad6, UnityKey.Numpad6),
            (GumKeys.NumPad7, UnityKey.Numpad7),
            (GumKeys.NumPad8, UnityKey.Numpad8),
            (GumKeys.NumPad9, UnityKey.Numpad9),
            (GumKeys.Multiply, UnityKey.NumpadMultiply),
            (GumKeys.Add, UnityKey.NumpadPlus),
            (GumKeys.Subtract, UnityKey.NumpadMinus),
            (GumKeys.Decimal, UnityKey.NumpadPeriod),
            (GumKeys.Divide, UnityKey.NumpadDivide),

            (GumKeys.F1, UnityKey.F1),
            (GumKeys.F2, UnityKey.F2),
            (GumKeys.F3, UnityKey.F3),
            (GumKeys.F4, UnityKey.F4),
            (GumKeys.F5, UnityKey.F5),
            (GumKeys.F6, UnityKey.F6),
            (GumKeys.F7, UnityKey.F7),
            (GumKeys.F8, UnityKey.F8),
            (GumKeys.F9, UnityKey.F9),
            (GumKeys.F10, UnityKey.F10),
            (GumKeys.F11, UnityKey.F11),
            (GumKeys.F12, UnityKey.F12),

            (GumKeys.NumLock, UnityKey.NumLock),
            (GumKeys.Scroll, UnityKey.ScrollLock),
            (GumKeys.LeftShift, UnityKey.LeftShift),
            (GumKeys.RightShift, UnityKey.RightShift),
            (GumKeys.LeftControl, UnityKey.LeftCtrl),
            (GumKeys.RightControl, UnityKey.RightCtrl),
            (GumKeys.LeftAlt, UnityKey.LeftAlt),
            (GumKeys.RightAlt, UnityKey.RightAlt),

            (GumKeys.OemSemicolon, UnityKey.Semicolon),
            (GumKeys.OemPlus, UnityKey.Equals),
            (GumKeys.OemComma, UnityKey.Comma),
            (GumKeys.OemMinus, UnityKey.Minus),
            (GumKeys.OemPeriod, UnityKey.Period),
            (GumKeys.OemQuestion, UnityKey.Slash),
            (GumKeys.OemTilde, UnityKey.Backquote),
            (GumKeys.OemOpenBrackets, UnityKey.LeftBracket),
            (GumKeys.OemPipe, UnityKey.Backslash),
            (GumKeys.OemCloseBrackets, UnityKey.RightBracket),
            (GumKeys.OemQuotes, UnityKey.Quote),
            (GumKeys.OemBackslash, UnityKey.OEM1),
        };

        readonly ScreenToCanvasMapper _mapper = new ScreenToCanvasMapper();
        readonly HashSet<GumKeys> _downThisFrame = new HashSet<GumKeys>();
        readonly List<UnityGamepadState> _gamepadStates = new List<UnityGamepadState>();
        readonly List<TouchLocation> _touches = new List<TouchLocation>();
        UnityKeyboard? _subscribedKeyboard;
        GumRenderer? _renderer;

        void Awake() => _renderer = GetComponent<GumRenderer>();

        void Update()
        {
            GumService gum = GumService.Default;
            if (!gum.IsInitialized)
            {
                return;
            }

            int canvasWidth = _renderer != null ? _renderer.CanvasPixelWidth : Screen.width;
            int canvasHeight = _renderer != null ? _renderer.CanvasPixelHeight : Screen.height;
            _mapper.SetSizes(Screen.width, Screen.height, canvasWidth, canvasHeight);

            // Null while a custom cursor or keyboard is installed with FormsUtilities.
            GumCursor? cursor = gum.Cursor;
            if (cursor != null)
            {
                cursor.IsMobile = Application.isMobilePlatform;
                PushMouse(cursor);
                PushTouches(cursor);
            }

            GumKeyboard? keyboard = gum.Keyboard;
            if (keyboard != null)
            {
                PushKeyboard(keyboard);
            }

            PushGamepads(gum.Gamepads);
        }

        void OnDisable() => UnsubscribeTextInput();

        void PushMouse(GumCursor cursor)
        {
            Mouse? mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            System.Numerics.Vector2 position = _mapper.Map(ToNumerics(mouse.position.ReadValue()));
            cursor.SetMouseState(
                position.X,
                position.Y,
                mouse.leftButton.isPressed,
                mouse.middleButton.isPressed,
                mouse.rightButton.isPressed);

            float scroll = mouse.scroll.ReadValue().y;
            if (InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange)
            {
                // Windows reports 120 per notch in the platform range.
                scroll /= 120f;
            }
            cursor.AddScrollNotches(scroll);
        }

        void PushTouches(GumCursor cursor)
        {
            Touchscreen? touchscreen = Touchscreen.current;
            _touches.Clear();
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    if (touch.press.isPressed)
                    {
                        System.Numerics.Vector2 position = _mapper.Map(ToNumerics(touch.position.ReadValue()));
                        _touches.Add(new TouchLocation(touch.touchId.ReadValue(), position));
                    }
                }
            }
            cursor.SetTouches(new TouchCollection(_touches.ToArray()));
        }

        void PushKeyboard(GumKeyboard gumKeyboard)
        {
            UnityKeyboard? unityKeyboard = UnityKeyboard.current;

            // The current keyboard can change when devices connect or disconnect, so the text
            // subscription follows it.
            if (unityKeyboard != _subscribedKeyboard)
            {
                UnsubscribeTextInput();
                if (unityKeyboard != null)
                {
                    unityKeyboard.onTextInput += HandleTextInput;
                    _subscribedKeyboard = unityKeyboard;
                }
            }

            _downThisFrame.Clear();
            if (unityKeyboard != null)
            {
                foreach (var (gum, unity) in KeyMap)
                {
                    if (unityKeyboard[unity].isPressed)
                    {
                        _downThisFrame.Add(gum);
                    }
                }
            }

            foreach (var (gum, _) in KeyMap)
            {
                gumKeyboard.SetKeyDown(gum, _downThisFrame.Contains(gum));
            }
        }

        // Reads each connected pad into a plain snapshot; UnityGamepadMapper (in UnityGum, testable
        // without Unity) maps the snapshots onto Gum's slots.
        void PushGamepads(GumGamePad[] gumGamepads)
        {
            _gamepadStates.Clear();
            var unityGamepads = UnityGamepad.all;
            for (int i = 0; i < unityGamepads.Count; i++)
            {
                UnityGamepad pad = unityGamepads[i];
                Vector2 leftStick = pad.leftStick.ReadValue();
                Vector2 rightStick = pad.rightStick.ReadValue();
                _gamepadStates.Add(new UnityGamepadState
                {
                    DPadUp = pad.dpad.up.isPressed,
                    DPadDown = pad.dpad.down.isPressed,
                    DPadLeft = pad.dpad.left.isPressed,
                    DPadRight = pad.dpad.right.isPressed,
                    ButtonSouth = pad.buttonSouth.isPressed,
                    ButtonEast = pad.buttonEast.isPressed,
                    ButtonWest = pad.buttonWest.isPressed,
                    ButtonNorth = pad.buttonNorth.isPressed,
                    LeftShoulder = pad.leftShoulder.isPressed,
                    RightShoulder = pad.rightShoulder.isPressed,
                    Start = pad.startButton.isPressed,
                    Select = pad.selectButton.isPressed,
                    LeftStickButton = pad.leftStickButton.isPressed,
                    RightStickButton = pad.rightStickButton.isPressed,
                    LeftTrigger = pad.leftTrigger.ReadValue(),
                    RightTrigger = pad.rightTrigger.ReadValue(),
                    LeftStickX = leftStick.x,
                    LeftStickY = leftStick.y,
                    RightStickX = rightStick.x,
                    RightStickY = rightStick.y,
                });
            }
            UnityGamepadMapper.Push(gumGamepads, _gamepadStates);
        }

        void HandleTextInput(char character) => GumService.Default.Keyboard?.AddTypedText(character);

        void UnsubscribeTextInput()
        {
            if (_subscribedKeyboard != null)
            {
                _subscribedKeyboard.onTextInput -= HandleTextInput;
                _subscribedKeyboard = null;
            }
        }

        static System.Numerics.Vector2 ToNumerics(Vector2 value) => new System.Numerics.Vector2(value.x, value.y);
    }
}
