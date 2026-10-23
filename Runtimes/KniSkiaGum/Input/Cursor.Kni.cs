using System;
using Gum.Wireframe;
using XnaButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using XnaMouse = Microsoft.Xna.Framework.Input.Mouse;
using XnaMouseCursor = Microsoft.Xna.Framework.Input.MouseCursor;
using XnaMouseState = Microsoft.Xna.Framework.Input.MouseState;
using XnaGameWindow = Microsoft.Xna.Framework.GameWindow;
using XnaTouchCollection = Microsoft.Xna.Framework.Input.Touch.TouchCollection;
using XnaTouchLocation = Microsoft.Xna.Framework.Input.Touch.TouchLocation;
using XnaTouchPanel = Microsoft.Xna.Framework.Input.Touch.TouchPanel;
using Vector2 = System.Numerics.Vector2;

namespace Gum.Input;

/// <summary>
/// KNI half of the shared <see cref="Cursor"/> partial. Reads KNI's mouse and touch panel and translates
/// them into Gum's engine-neutral <see cref="MouseState"/> and <see cref="TouchCollection"/>, which the
/// shared <c>Activity</c> turns into position and primary-button state as it does on KniGum. KNI's wheel value is already a running total in
/// 120-per-notch units, so it passes straight through to the shared scroll math. The KNI types are
/// aliased because they share simple names with the neutral ones.
/// </summary>
public partial class Cursor
{
    private readonly XnaGameWindow? _gameWindow;

    /// <summary>
    /// Constructs a <see cref="Cursor"/> that offsets mobile touch positions by the window's client
    /// bounds, as KniGum's <c>Cursor(GameWindow)</c> does.
    /// </summary>
    /// <param name="gameWindow">The game's window, or null to apply no offset.</param>
    public Cursor(XnaGameWindow? gameWindow) : this()
    {
        _gameWindow = gameWindow;
    }

    /// <summary>
    /// Reads the mouse. Replaced in tests; KNI's <c>Mouse.GetState</c> is a static device read.
    /// </summary>
    internal Func<XnaMouseState> ReadMouse { get; set; } = XnaMouse.GetState;

    /// <summary>
    /// Reads the touch panel, or returns null when KNI has no touch panel. Replaced in tests; KNI's
    /// <c>TouchPanel.GetState</c> is a static device read.
    /// </summary>
    internal Func<XnaTouchCollection?> ReadTouches { get; set; } = ReadTouchPanel;

    // Same guards as KniGum: KNI has no touch panel until a platform creates one, and reading it
    // throws without a platform input factory (as in tests), so no touch panel means no touches.
    private static readonly XnaTouchCollection NoTouches = new XnaTouchCollection(Array.Empty<XnaTouchLocation>());

    private static XnaTouchCollection? ReadTouchPanel()
    {
        try
        {
            return XnaTouchPanel.Current != null ? XnaTouchPanel.GetState() : null;
        }
        catch
        {
            return NoTouches;
        }
    }

    // Called by GetTouchCollection in Cursor.SkiaHost.cs before it returns the host's touches. The
    // shared Activity in Cursor.cs adds the mobile client-bounds offset, as on KniGum.
    partial void ReadHostTouches()
    {
        XnaTouchCollection? touches = ReadTouches();
        // As on KniGum: with no touch panel, keep last frame's touches.
        if (touches == null)
        {
            return;
        }
        XnaTouchCollection current = touches.Value;
        _hostTouches = TouchCollection.Create(
            current.Count,
            i => current[i].Id,
            i => new Vector2(current[i].Position.X, current[i].Position.Y));
    }

    /// <summary>
    /// Reads this frame's mouse state. Override to supply input from another source, such as a
    /// scripted cursor in automation, as MonoGameGum's and RaylibGum's cursors allow.
    /// </summary>
    protected virtual MouseState GetMouseState()
    {
        XnaMouseState mouse = ReadMouse();
        return new MouseState
        {
            X = mouse.X,
            Y = mouse.Y,
            LeftButton = Translate(mouse.LeftButton),
            MiddleButton = Translate(mouse.MiddleButton),
            RightButton = Translate(mouse.RightButton),
            ScrollWheelValue = mouse.ScrollWheelValue,
        };
    }

    private static ButtonState Translate(XnaButtonState state) =>
        state == XnaButtonState.Pressed ? ButtonState.Pressed : ButtonState.Released;

    // Maps the engine-neutral cursor shape onto KNI's mouse cursors. Called from the CustomCursor
    // setter in the shared Cursor.cs.
    partial void ApplyHostCustomCursor(Cursors? value)
    {
        XnaMouseCursor cursor = value switch
        {
            Cursors.SizeNS => XnaMouseCursor.SizeNS,
            Cursors.SizeWE => XnaMouseCursor.SizeWE,
            Cursors.SizeNWSE => XnaMouseCursor.SizeNWSE,
            Cursors.SizeNESW => XnaMouseCursor.SizeNESW,
            _ => XnaMouseCursor.Arrow,
        };
        XnaMouse.SetCursor(cursor);
    }
}
