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
/// What Enter and Escape do to a text box's text and focus (#7061). Enter and Escape reach the box twice per press:
/// as a key (HandleKeyDown) and as a character ('\r', ESC) from the platform's text input (HandleCharEntered).
/// </summary>
public class TextBoxEnterEscapeTests : BaseTestClass
{
    private const char EscapeCharacter = (char)27;

    private static TextBox CreateTextBox(string text, bool acceptsReturn = false)
    {
        TextBox textBox = new();
        textBox.AddToRoot();
        textBox.AcceptsReturn = acceptsReturn;
        textBox.Text = text;
        textBox.IsFocused = true;
        return textBox;
    }

    private static PasswordBox CreatePasswordBox(string password)
    {
        PasswordBox passwordBox = new();
        passwordBox.AddToRoot();
        passwordBox.Password = password;
        passwordBox.IsFocused = true;
        return passwordBox;
    }

    // Runs one frame of the receiver chain with Escape pushed: ancestors preview first, then the receiver acts.
    private static void PushEscape(Gum.Wireframe.GraphicalUiElement root)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(Keys.Escape)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(root, new Mock<ICursor>().Object, keyboard.Object, 0);
    }

    #region Text

    [Fact]
    public void HandleCharEntered_Enter_SingleLine_ShouldNotChangeText()
    {
        TextBox textBox = CreateTextBox("abc");
        textBox.CaretIndex = 1;

        textBox.HandleCharEntered('\r');

        textBox.Text.ShouldBe("abc");
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void HandleCharEntered_Enter_AcceptsReturn_ShouldInsertNewline()
    {
        TextBox textBox = CreateTextBox("abc", acceptsReturn: true);
        textBox.CaretIndex = 1;

        textBox.HandleCharEntered('\r');

        textBox.Text.ShouldBe("a\nbc");
    }

    [Fact]
    public void HandleCharEntered_Enter_AcceptsReturn_WithSelection_ShouldReplaceSelectionWithNewline()
    {
        TextBox textBox = CreateTextBox("abcd", acceptsReturn: true);
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 2;

        textBox.HandleCharEntered('\r');

        textBox.Text.ShouldBe("a\nd");
    }

    [Fact]
    public void HandleCharEntered_Escape_ShouldNotChangeText()
    {
        TextBox textBox = CreateTextBox("abc");

        textBox.HandleCharEntered(EscapeCharacter);

        textBox.Text.ShouldBe("abc");
    }

    [Fact]
    public void HandleCharEntered_Enter_PasswordBox_ShouldNotChangePassword()
    {
        PasswordBox passwordBox = CreatePasswordBox("abc");
        passwordBox.CaretIndex = 1;

        passwordBox.HandleCharEntered('\r');

        passwordBox.Password.ShouldBe("abc");
    }

    [Fact]
    public void HandleCharEntered_Enter_SingleLine_WithSelection_ShouldKeepTextAndSelection()
    {
        TextBox textBox = CreateTextBox("abcd");
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 2;

        textBox.HandleCharEntered('\r');

        textBox.Text.ShouldBe("abcd");
        textBox.SelectionStart.ShouldBe(1);
        textBox.SelectionLength.ShouldBe(2);
    }

    [Fact]
    public void HandleCharEntered_Newline_SingleLine_WithSelection_ShouldKeepText()
    {
        TextBox textBox = CreateTextBox("abcd");
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 2;

        textBox.HandleCharEntered('\n');

        textBox.Text.ShouldBe("abcd");
    }

    [Fact]
    public void HandleCharEntered_Escape_WithSelection_ShouldKeepTextAndSelection()
    {
        TextBox textBox = CreateTextBox("abcd");
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 2;

        textBox.HandleCharEntered(EscapeCharacter);

        textBox.Text.ShouldBe("abcd");
        textBox.SelectionLength.ShouldBe(2);
    }

    [Fact]
    public void HandleCharEntered_Enter_PasswordBox_WithSelection_ShouldKeepPasswordAndSelection()
    {
        PasswordBox passwordBox = CreatePasswordBox("abcd");
        passwordBox.SelectionStart = 1;
        passwordBox.SelectionLength = 2;

        passwordBox.HandleCharEntered('\r');

        passwordBox.Password.ShouldBe("abcd");
        passwordBox.SelectionLength.ShouldBe(2);
    }

    [Fact]
    public void HandleCharEntered_Letter_WithSelection_ShouldReplaceSelection()
    {
        TextBox textBox = CreateTextBox("abcd");
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 2;

        textBox.HandleCharEntered('x');

        textBox.Text.ShouldBe("axd");
    }

    #endregion

    #region Escape reaching a text box through ancestors

    [Fact]
    public void Escape_TextBoxAtRoot_ShouldStayFocused()
    {
        TextBox textBox = CreateTextBox("abc");

        PushEscape(textBox.Visual);

        textBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Escape_TextBoxInScrollViewer_ShouldStayFocused()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.AddToRoot();
        TextBox textBox = new();
        scrollViewer.AddChild(textBox);
        textBox.IsFocused = true;

        PushEscape(scrollViewer.Visual);

        textBox.IsFocused.ShouldBeTrue();
        scrollViewer.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void Escape_PasswordBoxInScrollViewer_ShouldStayFocused()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.AddToRoot();
        PasswordBox passwordBox = new();
        scrollViewer.AddChild(passwordBox);
        passwordBox.IsFocused = true;

        PushEscape(scrollViewer.Visual);

        passwordBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void Escape_ButtonInScrollViewer_ShouldFocusScrollViewer()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.AddToRoot();
        Button button = new();
        scrollViewer.AddChild(button);
        button.IsFocused = true;

        PushEscape(scrollViewer.Visual);

        scrollViewer.IsFocused.ShouldBeTrue();
        button.IsFocused.ShouldBeFalse();
    }

    [Fact]
    public void Escape_TextBoxInListBox_ShouldStayFocused()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        TextBox textBox = new();
        listBox.AddChild(textBox);
        textBox.IsFocused = true;

        PushEscape(listBox.Visual);

        textBox.IsFocused.ShouldBeTrue();
        listBox.IsFocused.ShouldBeFalse();
    }

    #endregion
}
