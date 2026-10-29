using Gum.Forms.Controls;
using Gum.Forms.Input;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Caret steps and deletes treat a UTF-16 surrogate pair (an emoji such as U+1F600) as one character
/// (#7091), the same way a "\r\n" is one. Indexes stay UTF-16 indexes. Grapheme clusters (a flag, a ZWJ
/// sequence, a skin-tone modifier, a combining mark) are NOT one unit yet: those tests document the
/// current behavior, which removes or steps one code point at a time.
/// </summary>
public class TextBoxSurrogatePairTests : BaseTestClass
{
    const string Emoji = "\U0001F600";

    // ----- Backspace -----

    [Theory]
    [InlineData("a" + Emoji + "b", 3, "ab", 1)]
    [InlineData(Emoji + "b", 2, "b", 0)]
    [InlineData("a" + Emoji, 3, "a", 1)]
    [InlineData(Emoji + Emoji, 4, Emoji, 2)]
    [InlineData(Emoji + Emoji, 2, Emoji, 0)]
    public void Backspace_AfterEmoji_ShouldRemoveWholePair(string text, int caret, string expectedText, int expectedCaret)
    {
        TextBox textBox = CreateTextBox(text);
        textBox.CaretIndex = caret;

        textBox.HandleBackspace();

        textBox.Text.ShouldBe(expectedText);
        textBox.CaretIndex.ShouldBe(expectedCaret);
    }

    [Fact]
    public void Backspace_Ascii_ShouldRemoveOneCharacter()
    {
        TextBox textBox = CreateTextBox("abc");
        textBox.CaretIndex = 2;

        textBox.HandleBackspace();

        textBox.Text.ShouldBe("ac");
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Backspace_CaretBetweenTheHalves_ShouldRemoveWholePair()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        textBox.CaretIndex = 2;

        textBox.HandleBackspace();

        textBox.Text.ShouldBe("ab");
        textBox.CaretIndex.ShouldBe(1);
    }

    // ----- Delete -----

    [Theory]
    [InlineData("a" + Emoji + "b", 1, "ab", 1)]
    [InlineData(Emoji + "b", 0, "b", 0)]
    [InlineData("a" + Emoji, 1, "a", 1)]
    [InlineData(Emoji + Emoji, 0, Emoji, 0)]
    [InlineData(Emoji + Emoji, 2, Emoji, 2)]
    public void Delete_BeforeEmoji_ShouldRemoveWholePair(string text, int caret, string expectedText, int expectedCaret)
    {
        TextBox textBox = CreateTextBox(text);
        textBox.CaretIndex = caret;

        textBox.HandleKeyDown(Keys.Delete, false, false, false);

        textBox.Text.ShouldBe(expectedText);
        textBox.CaretIndex.ShouldBe(expectedCaret);
    }

    [Fact]
    public void Delete_Ascii_ShouldRemoveOneCharacter()
    {
        TextBox textBox = CreateTextBox("abc");
        textBox.CaretIndex = 1;

        textBox.HandleKeyDown(Keys.Delete, false, false, false);

        textBox.Text.ShouldBe("ac");
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Delete_CaretBetweenTheHalves_ShouldRemoveWholePair()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        textBox.CaretIndex = 2;

        textBox.HandleKeyDown(Keys.Delete, false, false, false);

        textBox.Text.ShouldBe("ab");
        textBox.CaretIndex.ShouldBe(1);
    }

    // ----- Arrows -----

    [Fact]
    public void RightArrow_BeforeEmoji_ShouldStepOverWholePair()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        textBox.CaretIndex = 1;

        textBox.HandleKeyDown(Keys.Right, false, false, false);

        textBox.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void LeftArrow_AfterEmoji_ShouldStepOverWholePair()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        textBox.CaretIndex = 3;

        textBox.HandleKeyDown(Keys.Left, false, false, false);

        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Arrows_Ascii_ShouldStepOneCharacter()
    {
        TextBox textBox = CreateTextBox("abc");
        textBox.CaretIndex = 1;

        textBox.HandleKeyDown(Keys.Right, false, false, false);
        textBox.CaretIndex.ShouldBe(2);

        textBox.HandleKeyDown(Keys.Left, false, false, false);
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void RightArrow_AtEndAfterEmoji_ShouldStay()
    {
        TextBox textBox = CreateTextBox("a" + Emoji);
        textBox.CaretIndex = 3;

        textBox.HandleKeyDown(Keys.Right, false, false, false);

        textBox.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void LeftArrow_AtStartBeforeEmoji_ShouldStay()
    {
        TextBox textBox = CreateTextBox(Emoji + "a");
        textBox.CaretIndex = 0;

        textBox.HandleKeyDown(Keys.Left, false, false, false);

        textBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void RightArrow_WithSelection_ShouldCollapseToSelectionEnd()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 2;

        textBox.HandleKeyDown(Keys.Right, false, false, false);

        textBox.CaretIndex.ShouldBe(3);
        textBox.SelectionLength.ShouldBe(0);
    }

    // ----- Shift selection -----

    [Fact]
    public void ShiftRight_BeforeEmoji_ShouldSelectWholePair()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        textBox.CaretIndex = 1;

        textBox.HandleKeyDown(Keys.Right, true, false, false);

        textBox.SelectionStart.ShouldBe(1);
        textBox.SelectionLength.ShouldBe(2);
    }

    [Fact]
    public void ShiftLeft_AfterEmoji_ShouldSelectWholePair()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        textBox.CaretIndex = 3;

        textBox.HandleKeyDown(Keys.Left, true, false, false);

        textBox.SelectionStart.ShouldBe(1);
        textBox.SelectionLength.ShouldBe(2);
    }

    [Fact]
    public void ShiftRight_Ascii_ShouldSelectOneCharacter()
    {
        TextBox textBox = CreateTextBox("abc");
        textBox.CaretIndex = 1;

        textBox.HandleKeyDown(Keys.Right, true, false, false);

        textBox.SelectionStart.ShouldBe(1);
        textBox.SelectionLength.ShouldBe(1);
    }

    [Fact]
    public void DeleteSelection_EdgeInsideAPair_ShouldRemoveWholePair()
    {
        // A selection set from code can start between the halves; no half may be left behind.
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        textBox.SelectionStart = 2;
        textBox.SelectionLength = 1;

        textBox.DeleteSelection();

        textBox.Text.ShouldBe("ab");
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void DeleteSelection_EndInsideAPair_ShouldRemoveWholePair()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        textBox.SelectionStart = 0;
        textBox.SelectionLength = 2;

        textBox.DeleteSelection();

        textBox.Text.ShouldBe("b");
        textBox.CaretIndex.ShouldBe(0);
    }

    // ----- Word moves, Home, End, select all, typing, paste -----

    [Fact]
    public void CtrlRight_OverEmojiWord_ShouldLandAfterTheSpace()
    {
        TextBox textBox = CreateTextBox(Emoji + Emoji + " b");
        textBox.CaretIndex = 0;

        textBox.HandleKeyDown(Keys.Right, false, false, true);

        textBox.CaretIndex.ShouldBe(5);
    }

    [Fact]
    public void CtrlLeft_OverEmojiWord_ShouldLandBeforeTheWord()
    {
        TextBox textBox = CreateTextBox("b " + Emoji + Emoji);
        textBox.CaretIndex = 6;

        textBox.HandleKeyDown(Keys.Left, false, false, true);

        textBox.CaretIndex.ShouldBe(2);
    }

    [Fact]
    public void CtrlBackspace_OverEmojiWord_ShouldRemoveTheWholeWord()
    {
        TextBox textBox = CreateTextBox("b " + Emoji + Emoji);
        textBox.CaretIndex = 6;

        textBox.HandleBackspace(isCtrlDown: true);

        textBox.Text.ShouldBe("b ");
    }

    [Fact]
    public void HomeAndEnd_ShouldGoToTheEnds()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        textBox.CaretIndex = 2 + 1;

        textBox.HandleKeyDown(Keys.End, false, false, false);
        textBox.CaretIndex.ShouldBe(4);

        textBox.HandleKeyDown(Keys.Home, false, false, false);
        textBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void SelectAllThenType_ShouldReplaceEverything()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");

        textBox.SelectAll();
        textBox.HandleCharEntered('x');

        textBox.Text.ShouldBe("x");
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Typing_BothHalvesOfAnEmoji_ShouldInsertWholePair()
    {
        TextBox textBox = CreateTextBox("ab");
        textBox.CaretIndex = 1;

        textBox.HandleCharEntered(Emoji[0]);
        textBox.HandleCharEntered(Emoji[1]);

        textBox.Text.ShouldBe("a" + Emoji + "b");
        textBox.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void Paste_Emoji_ShouldInsertWholePair()
    {
        Gum.Clipboard.ClipboardImplementation.PushStringToClipboard(Emoji);
        TextBox textBox = CreateTextBox("ab");
        textBox.CaretIndex = 1;

        textBox.HandleKeyDown(Keys.V, false, false, true);

        textBox.Text.ShouldBe("a" + Emoji + "b");
        textBox.CaretIndex.ShouldBe(3);
    }

    // ----- MaxLength counts UTF-16 units; it never cuts a pair in half -----

    [Fact]
    public void MaxLength_CountsUtf16Units_WhenThePairFits()
    {
        TextBox textBox = CreateTextBox("");
        textBox.MaxLength = 3;

        textBox.Text = "a" + Emoji + "b";

        textBox.Text.ShouldBe("a" + Emoji);
    }

    [Fact]
    public void MaxLength_ThatCutsAPair_ShouldDropTheWholePair()
    {
        TextBox textBox = CreateTextBox("");
        textBox.MaxLength = 2;

        textBox.Text = "a" + Emoji + "b";

        textBox.Text.ShouldBe("a");
    }

    [Fact]
    public void MaxLength_ChangedToCutAPair_ShouldDropTheWholePair()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");

        textBox.MaxLength = 2;

        textBox.Text.ShouldBe("a");
    }

    [Fact]
    public void MaxLength_PastedEmojiThatDoesNotFit_ShouldBeRejectedWhole()
    {
        Gum.Clipboard.ClipboardImplementation.PushStringToClipboard(Emoji);
        TextBox textBox = CreateTextBox("a");
        textBox.MaxLength = 2;
        textBox.CaretIndex = 1;

        textBox.HandleKeyDown(Keys.V, false, false, true);

        textBox.Text.ShouldBe("a");
    }

    [Fact]
    public void MaxLength_TypedEmojiThatDoesNotFit_ShouldBeRejectedWhole()
    {
        TextBox textBox = CreateTextBox("a");
        textBox.MaxLength = 2;
        textBox.CaretIndex = 1;

        textBox.HandleCharEntered(Emoji[0]);
        textBox.HandleCharEntered(Emoji[1]);

        textBox.Text.ShouldBe("a");
    }

    // ----- Click to caret -----

    [Fact]
    public void ClickToCaret_AcrossAnEmoji_ShouldNeverLandBetweenTheHalves()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        float y = 80 + 10;
        for (float x = 90; x < 400; x += 1)
        {
            int index = textBox.GetCaretIndexAtPosition(x, y);
            index.ShouldNotBe(2, $"x={x}");
        }
    }

    // ----- Grapheme clusters: documented, not yet one unit -----

    [Fact]
    public void Backspace_AfterFlag_ShouldRemoveOneRegionalIndicatorAndLeaveTheOther()
    {
        const string flag = "\U0001F1FA\U0001F1F8";
        TextBox textBox = CreateTextBox(flag);
        textBox.CaretIndex = 4;

        textBox.HandleBackspace();

        textBox.Text.ShouldBe("\U0001F1FA");
        textBox.CaretIndex.ShouldBe(2);
    }

    [Fact]
    public void Backspace_AfterZwjFamily_ShouldRemoveOnlyTheLastEmoji()
    {
        const string man = "\U0001F468";
        const string woman = "\U0001F469";
        const string girl = "\U0001F467";
        TextBox textBox = CreateTextBox(man + "‍" + woman + "‍" + girl);
        textBox.CaretIndex = 8;

        textBox.HandleBackspace();

        textBox.Text.ShouldBe(man + "‍" + woman + "‍");
        textBox.CaretIndex.ShouldBe(6);
    }

    [Fact]
    public void Backspace_AfterSkinToneEmoji_ShouldRemoveOnlyTheModifier()
    {
        TextBox textBox = CreateTextBox("\U0001F44D\U0001F3FD");
        textBox.CaretIndex = 4;

        textBox.HandleBackspace();

        textBox.Text.ShouldBe("\U0001F44D");
        textBox.CaretIndex.ShouldBe(2);
    }

    [Fact]
    public void Backspace_AfterCombiningMark_ShouldRemoveOnlyTheMark()
    {
        TextBox textBox = CreateTextBox("é");
        textBox.CaretIndex = 2;

        textBox.HandleBackspace();

        textBox.Text.ShouldBe("e");
        textBox.CaretIndex.ShouldBe(1);
    }

    // ----- A lone surrogate is not a pair -----

    [Fact]
    public void Backspace_AfterLoneHighSurrogate_ShouldRemoveOneUnit()
    {
        TextBox textBox = CreateTextBox("a\uD83D");
        textBox.CaretIndex = 2;

        textBox.HandleBackspace();

        textBox.Text.ShouldBe("a");
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Backspace_AfterLoneLowSurrogate_ShouldRemoveOneUnit()
    {
        TextBox textBox = CreateTextBox("a\uDE00b");
        textBox.CaretIndex = 2;

        textBox.HandleBackspace();

        textBox.Text.ShouldBe("ab");
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Delete_BeforeLoneHighSurrogate_ShouldRemoveOneUnit()
    {
        TextBox textBox = CreateTextBox("a\uD83Db");
        textBox.CaretIndex = 1;

        textBox.HandleKeyDown(Keys.Delete, false, false, false);

        textBox.Text.ShouldBe("ab");
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void RightArrow_OverLoneHighSurrogate_ShouldStepOneUnit()
    {
        TextBox textBox = CreateTextBox("a\uD83Db");
        textBox.CaretIndex = 1;

        textBox.HandleKeyDown(Keys.Right, false, false, false);

        textBox.CaretIndex.ShouldBe(2);
    }

    static TextBox CreateTextBox(string text)
    {
        TextBox textBox = new();
        textBox.TextWrapping = global::Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;
        textBox.Height = 200;
        textBox.Width = 300;
        textBox.Visual.X = 100;
        textBox.Visual.Y = 80;
        textBox.Text = text;
        textBox.IsFocused = true;
        return textBox;
    }
}
