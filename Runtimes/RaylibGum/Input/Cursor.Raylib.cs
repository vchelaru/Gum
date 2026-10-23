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
namespace Gum.Input;
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

    /// <summary>
    /// Reads the mouse for this frame. <see cref="Activity(double)"/> calls it; a subclass overrides it to
    /// replace the input source, for automation and tests, without reading the OS mouse.
    /// </summary>
    protected virtual MouseState GetMouseState()
    {
        var state = new MouseState();

        state.X = Raylib.GetMouseX();
        state.Y = Raylib.GetMouseY();
        state.LeftButton = Raylib.IsMouseButtonDown(MouseButton.Left) ? ButtonState.Pressed : ButtonState.Released;
        state.MiddleButton = Raylib.IsMouseButtonDown(MouseButton.Middle) ? ButtonState.Pressed : ButtonState.Released;
        state.RightButton = Raylib.IsMouseButtonDown(MouseButton.Right) ? ButtonState.Pressed : ButtonState.Released;
        

        return state;
    }

    /// <summary>
    /// Reads the touch points for this frame. <see cref="Activity(double)"/> calls it; a subclass overrides it
    /// to replace the input source, for automation and tests, without reading the OS touch panel.
    /// </summary>
    // raylib reports touch points only on its touch backends (Android, web); desktop GLFW always
    // reports zero, so desktop keeps reading the mouse.
    protected virtual TouchCollection GetTouchCollection() =>
        TouchCollection.Create(Raylib.GetTouchPointCount(), Raylib.GetTouchPointId, Raylib.GetTouchPosition);

    private int? GetViewportLeft() =>
        0;

    private int? GetViewportTop() =>
        0;

}
