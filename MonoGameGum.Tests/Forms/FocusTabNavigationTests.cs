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
}
