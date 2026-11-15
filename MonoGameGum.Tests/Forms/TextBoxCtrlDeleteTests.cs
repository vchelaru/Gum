using Gum.Forms.Controls;
using Gum.Forms.Input;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Ctrl+Delete removes from the caret to the start of the next word (the Ctrl+Right target), or the
/// selection when there is one.
/// </summary>
public class TextBoxCtrlDeleteTests : BaseTestClass
{
    static TextBox Create(string text, int caret)
    {
        TextBox textBox = new();
        textBox.Width = 300;
        textBox.Text = text;
        textBox.IsFocused = true;
        textBox.CaretIndex = caret;
        return textBox;
    }

    static void CtrlDelete(TextBoxBase textBox) => textBox.HandleKeyDown(Keys.Delete, false, false, true);

    // Each expected string holds the resulting text for caret index 0..length, separated by '|'.
    [Theory]
    [InlineData("", "")]
    [InlineData("foo   bar", "bar|fbar|fobar|foobar|foo bar|foo  bar|foo   |foo   b|foo   ba|foo   bar")]
    [InlineData("  foo", "foo| foo|  |  f|  fo|  foo")]
    [InlineData("foo  ", "|f|fo|foo|foo |foo  ")]
    [InlineData("   ", "| |  |   ")]
    [InlineData("ab\n\ncd", "cd|acd|abcd|ab\ncd|ab\n\n|ab\n\nc|ab\n\ncd")]
    [InlineData("a\t\tb", "b|ab|a\tb|a\t\t|a\t\tb")]
    public void CtrlDelete_RemovesToTheNextWordStart_ForEveryCaret(string text, string expectedPerCaret)
    {
        string[] expected = expectedPerCaret.Split('|');
        for (int caret = 0; caret <= text.Length; caret++)
        {
            TextBox textBox = Create(text, caret);
            CtrlDelete(textBox);
            textBox.Text.ShouldBe(expected[caret], $"caret {caret}");
        }
    }

    [Fact]
    public void CtrlDelete_KeepsTheCaretWhereItWas()
    {
        TextBox textBox = Create("foo   bar", 1);
        CtrlDelete(textBox);
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void CtrlDelete_WithSelection_DeletesOnlyTheSelection()
    {
        TextBox textBox = Create("foo   bar", 0);
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 3;
        CtrlDelete(textBox);
        textBox.Text.ShouldBe("f  bar");
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void CtrlDelete_WhileReadOnly_DoesNothing()
    {
        TextBox textBox = Create("foo bar", 0);
        textBox.IsReadOnly = true;
        CtrlDelete(textBox);
        textBox.Text.ShouldBe("foo bar");
    }

    [Fact]
    public void CtrlDelete_WithMaxLength_StillDeletes()
    {
        TextBox textBox = Create("ab cd", 0);
        textBox.MaxLength = 5;
        CtrlDelete(textBox);
        textBox.Text.ShouldBe("cd");
    }

    [Fact]
    public void PlainDelete_StillRemovesOneCharacter()
    {
        TextBox textBox = Create("foo bar", 0);
        textBox.HandleKeyDown(Keys.Delete, false, false, false);
        textBox.Text.ShouldBe("oo bar");
    }

    [Fact]
    public void PasswordBox_CtrlDelete_RemovesToTheEndBecauseTheMaskHasNoWords()
    {
        PasswordBox passwordBox = new();
        passwordBox.Width = 300;
        passwordBox.Password = "foo bar";
        passwordBox.IsFocused = true;
        passwordBox.CaretIndex = 2;
        CtrlDelete(passwordBox);
        passwordBox.Password.ShouldBe("fo");
    }

    [Fact]
    public void PasswordBox_CtrlDelete_WithSelection_DeletesOnlyTheSelection()
    {
        PasswordBox passwordBox = new();
        passwordBox.Width = 300;
        passwordBox.Password = "foobar";
        passwordBox.IsFocused = true;
        passwordBox.SelectionStart = 1;
        passwordBox.SelectionLength = 2;
        CtrlDelete(passwordBox);
        passwordBox.Password.ShouldBe("fbar");
    }
}
