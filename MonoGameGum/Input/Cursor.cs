using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Input
{
    public enum InputDevice
    {
        TouchScreen = 1,
        Mouse = 2
    }

    public class Cursor : ICursor
    {
        public InputDevice LastInputDevice
        {
            get;
            private set;
        } = InputDevice.Mouse;

        public int X { get; private set; }

        public int Y { get; private set; }

        public int LastX { get; private set; }
        public int LastY { get; private set; }

        /// <summary>
        /// Returns the screen space (in pixels) change on the X axis since the last frame.
        /// </summary>
        public int XChange => X - LastX;

        /// <summary>
        /// Returns the screen space (in pixel) change on the Y axis since the last frame.
        /// </summary>
        public int YChange => Y - LastY;

        public int ScrollWheelChange => (_mouseState.ScrollWheelValue - mLastFrameMouseState.ScrollWheelValue) / 120;

        public bool PrimaryPush
        {
            get
            {
                if(LastInputDevice == InputDevice.Mouse)
                {
                    return this.mLastFrameMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released &&
                        this._mouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
                }
                else
                {
                    return _touchCollection.Count > 0 && _lastFrameTouchCollection.Count == 0;
                }
            }
        }

        public bool PrimaryDown
        {
            get
            {
                if(LastInputDevice == InputDevice.Mouse)
                {
                    return this._mouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
                }
                else
                {
                    return _touchCollection.Count > 0;
                }
            }
        }

        public bool PrimaryClick
        {
            get
            {
                if (LastInputDevice == InputDevice.Mouse)
                {
                    return this.mLastFrameMouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                        this._mouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released;
                }
                else
                {
                    return _touchCollection.Count == 0 && _lastFrameTouchCollection.Count > 0;
                }
            }
        }

        public bool PrimaryDoubleClick { get; private set; }

        // for now just return true, but we'll need to keep track of actual push/clicks eventually:
        public bool PrimaryClickNoSlide => PrimaryClick;

        public bool SecondaryPush
        {
            get
            {
                if (LastInputDevice == InputDevice.Mouse)
                {
                    return this.mLastFrameMouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Released &&
                        this._mouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool SecondaryDown
        {
            get
            {
                if (LastInputDevice == InputDevice.Mouse)
                {
                    return this._mouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool SecondaryClick
        {
            get
            {
                if (LastInputDevice == InputDevice.Mouse)
                {
                    return this.mLastFrameMouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                        this._mouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Released;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool SecondaryDoubleClick { get; private set; }

        public bool MiddlePush
        {
            get
            {
                return this.mLastFrameMouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Released &&
                    this._mouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }
        }

        public bool MiddleDown
        {
            get
            {
                return this._mouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
            }
        }

        public bool MiddleClick
        {
            get
            {
                return this.mLastFrameMouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                    this._mouseState.MiddleButton == Microsoft.Xna.Framework.Input.ButtonState.Released;
            }
        }

        public bool MiddleDoubleClick { get; private set; }

        public InteractiveGue WindowPushed { get; set; }
        public InteractiveGue WindowOver { get; set; }


        MouseState _mouseState;
        MouseState mLastFrameMouseState = new MouseState();

        TouchCollection _touchCollection;
        TouchCollection _lastFrameTouchCollection = new TouchCollection();


        public const float MaximumSecondsBetweenClickForDoubleClick = .25f;
        double mLastPrimaryClickTime = -999;
        double mLastSecondaryClickTime = -999;
        double mLastMiddleClickTime = -999;

        public void ClearInputValues()
        {
            _lastFrameTouchCollection = new TouchCollection();
            _touchCollection = new TouchCollection();

            mLastFrameMouseState = new MouseState();
            _mouseState = new MouseState();

            // do we want to change X and Y?
        }

        public void Activity(double currentTime)
        {
            mLastFrameMouseState = _mouseState;
            _lastFrameTouchCollection = _touchCollection;
            PrimaryDoubleClick = false;

            LastX = X;
            LastY = Y;

            if (System.OperatingSystem.IsAndroid() || System.OperatingSystem.IsIOS())
            {
                LastInputDevice = InputDevice.TouchScreen;
                _touchCollection = TouchPanel.GetState();

                if (_touchCollection.Count > 0)
                {
                    X = (int)_touchCollection[0].Position.X;
                    Y = (int)_touchCollection[0].Position.Y;
                }
            }
            else
            {
                LastInputDevice = InputDevice.Mouse;
                _mouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();
                X = _mouseState.X;
                Y = _mouseState.Y;
            }


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

        public override string ToString()
        {
            return $"Cursor at ({X}, {Y}) Push:{PrimaryPush} Down:{PrimaryDown} Click:{PrimaryClick}";
        }
    }
}
