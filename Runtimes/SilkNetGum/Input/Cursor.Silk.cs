using Gum.Wireframe;
using Silk.NET.Input;

namespace Gum.Input;

/// <summary>
/// Silk.NET half of the shared <see cref="Cursor"/> partial. Supplies the platform-specific
/// mouse reads the shared <c>Cursor</c> logic calls into (<see cref="GetMouseState"/> etc.),
/// backed by an <see cref="IMouse"/> from the caller-owned <see cref="IInputContext"/>.
/// Mirrors <c>Runtimes/RaylibGum/Input/Cursor.Raylib.cs</c> and
/// <c>Runtimes/SokolGum/Input/Cursor.Sokol.cs</c>.
/// </summary>
public partial class Cursor
{
    private IMouse? _mouse;

    /// <summary>
    /// Attaches this cursor to the first mouse of the supplied Silk input context. Called by
    /// <c>GumService.CreateCursor</c> after constructing the cursor. If the context has no mice
    /// (headless), the cursor simply reports a default (all-released, origin) state.
    /// </summary>
    internal void AttachSilkInput(IInputContext inputContext)
    {
        IMouse? mouse = inputContext.Mice.Count > 0 ? inputContext.Mice[0] : null;
        _mouse = mouse;

        if (mouse != null)
        {
            mouse.Scroll += HandleScroll;
            OnDispose(() => mouse.Scroll -= HandleScroll);
        }
    }

    // Silk reports a per-event scroll delta in notches; the shared half keeps the running total.
    private void HandleScroll(IMouse mouse, ScrollWheel wheel) => AddScrollUnits((int)(wheel.Y * ScrollUnitsPerNotch));

    private MouseState GetMouseState()
    {
        var state = new MouseState();

        if (_mouse != null)
        {
            state.X = (int)_mouse.Position.X;
            state.Y = (int)_mouse.Position.Y;
            state.LeftButton = _mouse.IsButtonPressed(MouseButton.Left) ? ButtonState.Pressed : ButtonState.Released;
            state.MiddleButton = _mouse.IsButtonPressed(MouseButton.Middle) ? ButtonState.Pressed : ButtonState.Released;
            state.RightButton = _mouse.IsButtonPressed(MouseButton.Right) ? ButtonState.Pressed : ButtonState.Released;
            state.ScrollWheelValue = ScrollWheelValue;
        }

        return state;
    }

    // Maps the engine-neutral cursor shape onto the Silk standard cursors. Called from the
    // CustomCursor setter in the shared Cursor.cs.
    partial void ApplyHostCustomCursor(Cursors? value)
    {
        if (_mouse == null)
        {
            return;
        }

        var silkCursor = _mouse.Cursor;
        silkCursor.Type = CursorType.Standard;
        switch (value)
        {
            case Cursors.Arrow:
            case null:
                silkCursor.StandardCursor = StandardCursor.Arrow;
                break;
            case Cursors.SizeNS:
                silkCursor.StandardCursor = StandardCursor.VResize;
                break;
            case Cursors.SizeWE:
                silkCursor.StandardCursor = StandardCursor.HResize;
                break;
            case Cursors.SizeNWSE:
                silkCursor.StandardCursor = StandardCursor.NwseResize;
                break;
            case Cursors.SizeNESW:
                silkCursor.StandardCursor = StandardCursor.NeswResize;
                break;
        }
    }
}
