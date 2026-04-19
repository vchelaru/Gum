using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.Input;
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
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<int>());
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
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<int>());
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
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<int>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        textBox2.OnFocusUpdate();

        textBox2.IsFocused.ShouldBeFalse();
        textBox1.IsFocused.ShouldBeTrue();
    }
}
