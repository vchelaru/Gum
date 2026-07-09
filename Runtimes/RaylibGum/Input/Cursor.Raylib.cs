using Gum.Forms.Controls;
using Gum.Wireframe;
using System;

#if RAYLIB
using System.Numerics;
namespace Gum.Input;
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
public partial class Cursor
{
    /// <summary>
    /// Constructs a <see cref="Cursor"/> for the current (Raylib) platform. Raylib's parameterless
    /// <see cref="Cursor()"/> ctor needs no game/window reference. Mirrors the MonoGame platform's
    /// <c>Cursor.CreateForCurrentPlatform(Game?)</c>.
    /// </summary>
    internal static Cursor CreateForCurrentPlatform() => new Cursor();

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

    private TouchCollection GetTouchCollection()
    {
        var touchCollection = new TouchCollection();
        int touchCount = Raylib.GetTouchPointCount();
        //TouchLocation[] touches = new TouchLocation[touchCount];
        //for (int i = 0; i < touchCount; i++)
        //{
        //    TouchPoint point = Raylib.GetTouchPoint(i);
        //    TouchLocation location = new TouchLocation(
        //        point.id,
        //        TouchLocationState.Moved,
        //        new Microsoft.Xna.Framework.Vector2(point.position.X, point.position.Y)
        //    );
        //    touches[i] = location;
        //}
        //touchCollection = new TouchCollection(touches);
        return touchCollection;
    }

    private int? GetViewportLeft() =>
        0;

    private int? GetViewportTop() =>
        0;

}
