using Gum.Forms.Controls;
using Gum.Forms.Input;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Caret movement and keyboard selection of a focused TextBox: Home, End, Ctrl+arrows, Shift combos and
/// the vertical keys. Each test states where the caret and the selection end up.
/// </summary>
public class TextBoxKeyboardNavigationTests : BaseTestClass
{
    static TextBox Create(string text, int caret, bool multiLine = false)
    {
        TextBox textBox = new();
        if (multiLine)
        {
            textBox.TextWrapping = global::Gum.Forms.TextWrapping.Wrap;
            textBox.AcceptsReturn = true;
            textBox.Height = 200;
        }
        textBox.Width = 300;
        textBox.Text = text;
        textBox.IsFocused = true;
        textBox.CaretIndex = caret;
        return textBox;
    }

    static void Press(TextBox textBox, Keys key, bool shift = false, bool ctrl = false) =>
        textBox.HandleKeyDown(key, shift, false, ctrl);

    static void ShouldBeSelected(TextBox textBox, int start, int length)
    {
        textBox.SelectionStart.ShouldBe(start);
        textBox.SelectionLength.ShouldBe(length);
    }

    // ----- Home and End -----

    [Fact]
    public void Home_FromMiddle_MovesToStart()
    {
        TextBox textBox = Create("hello world", 5);
        Press(textBox, Keys.Home);
        textBox.CaretIndex.ShouldBe(0);
        textBox.SelectionLength.ShouldBe(0);
    }

    [Fact]
    public void End_FromMiddle_MovesToEnd()
    {
        TextBox textBox = Create("hello world", 5);
        Press(textBox, Keys.End);
        textBox.CaretIndex.ShouldBe(11);
        textBox.SelectionLength.ShouldBe(0);
    }

    [Fact]
    public void End_OnEmptyText_StaysAtZero()
    {
        TextBox textBox = Create("", 0);
        Press(textBox, Keys.End);
        Press(textBox, Keys.Home);
        textBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void Home_MultiLine_MovesToStartOfTheCurrentLine()
    {
        TextBox textBox = Create("ab\ncd\nef", 4, multiLine: true);
        Press(textBox, Keys.Home);
        textBox.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void End_MultiLine_MovesToEndOfTheCurrentLine()
    {
        TextBox textBox = Create("ab\ncd\nef", 3, multiLine: true);
        Press(textBox, Keys.End);
        textBox.CaretIndex.ShouldBe(5);
    }

    [Fact]
    public void End_MultiLineLastLine_MovesToEndOfText()
    {
        TextBox textBox = Create("ab\ncd\nef", 6, multiLine: true);
        Press(textBox, Keys.End);
        textBox.CaretIndex.ShouldBe(8);
    }

    [Fact]
    public void ShiftHome_SelectsFromCaretToStart()
    {
        TextBox textBox = Create("hello world", 5);
        Press(textBox, Keys.Home, shift: true);
        ShouldBeSelected(textBox, 0, 5);
        textBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void ShiftEnd_SelectsFromCaretToEnd()
    {
        TextBox textBox = Create("hello world", 5);
        Press(textBox, Keys.End, shift: true);
        ShouldBeSelected(textBox, 5, 6);
        textBox.CaretIndex.ShouldBe(11);
    }

    [Fact]
    public void ShiftHome_AfterShiftLeft_KeepsTheAnchor()
    {
        TextBox textBox = Create("hello world", 5);
        Press(textBox, Keys.Left, shift: true);
        Press(textBox, Keys.Left, shift: true);
        Press(textBox, Keys.Home, shift: true);
        ShouldBeSelected(textBox, 0, 5);
    }

    [Fact]
    public void ShiftEnd_AfterShiftRight_KeepsTheAnchor()
    {
        TextBox textBox = Create("hello world", 5);
        Press(textBox, Keys.Right, shift: true);
        Press(textBox, Keys.Right, shift: true);
        Press(textBox, Keys.End, shift: true);
        ShouldBeSelected(textBox, 5, 6);
    }

    [Fact]
    public void ShiftHome_AfterShiftRight_FlipsTheSelectionAcrossTheAnchor()
    {
        TextBox textBox = Create("hello world", 5);
        Press(textBox, Keys.Right, shift: true);
        Press(textBox, Keys.Right, shift: true);
        Press(textBox, Keys.Home, shift: true);
        ShouldBeSelected(textBox, 0, 5);
    }

    // ----- Plain arrows with a selection -----

    [Fact]
    public void Left_WithSelection_CollapsesToTheSelectionStart()
    {
        TextBox textBox = Create("hello world", 2);
        Press(textBox, Keys.Right, shift: true);
        Press(textBox, Keys.Right, shift: true);
        Press(textBox, Keys.Left);
        textBox.CaretIndex.ShouldBe(2);
        textBox.SelectionLength.ShouldBe(0);
    }

    [Fact]
    public void Right_WithSelection_CollapsesToTheSelectionEnd()
    {
        TextBox textBox = Create("hello world", 2);
        Press(textBox, Keys.Right, shift: true);
        Press(textBox, Keys.Right, shift: true);
        Press(textBox, Keys.Right);
        textBox.CaretIndex.ShouldBe(4);
        textBox.SelectionLength.ShouldBe(0);
    }

    [Fact]
    public void Left_AtStart_StaysAndSelectsNothing()
    {
        TextBox textBox = Create("abc", 0);
        Press(textBox, Keys.Left, shift: true);
        textBox.CaretIndex.ShouldBe(0);
        textBox.SelectionLength.ShouldBe(0);
    }

    [Fact]
    public void Right_AtEnd_StaysAndSelectsNothing()
    {
        TextBox textBox = Create("abc", 3);
        Press(textBox, Keys.Right, shift: true);
        textBox.CaretIndex.ShouldBe(3);
        textBox.SelectionLength.ShouldBe(0);
    }

    // ----- Shift + arrows -----

    [Fact]
    public void ShiftLeftThenShiftRight_ShrinksTheSelectionToNothing()
    {
        TextBox textBox = Create("hello", 3);
        Press(textBox, Keys.Left, shift: true);
        Press(textBox, Keys.Left, shift: true);
        ShouldBeSelected(textBox, 1, 2);
        Press(textBox, Keys.Right, shift: true);
        Press(textBox, Keys.Right, shift: true);
        textBox.SelectionLength.ShouldBe(0);
        textBox.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void ShiftRight_ThenShiftLeftPastTheAnchor_SelectsTheOtherSide()
    {
        TextBox textBox = Create("hello world", 3);
        Press(textBox, Keys.Right, shift: true);
        Press(textBox, Keys.Right, shift: true);
        ShouldBeSelected(textBox, 3, 2);
        for (int i = 0; i < 4; i++)
        {
            Press(textBox, Keys.Left, shift: true);
        }
        ShouldBeSelected(textBox, 1, 2);
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void ShiftLeft_ThenShiftRightPastTheAnchor_SelectsTheOtherSide()
    {
        TextBox textBox = Create("hello world", 5);
        Press(textBox, Keys.Left, shift: true);
        Press(textBox, Keys.Left, shift: true);
        for (int i = 0; i < 5; i++)
        {
            Press(textBox, Keys.Right, shift: true);
        }
        ShouldBeSelected(textBox, 5, 3);
        textBox.CaretIndex.ShouldBe(8);
    }

    [Fact]
    public void ArrowWithoutShift_ClearsAKeyboardSelection()
    {
        TextBox textBox = Create("hello", 1);
        Press(textBox, Keys.Right, shift: true);
        Press(textBox, Keys.End);
        textBox.SelectionLength.ShouldBe(0);
        textBox.CaretIndex.ShouldBe(5);
    }

    // ----- Ctrl + arrows -----

    [Fact]
    public void CtrlLeft_StepsBackWordByWord()
    {
        TextBox textBox = Create("foo bar baz", 11);
        Press(textBox, Keys.Left, ctrl: true);
        textBox.CaretIndex.ShouldBe(8);
        Press(textBox, Keys.Left, ctrl: true);
        textBox.CaretIndex.ShouldBe(4);
        Press(textBox, Keys.Left, ctrl: true);
        textBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void CtrlLeft_FromMiddleOfWord_GoesToTheWordStart()
    {
        TextBox textBox = Create("foo bar", 6);
        Press(textBox, Keys.Left, ctrl: true);
        textBox.CaretIndex.ShouldBe(4);
    }

    [Fact]
    public void CtrlLeft_AtStart_StaysAtStart()
    {
        TextBox textBox = Create("foo bar", 0);
        Press(textBox, Keys.Left, ctrl: true);
        textBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void CtrlRight_StepsForwardWordByWord()
    {
        TextBox textBox = Create("foo bar baz", 0);
        Press(textBox, Keys.Right, ctrl: true);
        textBox.CaretIndex.ShouldBe(4);
        Press(textBox, Keys.Right, ctrl: true);
        textBox.CaretIndex.ShouldBe(8);
        Press(textBox, Keys.Right, ctrl: true);
        textBox.CaretIndex.ShouldBe(11);
    }

    [Fact]
    public void CtrlLeft_OverRepeatedSpaces_LandsOnThePreviousWordStart()
    {
        TextBox textBox = Create("foo   bar", 6);
        Press(textBox, Keys.Left, ctrl: true);
        textBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void CtrlRight_AtEnd_StaysAtEnd()
    {
        TextBox textBox = Create("foo bar", 7);
        Press(textBox, Keys.Right, ctrl: true);
        textBox.CaretIndex.ShouldBe(7);
    }

    [Fact]
    public void CtrlShiftLeft_SelectsToTheWordStart()
    {
        TextBox textBox = Create("foo bar baz", 11);
        Press(textBox, Keys.Left, shift: true, ctrl: true);
        ShouldBeSelected(textBox, 8, 3);
        Press(textBox, Keys.Left, shift: true, ctrl: true);
        ShouldBeSelected(textBox, 4, 7);
    }

    [Fact]
    public void CtrlShiftRight_SelectsToTheNextWordStart()
    {
        TextBox textBox = Create("foo bar baz", 0);
        Press(textBox, Keys.Right, shift: true, ctrl: true);
        ShouldBeSelected(textBox, 0, 4);
        Press(textBox, Keys.Right, shift: true, ctrl: true);
        ShouldBeSelected(textBox, 0, 8);
    }

    // ----- Up and Down -----

    [Fact]
    public void Down_MultiLine_MovesToTheNextLine()
    {
        TextBox textBox = Create("abc\ndef\nghi", 1, multiLine: true);
        Press(textBox, Keys.Down);
        textBox.CaretIndex.ShouldBeInRange(4, 7);
        Press(textBox, Keys.Down);
        textBox.CaretIndex.ShouldBeInRange(8, 11);
    }

    [Fact]
    public void Up_MultiLine_MovesToThePreviousLine()
    {
        TextBox textBox = Create("abc\ndef\nghi", 9, multiLine: true);
        Press(textBox, Keys.Up);
        textBox.CaretIndex.ShouldBeInRange(4, 7);
    }

    [Fact]
    public void Up_OnFirstLine_MovesToStartOfText()
    {
        TextBox textBox = Create("abc\ndef", 2, multiLine: true);
        Press(textBox, Keys.Up);
        textBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void Down_OnLastLine_MovesToEndOfText()
    {
        TextBox textBox = Create("abc\ndef", 5, multiLine: true);
        Press(textBox, Keys.Down);
        textBox.CaretIndex.ShouldBe(7);
    }

    [Fact]
    public void ShiftDown_MultiLine_SelectsFromTheCaretToTheNextLine()
    {
        TextBox textBox = Create("abc\ndef", 1, multiLine: true);
        Press(textBox, Keys.Down, shift: true);
        textBox.SelectionStart.ShouldBe(1);
        textBox.SelectionLength.ShouldBeGreaterThan(3);
        (textBox.SelectionStart + textBox.SelectionLength).ShouldBe(textBox.CaretIndex);
    }

    [Fact]
    public void ShiftUp_MultiLine_SelectsFromTheCaretToThePreviousLine()
    {
        TextBox textBox = Create("abc\ndef", 5, multiLine: true);
        Press(textBox, Keys.Up, shift: true);
        textBox.SelectionStart.ShouldBe(textBox.CaretIndex);
        (textBox.SelectionStart + textBox.SelectionLength).ShouldBe(5);
    }

    [Fact]
    public void Down_SingleLine_MovesToEndOfText()
    {
        TextBox textBox = Create("hello", 1);
        Press(textBox, Keys.Down);
        textBox.CaretIndex.ShouldBe(5);
    }

    // ----- Select all and unfocused input -----

    [Fact]
    public void CtrlA_SelectsEverythingAndPutsTheCaretAtTheEnd()
    {
        TextBox textBox = Create("hello", 2);
        Press(textBox, Keys.A, ctrl: true);
        ShouldBeSelected(textBox, 0, 5);
    }

    [Fact]
    public void CtrlA_OnEmptyText_SelectsNothing()
    {
        TextBox textBox = Create("", 0);
        Press(textBox, Keys.A, ctrl: true);
        textBox.SelectionLength.ShouldBe(0);
    }

    [Fact]
    public void CtrlA_WhileReadOnly_StillSelectsEverything()
    {
        TextBox textBox = Create("hello", 2);
        textBox.IsReadOnly = true;
        Press(textBox, Keys.A, ctrl: true);
        ShouldBeSelected(textBox, 0, 5);
    }

    [Fact]
    public void NavigationKeys_WhenNotFocused_DoNothing()
    {
        TextBox textBox = Create("hello", 2);
        textBox.IsFocused = false;
        Press(textBox, Keys.End);
        textBox.CaretIndex.ShouldBe(2);
    }
}
