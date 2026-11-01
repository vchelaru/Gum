using Gum.Forms.Controls;
using Gum.Forms.Controls.Games;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A DialogBox on a screen loaded from a packaged theme takes keyboard focus by Tab like any other stop, so
/// Tab and Shift+Tab must also lead out of it.
/// </summary>
public class ThemeDialogBoxKeyboardTests : FileThemeInputTestBase
{
    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Tab_FromFocusedDialogBox_MovesFocusToAnotherControl(string theme)
    {
        GraphicalUiElement screen = LoadDemoScreen(theme);
        List<FrameworkElement> all = new();
        CollectControls(screen, all);
        DialogBox dialog = all.OfType<DialogBox>().Single();
        dialog.IsFocused = true;
        Driver.Step();
        dialog.IsFocused.ShouldBeTrue($"{Label(theme)}: DialogBox did not accept focus");

        Press(Keys.Tab);

        InteractiveGue.CurrentInputReceiver.ShouldNotBeSameAs(dialog, $"{Label(theme)}: Tab is trapped in the DialogBox");
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void ShiftTab_FromFocusedDialogBox_MovesFocusToAnotherControl(string theme)
    {
        GraphicalUiElement screen = LoadDemoScreen(theme);
        List<FrameworkElement> all = new();
        CollectControls(screen, all);
        DialogBox dialog = all.OfType<DialogBox>().Single();
        dialog.IsFocused = true;
        Driver.Step();

        Press(Keys.Tab, Keys.LeftShift);

        InteractiveGue.CurrentInputReceiver.ShouldNotBeSameAs(dialog, $"{Label(theme)}: Shift+Tab is trapped in the DialogBox");
    }
}
