using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Input;


public class Cursor : ICursor
{
    Cursors? _customCursor;
    public Cursors? CustomCursor 
    {
        get => _customCursor;
        set
        {
            _customCursor = value;


#if MONOGAME || KNI
            switch(value)
            {
                case Cursors.Arrow:
                case null:
                    Microsoft.Xna.Framework.Input.Mouse.SetCursor(MouseCursor.Arrow);
                    break;
                case Cursors.SizeNS:
                    Microsoft.Xna.Framework.Input.Mouse.SetCursor(MouseCursor.SizeNS);

                    break;
                case Cursors.SizeWE:
                    Microsoft.Xna.Framework.Input.Mouse.SetCursor(MouseCursor.SizeWE);

                    break;

                case Cursors.SizeNWSE:
                    Microsoft.Xna.Framework.Input.Mouse.SetCursor(MouseCursor.SizeNWSE);

                    break;
                case Cursors.SizeNESW:
                    Microsoft.Xna.Framework.Input.Mouse.SetCursor(MouseCursor.SizeNESW);

                    break;
            }
#endif
        }
    }

    public Matrix TransformMatrix { get; set; } = Matrix.Identity;

    public InputDevice LastInputDevice
    {
        get;
        private set;
    } = InputDevice.Mouse;

    /// <summary>
    /// The cursor's Y position in screen space (pixels from left of the window).
    /// This is measured in screen pixels, and does not consider camera zoom.
    /// For zoom, see <see cref="XRespectingGumZoomAndBounds"/>.
    /// </summary>
    public int X { get; private set; }

    /// <summary>
    /// Returns the cursor's horizontal position considering camera zoom and the graphics device viewport bounds.
    /// </summary>
    /// <returns>The cursor's position</returns>
    public float XRespectingGumZoomAndBounds()
    {
        var renderer = RenderingLibrary.SystemManagers.Default.Renderer;
        var zoom = renderer.Camera.Zoom;
        return (X / zoom) - renderer.GraphicsDevice.Viewport.Bounds.Left;
    }

    /// <summary>
    /// The cursor's Y position in screen space (pixels from top of the window).
    /// This is measured in screen pixels, and does not consider camera zoom.
    /// For zoom, see <see cref="YRespectingGumZoomAndBounds"/>.
    /// </summary>
    public int Y { get; private set; }

    /// <summary>
    /// Returns the cursor's vertical position considering the camera zoom and the graphics device viewport bounds.
    /// </summary>
    /// <returns>The cursor's position</returns>
    public float YRespectingGumZoomAndBounds()
    {
        var renderer = RenderingLibrary.SystemManagers.Default.Renderer;
        var zoom = renderer.Camera.Zoom;
        return (Y / zoom) - renderer.GraphicsDevice.Viewport.Bounds.Top;
    }

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


    /// <summary>
    /// The movement rate of the controlling input (usually mouse) on the z axis. For the mouse this refers to the scroll wheel.
    /// </summary>
    public float ZVelocity => ScrollWheelChange;

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
    public bool PrimaryDoublePush { get; private set; }

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

    // This property follows the FRB naming conventions.
    // This is confusing for a number of reasons:
    // 1. What is a "window"? We have a Window class in forms, but property is not necessarily
    //    a Window. It could be anything.
    // 2. This actually represents a "runtime" or "visual", so we probably should have this be VisualPushed
    //    to follow the more modern naming conventions.
    // 3. Users may want to know which FrameworkElement was pushed without having to do casting. This should
    //    change in .NET 10 with extension properties.
    public InteractiveGue WindowPushed { get; set; }

    public InteractiveGue VisualRightPushed { get; set; }

    /// <summary>
    /// The last window that the cursor was over. This typically gets updated every frame in Update, usually by calls to 
    /// FormsUtilities.
    /// </summary>
    public InteractiveGue WindowOver { get; set; }


    MouseState _mouseState;
    MouseState mLastFrameMouseState = new MouseState();

    TouchCollection _touchCollection;
    TouchCollection _lastFrameTouchCollection = new TouchCollection();


    public const float MaximumSecondsBetweenClickForDoubleClick = .25f;
    double mLastPrimaryClickTime = -999;
    public double LastPrimaryClickTime => mLastPrimaryClickTime;
    double mLastPrimaryPushTime = -999;
    public double LastPrimaryPushTime => mLastPrimaryPushTime;
    double mLastSecondaryClickTime = -999;
    double mLastMiddleClickTime = -999;

    public Cursor()
    {
        // empty for now, but maybe we'll need something here in the future?
    }

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
        PrimaryDoublePush = false;

        LastX = X;
        LastY = Y;



        int? x = null;
        int? y = null;

        var supportsMouse =
            !System.OperatingSystem.IsAndroid() && !System.OperatingSystem.IsIOS();

        if(supportsMouse)
        {
            LastInputDevice = InputDevice.Mouse;

            _mouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();
            x = _mouseState.X;
            y = _mouseState.Y;
        }
        else
        {
            LastInputDevice = InputDevice.TouchScreen;
        }

        var shouldDoTouchPanel = true;

#if KNI
        shouldDoTouchPanel = TouchPanel.Current != null;
#endif

        if (shouldDoTouchPanel)
        {
            // In MonoGame there's no way to check if GameWindow has been set.
            // This code could pass its own GameWindow, but that requires assumptions
            // or additional objects being carried through Cursor to get to here. Instead
            // we'll try/catch it. The catch shouldn't happen in actual games so it should be
            // cheap.
            try
            {
                _touchCollection = TouchPanel.GetState();
            }
            catch
            {
                _touchCollection = new TouchCollection();
            }
        }

        var lastFrameTouchCollectionCount = 0;
        try
        {
            lastFrameTouchCollectionCount = _lastFrameTouchCollection.Count;
        }
        // FNA crashes here (maybe because XNA did?) if lastFrameTouchCollectionCount.GetState has never been called
        catch { }

        if (_touchCollection.Count > 0 || lastFrameTouchCollectionCount > 0)
        {
            LastInputDevice = InputDevice.TouchScreen;

            if(_touchCollection.Count > 0)
            {
                x = (int)_touchCollection[0].Position.X;
                y = (int)_touchCollection[0].Position.Y;
            }
            else if(_lastFrameTouchCollection.Count > 0)
            {
                x = (int)_lastFrameTouchCollection[0].Position.X;
                y = (int)_lastFrameTouchCollection[0].Position.Y;
            }
        }

        if(x != null)
        {
            var vector = new Vector2(x.Value, y.Value);
            vector = Vector2.Transform(vector, TransformMatrix);
            X = (int)vector.X;
            Y = (int)vector.Y;
        }
        else
        {
            // do nothing
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

        if (PrimaryPush)
        {
            if (currentTime - mLastPrimaryPushTime < MaximumSecondsBetweenClickForDoubleClick)
            {
                PrimaryDoublePush = true;
            }
            mLastPrimaryPushTime = currentTime;
        }

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
