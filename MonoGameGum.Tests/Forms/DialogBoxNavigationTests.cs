using Gum.Forms.Controls;
using Gum.Forms.Controls.Games;
using Shouldly;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

public class DialogBoxNavigationTests : BeginnerDriverTestBase
{
    public DialogBoxNavigationTests()
    {
        FrameworkElement.KeyboardsForUiControl.Add(Driver.Keyboard);
    }

    public override void Dispose()
    {
        FrameworkElement.KeyboardsForUiControl.Remove(Driver.Keyboard);
        base.Dispose();
    }

    [Fact]
    public void Tab_ConfirmMovedFocusSameFrame_DoesNotMoveFocusAgain()
    {
        StackPanel stack = new();
        stack.AddToRoot();
        TextBox before = new() { Name = "Before", AcceptsTab = true };
        stack.AddChild(before);
        DialogBox dialog = new() { Name = "Dialog" };
        stack.AddChild(dialog);
        Button after = new() { Name = "After" };
        stack.AddChild(after);
        dialog.LettersPerSecond = 0;
        dialog.Show(new[] { "first", "second" });
        Driver.Step();
        dialog.IsFocused = true;
        Driver.Step();

        // Enter advances the page and the game reacts by moving focus off the still-visible dialog; the Tab pressed in the same frame must not move it a second time.
        dialog.PageAdvanced += (_, _) => before.IsFocused = true;
        Driver.Keyboard.SetKey(Keys.Enter, true);
        Driver.Keyboard.SetKey(Keys.Tab, true);
        Driver.Step();

        Driver.Keyboard.SetKey(Keys.Enter, false);
        Driver.Keyboard.SetKey(Keys.Tab, false);

        before.IsFocused.ShouldBeTrue();
        after.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void Tab_ShiftTab_ModalDialogBox_StaysTrappedInModal()
    {
        Button behind = new() { Name = "Behind" };
        behind.AddToRoot();

        StackPanel modal = new();
        FrameworkElement.ModalRoot.Children!.Add(modal.Visual);
        DialogBox dialog = new() { Name = "Dialog" };
        modal.AddChild(dialog);
        Button ok = new() { Name = "Ok" };
        modal.AddChild(ok);
        Driver.Step();
        dialog.IsFocused = true;
        Driver.Step();

        Driver.PressKey(Keys.Tab);
        ok.IsFocused.ShouldBeTrue();
        Driver.PressKey(Keys.Tab);
        dialog.IsFocused.ShouldBeTrue();
        behind.IsFocused.ShouldBeFalse();

        Driver.SetKey(Keys.LeftShift, true);
        Driver.PressKey(Keys.Tab);
        Driver.SetKey(Keys.LeftShift, false);
        ok.IsFocused.ShouldBeTrue();
        behind.IsFocused.ShouldBeFalse();
    }
}
