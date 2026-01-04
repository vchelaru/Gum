using Gum.Forms.Controls;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if RAYLIB
using System.Numerics;
using Raylib_cs;
namespace RaylibGum.Input;
#else
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
namespace MonoGameGum.Input;
#endif

/// <summary>
/// A cursor implementation providing mouse and touch input functionality.
/// This class includes properties necessary for interacting with Gum UI elements, such 
/// as push, click, and position tracking.
/// </summary>
public class Cursor : ICursor
{
    Cursors? _customCursor;

    /// <summary>
    /// Gets or sets the custom mouse cursor to display within the application window.
    /// </summary>
    /// <remarks>Setting this property changes the mouse cursor to the specified value. If set to <see
    /// langword="null"/>, the default arrow cursor is used. The available cursor options may vary depending on platform
    /// support.</remarks>
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

#if RAYLIB
            switch (value)
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
#endif
        }
    }

    /// <summary>
    /// Gets or sets the transformation matrix applied to the object, used to determine the cursor's position
    /// when interacting with Gum UI elements.
    /// </summary>
    /// <remarks>The transformation matrix defines how the object's coordinates are mapped, including
    /// translation, rotation, scaling, or skewing. This is often used when Gum is rendered
    /// on a render target, or in a modified viewport.</remarks>
    public Matrix3x2 TransformMatrix { get; set; } = Matrix3x2.Identity;

    /// <summary>
    /// Gets the most recent input device used to interact with the application. This is used
    /// to modify how behavior is interpreted, such as whether to consider cursor hover states.
    /// </summary>
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
        return (X / zoom);
        //- renderer.GraphicsDevice.Viewport.Bounds.Left;
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
        return (Y / zoom);
    }

    /// <summary>
    /// Gets the value of the X value last frame, used to calculate changes.
    /// </summary>
    public int LastX { get; private set; }

    /// <summary>
    /// Gets the value of the Y value last frame, used to calculate changes.
    /// </summary>
    public int LastY { get; private set; }

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

    public bool PrimaryDoubleClick { get; private set; }

    public bool PrimaryDoublePush { get; private set; }

    public bool PrimaryClickNoSlide => PrimaryClick;

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

    // This property follows the FRB naming conventions.
    // This is confusing for a number of reasons:
    // 1. What is a "window"? We have a Window class in forms, but property is not necessarily
    //    a Window. It could be anything.
    // 2. This actually represents a "runtime" or "visual", so we probably should have this be VisualPushed
    //    to follow the more modern naming conventions.
    // 3. Users may want to know which FrameworkElement was pushed without having to do casting. This should
    //    change in .NET 10 with extension properties.
    [Obsolete("Use VisualPushed instead")]
    public InteractiveGue? WindowPushed
    {
        get => VisualPushed;
        set => VisualPushed = value;
    }

    /// <summary>
    /// Gets or sets the Visual that was under the cursor when the cursor (left button)
    /// was pushed.
    /// </summary>
    public InteractiveGue? VisualPushed { get; set; }

    /// <summary>
    /// Gets the control that was under the cursor when the cursor (left button) was pushed.
    /// </summary>
    public FrameworkElement? FrameworkElementPushed => VisualPushed?.FormsControlAsObject as FrameworkElement;

    /// <summary>
    /// Gets or sets the Visual that was under the cursor when the cursor right button
    /// was pushed.
    /// </summary>
    public InteractiveGue? VisualRightPushed { get; set; }

    /// <summary>
    /// Gets the control that was under the cursor when the cursor right button was pushed.
    /// </summary>
    public FrameworkElement? FrameworkElementRightPushed => VisualRightPushed?.FormsControlAsObject as FrameworkElement;

    [Obsolete("Use VisualOver instead")]
    public InteractiveGue? WindowOver
    {
        get => VisualOver;
        set => VisualOver = value;
    }

    /// <summary>
    /// Gets or sets the Visual that was under the cursor the last time it was updated.
    /// </summary>
    public InteractiveGue? VisualOver { get; set; }

    /// <summary>
    /// Gets the control that is currently under the cursor.
    /// </summary>
    public FrameworkElement? FrameworkElementOver => VisualOver?.FormsControlAsObject as FrameworkElement;

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


    // todo - need to have this actually change the cursor. For now doing this to satisfy the interface:
    Cursors? ICursor.CustomCursor
    { 
        get; set;
    }

    public void Activity(double gameTime)
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

        if (supportsMouse)
        {
            LastInputDevice = InputDevice.Mouse;

            _mouseState = GetMouseState();
            x = _mouseState.X;
            y = _mouseState.Y;
        }

        if (x != null)
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
        if (PrimaryPush)
        {
            if (gameTime - mLastPrimaryPushTime < MaximumSecondsBetweenClickForDoubleClick)
            {
                PrimaryDoublePush = true;
            }
            mLastPrimaryPushTime = gameTime;
        }

        if (PrimaryClick)
        {
            if (gameTime - mLastPrimaryClickTime < MaximumSecondsBetweenClickForDoubleClick)
            {
                PrimaryDoubleClick = true;
            }
            mLastPrimaryClickTime = gameTime;
        }

        if (SecondaryClick)
        {
            if (gameTime - mLastSecondaryClickTime < MaximumSecondsBetweenClickForDoubleClick)
            {
                SecondaryDoubleClick = true;
            }
            mLastSecondaryClickTime = gameTime;
        }

        if (MiddleClick)
        {
            if (gameTime - mLastMiddleClickTime < MaximumSecondsBetweenClickForDoubleClick)
            {
                MiddleDoubleClick = true;
            }
            mLastMiddleClickTime = gameTime;
        }
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
