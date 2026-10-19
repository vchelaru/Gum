using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using SkiaHostTests;

namespace UnityGum.Tests;

/// <summary>
/// Base for UnityGum tests. The shared reset lives in <see cref="SkiaHostTestBase"/>; Unity's input is
/// pushed in by the host and persists until overwritten, so this also puts every pushed device back.
/// </summary>
public class BaseTestClass : SkiaHostTestBase
{
    protected override void ResetInput()
    {
        // A fresh pushed cursor/keyboard so one test's held buttons and keys don't leak into the next.
        FormsUtilities.SetCursor(new Cursor());
        FormsUtilities.SetKeyboard(new Keyboard());

        // Pushed gamepad state persists until the host overwrites it, so disconnect and clear every slot.
        foreach (GamePad gamepad in GumService.Default.Gamepads)
        {
            gamepad.Clear();
            gamepad.SetConnected(false);
            gamepad.Activity(0);
            gamepad.Activity(0);
        }

        FrameworkElement.GamePadsForUiControl.Clear();
    }
}
