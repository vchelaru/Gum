using Stride.Input;

namespace Gum.Input;

/// <summary>
/// Stride half of the shared <see cref="Cursor"/> partial. Supplies the platform-specific mouse
/// reads the shared <c>Cursor</c> logic calls into (<see cref="GetMouseState"/> etc.), backed by an
/// <see cref="IMouseDevice"/> resolved once by the caller (<c>GumService.CreateCursor</c>) from the
/// Stride <see cref="InputManager"/>. Stored as the device interface, not the whole
/// <see cref="InputManager"/>, so it can be driven by a mock in tests -- <c>InputManager</c> itself
/// is a concrete class whose device lists only populate through real platform input sources, so it
/// can't stand in for a live mouse in a unit test. Mirrors Gum's
/// <c>Runtimes/SilkNetGum/Input/Cursor.Silk.cs</c>.
/// </summary>
public partial class Cursor : IInputEventListener<MouseWheelEvent>
{
    private IMouseDevice? _mouse;

    // Stride only reports a per-event wheel delta (MouseWheelEvent.WheelDelta), not a running
    // total, so accumulate here and scale to the XNA detent convention (120 units per notch) so
    // the shared ScrollWheelChange delta math in Cursor.cs works unchanged. Matches Cursor.Silk.cs.
    private int _scrollWheelValue;

    /// <summary>
    /// Attaches this cursor to the supplied mouse device (<c>inputManager.Mouse</c>, or
    /// <see langword="null"/> in the degenerate no-mouse case). Called by
    /// <c>GumService.CreateCursor</c> after constructing the cursor; that same call site separately
    /// registers this cursor as an <c>InputManager</c> listener so <see cref="MouseWheelEvent"/>s
    /// reach <see cref="IInputEventListener{MouseWheelEvent}.ProcessEvent"/> -- kept out of this
    /// method so it stays callable from a test with just a mocked <see cref="IMouseDevice"/>.
    /// </summary>
    internal void AttachStrideInput(IMouseDevice? mouse) => _mouse = mouse;

    void IInputEventListener<MouseWheelEvent>.ProcessEvent(MouseWheelEvent inputEvent) =>
        _scrollWheelValue += (int)(inputEvent.WheelDelta * 120);

    private MouseState GetMouseState()
    {
        var state = new MouseState();

        if (_mouse != null)
        {
            // Mouse.Position is normalized (0..1) to the surface; scale to pixels to match the
            // pixel-space X/Y the shared Cursor.cs logic expects.
            var pixelPosition = _mouse.Position * _mouse.SurfaceSize;
            state.X = (int)pixelPosition.X;
            state.Y = (int)pixelPosition.Y;
            state.LeftButton = _mouse.DownButtons.Contains(MouseButton.Left) ? ButtonState.Pressed : ButtonState.Released;
            state.MiddleButton = _mouse.DownButtons.Contains(MouseButton.Middle) ? ButtonState.Pressed : ButtonState.Released;
            state.RightButton = _mouse.DownButtons.Contains(MouseButton.Right) ? ButtonState.Pressed : ButtonState.Released;
            state.ScrollWheelValue = _scrollWheelValue;
        }

        return state;
    }

    // Stride's desktop mouse device already surfaces the pointer; touch is not wired for this
    // runtime (matches Cursor.Silk.cs).
    private TouchCollection GetTouchCollection() => new TouchCollection();

    private int? GetViewportLeft() => 0;

    private int? GetViewportTop() => 0;
}
