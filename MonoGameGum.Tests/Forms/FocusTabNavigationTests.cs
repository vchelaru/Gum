using Gum.Forms.Controls;
using Gum.Wireframe;
using Gum.Input;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Mirror of <c>RaylibGum.Tests.Forms.FocusTabNavigationTests</c> for the MonoGame
/// runtime. The <c>base.HandleKeyboardFocusUpdate()</c> call in
/// <c>TextBoxBase.OnFocusUpdate</c> was already compiled on MonoGame (via the
/// old <c>XNALIKE &amp;&amp; !FRB</c> guard) and still is (widened to <c>!FRB</c>),
/// so these tests serve as a regression check that widening did not change
/// MonoGame behavior.
/// </summary>
public class FocusTabNavigationTests : BaseTestClass
{
    [Fact]
    public void HandleKeyboardFocusUpdate_ShiftTabPressed_MovesFocusToPreviousFocusableSibling()
    {
        StackPanel stackPanel = new StackPanel();
        stackPanel.AddToRoot();

        TextBox textBox1 = new TextBox();
        stackPanel.AddChild(textBox1);
        TextBox textBox2 = new TextBox();
        stackPanel.AddChild(textBox2);

        textBox2.IsFocused = true;

        Mock<IInputReceiverKeyboardMonoGame> keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(Keys.LeftShift)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        textBox2.OnFocusUpdate();

        textBox2.IsFocused.ShouldBeFalse();
        textBox1.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_TabPressed_MovesFocusToNextFocusableSibling()
    {
        StackPanel stackPanel = new StackPanel();
        stackPanel.AddToRoot();

        TextBox textBox1 = new TextBox();
        stackPanel.AddChild(textBox1);
        TextBox textBox2 = new TextBox();
        stackPanel.AddChild(textBox2);

        textBox1.IsFocused = true;

        Mock<IInputReceiverKeyboardMonoGame> keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        textBox1.OnFocusUpdate();

        textBox1.IsFocused.ShouldBeFalse();
        textBox2.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_TabPressedAtEnd_WrapsToFirst()
    {
        StackPanel stackPanel = new StackPanel();
        stackPanel.AddToRoot();

        TextBox textBox1 = new TextBox();
        stackPanel.AddChild(textBox1);
        TextBox textBox2 = new TextBox();
        stackPanel.AddChild(textBox2);

        textBox2.IsFocused = true;

        Mock<IInputReceiverKeyboardMonoGame> keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Tab)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        textBox2.OnFocusUpdate();

        textBox2.IsFocused.ShouldBeFalse();
        textBox1.IsFocused.ShouldBeTrue();
    }

    private static void AddKeyboard(Keys pushed, params Keys[] held)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(pushed)).Returns(true);
        foreach (Keys key in held)
        {
            keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(key)).Returns(true);
        }
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);
    }

    private static (List<Button> Buttons, List<string> Log) CreateLoggedButtons(int count)
    {
        StackPanel stackPanel = new StackPanel();
        stackPanel.AddToRoot();
        List<string> log = new List<string>();
        List<Button> buttons = new List<Button>();
        for (int i = 0; i < count; i++)
        {
            Button button = new Button();
            int n = i;
            button.GotFocus += (_, _) => log.Add("got" + n);
            button.LostFocus += (_, _) => log.Add("lost" + n);
            stackPanel.AddChild(button);
            buttons.Add(button);
        }
        return (buttons, log);
    }

    [Theory]
    [InlineData(Keys.LeftShift)]
    [InlineData(Keys.RightShift)]
    public void HandleKeyboardFocusUpdate_ShiftTab_FocusesOnlyThePreviousElement(Keys shift)
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.Tab, shift);

        buttons[1].OnFocusUpdate();

        buttons[0].IsFocused.ShouldBeTrue();
        log.ShouldBe(new[] { "lost1", "got0" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_Tab_FocusesOnlyTheNextElement()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.Tab);

        buttons[1].OnFocusUpdate();

        buttons[2].IsFocused.ShouldBeTrue();
        log.ShouldBe(new[] { "lost1", "got2" });
    }

    [Theory]
    [InlineData(Keys.LeftControl)]
    [InlineData(Keys.RightControl)]
    [InlineData(Keys.LeftAlt)]
    [InlineData(Keys.RightAlt)]
    public void HandleKeyboardFocusUpdate_TabWithControlOrAlt_DoesNotMoveFocus(Keys modifier)
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.Tab, modifier);

        buttons[1].OnFocusUpdate();

        buttons[1].IsFocused.ShouldBeTrue();
        log.ShouldBeEmpty();
    }

    private static void AddRepeatedKeyboard(Keys typed, params Keys[] held)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        foreach (Keys key in held)
        {
            keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(key)).Returns(true);
        }
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys> { typed });
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);
    }

    private static (List<Button> Buttons, List<string> Log) CreateLoggedButtonsInTwoContainers()
    {
        StackPanel first = new StackPanel();
        first.AddToRoot();
        StackPanel second = new StackPanel();
        second.AddToRoot();
        List<string> log = new List<string>();
        List<Button> buttons = new List<Button>();
        for (int i = 0; i < 4; i++)
        {
            Button button = new Button();
            int n = i;
            button.GotFocus += (_, _) => log.Add("got" + n);
            button.LostFocus += (_, _) => log.Add("lost" + n);
            (i < 2 ? first : second).AddChild(button);
            buttons.Add(button);
        }
        return (buttons, log);
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_ShiftTabWithBothShiftsHeld_FocusesOnlyThePreviousElement()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.Tab, Keys.LeftShift, Keys.RightShift);

        buttons[1].OnFocusUpdate();

        log.ShouldBe(new[] { "lost1", "got0" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_ShiftTabAtFirst_WrapsToLastWithoutTouchingOthers()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[0].IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.Tab, Keys.LeftShift);

        buttons[0].OnFocusUpdate();

        log.ShouldBe(new[] { "lost0", "got2" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_TabAtLast_WrapsToFirstWithoutTouchingOthers()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[2].IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.Tab);

        buttons[2].OnFocusUpdate();

        log.ShouldBe(new[] { "lost2", "got0" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_ShiftTabWithDisabledPrevious_SkipsItWithoutFocusingIt()
    {
        var (buttons, log) = CreateLoggedButtons(4);
        buttons[2].IsFocused = true;
        buttons[1].IsEnabled = false;
        log.Clear();
        AddKeyboard(Keys.Tab, Keys.LeftShift);

        buttons[2].OnFocusUpdate();

        log.ShouldBe(new[] { "lost2", "got0" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_TabWithDisabledNext_SkipsItWithoutFocusingIt()
    {
        var (buttons, log) = CreateLoggedButtons(4);
        buttons[1].IsFocused = true;
        buttons[2].IsEnabled = false;
        log.Clear();
        AddKeyboard(Keys.Tab);

        buttons[1].OnFocusUpdate();

        log.ShouldBe(new[] { "lost1", "got3" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_ShiftTabWithHiddenPrevious_SkipsItWithoutFocusingIt()
    {
        var (buttons, log) = CreateLoggedButtons(4);
        buttons[2].IsFocused = true;
        buttons[1].IsVisible = false;
        log.Clear();
        AddKeyboard(Keys.Tab, Keys.RightShift);

        buttons[2].OnFocusUpdate();

        log.ShouldBe(new[] { "lost2", "got0" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_TabWithHiddenNext_SkipsItWithoutFocusingIt()
    {
        var (buttons, log) = CreateLoggedButtons(4);
        buttons[1].IsFocused = true;
        buttons[2].IsVisible = false;
        log.Clear();
        AddKeyboard(Keys.Tab);

        buttons[1].OnFocusUpdate();

        log.ShouldBe(new[] { "lost1", "got3" });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandleKeyboardFocusUpdate_TabOrShiftTabWithSingleControl_RaisesNoFocusEvents(bool shift)
    {
        var (buttons, log) = CreateLoggedButtons(1);
        buttons[0].IsFocused = true;
        log.Clear();
        if (shift)
        {
            AddKeyboard(Keys.Tab, Keys.LeftShift);
        }
        else
        {
            AddKeyboard(Keys.Tab);
        }

        buttons[0].OnFocusUpdate();

        buttons[0].IsFocused.ShouldBeTrue();
        log.ShouldBeEmpty();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_ShiftHeldWithoutTab_DoesNotMoveFocus()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.A, Keys.LeftShift);

        buttons[1].OnFocusUpdate();

        buttons[1].IsFocused.ShouldBeTrue();
        log.ShouldBeEmpty();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_TabHeldRepeating_MovesForwardOnce()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        log.Clear();
        AddRepeatedKeyboard(Keys.Tab);

        buttons[1].OnFocusUpdate();

        log.ShouldBe(new[] { "lost1", "got2" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_ShiftTabHeldRepeating_MovesBackOnceWithoutFocusingNext()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        log.Clear();
        AddRepeatedKeyboard(Keys.Tab, Keys.LeftShift);

        buttons[1].OnFocusUpdate();

        log.ShouldBe(new[] { "lost1", "got0" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_TabWithTwoKeyboardsOnePressing_MovesOnce()
    {
        var (buttons, log) = CreateLoggedButtons(4);
        buttons[1].IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.Tab);
        FrameworkElement.KeyboardsForUiControl.Add(new Mock<IInputReceiverKeyboardMonoGame>().Object);

        buttons[1].OnFocusUpdate();

        log.ShouldBe(new[] { "lost1", "got2" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_ShiftTabWithTwoKeyboardsOnePressing_MovesOnce()
    {
        var (buttons, log) = CreateLoggedButtons(4);
        buttons[2].IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.Tab, Keys.LeftShift);
        FrameworkElement.KeyboardsForUiControl.Add(new Mock<IInputReceiverKeyboardMonoGame>().Object);

        buttons[2].OnFocusUpdate();

        log.ShouldBe(new[] { "lost2", "got1" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_ShiftTabWithNoReverseCombos_DoesNotTabForward()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        FrameworkElement.TabReverseKeyCombos.Clear();
        log.Clear();
        AddKeyboard(Keys.Tab, Keys.LeftShift);

        buttons[1].OnFocusUpdate();

        buttons[1].IsFocused.ShouldBeTrue();
        log.ShouldBeEmpty();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_CustomForwardComboWithoutHeldKey_ShiftTabStillMovesOnlyBack()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        FrameworkElement.TabKeyCombos.Clear();
        FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Keys.Tab });
        log.Clear();
        AddKeyboard(Keys.Tab, Keys.LeftShift);

        buttons[1].OnFocusUpdate();

        log.ShouldBe(new[] { "lost1", "got0" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_CustomReverseComboCtrlTab_MovesOnlyBack()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        FrameworkElement.TabKeyCombos.Clear();
        FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Keys.Tab });
        FrameworkElement.TabReverseKeyCombos.Clear();
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo { PushedKey = Keys.Tab, HeldKey = Keys.LeftControl });
        log.Clear();
        AddKeyboard(Keys.Tab, Keys.LeftControl);

        buttons[1].OnFocusUpdate();

        log.ShouldBe(new[] { "lost1", "got0" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_CustomForwardComboDownArrow_MovesForward()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Keys.Down });
        log.Clear();
        AddKeyboard(Keys.Down);

        buttons[1].OnFocusUpdate();

        log.ShouldBe(new[] { "lost1", "got2" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_CustomForwardComboNeedingCtrl_PlainTabDoesNotMove()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        FrameworkElement.TabKeyCombos.Clear();
        FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Keys.Tab, HeldKey = Keys.LeftControl });
        log.Clear();
        AddKeyboard(Keys.Tab);

        buttons[1].OnFocusUpdate();

        log.ShouldBeEmpty();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_CustomForwardComboNeedingCtrl_CtrlTabMovesForward()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        FrameworkElement.TabKeyCombos.Clear();
        FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Keys.Tab, HeldKey = Keys.LeftControl });
        log.Clear();
        AddKeyboard(Keys.Tab, Keys.LeftControl);

        buttons[1].OnFocusUpdate();

        log.ShouldBe(new[] { "lost1", "got2" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_ShiftTabAcrossContainers_FocusesOnlyThePreviousContainersLastControl()
    {
        var (buttons, log) = CreateLoggedButtonsInTwoContainers();
        buttons[2].IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.Tab, Keys.LeftShift);

        buttons[2].OnFocusUpdate();

        log.ShouldBe(new[] { "lost2", "got1" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_TabAcrossContainers_FocusesOnlyTheNextContainersFirstControl()
    {
        var (buttons, log) = CreateLoggedButtonsInTwoContainers();
        buttons[1].IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.Tab);

        buttons[1].OnFocusUpdate();

        log.ShouldBe(new[] { "lost1", "got2" });
    }

    [Theory]
    [InlineData("TextBox")]
    [InlineData("CheckBox")]
    [InlineData("Slider")]
    [InlineData("ComboBox")]
    [InlineData("ListBox")]
    [InlineData("RadioButton")]
    [InlineData("ToggleButton")]
    [InlineData("PasswordBox")]
    public void HandleKeyboardFocusUpdate_ShiftTabFromControlType_FocusesOnlyThePreviousElement(string controlType)
    {
        StackPanel stackPanel = new StackPanel();
        stackPanel.AddToRoot();
        List<string> log = new List<string>();
        Button before = new Button();
        before.GotFocus += (_, _) => log.Add("gotBefore");
        Button after = new Button();
        after.GotFocus += (_, _) => log.Add("gotAfter");
        FrameworkElement middle = controlType switch
        {
            "TextBox" => new TextBox(),
            "CheckBox" => new CheckBox(),
            "Slider" => new Slider(),
            "ComboBox" => new ComboBox(),
            "ListBox" => new ListBox(),
            "RadioButton" => new RadioButton(),
            "ToggleButton" => new ToggleButton(),
            _ => new PasswordBox()
        };
        stackPanel.AddChild(before);
        stackPanel.AddChild(middle);
        stackPanel.AddChild(after);
        middle.IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.Tab, Keys.LeftShift);

        ((IInputReceiver)middle).OnFocusUpdate();

        before.IsFocused.ShouldBeTrue();
        log.ShouldBe(new[] { "gotBefore" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_ShiftTabInTextBoxThatAcceptsTab_DoesNotMoveFocus()
    {
        var (buttons, log) = CreateLoggedButtons(1);
        TextBox textBox = new TextBox();
        textBox.AcceptsTab = true;
        buttons[0].Visual.Parent!.Children.Add(textBox.Visual);
        textBox.IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.Tab, Keys.LeftShift);

        textBox.OnFocusUpdate();

        textBox.IsFocused.ShouldBeTrue();
        log.ShouldBeEmpty();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_TabInTextBoxThatAcceptsTab_DoesNotMoveFocus()
    {
        var (buttons, log) = CreateLoggedButtons(1);
        TextBox textBox = new TextBox();
        textBox.AcceptsTab = true;
        buttons[0].Visual.Parent!.Children.Add(textBox.Visual);
        textBox.IsFocused = true;
        log.Clear();
        AddKeyboard(Keys.Tab);

        textBox.OnFocusUpdate();

        textBox.IsFocused.ShouldBeTrue();
        log.ShouldBeEmpty();
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_GamepadDPadDown_FocusesOnlyTheNextElement()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        log.Clear();
        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadDown, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        buttons[1].OnFocusUpdate();

        log.ShouldBe(new[] { "lost1", "got2" });
    }

    [Fact]
    public void HandleKeyboardFocusUpdate_GamepadDPadUp_FocusesOnlyThePreviousElement()
    {
        var (buttons, log) = CreateLoggedButtons(3);
        buttons[1].IsFocused = true;
        log.Clear();
        GamePad gamepad = new GamePad();
        gamepad.SetButtonState(GamepadButton.DPadUp, true);
        gamepad.Activity(1);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        buttons[1].OnFocusUpdate();

        log.ShouldBe(new[] { "lost1", "got0" });
    }
}
