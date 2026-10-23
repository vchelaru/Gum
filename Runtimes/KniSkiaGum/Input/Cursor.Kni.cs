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
    /// Reads the touch panel. Replaced in tests; KNI's <c>TouchPanel.GetState</c> is a static device read.
    /// </summary>
    internal Func<XnaTouchCollection> ReadTouches { get; set; } = ReadTouchPanel;

    // Same guards as KniGum: KNI has no touch panel until a platform creates one, and reading it
    // throws without a platform input factory (as in tests), so no touch panel means no touches.
    private static readonly XnaTouchCollection NoTouches = new XnaTouchCollection(Array.Empty<XnaTouchLocation>());

    private static XnaTouchCollection ReadTouchPanel()
    {
        try
        {
            return XnaTouchPanel.Current != null ? XnaTouchPanel.GetState() : NoTouches;
        }
        catch
        {
            return NoTouches;
        }
    }

    // Called by GetTouchCollection in Cursor.SkiaHost.cs before it returns the host's touches.
    partial void ReadHostTouches()
    {
        XnaTouchCollection touches = ReadTouches();
        // As on KniGum: on mobile, touches are relative to the client bounds, so add their origin.
        bool offset = IsMobile && _gameWindow != null;
        int xOffset = offset ? _gameWindow!.ClientBounds.X : 0;
        int yOffset = offset ? _gameWindow!.ClientBounds.Y : 0;
        _hostTouches = TouchCollection.Create(
            touches.Count,
            i => touches[i].Id,
            i => new Vector2(touches[i].Position.X + xOffset, touches[i].Position.Y + yOffset));
    }

    private MouseState GetMouseState()
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
