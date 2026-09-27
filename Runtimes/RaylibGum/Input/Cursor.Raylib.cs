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

    static readonly TouchCollection EmptyTouches = new TouchCollection();

    // raylib reports touch points only on its touch backends (Android, web); desktop GLFW always
    // reports zero, so desktop keeps reading the mouse.
    private TouchCollection GetTouchCollection() =>
        CreateTouchCollection(Raylib.GetTouchPointCount(), Raylib.GetTouchPointId, Raylib.GetTouchPosition);

    /// <summary>
    /// Builds a <see cref="TouchCollection"/> from raylib-style indexed touch points. Takes the
    /// reads as delegates so tests can supply touches without touch hardware.
    /// </summary>
    internal static TouchCollection CreateTouchCollection(int count, Func<int, int> getId, Func<int, Vector2> getPosition)
    {
        if (count <= 0)
        {
            return EmptyTouches;
        }

        var touches = new TouchLocation[count];
        for (int i = 0; i < count; i++)
        {
            touches[i] = new TouchLocation(getId(i), getPosition(i));
        }
        return new TouchCollection(touches);
    }

    private int? GetViewportLeft() =>
        0;

    private int? GetViewportTop() =>
        0;

}
