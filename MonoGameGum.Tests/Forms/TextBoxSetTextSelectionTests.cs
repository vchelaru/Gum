using Gum.Forms.Controls;
using Shouldly;
using Xunit;
using GumKeys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Setting Text or Password from code pulls a selection that now points past the end of the text back
/// inside it, so the next typed character appends instead of replacing the whole new text.
/// </summary>
public class TextBoxSetTextSelectionTests : BeginnerDriverTestBase
{
    private TextBox CreateFocusedTextBox()
    {
        TextBox textBox = new TextBox();
        textBox.AddToRoot();
        textBox.IsFocused = true;
        Driver.Step(2);
        return textBox;
    }

    private PasswordBox CreateFocusedPasswordBox()
    {
        PasswordBox passwordBox = new PasswordBox();
        passwordBox.AddToRoot();
        passwordBox.IsFocused = true;
        Driver.Step(2);
        return passwordBox;
    }

    private void SelectAll()
    {
        Driver.SetKey(GumKeys.LeftControl, true);
        Driver.PressKey(GumKeys.A);
        Driver.SetKey(GumKeys.LeftControl, false);
    }

    [Fact]
    public void TextSetFromCode_ShorterThanSelection_SelectionAndCaretAreClamped()
    {
        TextBox textBox = CreateFocusedTextBox();
        Driver.Type("hello world");
        SelectAll();

        textBox.Text = "hi";
        Driver.Step(2);

        textBox.CaretIndex.ShouldBeLessThanOrEqualTo(2);
        (textBox.SelectionStart + textBox.SelectionLength).ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public void TextSetFromCode_ShorterThanSelection_TypingAppendsToTheNewText()
    {
        TextBox textBox = CreateFocusedTextBox();
        Driver.Type("hello world");
        SelectAll();
        textBox.Text = "hi";
        Driver.Step(2);

        Driver.Type("!");

        textBox.Text.ShouldBe("hi!");
    }

    [Fact]
    public void TextSetFromCode_ShorterThanSelection_RaisesSelectionChangedOnce()
    {
        TextBox textBox = CreateFocusedTextBox();
        Driver.Type("hello world");
        SelectAll();
        int raised = 0;
        textBox.SelectionChanged += (_, _) => raised++;

        textBox.Text = "hi";

        raised.ShouldBe(1);
    }

    [Fact]
    public void TextSetFromCode_WithNoSelection_DoesNotRaiseSelectionChanged()
    {
        TextBox textBox = CreateFocusedTextBox();
        Driver.Type("hello world");
        int raised = 0;
        textBox.SelectionChanged += (_, _) => raised++;

        textBox.Text = "hello there";

        raised.ShouldBe(0);
    }

    [Fact]
    public void DeleteSelection_PartOfTheText_RaisesSelectionChangedOnce()
    {
        TextBox textBox = CreateFocusedTextBox();
        Driver.Type("hello world");
        textBox.SelectionStart = 3;
        textBox.SelectionLength = 5;
        int raised = 0;
        textBox.SelectionChanged += (_, _) => raised++;

        textBox.DeleteSelection();

        textBox.Text.ShouldBe("helrld");
        raised.ShouldBe(1);
    }

    [Fact]
    public void PasswordSetFromCode_ShorterThanSelection_SelectionAndCaretAreClamped()
    {
        PasswordBox passwordBox = CreateFocusedPasswordBox();
        Driver.Type("hello world");
        SelectAll();

        passwordBox.Password = "hi";
        Driver.Step(2);

        passwordBox.CaretIndex.ShouldBeLessThanOrEqualTo(2);
        (passwordBox.SelectionStart + passwordBox.SelectionLength).ShouldBeLessThanOrEqualTo(2);
    }

    [Fact]
    public void PasswordSetFromCode_ShorterThanSelection_TypingAppendsToTheNewPassword()
    {
        PasswordBox passwordBox = CreateFocusedPasswordBox();
        Driver.Type("hello world");
        SelectAll();
        passwordBox.Password = "hi";
        Driver.Step(2);

        Driver.Type("!");

        passwordBox.Password.ShouldBe("hi!");
    }

    [Fact]
    public void PasswordDeleteSelection_PartOfThePassword_RaisesSelectionChangedOnce()
    {
        PasswordBox passwordBox = CreateFocusedPasswordBox();
        Driver.Type("hello world");
        passwordBox.SelectionStart = 3;
        passwordBox.SelectionLength = 5;
        int raised = 0;
        passwordBox.SelectionChanged += (_, _) => raised++;

        passwordBox.DeleteSelection();

        passwordBox.Password.ShouldBe("helrld");
        raised.ShouldBe(1);
    }
}
