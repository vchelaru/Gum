using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Shouldly;

namespace MonoGameGum.Tests.V3;

/// <summary>
/// <see cref="FormsUtilities.Cursor"/> and <see cref="FormsUtilities.Keyboard"/> are the same slots as
/// <see cref="FrameworkElement.MainCursor"/> and <see cref="FrameworkElement.MainKeyboard"/> (#6066). Tests
/// restore the prior values so the rest of the suite is not affected.
/// </summary>
public class FormsUtilitiesCursorTests
{
    [Fact]
    public void Cursor_WhenMainCursorIsAssignedDirectly_ShouldReturnIt()
    {
        ICursor priorCursor = FormsUtilities.Cursor.ShouldNotBeNull();
        Cursor replacement = new Cursor(null);

        try
        {
            FrameworkElement.MainCursor = replacement;

            FormsUtilities.Cursor.ShouldBeSameAs(replacement);
        }
        finally
        {
            FormsUtilities.SetCursor(priorCursor);
        }
    }

    [Fact]
    public void Keyboard_WhenMainKeyboardIsAssignedDirectly_ShouldReturnIt()
    {
        IInputReceiverKeyboard priorKeyboard = FormsUtilities.Keyboard.ShouldNotBeNull();
        Keyboard replacement = new Keyboard();

        try
        {
            FrameworkElement.MainKeyboard = replacement;

            FormsUtilities.Keyboard.ShouldBeSameAs(replacement);
        }
        finally
        {
            FormsUtilities.SetKeyboard(priorKeyboard);
        }
    }
}
