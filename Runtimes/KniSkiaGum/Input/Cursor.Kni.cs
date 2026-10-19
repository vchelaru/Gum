using System;
using Gum.Wireframe;
using XnaButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using XnaMouse = Microsoft.Xna.Framework.Input.Mouse;
using XnaMouseCursor = Microsoft.Xna.Framework.Input.MouseCursor;
using XnaMouseState = Microsoft.Xna.Framework.Input.MouseState;

namespace Gum.Input;

/// <summary>
/// KNI half of the shared <see cref="Cursor"/> partial. Reads KNI's mouse and translates it into
/// Gum's engine-neutral <see cref="MouseState"/>. KNI's wheel value is already a running total in
/// 120-per-notch units, so it passes straight through to the shared scroll math. The KNI types are
/// aliased because they share simple names with the neutral ones.
/// </summary>
public partial class Cursor
{
    /// <summary>
    /// Reads the mouse. Replaced in tests; KNI's <c>Mouse.GetState</c> is a static device read.
    /// </summary>
    internal Func<XnaMouseState> ReadMouse { get; set; } = XnaMouse.GetState;

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
