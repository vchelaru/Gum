using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Tests.Forms.Linked;

namespace SkiaGum.Tests.Forms;

/// <summary>
/// TextBox behavior over Skia's RichTextKit-backed Text renderable (#6511). Ported from
/// MonoGameGum.Tests TextBoxTests / TextBoxWrappingTests; those files cannot be linked because they
/// reach the MonoGame core <c>RenderingLibrary.Graphics.Text</c> and TextCopy directly.
/// </summary>
public class TextBoxTests : SkiaFormsTestBase
{
    const string WrappingText =
        "This little shack is where the Smith stays. I should not go in if I am not invited";

    static TextBoxBaseVisual VisualOf(TextBox textBox) => (TextBoxBaseVisual)textBox.Visual;

    static IFormsText CoreText(TextBox textBox) =>
        (IFormsText)VisualOf(textBox).TextInstance.RenderableComponent;

    [Fact]
    public void AcceptsReturn_ShouldAddMultipleLines_OnEnterPress()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;

        textBox.HandleCharEntered('1');
        textBox.HandleCharEntered('\n');
        textBox.HandleCharEntered('2');
        textBox.HandleCharEntered('\n');
        textBox.HandleCharEntered('3');

        IFormsText text = CoreText(textBox);
        text.WrappedText.Count.ShouldBe(3);
        text.WrappedText[0].Trim().ShouldBe("1");
        text.WrappedText[1].Trim().ShouldBe("2");
        text.WrappedText[2].Trim().ShouldBe("3");
    }

    [Fact]
    public void HandleCharEntered_ShouldAddCharacter_AndAdvanceCaret()
    {
        TextBox textBox = new();
        textBox.IsFocused = true;

        textBox.HandleCharEntered('a');
        textBox.HandleCharEntered('b');

        textBox.Text.ShouldBe("ab");
        textBox.CaretIndex.ShouldBe(2);
    }

    [Fact]
    public void HandleBackspace_ShouldRemoveCharacterBeforeCaret()
    {
        TextBox textBox = new();
        textBox.IsFocused = true;
        textBox.Text = "abc";
        textBox.CaretIndex = 2;

        textBox.HandleBackspace();

        textBox.Text.ShouldBe("ac");
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void CaretIndex_ShouldMoveCaretRight_WhenIncreased()
    {
        TextBox textBox = new();
        textBox.IsFocused = true;
        textBox.Text = "hello";
        textBox.CaretIndex = 0;
        float xAtStart = VisualOf(textBox).CaretInstance.AbsoluteLeft;

        textBox.CaretIndex = 3;

        VisualOf(textBox).CaretInstance.AbsoluteLeft.ShouldBeGreaterThan(xAtStart);
    }

    [Fact]
    public void CaretIndex_ShouldMoveCaretDown_OnSecondLine_Multiline()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;
        textBox.Height = 200;
        textBox.IsFocused = true;
        textBox.Text = "one\ntwo";
        textBox.CaretIndex = 0;
        float yFirstLine = VisualOf(textBox).CaretInstance.AbsoluteTop;

        textBox.CaretIndex = 5;

        VisualOf(textBox).CaretInstance.AbsoluteTop.ShouldBeGreaterThan(yFirstLine);
    }

    [Fact]
    public void SelectionLength_ShouldShowSelectionInstance_WhenNonZero()
    {
        TextBox textBox = new();
        textBox.IsFocused = true;
        textBox.Text = "hello world";

        textBox.SelectionStart = 0;
        textBox.SelectionLength = 5;

        textBox.Text.Substring(textBox.SelectionStart, textBox.SelectionLength).ShouldBe("hello");
        VisualOf(textBox).SelectionInstance.Visible.ShouldBeTrue();
    }

    [Fact]
    public void SelectionLength_ShouldHideSelectionInstance_WhenZero()
    {
        TextBox textBox = new();
        textBox.IsFocused = true;
        textBox.Text = "hello world";
        textBox.SelectionStart = 0;
        textBox.SelectionLength = 5;

        textBox.SelectionLength = 0;

        VisualOf(textBox).SelectionInstance.Visible.ShouldBeFalse();
    }

    [Fact]
    public void HandleCharEntered_ShouldReplaceSelection()
    {
        TextBox textBox = new();
        textBox.IsFocused = true;
        textBox.Text = "hello world";
        textBox.SelectionStart = 0;
        textBox.SelectionLength = 5;

        textBox.HandleCharEntered('X');

        textBox.Text.ShouldBe("X world");
    }

    [Fact]
    public void HandleCharEntered_ShouldHidePlaceholder_WhenTypingIntoEmptyTextBox()
    {
        TextBox textBox = new();
        textBox.Placeholder = "Enter text";
        textBox.IsFocused = true;
        VisualOf(textBox).PlaceholderTextInstance.Visible.ShouldBeTrue();

        textBox.HandleCharEntered('a');

        VisualOf(textBox).PlaceholderTextInstance.Visible.ShouldBeFalse();
    }

    [Fact]
    public void TextChangedByUi_ShouldRaise_WhenCharTyped_AndNotWhenSetProgrammatically()
    {
        TextBox textBox = new();
        textBox.IsFocused = true;
        int raised = 0;
        textBox.TextChangedByUi += (_, _) => raised++;

        textBox.Text = "programmatic";
        raised.ShouldBe(0);

        textBox.HandleCharEntered('x');
        raised.ShouldBe(1);
    }

    [Fact]
    public void TextChangedByUi_ShouldNotRaise_WhenReadOnly()
    {
        TextBox textBox = new();
        textBox.IsFocused = true;
        textBox.IsReadOnly = true;
        int raised = 0;
        textBox.TextChangedByUi += (_, _) => raised++;

        textBox.HandleCharEntered('x');

        raised.ShouldBe(0);
    }

    [Fact]
    public void IsFocused_ShouldRaiseLostFocus_WhenSetToFalse()
    {
        TextBox textBox = new();
        textBox.IsFocused = true;
        bool lost = false;
        textBox.LostFocus += (_, _) => lost = true;

        textBox.IsFocused = false;

        lost.ShouldBeTrue();
    }

    // Wrapping and horizontal scrolling are two sides of one decision (#4393): text that wraps has
    // nothing off-screen to scroll to.
    [Fact]
    public void Wrap_ShouldNotScrollTextHorizontally_WhenCaretAtEnd()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;
        textBox.Height = 200;
        textBox.IsFocused = true;
        textBox.Text = WrappingText;
        textBox.CaretIndex = 0;
        float restingX = VisualOf(textBox).TextInstance.X;

        textBox.CaretIndex = WrappingText.Length;

        CoreText(textBox).WrappedText.Count.ShouldBeGreaterThan(1);
        VisualOf(textBox).TextInstance.X.ShouldBe(restingX);
    }

    [Fact]
    public void NoWrap_ShouldScrollTextLeft_WhenCaretPastRightEdge()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.NoWrap;
        textBox.Width = 100;
        textBox.IsFocused = true;
        textBox.Text = WrappingText;
        textBox.CaretIndex = WrappingText.Length;

        VisualOf(textBox).TextInstance.X.ShouldBeLessThan(0f);
    }

    [Fact]
    public void Wrap_WithoutAcceptsReturn_ShouldWrapLongText()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = false;
        textBox.Width = 100;
        textBox.Height = 200;

        textBox.Text = WrappingText;

        CoreText(textBox).WrappedText.Count.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void NoWrap_WithoutAcceptsReturn_ShouldKeepSingleLine()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.NoWrap;
        textBox.AcceptsReturn = false;
        textBox.Width = 100;

        textBox.Text = WrappingText;

        CoreText(textBox).WrappedText.Count.ShouldBe(1);
    }

    [Fact]
    public void Caret_ShouldBeVisible_OnlyWhenFocusedWithoutSelection()
    {
        TextBox textBox = new();
        textBox.Text = "hello world";
        VisualOf(textBox).CaretInstance.Visible.ShouldBeFalse();

        textBox.IsFocused = true;
        VisualOf(textBox).CaretInstance.Visible.ShouldBeTrue();

        textBox.SelectionStart = 0;
        textBox.SelectionLength = 5;
        VisualOf(textBox).CaretInstance.Visible.ShouldBeFalse();
    }
}
