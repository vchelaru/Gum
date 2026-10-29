using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Issue #6934: hiding a focused control, or any ancestor, clears its focus like removing it from a root does.
/// </summary>
public class HideClearsFocusTests : BaseTestClass
{
    private static (StackPanel menu, Button resume, Button options) CreateMenu()
    {
        StackPanel menu = new StackPanel();
        menu.AddToRoot();
        Button resume = new Button();
        menu.AddChild(resume);
        Button options = new Button();
        menu.AddChild(options);
        return (menu, resume, options);
    }

    private static GamePad PressA()
    {
        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.A, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);
        return gamepad;
    }

    #region Pinned behavior

    [Fact]
    public void Hide_UnfocusedControl_DoesNotAffectOtherFocus()
    {
        (_, Button resume, Button options) = CreateMenu();
        resume.IsFocused = true;

        options.IsVisible = false;

        resume.IsFocused.ShouldBeTrue();
        InteractiveGue.CurrentInputReceiver.ShouldBe(resume);
    }

    [Fact]
    public void Hide_UnfocusedPanel_DoesNotRaiseLostFocus()
    {
        (StackPanel menu, Button resume, _) = CreateMenu();
        int lostFocus = 0;
        resume.LostFocus += (_, _) => lostFocus++;

        menu.IsVisible = false;

        lostFocus.ShouldBe(0);
    }

    [Fact]
    public void Disable_FocusedControl_KeepsFocus()
    {
        (_, Button resume, _) = CreateMenu();
        resume.IsFocused = true;

        resume.IsEnabled = false;

        resume.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Hide_PanelWhileUnrelatedControlFocused_KeepsThatFocus()
    {
        (StackPanel menu, _, _) = CreateMenu();
        Button outside = new Button();
        outside.AddToRoot();
        outside.IsFocused = true;

        menu.IsVisible = false;

        outside.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void RemoveFromRoot_FocusedControl_ClearsFocus()
    {
        (StackPanel menu, Button resume, _) = CreateMenu();
        resume.IsFocused = true;

        menu.RemoveFromRoot();

        resume.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    #endregion

    #region Hiding clears focus

    [Fact]
    public void Hide_FocusedControl_ClearsFocus()
    {
        (_, Button resume, _) = CreateMenu();
        resume.IsFocused = true;

        resume.IsVisible = false;

        resume.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void Hide_FocusedControl_RaisesLostFocusOnce()
    {
        (_, Button resume, _) = CreateMenu();
        resume.IsFocused = true;
        int lostFocus = 0;
        resume.LostFocus += (_, _) => lostFocus++;

        resume.IsVisible = false;

        lostFocus.ShouldBe(1);
    }

    [Fact]
    public void Hide_MenuFromFocusedButtonClick_LaterGamepadAIsIgnored()
    {
        (StackPanel menu, Button resume, _) = CreateMenu();
        int clicks = 0;
        resume.Click += (_, _) => { clicks++; menu.IsVisible = false; };
        resume.IsFocused = true;
        PressA();

        InteractiveGue.CurrentInputReceiver!.OnFocusUpdate();
        clicks.ShouldBe(1);
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();

        // The per-frame update only reaches CurrentInputReceiver.
        InteractiveGue.CurrentInputReceiver?.OnFocusUpdate();

        clicks.ShouldBe(1);
        resume.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void Hide_ParentOfFocusedControl_ClearsFocus()
    {
        (StackPanel menu, Button resume, _) = CreateMenu();
        resume.IsFocused = true;

        menu.IsVisible = false;

        resume.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void Hide_VisualOfFocusedControl_ClearsFocus()
    {
        (_, Button resume, _) = CreateMenu();
        resume.IsFocused = true;

        resume.Visual.Visible = false;

        resume.IsFocused.ShouldBeFalse();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void Hide_AncestorAtDepth_ClearsFocus(int depth)
    {
        StackPanel top = new StackPanel();
        top.AddToRoot();
        StackPanel[] chain = new StackPanel[depth];
        StackPanel parent = top;
        chain[0] = top;
        for (int i = 1; i < depth; i++)
        {
            chain[i] = new StackPanel();
            parent.AddChild(chain[i]);
            parent = chain[i];
        }
        Button button = new Button();
        parent.AddChild(button);
        button.IsFocused = true;

        // A middle ancestor, neither the top nor the direct parent when depth allows.
        chain[depth / 2].IsVisible = false;

        button.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void Hide_ThenRemoveFromRoot_DoesNotThrow()
    {
        (StackPanel menu, Button resume, _) = CreateMenu();
        resume.IsFocused = true;
        menu.IsVisible = false;

        menu.RemoveFromRoot();

        resume.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void Hide_FocusedTextBox_ClearsFocusAndStopsInput()
    {
        (StackPanel menu, _, _) = CreateMenu();
        TextBox textBox = new TextBox();
        menu.AddChild(textBox);
        textBox.IsFocused = true;
        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        menu.IsVisible = false;

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void Hide_FocusedWithAllowFocusWhenDisabled_ClearsFocus()
    {
        (_, Button resume, _) = CreateMenu();
        resume.IsEnabled = false;
        resume.AllowFocusWhenDisabled = true;
        resume.IsFocused = true;

        resume.IsVisible = false;

        resume.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void Hide_FocusedControlWithSpatialNavigation_NothingLeftFocused()
    {
        Panel panel = new Panel();
        panel.AddToRoot();
        panel.GamepadNavigationMode = GamepadNavigationMode.Spatial;
        Button outside = new Button();
        panel.AddChild(outside);
        Button resume = new Button();
        panel.AddChild(resume);
        resume.X = 200;
        resume.IsFocused = true;

        resume.IsVisible = false;

        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
        outside.IsFocused.ShouldBeFalse();
        resume.IsFocused.ShouldBeFalse();
    }

    #endregion

    #region Reparenting

    [Fact]
    public void Reparent_FocusedControlIntoHiddenParent_ClearsFocus()
    {
        (_, Button resume, _) = CreateMenu();
        StackPanel hidden = new StackPanel();
        hidden.AddToRoot();
        hidden.IsVisible = false;
        resume.IsFocused = true;

        hidden.AddChild(resume);

        resume.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void Reparent_AncestorOfFocusedControlIntoHiddenParent_ClearsFocus()
    {
        (StackPanel menu, Button resume, _) = CreateMenu();
        StackPanel outer = new StackPanel();
        outer.AddToRoot();
        outer.AddChild(menu);
        StackPanel hidden = new StackPanel();
        hidden.AddToRoot();
        hidden.IsVisible = false;
        resume.IsFocused = true;

        hidden.AddChild(menu);

        resume.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void Reparent_FocusedControlToVisibleParent_KeepsFocusAndWatchesNewAncestors()
    {
        (_, Button resume, _) = CreateMenu();
        StackPanel other = new StackPanel();
        other.AddToRoot();
        resume.IsFocused = true;

        other.AddChild(resume);
        resume.IsFocused.ShouldBeTrue();

        other.IsVisible = false;

        resume.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void Hide_OldParentAfterReparent_KeepsFocus()
    {
        (StackPanel menu, Button resume, _) = CreateMenu();
        StackPanel other = new StackPanel();
        other.AddToRoot();
        resume.IsFocused = true;
        other.AddChild(resume);

        menu.IsVisible = false;

        resume.IsFocused.ShouldBeTrue();
    }

    #endregion

    #region Showing again

    [Fact]
    public void Show_AfterHidingFocusedControl_DoesNotRestoreFocus()
    {
        (StackPanel menu, Button resume, _) = CreateMenu();
        resume.IsFocused = true;
        menu.IsVisible = false;

        menu.IsVisible = true;

        resume.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void Hide_AfterRefocusing_ClearsFocusAgain()
    {
        (StackPanel menu, Button resume, _) = CreateMenu();
        resume.IsFocused = true;
        menu.IsVisible = false;
        menu.IsVisible = true;
        resume.IsFocused = true;

        menu.IsVisible = false;

        resume.IsFocused.ShouldBeFalse();
    }

    #endregion
}
