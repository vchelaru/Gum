using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Forms.Input;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A "\r\n" in Text is one character as far as the caret goes (#6996): the wrapped text collapses it to
/// "\n", so every Text-index to wrapped-line mapping must skip the '\r'. Each test builds the same
/// content with "\n" and checks the CRLF box lands on the matching spot.
/// </summary>
public class TextBoxCrlfTests : BaseTestClass
{
    const string Lf = "one\ntwo\nthree";
    const string Crlf = "one\r\ntwo\r\nthree";

    // Text index in Crlf for an index in Lf.
    static int ToCrlf(int lfIndex) => lfIndex + Lf.Take(lfIndex).Count(c => c == '\n');

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(13)]
    public void CaretPosition_Crlf_ShouldMatchLf(int lfIndex)
    {
        TextBox lf = CreateTextBox(Lf);
        TextBox crlf = CreateTextBox(Crlf);

        lf.CaretIndex = lfIndex;
        crlf.CaretIndex = ToCrlf(lfIndex);

        GetCaret(crlf).AbsoluteLeft.ShouldBe(GetCaret(lf).AbsoluteLeft, tolerance: 0.01f);
        GetCaret(crlf).AbsoluteTop.ShouldBe(GetCaret(lf).AbsoluteTop, tolerance: 0.01f);
    }

    [Fact]
    public void CaretPosition_BetweenCrAndLf_ShouldBeBeforeTheLineBreak()
    {
        TextBox lf = CreateTextBox(Lf);
        TextBox crlf = CreateTextBox(Crlf);

        lf.CaretIndex = 3;
        crlf.CaretIndex = 4;

        GetCaret(crlf).AbsoluteLeft.ShouldBe(GetCaret(lf).AbsoluteLeft, tolerance: 0.01f);
        GetCaret(crlf).AbsoluteTop.ShouldBe(GetCaret(lf).AbsoluteTop, tolerance: 0.01f);
    }

    [Theory]
    [InlineData(1, 6)]
    [InlineData(5, 3)]
    [InlineData(0, 13)]
    public void Selection_Crlf_ShouldMatchLf(int lfStart, int lfLength)
    {
        TextBox lf = CreateTextBox(Lf);
        TextBox crlf = CreateTextBox(Crlf);

        lf.SelectionStart = lfStart;
        lf.SelectionLength = lfLength;
        crlf.SelectionStart = ToCrlf(lfStart);
        crlf.SelectionLength = ToCrlf(lfStart + lfLength) - ToCrlf(lfStart);

        GraphicalUiElement[] expected = GetVisibleSelections(lf);
        GraphicalUiElement[] actual = GetVisibleSelections(crlf);
        actual.Length.ShouldBe(expected.Length);
        for (int i = 0; i < actual.Length; i++)
        {
            actual[i].AbsoluteLeft.ShouldBe(expected[i].AbsoluteLeft, tolerance: 0.01f);
            actual[i].AbsoluteTop.ShouldBe(expected[i].AbsoluteTop, tolerance: 0.01f);
            actual[i].AbsoluteWidth.ShouldBe(expected[i].AbsoluteWidth, tolerance: 0.01f);
        }
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(11)]
    public void ClickToCaret_Crlf_ShouldReturnTextIndex(int lfIndex)
    {
        TextBox lf = CreateTextBox(Lf);
        lf.CaretIndex = lfIndex;
        float x = GetCaret(lf).AbsoluteLeft;
        float y = GetCaret(lf).AbsoluteTop + GetCaret(lf).AbsoluteHeight / 2f;
        TextBox crlf = CreateTextBox(Crlf);

        lf.GetCaretIndexAtPosition(x, y).ShouldBe(lfIndex);
        crlf.GetCaretIndexAtPosition(x, y).ShouldBe(ToCrlf(lfIndex));
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(5, 9)]
    [InlineData(6, 10)]
    public void DownArrow_Crlf_ShouldMoveToSameColumn(int lfFrom, int lfTo)
    {
        TextBox crlf = CreateTextBox(Crlf);
        crlf.CaretIndex = ToCrlf(lfFrom);

        crlf.HandleKeyDown(Keys.Down, false, false, false);

        crlf.CaretIndex.ShouldBe(ToCrlf(lfTo));
    }

    [Theory]
    [InlineData(5, 1)]
    [InlineData(10, 6)]
    public void UpArrow_Crlf_ShouldMoveToSameColumn(int lfFrom, int lfTo)
    {
        TextBox crlf = CreateTextBox(Crlf);
        crlf.CaretIndex = ToCrlf(lfFrom);

        crlf.HandleKeyDown(Keys.Up, false, false, false);

        crlf.CaretIndex.ShouldBe(ToCrlf(lfTo));
    }

    [Theory]
    [InlineData(6, 4)]
    [InlineData(10, 8)]
    public void Home_Crlf_ShouldGoToStartOfLine(int lfFrom, int lfTo)
    {
        TextBox crlf = CreateTextBox(Crlf);
        crlf.CaretIndex = ToCrlf(lfFrom);

        crlf.HandleKeyDown(Keys.Home, false, false, false);

        crlf.CaretIndex.ShouldBe(ToCrlf(lfTo));
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(5, 7)]
    [InlineData(9, 13)]
    public void End_Crlf_ShouldGoToEndOfLineBeforeTheBreak(int lfFrom, int lfTo)
    {
        TextBox crlf = CreateTextBox(Crlf);
        crlf.CaretIndex = ToCrlf(lfFrom);

        crlf.HandleKeyDown(Keys.End, false, false, false);

        crlf.CaretIndex.ShouldBe(ToCrlf(lfTo));
    }

    [Fact]
    public void RightArrow_AtCrlf_ShouldStepOverBothCharacters()
    {
        TextBox crlf = CreateTextBox(Crlf);
        crlf.CaretIndex = 3;

        crlf.HandleKeyDown(Keys.Right, false, false, false);

        crlf.CaretIndex.ShouldBe(5);
    }

    [Fact]
    public void LeftArrow_AfterCrlf_ShouldStepOverBothCharacters()
    {
        TextBox crlf = CreateTextBox(Crlf);
        crlf.CaretIndex = 5;

        crlf.HandleKeyDown(Keys.Left, false, false, false);

        crlf.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void Backspace_AfterCrlf_ShouldDeleteBothCharacters()
    {
        TextBox crlf = CreateTextBox(Crlf);
        crlf.CaretIndex = 5;

        crlf.HandleBackspace();

        crlf.Text.ShouldBe("onetwo\r\nthree");
        crlf.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void Delete_BeforeCrlf_ShouldDeleteBothCharacters()
    {
        TextBox crlf = CreateTextBox(Crlf);
        crlf.CaretIndex = 3;

        crlf.HandleKeyDown(Keys.Delete, false, false, false);

        crlf.Text.ShouldBe("onetwo\r\nthree");
        crlf.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void Shift_Right_AtCrlf_ShouldSelectBothCharacters()
    {
        TextBox crlf = CreateTextBox(Crlf);
        crlf.CaretIndex = 3;

        crlf.HandleKeyDown(Keys.Right, true, false, false);

        crlf.SelectionStart.ShouldBe(3);
        crlf.SelectionLength.ShouldBe(2);
    }

    const string WordsLf = "one two\nthree four";
    const string WordsCrlf = "one two\r\nthree four";

    static int WordsToCrlf(int lfIndex) => lfIndex + WordsLf.Take(lfIndex).Count(c => c == '\n');

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(13)]
    [InlineData(18)]
    public void CtrlRight_Crlf_ShouldMatchLf(int lfFrom)
    {
        // Only one TextBox holds focus at a time, so finish with the LF box first.
        TextBox lf = CreateTextBox(WordsLf);
        lf.CaretIndex = lfFrom;
        lf.HandleKeyDown(Keys.Right, false, false, true);
        int lfResult = lf.CaretIndex;
        TextBox crlf = CreateTextBox(WordsCrlf);
        crlf.CaretIndex = WordsToCrlf(lfFrom);

        crlf.HandleKeyDown(Keys.Right, false, false, true);

        crlf.CaretIndex.ShouldBe(WordsToCrlf(lfResult));
    }

    [Theory]
    [InlineData(18)]
    [InlineData(13)]
    [InlineData(8)]
    [InlineData(7)]
    [InlineData(4)]
    [InlineData(3)]
    public void CtrlLeft_Crlf_ShouldMatchLf(int lfFrom)
    {
        // Only one TextBox holds focus at a time, so finish with the LF box first.
        TextBox lf = CreateTextBox(WordsLf);
        lf.CaretIndex = lfFrom;
        lf.HandleKeyDown(Keys.Left, false, false, true);
        int lfResult = lf.CaretIndex;
        TextBox crlf = CreateTextBox(WordsCrlf);
        crlf.CaretIndex = WordsToCrlf(lfFrom);

        crlf.HandleKeyDown(Keys.Left, false, false, true);

        crlf.CaretIndex.ShouldBe(WordsToCrlf(lfResult));
    }

    [Theory]
    [InlineData(13)]
    [InlineData(8)]
    public void CtrlBackspace_Crlf_ShouldMatchLf(int lfFrom)
    {
        TextBox lf = CreateTextBox(WordsLf);
        TextBox crlf = CreateTextBox(WordsCrlf);
        lf.CaretIndex = lfFrom;
        crlf.CaretIndex = WordsToCrlf(lfFrom);

        lf.HandleBackspace(true);
        crlf.HandleBackspace(true);

        crlf.Text.ShouldBe(lf.Text!.Replace("\n", "\r\n"));
        crlf.CaretIndex.ShouldBe(WordsToCrlf(lf.CaretIndex));
    }

    const string ColumnLf = "a\nb\nc\nd";
    const string ColumnCrlf = "a\r\nb\r\nc\r\nd";

    [Theory]
    [InlineData(0, Keys.End, Keys.Delete)]
    [InlineData(0, Keys.Right, Keys.Delete)]
    [InlineData(1, Keys.Right, Keys.Delete)]
    [InlineData(2, Keys.End, Keys.Delete)]
    [InlineData(2, Keys.Right, Keys.Back)]
    public void ShiftSelectThenDelete_Crlf_ShouldLeaveSameSelectionAsLf(int lfCaret, Keys selectKey, Keys deleteKey)
    {
        // Only one TextBox holds focus at a time, so finish with the LF box first.
        TextBox lf = CreateTextBox(ColumnLf);
        lf.CaretIndex = lfCaret;
        lf.HandleKeyDown(selectKey, true, false, false);
        lf.HandleKeyDown(deleteKey, false, false, false);
        string lfText = lf.Text!;
        int lfCaretAfter = lf.CaretIndex;
        int lfSelectionLength = lf.SelectionLength;
        int lfVisibleSelections = GetVisibleSelections(lf).Length;
        TextBox crlf = CreateTextBox(ColumnCrlf);
        crlf.CaretIndex = lfCaret == 0 ? 0 : ColumnToCrlf(lfCaret);

        crlf.HandleKeyDown(selectKey, true, false, false);
        crlf.HandleKeyDown(deleteKey, false, false, false);

        crlf.Text.ShouldBe(lfText.Replace("\n", "\r\n"));
        crlf.CaretIndex.ShouldBe(ColumnToCrlf(lfCaretAfter));
        crlf.SelectionLength.ShouldBe(lfSelectionLength);
        GetVisibleSelections(crlf).Length.ShouldBe(lfVisibleSelections);
    }

    static int ColumnToCrlf(int lfIndex) => lfIndex + ColumnLf.Take(lfIndex).Count(c => c == '\n');

    [Fact]
    public void Backspace_CaretBetweenCrAndLf_ShouldDeleteWholeBreak()
    {
        TextBox crlf = CreateTextBox("a\r\nb");
        crlf.CaretIndex = 2;

        crlf.HandleBackspace();

        crlf.Text.ShouldBe("ab");
        crlf.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Delete_CaretBetweenCrAndLf_ShouldDeleteWholeBreak()
    {
        TextBox crlf = CreateTextBox("a\r\nb");
        crlf.CaretIndex = 2;

        crlf.HandleKeyDown(Keys.Delete, false, false, false);

        crlf.Text.ShouldBe("ab");
        crlf.CaretIndex.ShouldBe(1);
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

    static Gum.GueDeriving.SpriteRuntime GetCaret(TextBox textBox) =>
        ((TextBoxBaseVisual)textBox.Visual).CaretInstance;

    static GraphicalUiElement[] GetVisibleSelections(TextBox textBox) =>
        ((TextBoxBaseVisual)textBox.Visual).ClipContainer.Children
            .OfType<GraphicalUiElement>()
            .Where(child => child.Name == "SelectionInstance" && child.Visible)
            .ToArray();
}
