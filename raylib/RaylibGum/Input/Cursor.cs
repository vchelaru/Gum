using Gum.Wireframe;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.Input;
public class Cursor : ICursor
{
    Cursors? _customCursor;

    public Cursors? CustomCursor
    {
        get => _customCursor;
        set
        {
            _customCursor = value;


            switch(value)
            {
                case Cursors.Arrow:
                case null:
                    // Horizontal resize

                    Raylib.SetMouseCursor(MouseCursor.Arrow);
                    break;
                case Cursors.SizeNS:
                    Raylib.SetMouseCursor(MouseCursor.ResizeNs);

                    break;
                case Cursors.SizeWE:
                    Raylib.SetMouseCursor(MouseCursor.ResizeEw);

                    break;

                case Cursors.SizeNWSE:
                    Raylib.SetMouseCursor(MouseCursor.ResizeNwse);

                    break;
                case Cursors.SizeNESW:
                    Raylib.SetMouseCursor(MouseCursor.ResizeNesw);

                    break;
            }
        }
    }


    MouseState _mouseState;



    MouseState mLastFrameMouseState = new MouseState();

    TouchCollection _touchCollection;
    TouchCollection _lastFrameTouchCollection = new TouchCollection();


    public InputDevice LastInputDevice => InputDevice.Mouse;

    public int X { get; private set; }

    public float XRespectingGumZoomAndBounds()
    {
        var renderer = RenderingLibrary.SystemManagers.Default.Renderer;
        var zoom = renderer.Camera.Zoom;
        return (X / zoom);
        //- renderer.GraphicsDevice.Viewport.Bounds.Left;
    }


    public int Y { get; private set; }

    public float YRespectingGumZoomAndBounds()
    {
        var renderer = RenderingLibrary.SystemManagers.Default.Renderer;
        var zoom = renderer.Camera.Zoom;
        return (Y / zoom);
        //- renderer.GraphicsDevice.Viewport.Bounds.Top;
    }



    public int LastX { get; private set; }
    public int LastY { get; private set; }

    public double LastPrimaryPushTime => 0;

    public double LastPrimaryClickTime => 0;

    /// <summary>
    /// Returns the screen space (in pixels) change on the X axis since the last frame.
    /// </summary>
    public int XChange => X - LastX;

    /// <summary>
    /// Returns the screen space (in pixel) change on the Y axis since the last frame.
    /// </summary>
    public int YChange => Y - LastY;

    public int ScrollWheelChange => 0;

    public float ZVelocity => 0;

    public bool PrimaryPush
    {
        get
        {
            if (LastInputDevice == InputDevice.Mouse)
            {
                return this.mLastFrameMouseState.LeftButton == ButtonState.Released &&
                    this._mouseState.LeftButton == Input.ButtonState.Pressed;
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
            if (LastInputDevice == InputDevice.Mouse)
            {
                return this._mouseState.LeftButton == ButtonState.Pressed;
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
                return this.mLastFrameMouseState.LeftButton == ButtonState.Pressed &&
                    this._mouseState.LeftButton == ButtonState.Released;
            }
            else
            {
                return _touchCollection.Count == 0 && _lastFrameTouchCollection.Count > 0;
            }
        }
    }

    public bool PrimaryClickNoSlide => PrimaryClick;

    public bool PrimaryDoubleClick { get; private set; }
    public bool PrimaryDoublePush { get; private set; }

    public bool SecondaryPush
    {
        get
        {
            if (LastInputDevice == InputDevice.Mouse)
            {
                return this.mLastFrameMouseState.RightButton == ButtonState.Released &&
                    this._mouseState.RightButton == ButtonState.Pressed;
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
                return this._mouseState.RightButton == ButtonState.Pressed;
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
                return this.mLastFrameMouseState.RightButton == ButtonState.Pressed &&
                    this._mouseState.RightButton == ButtonState.Released;
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
            return this.mLastFrameMouseState.MiddleButton == ButtonState.Released &&
                this._mouseState.MiddleButton == ButtonState.Pressed;
        }
    }

    public bool MiddleDown
    {
        get
        {
            return this._mouseState.MiddleButton == ButtonState.Pressed;
        }
    }

    public bool MiddleClick
    {
        get
        {
            return this.mLastFrameMouseState.MiddleButton == ButtonState.Pressed &&
                this._mouseState.MiddleButton == ButtonState.Released;
        }
    }

    public bool MiddleDoubleClick { get; private set; }


    public InteractiveGue WindowPushed { get; set; }

    /// <summary>
    /// The last window that the cursor was over. This typically gets updated every frame in Update, usually by calls to 
    /// FormsUtilities.
    /// </summary>
    public InteractiveGue WindowOver { get; set; }

    // todo - need to have this actually change the cursor. For now doing this to satisfy the interface:
    Cursors? ICursor.CustomCursor
    { 
        get; set;
    }

    internal void Activity(float gameTime)
    {
        mLastFrameMouseState = _mouseState;
        _lastFrameTouchCollection = _touchCollection;
        PrimaryDoubleClick = false;
        PrimaryDoublePush = false;

        LastX = X;
        LastY = Y;

        int? x = null;
        int? y = null;

        var supportsMouse = true;

        if (supportsMouse)
        {
            //LastInputDevice = InputDevice.Mouse;

            _mouseState = GetMouseState();
            x = _mouseState.X;
            y = _mouseState.Y;
        }

        if (x != null)
        {
            var vector = new Vector2(x.Value, y.Value);
            vector = Vector2.Transform(vector, Matrix3x2.Identity);
            //vector = Vector2.Transform(vector, TransformMatrix);
            X = (int)vector.X;
            Y = (int)vector.Y;
        }
        else
        {
            // do nothing
        }
        //if (PrimaryPush)
        //{
        //    if (currentTime - mLastPrimaryPushTime < MaximumSecondsBetweenClickForDoubleClick)
        //    {
        //        PrimaryDoublePush = true;
        //    }
        //    mLastPrimaryPushTime = currentTime;
        //}

        //if (PrimaryClick)
        //{
        //    if (currentTime - mLastPrimaryClickTime < MaximumSecondsBetweenClickForDoubleClick)
        //    {
        //        PrimaryDoubleClick = true;
        //    }
        //    mLastPrimaryClickTime = currentTime;
        //}

        //if (SecondaryClick)
        //{
        //    if (currentTime - mLastSecondaryClickTime < MaximumSecondsBetweenClickForDoubleClick)
        //    {
        //        SecondaryDoubleClick = true;
        //    }
        //    mLastSecondaryClickTime = currentTime;
        //}

        //if (MiddleClick)
        //{
        //    if (currentTime - mLastMiddleClickTime < MaximumSecondsBetweenClickForDoubleClick)
        //    {
        //        MiddleDoubleClick = true;
        //    }
        //    mLastMiddleClickTime = currentTime;
        //}
    }

    private MouseState GetMouseState()
    {
        var state = new MouseState();

        state.X = Raylib.GetMouseX();
        state.Y = Raylib.GetMouseY();
        state.LeftButton = Raylib.IsMouseButtonDown(MouseButton.Left) ? ButtonState.Pressed : ButtonState.Released;
        state.MiddleButton = Raylib.IsMouseButtonDown(MouseButton.Middle) ? ButtonState.Pressed : ButtonState.Released;
        state.RightButton = Raylib.IsMouseButtonDown(MouseButton.Right) ? ButtonState.Pressed : ButtonState.Released;

        return state;
    }
}
