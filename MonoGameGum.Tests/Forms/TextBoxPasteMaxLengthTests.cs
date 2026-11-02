using Gum.Forms.Controls;
using Gum.Forms.Input;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A paste that does not fit MaxLength (#7099) follows the typing rule: the text is inserted at the caret,
/// then cut to MaxLength from the end, never leaving half of an emoji. It must not throw.
/// </summary>
public class TextBoxPasteMaxLengthTests : BaseTestClass
{
    const string Emoji = "\U0001F600";

    // text, caret, selectionStart, selectionLength, clipboard, maxLength, expected text, expected caret
    public static TheoryData<string, int, int, int, string, int?, string, int> Cases => new()
    {
        { "abc", 3, 0, 0, "defgh", 5, "abcde", 5 },                  // overflows at the end
        { "abc", 3, 0, 0, "def", 5, "abcde", 5 },                    // one too many
        { "abc", 3, 0, 0, "de", 5, "abcde", 5 },                     // fills exactly
        { "abc", 3, 0, 0, "d", 5, "abcd", 4 },                       // fits
        { "abc", 1, 0, 0, "XYZ", 5, "aXYZb", 4 },                    // mid-text overflow drops the tail
        { "abc", 1, 0, 0, "VWXYZ", 5, "aVWXY", 5 },                  // pasted text alone fills the box
        { "abcde", 3, 1, 2, "XYZW", 5, "aXYZW", 5 },                 // selection is replaced first
        { "abcde", 3, 1, 2, "X", 5, "aXde", 2 },                     // selection replaced, fits
        { "abc", 3, 0, 0, Emoji + "z", 4, "abc", 3 },                // a pair that straddles the limit is dropped whole
        { "abc", 3, 0, 0, Emoji + "z", 5, "abc" + Emoji, 5 },        // a pair that fits stays whole
        { "abc", 3, 0, 0, "", 5, "abc", 3 },                         // empty clipboard changes nothing
        { "abc", 3, 0, 0, "defgh", null, "abcdefgh", 8 },            // no MaxLength
        { "", 0, 0, 0, "abc", 0, "", 0 },                            // MaxLength 0 takes nothing
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Paste_ShouldInsertWhatFitsAndNotThrow_TextBox(
        string text, int caret, int selectionStart, int selectionLength, string clipboard, int? maxLength,
        string expectedText, int expectedCaret)
    {
        Gum.Clipboard.ClipboardImplementation.PushStringToClipboard(clipboard);
        TextBox textBox = new();
        textBox.IsFocused = true;
        textBox.Text = text;
        textBox.MaxLength = maxLength;
        textBox.CaretIndex = caret;
        textBox.SelectionStart = selectionStart;
        textBox.SelectionLength = selectionLength;

        Should.NotThrow(() => textBox.HandleKeyDown(Keys.V, false, false, true));

        textBox.Text.ShouldBe(expectedText);
        textBox.CaretIndex.ShouldBe(expectedCaret);
        textBox.SelectionLength.ShouldBe(0);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Paste_ShouldInsertWhatFitsAndNotThrow_PasswordBox(
        string text, int caret, int selectionStart, int selectionLength, string clipboard, int? maxLength,
        string expectedText, int expectedCaret)
    {
        Gum.Clipboard.ClipboardImplementation.PushStringToClipboard(clipboard);
        PasswordBox passwordBox = new();
        passwordBox.IsFocused = true;
        passwordBox.Password = text;
        passwordBox.MaxLength = maxLength;
        passwordBox.CaretIndex = caret;
        passwordBox.SelectionStart = selectionStart;
        passwordBox.SelectionLength = selectionLength;

        Should.NotThrow(() => passwordBox.HandleKeyDown(Keys.V, false, false, true));

        passwordBox.Password.ShouldBe(expectedText);
        passwordBox.CaretIndex.ShouldBe(expectedCaret);
        passwordBox.SelectionLength.ShouldBe(0);
    }

    [Fact]
    public void Paste_Crlf_ShouldCountAsOneCharacterTowardMaxLength()
    {
        Gum.Clipboard.ClipboardImplementation.PushStringToClipboard("x\r\ny");
        TextBox textBox = new();
        textBox.IsFocused = true;
        textBox.Text = "ab";
        textBox.MaxLength = 4;
        textBox.CaretIndex = 2;

        Should.NotThrow(() => textBox.HandleKeyDown(Keys.V, false, false, true));

        textBox.Text.ShouldBe("abx\n");
        textBox.CaretIndex.ShouldBe(4);
    }

    [Theory]
    [InlineData("defgh")]
    [InlineData("de")]
    public void Paste_ShouldRaiseTextChangedOnce(string clipboard)
    {
        Gum.Clipboard.ClipboardImplementation.PushStringToClipboard(clipboard);
        TextBox textBox = new();
        textBox.IsFocused = true;
        textBox.Text = "abc";
        textBox.MaxLength = 5;
        textBox.CaretIndex = 3;
        int count = 0;
        textBox.TextChanged += (_, _) => count++;

        textBox.HandleKeyDown(Keys.V, false, false, true);

        count.ShouldBe(1);
    }

    [Fact]
    public void Paste_ShouldRaisePasswordChangedOnce()
    {
        Gum.Clipboard.ClipboardImplementation.PushStringToClipboard("defgh");
        PasswordBox passwordBox = new();
        passwordBox.IsFocused = true;
        passwordBox.Password = "abc";
        passwordBox.MaxLength = 5;
        passwordBox.CaretIndex = 3;
        int count = 0;
        passwordBox.PasswordChanged += (_, _) => count++;

        passwordBox.HandleKeyDown(Keys.V, false, false, true);

        count.ShouldBe(1);
    }

    [Fact]
    public void Paste_OverflowAtEnd_ThenTyping_ShouldNotThrow()
    {
        Gum.Clipboard.ClipboardImplementation.PushStringToClipboard("defgh");
        PasswordBox passwordBox = new();
        passwordBox.IsFocused = true;
        passwordBox.Password = "abc";
        passwordBox.MaxLength = 5;
        passwordBox.CaretIndex = 3;
        passwordBox.HandleKeyDown(Keys.V, false, false, true);

        Should.NotThrow(() => passwordBox.HandleBackspace());

        passwordBox.Password.ShouldBe("abcd");
    }
}
