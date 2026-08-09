using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.Wireframe;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Coverage for the interaction between wrapping and horizontal scrolling. A TextBox whose text
/// wraps must never scroll horizontally — every visual line already fits the container, so a
/// leftward shift only pushes characters out of the clipped region. See issue #4393.
/// </summary>
public class TextBoxWrappingTests : BaseTestClass
{
    // Long enough to wrap several times at the default 100px width, and with spaces so the
    // wrapping is word-based like the reported case rather than a single unbreakable run.
    const string WrappingText =
        "This little shack is where the Smith stays. I shouldn't go in if I'm not invited";

    static TextBox CreateWrappingTextBox()
    {
        TextBox textBox = new();
        textBox.TextWrapping = global::Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;
        textBox.Height = 200;
        textBox.IsFocused = true;
        return textBox;
    }

    static RenderingLibrary.Graphics.Text GetCoreText(TextBox textBox) =>
        (RenderingLibrary.Graphics.Text)((DefaultTextBoxBaseRuntime)textBox.Visual)
            .TextInstance.RenderableComponent;

    [Fact]
    public void CaretAtEndOfWrappedLine_ShouldStayWithinHorizontalBounds()
    {
        TextBox textBox = CreateWrappingTextBox();
        textBox.Text = WrappingText;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        var wrappedText = GetCoreText(textBox).WrappedText;
        wrappedText.Count.ShouldBeGreaterThan(1, "sanity: the text should wrap at this width");

        // Walk the caret through every index of the first wrapped line. Because the line was
        // wrapped to fit, no index on it can legitimately place the caret past the right edge.
        for (int i = 0; i <= wrappedText[0].Length; i++)
        {
            textBox.CaretIndex = i;

            float caretRight = visual.CaretInstance.AbsoluteLeft + visual.CaretInstance.AbsoluteWidth;
            caretRight.ShouldBeLessThanOrEqualTo(visual.AbsoluteLeft + visual.AbsoluteWidth,
                $"because the caret at index {i} is on a line that was wrapped to fit, so it must not sit past the right edge");
        }
    }

    [Fact]
    public void ClickOnSecondLine_ShouldMapToIndexOnSecondLine()
    {
        TextBox textBox = CreateWrappingTextBox();
        textBox.Text = WrappingText;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        var wrappedText = GetCoreText(textBox).WrappedText;
        wrappedText.Count.ShouldBeGreaterThan(1, "sanity: the text should wrap at this width");

        // Find the vertical center of line 1 by asking the caret where that line lives.
        textBox.CaretIndex = wrappedText[0].Length;
        float secondLineCenterY = visual.CaretInstance.AbsoluteTop + visual.CaretInstance.AbsoluteHeight / 2f;

        int firstLineLength = wrappedText[0].Length;
        int index = textBox.GetCaretIndexAtPosition(visual.TextInstance.AbsoluteLeft + 1, secondLineCenterY);

        index.ShouldBeGreaterThanOrEqualTo(firstLineLength,
            "because clicking on the second visual line must not resolve to an index on the first line");
        index.ShouldBeLessThanOrEqualTo(firstLineLength + wrappedText[1].Length,
            "because clicking near the left of the second line must not run past that line's end");
    }

    [Fact]
    public void ClickRoundTrip_ShouldReturnSameIndex_OnSecondWrappedLine()
    {
        // Pins that caret placement (measure) and click hit-testing (per-character advance) agree
        // on a wrapped line. A drift between the two is what makes the caret land on the wrong
        // glyph -- and, when the caret drifts far enough right, is what would push a wrapped line
        // into horizontal scrolling.
        TextBox textBox = CreateWrappingTextBox();
        textBox.Text = WrappingText;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        var wrappedText = GetCoreText(textBox).WrappedText;
        wrappedText.Count.ShouldBeGreaterThan(1, "sanity: the text should wrap at this width");

        int firstLineLength = wrappedText[0].Length;

        for (int offsetIntoLine = 0; offsetIntoLine < wrappedText[1].Length; offsetIntoLine++)
        {
            int caretIndex = firstLineLength + offsetIntoLine;
            textBox.CaretIndex = caretIndex;

            float caretX = visual.CaretInstance.AbsoluteLeft;
            float caretCenterY = visual.CaretInstance.AbsoluteTop + visual.CaretInstance.AbsoluteHeight / 2f;

            textBox.GetCaretIndexAtPosition(caretX, caretCenterY).ShouldBe(caretIndex,
                $"because clicking exactly where the caret was drawn for index {caretIndex} should return that same index");
        }
    }

    [Fact]
    public void Paste_ShouldNotScrollHorizontally_WhenVisualWrapsAutomatically()
    {
        Gum.Clipboard.ClipboardImplementation.PushStringToClipboard(WrappingText);

        TextBox textBox = new();
        textBox.Height = 200;
        textBox.IsFocused = true;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        visual.TextInstance.Width = -8f;
        visual.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        float restingTextX = visual.TextInstance.X;

        textBox.HandleKeyDown(global::Gum.Forms.Input.Keys.V, false, false, isCtrlDown: true);

        GetCoreText(textBox).WrappedText.Count.ShouldBeGreaterThan(1,
            "sanity: the pasted text should have wrapped");
        visual.TextInstance.X.ShouldBe(restingTextX,
            "because pasted text that wraps stays inside the container horizontally");
    }

    [Fact]
    public void MultiLineTextInSingleLineMode_ShouldMapClickOnSecondLineToThatLine()
    {
        // Hit-testing has to use the same model of the text as caret placement, or clicking where
        // the caret is drawn moves it somewhere else.
        TextBox textBox = new();
        textBox.Height = 200;
        textBox.IsFocused = true;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        textBox.Text = "First line here\nSecond line here";

        int firstLineLength = GetCoreText(textBox).WrappedText[0].Length;

        textBox.CaretIndex = firstLineLength + 3;
        float caretX = visual.CaretInstance.AbsoluteLeft;
        float caretCenterY = visual.CaretInstance.AbsoluteTop + visual.CaretInstance.AbsoluteHeight / 2f;

        textBox.GetCaretIndexAtPosition(caretX, caretCenterY).ShouldBe(firstLineLength + 3,
            "because clicking where the caret was drawn must return the index the caret was placed for");
    }

    [Fact]
    public void MultiLineTextInSingleLineMode_ShouldSplitSelectionAcrossLines()
    {
        // Selection highlighting shares the same single-line-vs-per-line branch as the caret. On
        // one line it would draw a single rectangle as wide as both lines combined.
        TextBox textBox = new();
        textBox.Height = 200;
        textBox.IsFocused = true;

        textBox.Text = "First line here\nSecond line here";
        textBox.SelectionStart = 0;
        textBox.SelectionLength = textBox.Text.Length;

        int visibleSelectionCount = textBox.Visual.Children
            .OfType<GraphicalUiElement>()
            .Count(item => item.Name == "SelectionInstance" && item.Visible);

        visibleSelectionCount.ShouldBe(2,
            "because a selection spanning two visual lines needs one highlight rectangle per line");
    }

    [Fact]
    public void MultiLineTextInSingleLineMode_ShouldNotScrollHorizontally_WhenVisualWrapsAutomatically()
    {
        // Same defect as MultiLineTextInSingleLineMode_ShouldNotScrollHorizontally, reached the
        // way a custom Gum-tool component does: the component's text instance is sized relative
        // to its parent, so long text wraps on its own. The Forms control is still in single-line
        // mode, so the caret is placed as though all of that text were on one line.
        TextBox textBox = new();
        textBox.Height = 200;
        textBox.IsFocused = true;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        visual.TextInstance.Width = -8f;
        visual.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        float restingTextX = visual.TextInstance.X;

        textBox.Text = WrappingText;

        GetCoreText(textBox).WrappedText.Count.ShouldBeGreaterThan(1,
            "sanity: a parent-relative text instance wraps long text regardless of the Forms TextWrapping value");

        textBox.CaretIndex = WrappingText.IndexOf("shouldn't") + 1;

        visual.TextInstance.X.ShouldBe(restingTextX,
            "because every wrapped line already fits the container, so no horizontal scrolling is warranted");
    }

    [Fact]
    public void MultiNoWrap_ShouldNotWrapLongText()
    {
        // The other half of the state contract: with wrapping off, the visual's MultiNoWrap state
        // sizes TextInstance to its children so long lines overflow horizontally instead of
        // wrapping. This is the configuration horizontal scrolling exists for.
        TextBox textBox = new();
        textBox.TextWrapping = global::Gum.Forms.TextWrapping.NoWrap;
        textBox.AcceptsReturn = true;
        textBox.Height = 200;
        textBox.IsFocused = true;

        textBox.Text = WrappingText;

        GetCoreText(textBox).WrappedText.Count.ShouldBe(1,
            "because NoWrap must leave the text on a single visual line no matter how long it is");
    }

    [Fact]
    public void MultiLineTextInSingleLineMode_ShouldNotScrollHorizontally()
    {
        // Issue #4393. A single-line-configured TextBox still renders '\n' in programmatically
        // assigned text as multiple visual lines, but IsSingleLineMode makes the caret math
        // measure the whole prefix as one line. So the caret for an index on line 2 is placed at
        // (line 1 width + line 2 width) -- far past the right edge -- and the horizontal scroll
        // branch drags the text left to "reveal" it, even though the character is already visible.
        TextBox textBox = new();
        textBox.Height = 200;
        textBox.IsFocused = true;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        float restingTextX = visual.TextInstance.X;

        textBox.Text = "This little shack is where the Smith stays.\nI shouldn't go in if I'm not invited";

        GetCoreText(textBox).WrappedText.Count.ShouldBeGreaterThan(1,
            "sanity: an explicit newline renders as multiple visual lines regardless of AcceptsReturn");

        // Put the caret just after the "I" of the second line.
        textBox.CaretIndex = textBox.Text.IndexOf('\n') + 2;

        visual.TextInstance.X.ShouldBe(restingTextX,
            "because the caret is near the left edge of the second visual line, so there is nothing off-screen to scroll to");
    }

    [Fact]
    public void NoWrapWithoutMultiNoWrapState_ShouldNotScrollHorizontally_WhenVisualWraps()
    {
        // Issue #4393. UpdateStateForSingleOrMultiLine falls back to the "Multi" state when the
        // visual has no "MultiNoWrap" state -- and "Multi" sizes TextInstance relative to its
        // parent, which wraps. The horizontal scroll branch, however, keys off the TextWrapping
        // property rather than the resulting layout, so it still scrolls a text box whose lines
        // are visibly wrapping. Older Gum-tool components (authored before MultiNoWrap existed)
        // hit exactly this path.
        TextBox textBox = new();
        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;

        textBox.Visual.Categories["LineModeCategory"].States
            .RemoveAll(item => item.Name == "MultiNoWrap");

        textBox.TextWrapping = global::Gum.Forms.TextWrapping.NoWrap;
        textBox.AcceptsReturn = true;
        textBox.Height = 200;
        textBox.IsFocused = true;

        float restingTextX = visual.TextInstance.X;

        // Type it out rather than assigning it: the defect surfaces on the keystroke that wraps a
        // line, where the caret sits at the end of the line *including* the trailing space that
        // pushed the wrap. Assigning the whole string skips straight past that state.
        foreach (char character in WrappingText)
        {
            textBox.HandleCharEntered(character);

            visual.TextInstance.X.ShouldBe(restingTextX,
                $"because the text wraps, so no keystroke should scroll it horizontally (broke after typing up to \"{WrappingText.Substring(0, WrappingText.IndexOf(character) + 1)}\")");
        }

        GetCoreText(textBox).WrappedText.Count.ShouldBeGreaterThan(1,
            "sanity: without a MultiNoWrap state the visual falls back to Multi, which wraps");
    }

    [Fact]
    public void Wrap_ShouldNotShiftTextX_WhenCaretMovedIntoWrappedLine()
    {
        // The reported repro: with the caret placed partway into a wrapped line, the text jumped
        // left as though it were a single-line box.
        TextBox textBox = CreateWrappingTextBox();

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        float restingTextX = visual.TextInstance.X;

        textBox.Text = WrappingText;

        for (int i = 0; i <= WrappingText.Length; i++)
        {
            textBox.CaretIndex = i;

            visual.TextInstance.X.ShouldBe(restingTextX,
                $"because moving the caret to index {i} of a wrapping text box must never scroll it horizontally");
        }
    }

    [Fact]
    public void Wrap_ShouldNotShiftTextX_WhenTypingPastRightEdge()
    {
        // Mirror of TextWrapping_NoWrap_ShouldRenderCorrectlyWithAcceptsReturn, which asserts the
        // NoWrap case *does* scroll. Nothing previously asserted that Wrap does not.
        TextBox textBox = CreateWrappingTextBox();

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        float restingTextX = visual.TextInstance.X;

        foreach (char character in WrappingText)
        {
            textBox.HandleCharEntered(character);
        }

        GetCoreText(textBox).WrappedText.Count.ShouldBeGreaterThan(1,
            "sanity: typing this much text should have wrapped it");
        visual.TextInstance.X.ShouldBe(restingTextX,
            "because typed text that wraps stays inside the container horizontally; there is nothing to scroll to");
    }

    [Fact]
    public void Wrap_ShouldWrapLongText()
    {
        // Pins the state contract from the other direction: the "Multi" state must size
        // TextInstance relative to its parent so long text wraps rather than overflowing.
        TextBox textBox = CreateWrappingTextBox();

        textBox.Text = WrappingText;

        GetCoreText(textBox).WrappedText.Count.ShouldBeGreaterThan(1,
            "because Wrap must break text that exceeds the container width onto additional lines");
    }
}
