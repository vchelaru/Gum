using Gum.Wireframe;

namespace Gum.Input;

/// <summary>
/// BRUTAL half of the shared <see cref="Cursor"/> partial: mouse reads from the host through
/// <see cref="IBrutalInput"/>.
/// </summary>
public partial class Cursor
{
    private IBrutalInput? _input;

    // The host reports notches per event; the shared half keeps the running total.
    internal void AttachBrutalInput(IBrutalInput input)
    {
        _input = input;
        input.Scrolled += HandleScroll;
        OnDispose(() => input.Scrolled -= HandleScroll);
    }

    private void HandleScroll(double notches) => AddScrollUnits(notches * ScrollUnitsPerNotch);

    private MouseState GetMouseState()
    {
        var state = new MouseState();

        if (_input != null)
        {
            // Gum's canvas is the framebuffer; the host reports the cursor in window coordinates.
            state.X = (int)BrutalScale.WindowToFramebuffer(_input.CursorX, _input.WindowWidth, _input.FramebufferWidth);
            state.Y = (int)BrutalScale.WindowToFramebuffer(_input.CursorY, _input.WindowHeight, _input.FramebufferHeight);
            state.LeftButton = ToState(MouseButton.Left);
            state.MiddleButton = ToState(MouseButton.Middle);
            state.RightButton = ToState(MouseButton.Right);
            state.ScrollWheelValue = ScrollWheelValue;
        }

        return state;
    }

    private ButtonState ToState(MouseButton button) =>
        _input!.IsMouseButtonDown(button) ? ButtonState.Pressed : ButtonState.Released;

    partial void ApplyHostCustomCursor(Cursors? value) => _input?.SetCursorShape(value ?? Cursors.Arrow);
}
