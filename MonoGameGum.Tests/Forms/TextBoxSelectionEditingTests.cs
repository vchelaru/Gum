using Gum.Forms.Controls;
using Gum.Forms.Input;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Typing, Backspace and Delete while text is selected, MaxLength, read-only, the placeholder and how
/// caret and selection values set from code are kept inside the text.
/// </summary>
public class TextBoxSelectionEditingTests : BaseTestClass
{
    static TextBox Create(string text, int selectionStart = 0, int selectionLength = 0, int? caret = null)
    {
        TextBox textBox = new();
        textBox.Width = 300;
        textBox.Text = text;
        textBox.IsFocused = true;
        textBox.CaretIndex = caret ?? selectionStart;
        textBox.SelectionStart = selectionStart;
        textBox.SelectionLength = selectionLength;
        return textBox;
    }

    [Fact]
    public void Backspace_WithSelection_DeletesTheSelectionOnly()
    {
        TextBox textBox = Create("hello world", 2, 4);
        textBox.HandleBackspace();
        textBox.Text.ShouldBe("heworld");
        textBox.CaretIndex.ShouldBe(2);
        textBox.SelectionLength.ShouldBe(0);
    }

    [Fact]
    public void Delete_WithSelection_DeletesTheSelectionOnly()
    {
        TextBox textBox = Create("hello world", 2, 4);
        textBox.HandleKeyDown(Keys.Delete, false, false, false);
        textBox.Text.ShouldBe("heworld");
        textBox.CaretIndex.ShouldBe(2);
        textBox.SelectionLength.ShouldBe(0);
    }

    [Fact]
    public void Delete_WithSelectionToTheEnd_DeletesTheSelection()
    {
        TextBox textBox = Create("hello", 2, 3);
        textBox.HandleKeyDown(Keys.Delete, false, false, false);
        textBox.Text.ShouldBe("he");
    }

    [Fact]
    public void CtrlBackspace_WithSelection_DeletesOnlyTheSelection()
    {
        TextBox textBox = Create("foo bar baz", 4, 3);
        textBox.HandleBackspace(isCtrlDown: true);
        textBox.Text.ShouldBe("foo  baz");
    }

    [Fact]
    public void CtrlBackspace_DeletesThePreviousWord()
    {
        TextBox textBox = Create("foo bar", 7);
        textBox.HandleBackspace(isCtrlDown: true);
        textBox.Text.ShouldBe("foo ");
        textBox.CaretIndex.ShouldBe(4);
    }

    [Fact]
    public void Backspace_AtStart_ChangesNothing()
    {
        TextBox textBox = Create("abc", 0);
        textBox.HandleBackspace();
        textBox.Text.ShouldBe("abc");
        textBox.CaretIndex.ShouldBe(0);
    }

    [Fact]
    public void Delete_AtEnd_ChangesNothing()
    {
        TextBox textBox = Create("abc", 3);
        textBox.HandleKeyDown(Keys.Delete, false, false, false);
        textBox.Text.ShouldBe("abc");
        textBox.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void Delete_WithoutSelection_RemovesTheCharacterAfterTheCaret()
    {
        TextBox textBox = Create("abc", 1);
        textBox.HandleKeyDown(Keys.Delete, false, false, false);
        textBox.Text.ShouldBe("ac");
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Typing_WithSelection_ReplacesTheSelection()
    {
        TextBox textBox = Create("hello world", 0, 5);
        textBox.HandleCharEntered('X');
        textBox.Text.ShouldBe("X world");
        textBox.CaretIndex.ShouldBe(1);
        textBox.SelectionLength.ShouldBe(0);
    }

    [Fact]
    public void Typing_WithBackwardSelection_ReplacesTheSelection()
    {
        TextBox textBox = Create("hello", 3);
        textBox.HandleKeyDown(Keys.Left, true, false, false);
        textBox.HandleKeyDown(Keys.Left, true, false, false);
        textBox.HandleCharEntered('X');
        textBox.Text.ShouldBe("hXlo");
        textBox.CaretIndex.ShouldBe(2);
    }

    [Fact]
    public void Typing_InTheMiddle_InsertsAtTheCaret()
    {
        TextBox textBox = Create("ac", 1);
        textBox.HandleCharEntered('b');
        textBox.Text.ShouldBe("abc");
        textBox.CaretIndex.ShouldBe(2);
    }

    [Fact]
    public void Typing_WithAllSelected_ReplacesEverything()
    {
        TextBox textBox = Create("hello", 0, 5);
        textBox.HandleCharEntered('a');
        textBox.Text.ShouldBe("a");
    }

    [Fact]
    public void MaxLength_TypingWithSelection_ReplacesWithinTheLimit()
    {
        TextBox textBox = Create("abcde", 1, 2);
        textBox.MaxLength = 5;
        textBox.HandleCharEntered('X');
        textBox.Text.ShouldBe("aXde");
    }

    [Fact]
    public void MaxLength_TypingAtTheLimit_IsIgnored()
    {
        TextBox textBox = Create("abcde", 5);
        textBox.MaxLength = 5;
        textBox.HandleCharEntered('X');
        textBox.Text.ShouldBe("abcde");
        textBox.CaretIndex.ShouldBe(5);
    }

    [Fact]
    public void MaxLength_SetBelowTextLength_TruncatesTheText()
    {
        TextBox textBox = Create("abcdef", 6);
        textBox.MaxLength = 3;
        textBox.Text.ShouldBe("abc");
        textBox.CaretIndex.ShouldBeLessThanOrEqualTo(3);
    }

    [Fact]
    public void MaxLength_Zero_AllowsNoText()
    {
        TextBox textBox = Create("", 0);
        textBox.MaxLength = 0;
        textBox.HandleCharEntered('X');
        textBox.Text.ShouldBe("");
    }

    [Fact]
    public void MaxLength_Null_AllowsAnyLength()
    {
        TextBox textBox = Create("", 0);
        textBox.MaxLength = 2;
        textBox.MaxLength = null;
        for (int i = 0; i < 10; i++)
        {
            textBox.HandleCharEntered('x');
        }
        textBox.Text.ShouldBe("xxxxxxxxxx");
    }

    [Fact]
    public void ReadOnly_DeleteAndBackspaceWithSelection_ChangeNothing()
    {
        TextBox textBox = Create("hello", 1, 2);
        textBox.IsReadOnly = true;
        textBox.HandleKeyDown(Keys.Delete, false, false, false);
        textBox.HandleKeyDown(Keys.Back, false, false, false);
        textBox.HandleCharEntered('X');
        textBox.Text.ShouldBe("hello");
        textBox.SelectionLength.ShouldBe(2);
    }

    [Fact]
    public void ReadOnly_ArrowKeys_StillMoveTheCaret()
    {
        TextBox textBox = Create("hello", 0);
        textBox.IsReadOnly = true;
        textBox.HandleKeyDown(Keys.Right, false, false, false);
        textBox.CaretIndex.ShouldBe(1);
    }

    // ----- Values set from code stay inside the text -----

    [Fact]
    public void SelectionLength_PastTheEnd_IsClamped()
    {
        TextBox textBox = Create("abc", 1);
        textBox.SelectionStart = 1;
        textBox.SelectionLength = 50;
        (textBox.SelectionStart + textBox.SelectionLength).ShouldBeLessThanOrEqualTo(3);
    }

    [Fact]
    public void SelectionStart_PastTheEnd_LeavesNoSelectionBeyondTheText()
    {
        TextBox textBox = Create("abc", 0);
        textBox.SelectionStart = 10;
        textBox.SelectionLength = 2;
        (textBox.SelectionStart + textBox.SelectionLength).ShouldBeLessThanOrEqualTo(3);
        textBox.SelectionStart.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void SelectAll_RaisesSelectionChangedOnce()
    {
        TextBox textBox = Create("hello", 0);
        int raised = 0;
        textBox.SelectionChanged += (_, _) => raised++;
        textBox.SelectAll();
        raised.ShouldBe(1);
    }

    [Fact]
    public void CaretIndex_Set_RaisesCaretIndexChangedOnce()
    {
        TextBox textBox = Create("hello", 0);
        int raised = 0;
        textBox.CaretIndexChanged += (_, _) => raised++;
        textBox.CaretIndex = 3;
        raised.ShouldBe(1);
        textBox.CaretIndex = 3;
        raised.ShouldBe(1);
    }

    // ----- Placeholder -----

    [Fact]
    public void Placeholder_ShownForEmptyText_HiddenForText()
    {
        TextBox textBox = new();
        textBox.Placeholder = "type here";
        IsPlaceholderVisible(textBox).ShouldBeTrue();
        textBox.Text = "x";
        IsPlaceholderVisible(textBox).ShouldBeFalse();
        textBox.Text = "";
        IsPlaceholderVisible(textBox).ShouldBeTrue();
        textBox.Text = null;
        IsPlaceholderVisible(textBox).ShouldBeTrue();
    }

    [Fact]
    public void Placeholder_AssignedAfterText_StaysHidden()
    {
        TextBox textBox = new();
        textBox.Text = "x";
        textBox.Placeholder = "type here";
        IsPlaceholderVisible(textBox).ShouldBeFalse();
    }

    [Fact]
    public void Placeholder_ShowsAgainAfterDeletingTheLastCharacter()
    {
        TextBox textBox = Create("a", 1);
        textBox.Placeholder = "type here";
        IsPlaceholderVisible(textBox).ShouldBeFalse();
        textBox.HandleBackspace();
        IsPlaceholderVisible(textBox).ShouldBeTrue();
    }

    static bool IsPlaceholderVisible(TextBox textBox) =>
        textBox.Visual.GetGraphicalUiElementByName("PlaceholderTextInstance")!.Visible;

    // ----- PasswordBox -----

    [Fact]
    public void PasswordBox_Backspace_WithSelection_DeletesTheSelectionOnly()
    {
        PasswordBox box = new();
        box.Width = 300;
        box.Password = "secret";
        box.IsFocused = true;
        box.SelectionStart = 1;
        box.SelectionLength = 3;
        box.HandleBackspace();
        box.Password.ShouldBe("set");
    }

    [Fact]
    public void PasswordBox_HomeEndAndShiftEnd_SelectOverThePassword()
    {
        PasswordBox box = new();
        box.Width = 300;
        box.Password = "secret";
        box.IsFocused = true;
        box.CaretIndex = 2;
        box.HandleKeyDown(Keys.End, true, false, false);
        box.SelectionStart.ShouldBe(2);
        box.SelectionLength.ShouldBe(4);
        box.HandleKeyDown(Keys.Home, false, false, false);
        box.CaretIndex.ShouldBe(0);
    }
}
