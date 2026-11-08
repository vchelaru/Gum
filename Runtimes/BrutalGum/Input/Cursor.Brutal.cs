using Brutal.GlfwApi;
using Gum.Wireframe;

namespace Gum.Input;

/// <summary>
/// BRUTAL half of the shared <see cref="Cursor"/> partial: mouse reads from a GLFW window through
/// <see cref="IBrutalInput"/>.
/// </summary>
public partial class Cursor
{
    private IBrutalInput? _input;

    // GLFW reports notches per event; the shared half keeps the running total.
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
            state.X = (int)_input.CursorX;
            state.Y = (int)_input.CursorY;
            state.LeftButton = ToState(GlfwMouseButton.Left);
            state.MiddleButton = ToState(GlfwMouseButton.Middle);
            state.RightButton = ToState(GlfwMouseButton.Right);
            state.ScrollWheelValue = ScrollWheelValue;
        }

        return state;
    }

    private ButtonState ToState(GlfwMouseButton button) =>
        _input!.IsMouseButtonDown(button) ? ButtonState.Pressed : ButtonState.Released;

    partial void ApplyHostCustomCursor(Cursors? value)
    {
        if (_input == null)
        {
            return;
        }

        GlfwStandardCursor shape = value switch
        {
            Cursors.SizeNS => GlfwStandardCursor.ResizeNs,
            Cursors.SizeWE => GlfwStandardCursor.ResizeEw,
            Cursors.SizeNWSE => GlfwStandardCursor.ResizeNwse,
            Cursors.SizeNESW => GlfwStandardCursor.ResizeNesw,
            _ => GlfwStandardCursor.Arrow,
        };
        _input.SetCursorShape(shape);
    }
}
