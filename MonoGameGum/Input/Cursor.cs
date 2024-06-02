using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Input
{
    public class Cursor : ICursor
    {
        public int X => mMouseState.X;

        public int Y => mMouseState.Y;

        /// <summary>
        /// Returns the screen space (in pixels) change on the X axis since the last frame.
        /// </summary>
        public int XChange
        {
            get
            {
                return mMouseState.X - mLastFrameMouseState.X;
            }
        }

        /// <summary>
        /// Returns the screen space (in pixel) change on the Y axis since the last frame.
        /// </summary>
        public int YChange
        {
            get
            {
                return mMouseState.Y - mLastFrameMouseState.Y;
            }
        }

        public bool PrimaryPush
        {
            get
            {
                return this.mLastFrameMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released &&
                    this.mMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }
        }

        public bool PrimaryDown
        {
            get
            {
                return this.mMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }
        }

        public bool PrimaryClick
        {
            get
            {
                return this.mLastFrameMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                    this.mMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released;
            }
        }

        public bool PrimaryDoubleClick { get; private set; }

        public bool SecondaryPush
        {
            get
            {
                return this.mLastFrameMouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Released &&
                    this.mMouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }
        }

        public bool SecondaryDown
        {
            get
            {
                return this.mMouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }
        }

        public bool SecondaryClick
        {
            get
            {
                return this.mLastFrameMouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                    this.mMouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Released;
            }
        }

        public bool SecondaryDoubleClick { get; private set; }

        public bool MiddlePush
        {
            get
            {
                return this.mLastFrameMouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Released &&
                    this.mMouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }
        }

        public bool MiddleDown
        {
            get
            {
                return this.mMouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }
        }

        public bool MiddleClick
        {
            get
            {
                return this.mLastFrameMouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                    this.mMouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Released;
            }
        }

        public bool MiddleDoubleClick { get; private set; }

        public InteractiveGue WindowPushed { get; set; }
        public InteractiveGue WindowOver { get; set; }


        MouseState mMouseState;
        MouseState mLastFrameMouseState = new MouseState();

        public const float MaximumSecondsBetweenClickForDoubleClick = .25f;
        double mLastPrimaryClickTime = -999;
        double mLastSecondaryClickTime = -999;
        double mLastMiddleClickTime = -999;

        public void Activity(double currentTime)
        {
            mLastFrameMouseState = mMouseState;
            PrimaryDoubleClick = false;

            mMouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();

            // We want to keep track of whether
            // the user pushed in the window or not
            // to prevent the user from pushing outside
            // of the window and dragging "inward" (which 
            // happens if the user is moving the resize bar).
            // To do this we need to track if the user pushed in
            // the window or not.  We can't use PrimaryPush because
            // that checks IsInWindow, so we will manually do the MouseState
            // checks here.
            // Update, maybe we don't need this now that the wireframe window
            // can receive focus.
            //if(this.mLastFrameMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released &&
            //        this.mMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            //{
            //    mPushedInWindow = IsInWindow;
            //}


            if (PrimaryClick)
            {
                if (currentTime - mLastPrimaryClickTime < MaximumSecondsBetweenClickForDoubleClick)
                {
                    PrimaryDoubleClick = true;
                }
                mLastPrimaryClickTime = currentTime;
            }

            if (SecondaryClick)
            {
                if (currentTime - mLastSecondaryClickTime < MaximumSecondsBetweenClickForDoubleClick)
                {
                    SecondaryDoubleClick = true;
                }
                mLastSecondaryClickTime = currentTime;
            }

            if(MiddleClick)
            {
                if (currentTime - mLastMiddleClickTime < MaximumSecondsBetweenClickForDoubleClick)
                {
                    MiddleDoubleClick = true;
                }
                mLastMiddleClickTime = currentTime;
            }

        }

    }
}
