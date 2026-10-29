using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// EditCommitted (Enter) and EditCancelled (Escape) on TextBoxBase (#7061). They are raised from the key path
/// only and never change focus.
/// </summary>
public class TextBoxEditEventsTests : BaseTestClass
{
    private int _committed;
    private int _cancelled;

    private T Watch<T>(T box) where T : TextBoxBase
    {
        box.EditCommitted += (_, _) => _committed++;
        box.EditCancelled += (_, _) => _cancelled++;
        return box;
    }

    private TextBox CreateTextBox(bool acceptsReturn = false, bool focused = true)
    {
        TextBox textBox = new();
        textBox.AddToRoot();
        textBox.AcceptsReturn = acceptsReturn;
        textBox.Text = "abc";
        textBox.IsFocused = focused;
        return Watch(textBox);
    }

    private PasswordBox CreatePasswordBox(bool focused = true)
    {
        PasswordBox passwordBox = new();
        passwordBox.AddToRoot();
        passwordBox.Password = "abc";
        passwordBox.IsFocused = focused;
        return Watch(passwordBox);
    }

    private static Mock<IInputReceiverKeyboardMonoGame> CreateKeyboard(Keys keyPushed, string stringTyped)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys> { keyPushed });
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(keyPushed)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.GetStringTyped()).Returns(stringTyped);
        return keyboard;
    }

    #region EditCommitted

    [Fact]
    public void Enter_SingleLineTextBox_ShouldRaiseEditCommitted()
    {
        TextBox textBox = CreateTextBox();

        textBox.HandleKeyDown(Keys.Enter, false, false, false);

        _committed.ShouldBe(1);
        _cancelled.ShouldBe(0);
    }

    [Fact]
    public void Enter_TextBoxAcceptingReturn_ShouldNotRaiseEditCommitted()
    {
        TextBox textBox = CreateTextBox(acceptsReturn: true);

        textBox.HandleKeyDown(Keys.Enter, false, false, false);

        _committed.ShouldBe(0);
    }

    [Fact]
    public void Enter_PasswordBox_ShouldRaiseEditCommitted()
    {
        PasswordBox passwordBox = CreatePasswordBox();

        passwordBox.HandleKeyDown(Keys.Enter, false, false, false);

        _committed.ShouldBe(1);
    }

    [Fact]
    public void Enter_PasswordBoxWithAcceptsReturn_ShouldRaiseEditCommitted()
    {
        PasswordBox passwordBox = CreatePasswordBox();
        passwordBox.AcceptsReturn = true;

        passwordBox.HandleKeyDown(Keys.Enter, false, false, false);

        _committed.ShouldBe(1);
    }

    [Fact]
    public void Enter_UnfocusedTextBox_ShouldNotRaiseEditCommitted()
    {
        TextBox textBox = CreateTextBox(focused: false);

        textBox.HandleKeyDown(Keys.Enter, false, false, false);

        _committed.ShouldBe(0);
    }

    [Fact]
    public void Enter_ShouldNotChangeFocus()
    {
        TextBox textBox = CreateTextBox();

        textBox.HandleKeyDown(Keys.Enter, false, false, false);

        textBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Enter_HandlerThatUnfocuses_ShouldLeaveBoxUnfocused()
    {
        TextBox textBox = CreateTextBox();
        textBox.EditCommitted += (_, _) => textBox.IsFocused = false;

        textBox.HandleKeyDown(Keys.Enter, false, false, false);

        textBox.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void Enter_FullKeyboardPath_ShouldRaiseEditCommittedOnce()
    {
        // The platform delivers the Enter key and an '\r' character; only the key raises the event.
        TextBox textBox = CreateTextBox();
        Mock<IInputReceiverKeyboardMonoGame> keyboard = CreateKeyboard(Keys.Enter, "\r");

        textBox.DoKeyboardAction(keyboard.Object);

        _committed.ShouldBe(1);
    }

    [Fact]
    public void Enter_FullKeyboardPath_PasswordBox_ShouldRaiseEditCommittedOnce()
    {
        PasswordBox passwordBox = CreatePasswordBox();
        Mock<IInputReceiverKeyboardMonoGame> keyboard = CreateKeyboard(Keys.Enter, "\r");

        passwordBox.DoKeyboardAction(keyboard.Object);

        _committed.ShouldBe(1);
    }

    [Fact]
    public void Enter_CharacterOnly_ShouldNotRaiseEditCommitted()
    {
        // An on-screen keyboard or a script adds characters without keys.
        TextBox textBox = CreateTextBox();

        textBox.HandleCharEntered('\r');

        _committed.ShouldBe(0);
    }

    #endregion

    #region EditCancelled

    [Fact]
    public void Escape_TextBox_ShouldRaiseEditCancelled()
    {
        TextBox textBox = CreateTextBox();

        textBox.HandleKeyDown(Keys.Escape, false, false, false);

        _cancelled.ShouldBe(1);
        _committed.ShouldBe(0);
    }

    [Fact]
    public void Escape_TextBoxAcceptingReturn_ShouldRaiseEditCancelled()
    {
        TextBox textBox = CreateTextBox(acceptsReturn: true);

        textBox.HandleKeyDown(Keys.Escape, false, false, false);

        _cancelled.ShouldBe(1);
    }

    [Fact]
    public void Escape_PasswordBox_ShouldRaiseEditCancelled()
    {
        PasswordBox passwordBox = CreatePasswordBox();

        passwordBox.HandleKeyDown(Keys.Escape, false, false, false);

        _cancelled.ShouldBe(1);
    }

    [Fact]
    public void Escape_UnfocusedTextBox_ShouldNotRaiseEditCancelled()
    {
        TextBox textBox = CreateTextBox(focused: false);

        textBox.HandleKeyDown(Keys.Escape, false, false, false);

        _cancelled.ShouldBe(0);
    }

    [Fact]
    public void Escape_ShouldNotChangeFocusOrText()
    {
        TextBox textBox = CreateTextBox();

        textBox.HandleKeyDown(Keys.Escape, false, false, false);

        textBox.IsFocused.ShouldBeTrue();
        textBox.Text.ShouldBe("abc");
    }

    [Fact]
    public void Escape_FullKeyboardPath_ShouldRaiseEditCancelledOnce()
    {
        TextBox textBox = CreateTextBox();
        Mock<IInputReceiverKeyboardMonoGame> keyboard = CreateKeyboard(Keys.Escape, "\u001b");

        textBox.DoKeyboardAction(keyboard.Object);

        _cancelled.ShouldBe(1);
    }

    [Fact]
    public void OtherKeys_ShouldRaiseNeitherEvent()
    {
        TextBox textBox = CreateTextBox();

        textBox.HandleKeyDown(Keys.Left, false, false, false);
        textBox.HandleKeyDown(Keys.Tab, false, false, false);

        _committed.ShouldBe(0);
        _cancelled.ShouldBe(0);
    }

    [Fact]
    public void Escape_TextBoxInScrollViewer_ShouldRaiseEditCancelled()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.AddToRoot();
        TextBox textBox = Watch(new TextBox());
        scrollViewer.AddChild(textBox);
        textBox.IsFocused = true;
        Mock<IInputReceiverKeyboardMonoGame> keyboard = CreateKeyboard(Keys.Escape, null!);
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            scrollViewer.Visual, new Mock<ICursor>().Object, keyboard.Object, 0);

        _cancelled.ShouldBe(1);
        textBox.IsFocused.ShouldBeTrue();
    }

    #endregion
}
