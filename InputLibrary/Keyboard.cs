using System;
using Microsoft.Xna.Framework.Input;

namespace InputLibrary
{
    /// <summary>
    /// Polled keyboard state for an editor canvas, sampled from the host once per frame through
    /// <see cref="IInputHostControl.GetKeyboardState"/>. Keys only count while the host has focus.
    /// </summary>
    public class Keyboard
    {
        static Keyboard? mSelf;

        KeyboardState mKeyboardState;
        KeyboardState mLastKeyboardState = new KeyboardState();

        IInputHostControl? mControl;

        public static Keyboard Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new Keyboard();
                }
                return mSelf;
            }
        }

        public void Activity()
        {
            mLastKeyboardState = mKeyboardState;
            mKeyboardState = mControl?.GetKeyboardState() ?? new KeyboardState();
        }

        public void Initialize(IInputHostControl control)
        {
            if (control == null)
            {
                throw new ArgumentException("Control must not be null", "control");
            }
            mControl = control;
        }

        public bool KeyPushed(Keys key)
        {
            if (mControl != null && mControl.Focused)
            {
                return mKeyboardState.IsKeyDown(key) && !mLastKeyboardState.IsKeyDown(key);
            }
            else
            {
                return false;
            }
        }

        public bool KeyDown(Keys key)
        {
            if (mControl == null)
            {
                throw new Exception("The Keyboard must be initialized before calling KeyDown");
            }

            if (mControl.Focused)
            {
                return mKeyboardState.IsKeyDown(key);
            }
            else
            {
                return false;
            }
        }
    }
}
