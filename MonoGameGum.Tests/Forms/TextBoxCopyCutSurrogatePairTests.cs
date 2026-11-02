using Gum.Clipboard;
using Gum.Forms.Controls;
using Gum.Forms.Input;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Copy and Cut take the same range the selection delete does (#7113): a selection edge between the
/// halves of an emoji (or of a "\r\n"), which only code can set, widens to the whole character instead of
/// putting a lone surrogate half on the clipboard. PasswordBox never puts its text on the clipboard.
/// </summary>
public class TextBoxCopyCutSurrogatePairTests : BaseTestClass
{
    const string Emoji = "\U0001F600";
    const string Sentinel = "sentinel-7113";

    // text, selection start, selection length, expected clipboard, expected text after Cut
    public static TheoryData<string, int, int, string, string> Cases => new()
    {
        { "a" + Emoji + "b", 2, 2, Emoji + "b", "a" },     // start between the halves
        { "a" + Emoji + "b", 0, 2, "a" + Emoji, "b" },     // end between the halves
        { "a" + Emoji + "b", 2, 1, Emoji, "ab" },          // second half only
        { "a" + Emoji + "b", 1, 1, Emoji, "ab" },          // first half only
        { "a" + Emoji + "b", 2, 0, Sentinel, "a" + Emoji + "b" }, // nothing selected between the halves
        { "a" + Emoji + "b", 1, 2, Emoji, "ab" },          // already whole
        { "a" + Emoji + "b", 0, 4, "a" + Emoji + "b", "" },     // everything
        { Emoji + Emoji, 1, 2, Emoji + Emoji, "" },        // both edges inside a pair
        { "abcd", 1, 2, "bc", "ad" },                      // ASCII unchanged
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Copy_ShouldPutWholeCharactersOnTheClipboard(
        string text, int selectionStart, int selectionLength, string expectedClipboard, string _)
    {
        ClipboardImplementation.PushStringToClipboard(Sentinel);
        TextBox textBox = CreateTextBox(text);
        textBox.SelectionStart = selectionStart;
        textBox.SelectionLength = selectionLength;

        textBox.HandleKeyDown(Keys.C, false, false, true);

        ClipboardImplementation.GetText(null).ShouldBe(expectedClipboard);
        textBox.Text.ShouldBe(text);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Cut_ShouldPutWholeCharactersOnTheClipboardAndRemoveThem(
        string text, int selectionStart, int selectionLength, string expectedClipboard, string expectedText)
    {
        ClipboardImplementation.PushStringToClipboard(Sentinel);
        TextBox textBox = CreateTextBox(text);
        textBox.SelectionStart = selectionStart;
        textBox.SelectionLength = selectionLength;

        textBox.HandleKeyDown(Keys.X, false, false, true);

        ClipboardImplementation.GetText(null).ShouldBe(expectedClipboard);
        textBox.Text.ShouldBe(expectedText);
    }

    [Fact]
    public void PasswordBox_CopyAndCut_ShouldNeverPutThePasswordOnTheClipboard()
    {
        ClipboardImplementation.PushStringToClipboard(Sentinel);
        PasswordBox passwordBox = new();
        passwordBox.IsFocused = true;
        passwordBox.Password = "a" + Emoji + "b";
        passwordBox.SelectionStart = 2;
        passwordBox.SelectionLength = 2;

        passwordBox.HandleKeyDown(Keys.C, false, false, true);
        passwordBox.HandleKeyDown(Keys.X, false, false, true);

        ClipboardImplementation.GetText(null).ShouldBe(Sentinel);
        passwordBox.Password.ShouldBe("a" + Emoji + "b");
    }

    // A selection set from code can start before the text or run past it (the setters do not clamp the
    // start). The range is intersected with the text, never shifted: [-1, 2) is just "ab".
    [Theory]
    [InlineData("abc", -1, 3, 0, 2)]
    [InlineData("x" + Emoji, -1, 1, 0, 0)]
    [InlineData("abc", -5, 2, 0, 0)]
    [InlineData("abc", 1, 10, 1, 2)]
    [InlineData("abc", 5, 2, 3, 0)]
    [InlineData("a" + Emoji + "b", -1, 3, 0, 3)]
    public void GetWholeCharacterSelection_ShouldIntersectTheSelectionWithTheText(
        string text, int selectionStart, int selectionLength, int expectedStart, int expectedLength)
    {
        var (start, length) = TextBoxBase.GetWholeCharacterSelection(text, selectionStart, selectionLength);

        start.ShouldBe(expectedStart);
        length.ShouldBe(expectedLength);
    }


    static TextBox CreateTextBox(string text)
    {
        TextBox textBox = new();
        textBox.AcceptsReturn = true;
        textBox.Text = text;
        textBox.IsFocused = true;
        return textBox;
    }
}
