using System.Linq;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Beginner screen switching: a screen is a container added to Root and removed again, often from
/// inside one of its own button handlers, repeatedly, with focus, hover and popups in flight.
/// </summary>
public class ScreenSwitchingBeginnerTests : BeginnerDriverTestBase
{
    private static Gum.GumService Service => Gum.GumService.Default;

    private StackPanel BuildScreen(string name, out Button button)
    {
        StackPanel screen = new() { Name = name };
        button = new Button { Name = name + "Button", Text = name };
        screen.AddChild(button);
        screen.AddToRoot();
        Driver.Step();
        return screen;
    }

    [Fact]
    public void RemoveFromRoot_ShouldNotRaiseClick_WhenRemovedButtonIsClickedAtItsOldPosition()
    {
        StackPanel screen = BuildScreen("Menu", out Button button);
        (int x, int y) = Driver.CenterOf("MenuButton");
        int clicks = 0;
        button.Click += (_, _) => clicks++;

        screen.RemoveFromRoot();
        Driver.Step();
        Driver.Click(x, y);

        clicks.ShouldBe(0);
        Service.Root.Children!.ShouldNotContain(screen.Visual);
    }

    [Fact]
    public void RemoveFromRoot_ShouldNotThrow_WhenScreenRemovesItselfInsideButtonClick()
    {
        StackPanel menu = BuildScreen("Menu", out Button play);
        StackPanel? game = null;
        play.Click += (_, _) =>
        {
            menu.RemoveFromRoot();
            game = new StackPanel { Name = "Game" };
            game.AddChild(new Button { Name = "GameButton" });
            game.AddToRoot();
        };

        Should.NotThrow(() => Driver.Click("MenuButton"));
        Driver.Step(2);

        Service.Root.Children!.ShouldContain(game!.Visual);
        Service.Root.Children!.ShouldNotContain(menu.Visual);
    }

    [Fact]
    public void RemoveFromRoot_ShouldLeaveOneScreen_WhenSwitchedBackAndForthManyTimes()
    {
        StackPanel menu = new() { Name = "Menu" };
        menu.AddChild(new Button());
        StackPanel options = new() { Name = "Options" };
        options.AddChild(new Button());
        menu.AddToRoot();

        for (int i = 0; i < 25; i++)
        {
            menu.RemoveFromRoot();
            options.AddToRoot();
            Driver.Step();
            options.RemoveFromRoot();
            menu.AddToRoot();
            Driver.Step();
        }

        Service.Root.Children!.Count.ShouldBe(1);
        Service.Root.Children![0].ShouldBeSameAs(menu.Visual);
        options.Visual.Parent.ShouldBeNull();
    }

    [Fact]
    public void Click_ShouldWork_WhenScreenIsRemovedAndAddedBackToRoot()
    {
        StackPanel screen = BuildScreen("Menu", out Button button);
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        screen.RemoveFromRoot();
        Driver.Step();
        screen.AddToRoot();
        Driver.Step();

        Driver.Click("MenuButton");

        clicks.ShouldBe(1);
    }

    [Fact]
    public void Click_ShouldNotRaise_WhenButtonVisualIsHiddenAfterBeingHovered()
    {
        Button button = AddToRoot(new Button { Text = "Hi" }, "Btn");
        int clicks = 0;
        button.Click += (_, _) => clicks++;
        Driver.MoveTo("Btn");
        (int x, int y) = Driver.CenterOf("Btn");

        button.Visual.Visible = false;
        Driver.Step();
        Driver.Click(x, y);

        clicks.ShouldBe(0);
    }

    [Fact]
    public void Cursor_VisualOver_ShouldNotBeRemovedButton_AfterScreenIsRemovedWhileHovered()
    {
        StackPanel screen = BuildScreen("Menu", out Button button);
        Driver.MoveTo("MenuButton");
        Driver.Cursor.VisualOver.ShouldBeSameAs(button.Visual);

        screen.RemoveFromRoot();
        Driver.Step(2);

        Driver.Cursor.VisualOver.ShouldNotBeSameAs(button.Visual);
    }

    [Fact]
    public void Button_ShouldNotShowPushedState_WhenRemovedWhilePushedAndAddedBack()
    {
        StackPanel screen = BuildScreen("Menu", out Button button);
        string lastState = string.Empty;
        foreach (string state in new[] { FrameworkElement.EnabledStateName, FrameworkElement.HighlightedStateName, FrameworkElement.PushedStateName })
        {
            button.GetState(state).Apply = () => lastState = state;
        }
        Driver.MoveTo("MenuButton");
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: true);
        lastState.ShouldBe(FrameworkElement.PushedStateName);

        screen.RemoveFromRoot();
        Driver.SetButton(Gum.Automation.MouseButton.Primary, held: false);
        screen.AddToRoot();
        Driver.MoveTo(700, 500);
        Driver.Step(2);

        lastState.ShouldBe(FrameworkElement.EnabledStateName);
    }

    [Fact]
    public void Typing_ShouldNotChangeRemovedTextBox_WhenScreenWithFocusedTextBoxIsRemoved()
    {
        StackPanel screen = new() { Name = "Form" };
        TextBox textBox = new() { Name = "Name" };
        screen.AddChild(textBox);
        screen.AddToRoot();
        Driver.Step();
        Driver.Click("Name");
        textBox.IsFocused.ShouldBeTrue();

        screen.RemoveFromRoot();
        Driver.Step();
        Driver.Type("abc");

        textBox.Text.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void RemoveFromRoot_ShouldCloseDropDown_WhenScreenWithOpenComboBoxIsRemoved()
    {
        StackPanel screen = new() { Name = "Settings" };
        ComboBox combo = new() { Name = "Quality" };
        combo.Items.Add("Low");
        combo.Items.Add("High");
        screen.AddChild(combo);
        screen.AddToRoot();
        Driver.Step();
        Driver.Click("Quality");
        Service.PopupRoot.Children!.Count.ShouldBeGreaterThan(0);

        screen.RemoveFromRoot();
        Driver.Step(2);

        Service.PopupRoot.Children!.Count.ShouldBe(0);
    }
}
