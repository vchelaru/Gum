using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using System.Windows.Forms;

namespace InputLibrary
{
    public class Keyboard
    {
        static Keyboard mSelf;

        KeyboardState mKeyboardState;
        KeyboardState mLastKeyboardState = new KeyboardState();

        Control mControl;

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

            mKeyboardState = Microsoft.Xna.Framework.Input.Keyboard.GetState();

        }


        
        public void Initialize(Control control)
        {
            mControl = control;

        }

        public bool KeyPushed(Microsoft.Xna.Framework.Input.Keys key)
        {
            if (mControl.Focused)
            {
                return mKeyboardState.IsKeyDown(key) && !mLastKeyboardState.IsKeyDown(key);
            }
            else
            {
                return false;
            }
        }

        public bool KeyDown(Microsoft.Xna.Framework.Input.Keys key)
        {
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
