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

    // Silk exposes only a per-event scroll delta (IMouse.Scroll), not a running total, so we
    // accumulate here and scale to the XNA detent convention (120 units per notch) so the shared
    // ScrollWheelChange delta math in Cursor.cs works unchanged. Matches Cursor.Sokol.cs.
    private int _scrollWheelValue;

    /// <summary>
    /// Attaches this cursor to the first mouse of the supplied Silk input context. Called by
    /// <c>GumService.CreateCursor</c> after constructing the cursor. If the context has no mice
    /// (headless), the cursor simply reports a default (all-released, origin) state.
    /// </summary>
    internal void AttachSilkInput(IInputContext inputContext)
    {
        _mouse = inputContext.Mice.Count > 0 ? inputContext.Mice[0] : null;

        if (_mouse != null)
        {
            _mouse.Scroll += (_, wheel) => _scrollWheelValue += (int)(wheel.Y * 120);
        }
    }

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
            state.ScrollWheelValue = _scrollWheelValue;
        }

        return state;
    }

    // Silk.NET desktop input surfaces the pointer as a mouse; touch is not wired for this runtime.
    private TouchCollection GetTouchCollection() => new TouchCollection();

    private int? GetViewportLeft() => 0;

    private int? GetViewportTop() => 0;
}
