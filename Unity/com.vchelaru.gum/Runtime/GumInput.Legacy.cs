#if GUM_PACKAGE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
#define GUM_INPUT_SYSTEM
#endif
#if !GUM_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
#define GUM_LEGACY_INPUT
#endif
#if GUM_LEGACY_INPUT
using System;
using Gum.Input;
using UnityEngine;
using GumCursor = Gum.Input.Cursor;
using GumKeyboard = Gum.Input.Keyboard;
using TouchLocation = Gum.Input.TouchLocation;

namespace Gum.Unity
{
    // The Input Manager (Old) backend: used when Active Input Handling is Input Manager (Old), or when
    // the Input System package isn't installed. Gamepads are not read: the legacy API exposes them only
    // as axes and buttons a project defines itself in the Input Manager, with no standard layout.
    public sealed partial class GumInput
    {
        static readonly Func<int, bool> IsKeyHeld = keyCode => UnityEngine.Input.GetKey((KeyCode)keyCode);

        partial void PushMouse(GumCursor cursor)
        {
            Vector3 mouse = UnityEngine.Input.mousePosition;
            System.Numerics.Vector2 position = _mapper.Map(new System.Numerics.Vector2(mouse.x, mouse.y));
            // Legacy buttons: 0 left, 1 right, 2 middle.
            cursor.SetMouseState(
                position.X,
                position.Y,
                UnityEngine.Input.GetMouseButton(0),
                UnityEngine.Input.GetMouseButton(2),
                UnityEngine.Input.GetMouseButton(1));

            // Already in notches (one per wheel click).
            cursor.AddScrollNotches(UnityEngine.Input.mouseScrollDelta.y);
        }

        partial void PushTouches(GumCursor cursor)
        {
            _touches.Clear();
            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                Touch touch = UnityEngine.Input.GetTouch(i);
                if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                {
                    System.Numerics.Vector2 position = _mapper.Map(new System.Numerics.Vector2(touch.position.x, touch.position.y));
                    _touches.Add(new TouchLocation(touch.fingerId, position));
                }
            }
            cursor.SetTouches(new TouchCollection(_touches.ToArray()));
        }

        partial void PushKeyboard(GumKeyboard gumKeyboard)
        {
            LegacyKeyMap.Push(gumKeyboard, IsKeyHeld);

            // The characters typed this frame, including the OS's own key repeat. Control characters
            // such as backspace are dropped by the keyboard.
            string typed = UnityEngine.Input.inputString;
            for (int i = 0; i < typed.Length; i++)
            {
                gumKeyboard.AddTypedText(typed[i]);
            }
        }
    }
}
#endif
