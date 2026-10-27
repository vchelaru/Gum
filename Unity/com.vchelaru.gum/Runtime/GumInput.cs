#nullable enable
#if GUM_PACKAGE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
#define GUM_INPUT_SYSTEM
#endif
#if !GUM_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
#define GUM_LEGACY_INPUT
#endif
using Gum.Input;
using UnityEngine;
using GumCursor = Gum.Input.Cursor;
using GumGamePad = Gum.Input.GamePad;
using GumKeyboard = Gum.Input.Keyboard;
using TouchLocation = Gum.Input.TouchLocation;

namespace Gum.Unity
{
    /// <summary>
    /// Reads Unity's input every frame and pushes the mouse, touches, keyboard and gamepads into
    /// <see cref="GumService.Default"/>'s cursor, keyboard and gamepads, before <see cref="GumRenderer"/> runs
    /// Gum's update. Put it on the same GameObject as the <see cref="GumRenderer"/>.
    /// </summary>
    /// <remarks>
    /// With the Input System package installed and enabled (Active Input Handling set to Input System
    /// Package or Both), it reads the Input System. Otherwise, on Input Manager (Old), it reads
    /// <c>UnityEngine.Input</c>, which covers the mouse, touches and keyboard but not gamepads.
    /// </remarks>
    [DefaultExecutionOrder(-100)]
    public sealed partial class GumInput : MonoBehaviour
    {
        readonly ScreenToCanvasMapper _mapper = new ScreenToCanvasMapper();
        readonly System.Collections.Generic.List<TouchLocation> _touches = new System.Collections.Generic.List<TouchLocation>();
        GumRenderer? _renderer;

        void Awake()
        {
            _renderer = GetComponent<GumRenderer>();
#if !GUM_INPUT_SYSTEM && !GUM_LEGACY_INPUT
            Debug.LogWarning("Gum: no input backend is available. Install the Input System package, or set Player Settings > Active Input Handling to Input Manager (Old) or Both.");
#endif
        }

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

        // Each backend implements the ones it can; the others compile away.
        partial void PushMouse(GumCursor cursor);
        partial void PushTouches(GumCursor cursor);
        partial void PushKeyboard(GumKeyboard gumKeyboard);
        partial void PushGamepads(GumGamePad[] gumGamepads);
        partial void UnsubscribeTextInput();

        static System.Numerics.Vector2 ToNumerics(Vector2 value) => new System.Numerics.Vector2(value.x, value.y);
    }
}
