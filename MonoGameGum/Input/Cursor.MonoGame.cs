using Gum.Forms.Controls;
using Gum.Wireframe;
using System;

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
public partial class Cursor
{
    
    private MouseState GetMouseState()
    {
        return Microsoft.Xna.Framework.Input.Mouse.GetState();
    }

    private TouchCollection GetTouchCollection()
    {
        TouchCollection touchCollection;
        // In MonoGame there's no way to check if GameWindow has been set.
        // This code could pass its own GameWindow, but that requires assumptions
        // or additional objects being carried through Cursor to get to here. Instead
        // we'll try/catch it. The catch shouldn't happen in actual games so it should be
        // cheap.
        try
        {
            touchCollection = TouchPanel.GetState();
        }
        catch
        {
            touchCollection = new TouchCollection();
        }
        return touchCollection;
    }

    private int? GetViewportLeft() => 
        RenderingLibrary.SystemManagers.Default.Renderer.GraphicsDevice?.Viewport.Bounds.Left;

    private int? GetViewportTop() =>
        RenderingLibrary.SystemManagers.Default.Renderer.GraphicsDevice?.Viewport.Bounds.Left;
}
