using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Localization;
using Gum.Mvvm;
using Gum.Wireframe;
using MonoGameGum.Forms.DefaultVisuals;
using Gum.GueDeriving;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextCopy;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class TextBoxTests : BaseTestClass
{

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


        var textInstance =
            textBox.Visual.Find<TextRuntime>("TextInstance")!;

        var innerTextObject = (RenderingLibrary.Graphics.Text)textInstance.RenderableComponent;

        innerTextObject.WrappedText.Count.ShouldBe(3);
        innerTextObject.WrappedText[0].Trim().ShouldBe("1");
        innerTextObject.WrappedText[1].Trim().ShouldBe("2");
        innerTextObject.WrappedText[2].Trim().ShouldBe("3");
    }

    [Fact]
    public void Backspace_ShouldNotLeaveBlankSpaceBelowText_Multiline()
    {
        TextBox textBox = new();
        textBox.AcceptsReturn = true;
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.Height = 60;
        textBox.IsFocused = true;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;

        for (int i = 0; i < 10; i++)
        {
            textBox.HandleCharEntered('\n');
        }
        visual.TextInstance.Y.ShouldBeLessThan(0f, "sanity: typing should have scrolled the text up");

        for (int i = 0; i < 10; i++)
        {
            textBox.HandleBackspace();
        }

        visual.TextInstance.Y.ShouldBe(0f,
            "because once content shrinks back to a single line, no vertical scroll is needed and there should be no blank band above the text");
    }

    [Fact]
    public void CaretAboveVisibleArea_ShouldShiftTextDown_Multiline()
    {
        TextBox textBox = new();
        textBox.AcceptsReturn = true;
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.Height = 60;
        textBox.IsFocused = true;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;

        for (int i = 0; i < 10; i++)
        {
            textBox.HandleCharEntered('\n');
        }

        float scrolledY = visual.TextInstance.Y;
        scrolledY.ShouldBeLessThan(0f, "because adding many lines should have scrolled the text up");

        textBox.CaretIndex = 0;

        visual.TextInstance.Y.ShouldBeGreaterThan(scrolledY,
            "because the caret moved back to line 0 and the text needs to shift down to keep it in view");
    }

    [Fact]
    public void CaretBelowVisibleArea_ShouldShiftTextUp_Multiline()
    {
        TextBox textBox = new();
        textBox.AcceptsReturn = true;
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.Height = 60;
        textBox.IsFocused = true;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        float originalTextY = visual.TextInstance.Y;

        for (int i = 0; i < 10; i++)
        {
            textBox.HandleCharEntered('\n');
        }

        visual.TextInstance.Y.ShouldBeLessThan(originalTextY,
            "because the caret moved past the bottom of the visible area and the text should scroll up");

        // Caret should be roughly at the bottom of the visual after typing past
        // the bottom. Tolerance matches the convention used by the existing X-axis
        // scroll tests (font/layout geometry has small implementation-dependent
        // variance).
        float caretAbsoluteCenter = visual.CaretInstance.AbsoluteTop + visual.CaretInstance.GetAbsoluteHeight() / 2f;
        float visualBottom = visual.AbsoluteTop + visual.GetAbsoluteHeight();
        caretAbsoluteCenter.ShouldBeLessThanOrEqualTo(visualBottom,
            "because the caret center should remain inside the visible area after vertical scrolling");
    }

    [Fact]
    public void CaretIndex_ShouldAdjustCaretPosition()
    {
        TextBox textBox = new();
        textBox.Text = "Hello";

        GraphicalUiElement caret = 
            textBox.Visual.FindByName("CaretInstance")!;

        textBox.CaretIndex = 0;
        float absolutePosition = caret.AbsoluteLeft;

        textBox.CaretIndex = 2;
        caret.AbsoluteLeft.ShouldBeGreaterThan(absolutePosition);
        float positionAt2 = caret.AbsoluteLeft;

        textBox.CaretIndex = 4;
        caret.AbsoluteLeft.ShouldBeGreaterThan(positionAt2);

        textBox.CaretIndex = 5;
        float positionAt5 = caret.AbsoluteLeft;
        textBox.CaretIndex = 6;
        caret.AbsoluteLeft.ShouldBe(positionAt5);
    }

    [Fact]
    public void CaretIndex_ShouldAdjustCaretPosition_Multiline()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;
        // Tall enough to fit all 4 lines without triggering the vertical scroll
        // logic; this test only cares that caret Y moves with the line index.
        textBox.Height = 200;

        textBox.HandleCharEntered('\n');
        textBox.HandleCharEntered('\n');
        textBox.HandleCharEntered('\n');

        // give it focus so that the caret is visible:
        textBox.IsFocused = true;

        GraphicalUiElement caret =
            textBox.Visual.FindByName("CaretInstance")!;

        textBox.CaretIndex = 0;
        float absolutePosition = caret.AbsoluteTop;

        textBox.CaretIndex = 1;
        caret.AbsoluteTop.ShouldBeGreaterThan(absolutePosition);
        float positionAt1 = caret.AbsoluteTop;

        textBox.CaretIndex = 2;
        caret.AbsoluteTop.ShouldBeGreaterThan(positionAt1);
        float positionAt2 = caret.AbsoluteTop;

        textBox.CaretIndex = 3;
        caret.AbsoluteTop.ShouldBeGreaterThan(positionAt2);
        float positionAt3 = caret.AbsoluteTop;

        textBox.CaretIndex = 4;
        caret.AbsoluteTop.ShouldBe(positionAt3); // Should not move past the last line
    }

    [Fact]
    public void CaretMovingWithinVisibleArea_ShouldNotShiftText_Multiline()
    {
        TextBox textBox = new();
        textBox.AcceptsReturn = true;
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.Height = 200;
        textBox.IsFocused = true;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        float originalTextY = visual.TextInstance.Y;

        textBox.HandleCharEntered('a');
        textBox.HandleCharEntered('\n');
        textBox.HandleCharEntered('b');

        textBox.CaretIndex = 0;

        visual.TextInstance.Y.ShouldBe(originalTextY,
            "because both lines fit within the visible area; no scrolling should occur");
    }

    [Fact]
    public void LineHeightMultiplier_ShouldScaleCaretLineSpacing_Multiline()
    {
        // Issue #2712: LineHeightMultiplier was being ignored by the caret/selection
        // line-position math in TextBoxBase, so the caret sat at the wrong Y for any
        // line beyond line 0 whenever the multiplier was not 1. Multiplier is set
        // BEFORE the caret is positioned to isolate the math from any reactivity.

        static float CaretTopForLine(float multiplier, int caretIndex)
        {
            TextBox tb = new();
            tb.TextWrapping = Gum.Forms.TextWrapping.Wrap;
            tb.AcceptsReturn = true;
            tb.Height = 400;
            tb.IsFocused = true;

            DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)tb.Visual;
            visual.TextInstance.LineHeightMultiplier = multiplier;

            tb.HandleCharEntered('\n');
            tb.HandleCharEntered('\n');
            tb.HandleCharEntered('\n');
            tb.CaretIndex = caretIndex;

            return visual.CaretInstance.AbsoluteTop;
        }

        // Gap between line 0 and line 2 at 1x and at 2x. With a 2x multiplier the
        // gap should be roughly 2x as large; we assert at least 1.5x to allow for
        // half-line center/edge adjustments without being brittle to font metrics.
        float gap1x = CaretTopForLine(1.0f, 2) - CaretTopForLine(1.0f, 0);
        float gap2x = CaretTopForLine(2.0f, 2) - CaretTopForLine(2.0f, 0);

        gap1x.ShouldBeGreaterThan(0f, "sanity: line 2's caret should be below line 0's");
        gap2x.ShouldBeGreaterThan(gap1x * 1.5f,
            "because doubling the line height multiplier should roughly double the " +
            "line-to-line gap; if the gap is unchanged the multiplier is being ignored " +
            "by the caret-position math (regression of #2712)");
    }

    [Fact]
    public void LineHeightMultiplier_ShouldNotShiftCaretOnLine0()
    {
        // Issue #2687 (follow-up): even after the gap between lines was fixed to
        // honor LineHeightMultiplier, every line — including line 0 — was offset
        // by a constant (multiplier - 1) * lineHeight / 2 because the per-line
        // half-step used the *effective* line height instead of the raw line
        // height. Line 0's glyph row is drawn at the same Y regardless of the
        // multiplier (the multiplier only adds spacing *between* lines), so the
        // caret on line 0 must not move when the multiplier changes.

        static float CaretTopOnLine0(float multiplier)
        {
            TextBox tb = new();
            tb.TextWrapping = Gum.Forms.TextWrapping.Wrap;
            tb.AcceptsReturn = true;
            tb.Height = 400;
            tb.IsFocused = true;

            DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)tb.Visual;
            visual.TextInstance.LineHeightMultiplier = multiplier;

            tb.HandleCharEntered('a');
            tb.CaretIndex = 1;

            return visual.CaretInstance.AbsoluteTop;
        }

        float caret1x = CaretTopOnLine0(1.0f);
        float caret2x = CaretTopOnLine0(2.0f);

        caret2x.ShouldBe(caret1x, tolerance: 0.5f,
            "because line 0 is drawn at the same Y regardless of LineHeightMultiplier; " +
            "the multiplier only inflates the gap *between* lines, not the position of line 0 itself");
    }

    [Fact]
    public void FontScale_ShouldScaleCaretXOnFirstLine()
    {
        // Issue #2687: FontScale was ignored in the caret X math. The caret X
        // comes from Text.MeasureString, which explicitly returns the raw
        // (unscaled) glyph width — so the caret sat at the unscaled offset
        // while the rendered glyphs were FontScale-wider. The caret's X
        // advance (caret-at-end minus caret-at-start) for the same characters
        // should roughly double when FontScale doubles.

        static float CaretXAdvance(float fontScale)
        {
            TextBox tb = new();
            tb.IsFocused = true;

            DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)tb.Visual;
            visual.TextInstance.FontScale = fontScale;

            float start = visual.CaretInstance.AbsoluteLeft;
            tb.HandleCharEntered('a');
            tb.HandleCharEntered('a');
            tb.HandleCharEntered('a');
            float end = visual.CaretInstance.AbsoluteLeft;

            return end - start;
        }

        float advance1x = CaretXAdvance(1.0f);
        float advance2x = CaretXAdvance(2.0f);

        advance1x.ShouldBeGreaterThan(0f, "sanity: caret should advance after typing characters");
        advance2x.ShouldBeGreaterThan(advance1x * 1.5f,
            "because doubling FontScale should roughly double the caret X advance " +
            "(rendered glyphs are 2× wider); if the advance is unchanged the caret X " +
            "math is using unscaled MeasureString (issue #2687)");
    }

    [Fact]
    public void FontScale_ShouldScaleCaretLineSpacing_Multiline()
    {
        // Issue #2687: when FontScale on the inner TextInstance is set to a
        // value other than 1.0, the rendered text scales but the caret's
        // line-to-line spacing in TextBoxBase ignored FontScale and used only
        // the raw (unscaled) font line height. As a result, the caret drifted
        // out of sync with the glyphs on any line past line 0.

        static float CaretTopForLine(float fontScale, int caretIndex)
        {
            TextBox tb = new();
            tb.TextWrapping = Gum.Forms.TextWrapping.Wrap;
            tb.AcceptsReturn = true;
            tb.Height = 400;
            tb.IsFocused = true;

            DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)tb.Visual;
            visual.TextInstance.FontScale = fontScale;

            tb.HandleCharEntered('\n');
            tb.HandleCharEntered('\n');
            tb.HandleCharEntered('\n');
            tb.CaretIndex = caretIndex;

            return visual.CaretInstance.AbsoluteTop;
        }

        float gap1x = CaretTopForLine(1.0f, 2) - CaretTopForLine(1.0f, 0);
        float gap2x = CaretTopForLine(2.0f, 2) - CaretTopForLine(2.0f, 0);

        gap1x.ShouldBeGreaterThan(0f, "sanity: line 2's caret should be below line 0's");
        gap2x.ShouldBeGreaterThan(gap1x * 1.5f,
            "because doubling the font scale should roughly double the line-to-line " +
            "gap; if the gap is unchanged the caret-position math is ignoring FontScale (issue #2687)");
    }

    [Fact]
    public void Width_ShouldNotShiftTextX_AfterTransientNegativeWidth()
    {
        // Repro for issue #2680: when a TextBox's absolute width transitions
        // from a transient bad value (e.g. -346, which arises during init when
        // Width and WidthUnits are applied in sequence and the relative-to-parent
        // math hasn't settled) to a valid positive value, the text instance's
        // X-offset should reset to 0. Previously KeepCaretEdgeInsideParent was
        // asymmetric: it shifted the text right when the caret fell to the left
        // of the (then-tiny) parent, and never snapped back once the parent's
        // width became reasonable, leaving a permanent ~5px gap on the left.
        TextBox textBox = new();
        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        float restingTextX = visual.TextInstance.X;

        textBox.Visual.Width = -346;
        textBox.Visual.Width = 294;

        visual.TextInstance.X.ShouldBe(restingTextX,
            "because once the parent has a valid width and the caret is comfortably inside it, the text X should match its at-rest value; transient bad widths must not leave a sticky horizontal offset");
    }

    [Fact]
    public void IdenticallyConfiguredTextBoxes_ShouldHaveMatchingTextAndCaretX()
    {
        // Smoke test for the symptom reported in issue #2680 (Raylib runtime
        // screenshot showed caret columns drifting between visually-identical
        // TextBoxes). The root cause was layout-order-dependent shifts in
        // KeepCaretEdgeInsideParent; this test guards against any future cause
        // by asserting that two TextBoxes set up the same way end up at the
        // same horizontal text/caret position.
        TextBox first = new();
        first.Visual.Width = -346;
        first.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        TextBox second = new();
        second.Visual.Width = -346;
        second.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        DefaultTextBoxBaseRuntime firstVisual = (DefaultTextBoxBaseRuntime)first.Visual;
        DefaultTextBoxBaseRuntime secondVisual = (DefaultTextBoxBaseRuntime)second.Visual;

        secondVisual.TextInstance.X.ShouldBe(firstVisual.TextInstance.X,
            "because identically-configured TextBoxes must not drift apart in their text X — that's the visible symptom reported in #2680");
        secondVisual.CaretInstance.X.ShouldBe(firstVisual.CaretInstance.X,
            "because identically-configured TextBoxes must not drift apart in their caret X");
    }

    [Fact]
    public void DeletingScrolledText_ShouldSnapTextXBackToResting()
    {
        // Issue #2683: KeepCaretEdgeInsideParent only shifts to bring the caret
        // into view; it never undoes a prior shift once the caret is comfortably
        // inside the band. So after typing past the right edge (which scrolls
        // textComponent.X negative) and then deleting the text, textComponent.X
        // is left holding a stale offset and the leading characters sit off to
        // the left of the visible area. Assert that the text X snaps back to its
        // resting value when the content shrinks back to fit.
        TextBox textBox = new();
        textBox.IsFocused = true;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        float restingTextX = visual.TextInstance.X;

        // Type enough characters to overflow the default width and force a scroll.
        for (int i = 0; i < 60; i++)
        {
            textBox.HandleCharEntered('a');
        }
        visual.TextInstance.X.ShouldBeLessThan(restingTextX,
            "sanity: typing past the right edge should have scrolled the text left");

        // Delete everything.
        for (int i = 0; i < 60; i++)
        {
            textBox.HandleBackspace();
        }

        visual.TextInstance.X.ShouldBe(restingTextX,
            "because once the content fits again with the caret at index 0, the text X should return to its at-rest value rather than hold the scroll-left offset from earlier");
    }

    [Fact]
    public void Height_ShouldNotShiftTextY_AfterTransientNegativeHeight_Multiline()
    {
        // Y-axis analog of Width_ShouldNotShiftTextX_AfterTransientNegativeWidth (#2680).
        // The vertical branch of KeepCaretEdgeInsideParent received the same
        // invalid-geometry guard but is otherwise untested. Pin the behavior so
        // a future refactor of either branch can't silently regress only one axis.
        TextBox textBox = new();
        textBox.AcceptsReturn = true;
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        float restingTextY = visual.TextInstance.Y;

        textBox.Visual.Height = -200;
        textBox.Visual.Height = 200;

        visual.TextInstance.Y.ShouldBe(restingTextY,
            "because once the parent has a valid height and the caret is comfortably inside it, the text Y should match its at-rest value; transient bad heights must not leave a sticky vertical offset");
    }

    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        TextBox textBox = new();
        InteractiveGue visual = textBox.Visual;

        List<ContainerRuntime> children = visual.Descendants().OfType<ContainerRuntime>().ToList();

        foreach (var child in children)
        {
            child.HasEvents.ShouldBeFalse(
                $"Because child {child.Name} with parent {child.Parent?.Name} should not be clickable, but it is so it eats events");
        }
    }

    [Fact]
    public void Click_ShouldSetIsFocused()
    {
        TextBox textBox = new();
        textBox.Visual.CallClick();
        textBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void HandleCharEntered_ShouldAddCharacter_WhenNonEnterKeyTyped()
    {
        TextBox textBox = new();

        textBox.HandleCharEntered('a');

        textBox.Text.ShouldBe("a");
    }

    [Fact]
    public void HandleCharEntered_ShouldUpdateBindingSource_OnEnterNoAcceptsReturn()
    {
        TextBox textBox = new();
        TestViewModel vm = new ()
        {
            Text = null
        };
        textBox.BindingContext = vm;
        Binding binding = new (() => textBox.Text)
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        };

        textBox.SetBinding(nameof(textBox.Text), binding);

        textBox.AcceptsReturn = false;
        textBox.Text = "Test text";
        textBox.HandleCharEntered('\n'); // Simulate Enter key press

        vm.Text.ShouldBe("Test text");
    }

    [Fact]
    public void HandleCharEntered_ShouldNotLocalizeText_WhenTypingCharacter()
    {
        var mockLocalization = new Mock<ILocalizationService>();
        mockLocalization.Setup(x => x.Translate(It.IsAny<string>())).Returns("TRANSLATED");
        CustomSetPropertyOnRenderable.LocalizationService = mockLocalization.Object;

        TextBox textBox = new();
        textBox.Text.ShouldBeNullOrEmpty();
        textBox.HandleCharEntered('a');

        textBox.Text.ShouldBe("a");
    }

    [Fact]
    public void HandleCharEntered_ShouldNotUpdateBindingSource_OnEnterAcceptsReturn()
    {

        TextBox textBox = new();
        TestViewModel vm = new ()
        {
            Text = null
        };
        textBox.BindingContext = vm;
        Binding binding = new (() => textBox.Text)
        {
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        };

        textBox.SetBinding(nameof(textBox.Text), binding);

        textBox.IsFocused = true;
        textBox.AcceptsReturn = true;
        textBox.Text = "Test text";
        textBox.CaretIndex = 0;
        textBox.HandleCharEntered('\n'); // Simulate Enter key press

        vm.Text.ShouldBe(null);

        textBox.IsFocused = false;

        vm.Text.ShouldBe("\nTest text");
    }

    [Fact]
    public void HandleCharEntered_ShouldAddMultipleLines_IfWrap()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.IsFocused = true;
        textBox.Width = 50;
        textBox.AcceptsReturn = true;

        for(int i = 0; i < 10; i++)
        {
            textBox.HandleCharEntered('a');
            textBox.HandleCharEntered(' ');
        }

        var textInstance = textBox.Visual.Find<TextRuntime>("TextInstance")!;
        var innerTextObject = (RenderingLibrary.Graphics.Text)textInstance.RenderableComponent;
        innerTextObject.WrappedText.Count.ShouldBeGreaterThan(1);
    }

    [Fact]
    public void HandleKeyDown_Paste_ShouldReplaceSlashRSlashN_WithSlashN()
    {
        Gum.Clipboard.ClipboardImplementation.PushStringToClipboard("Line1\r\nLine2");

        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.IsFocused = true;
        textBox.HandleKeyDown(Microsoft.Xna.Framework.Input.Keys.V, false, false, isCtrlDown: true);
        textBox.Text.ShouldBe("Line1\nLine2", "because TextBox expects a single-character newline for proper caret positioning");
    }

    [Fact]
    public void IsFocused_ShouldRaiseLostFocus_IfIsFocusedSetToFalse()
    {
        TextBox textBox = new();
        bool lostFocusRaised = false;
        textBox.LostFocus += (s, e) => lostFocusRaised = true;
        textBox.IsFocused = true;
        textBox.IsFocused = false;
        lostFocusRaised.ShouldBeTrue();
    }

    [Fact]
    public void SingleLine_ShouldNotShiftTextVertically_OnTyping()
    {
        TextBox textBox = new();
        textBox.IsFocused = true;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;
        float originalTextY = visual.TextInstance.Y;
        float originalCaretY = visual.CaretInstance.Y;

        for (int i = 0; i < 40; i++)
        {
            textBox.HandleCharEntered('a');
        }

        visual.TextInstance.Y.ShouldBe(originalTextY,
            "because single-line mode should never scroll text vertically");
        visual.CaretInstance.Y.ShouldBe(originalCaretY,
            "because single-line mode should never shift the caret vertically");
    }

    [Fact]
    public void SingleLine_ShouldNotShiftTextVertically_WhenCaretWouldBeOutOfBoundsY()
    {
        TextBox textBox = new();
        textBox.IsFocused = true;

        DefaultTextBoxBaseRuntime visual = (DefaultTextBoxBaseRuntime)textBox.Visual;

        // Force the text instance well outside the visible vertical range so the
        // caret would be visually clipped. Single-line mode must NOT react to this
        // by shifting Y; only the horizontal axis should be considered.
        visual.TextInstance.Y = 500f;
        float forcedTextY = visual.TextInstance.Y;

        for (int i = 0; i < 10; i++)
        {
            textBox.HandleCharEntered('a');
        }

        visual.TextInstance.Y.ShouldBe(forcedTextY,
            "because single-line mode must opt out of vertical scrolling entirely");

        textBox.CaretIndex = 0;
        visual.TextInstance.Y.ShouldBe(forcedTextY,
            "because moving the caret in single-line mode must never trigger a vertical shift");
    }

    [Fact]
    public void TextBox_ShouldHaveSelectionInstance()
    {
        var textBox = new TextBox();
        var selection = textBox.Visual.Find<ColoredRectangleRuntime>("SelectionInstance")!;

        selection.Color = Microsoft.Xna.Framework.Color.Blue;

        selection.ShouldNotBeNull();
    }

    [Fact]
    public void TextBox_ShouldRestrictLinesWhenMaxLinesSetBefore()
    {
        var textBox = new TextBox();
        textBox.MaxNumberOfLines = 3;
        textBox.Text = "line1\nline2\nline3\nline4\nline5";
        
        var textInstance = textBox.Visual.Find<TextRuntime>("TextInstance")!;

        var innerTextObject = (RenderingLibrary.Graphics.Text)textInstance.RenderableComponent;

        innerTextObject.WrappedText.Count.ShouldBe(3);
        innerTextObject.WrappedText[0].ShouldBe("line1\n");
        innerTextObject.WrappedText[1].ShouldBe("line2\n");
        innerTextObject.WrappedText[2].ShouldBe("line3\n");
    }

    [Fact]
    public void TextBox_ShouldRestrictLinesWhenMaxLinesSetAfter()
    {
        var textBox = new TextBox();
        textBox.Text = "line1\nline2\nline3\nline4\nline5";
        textBox.MaxNumberOfLines = 3;

        var textInstance = textBox.Visual.Find<TextRuntime>("TextInstance")!;

        var innerTextObject = (RenderingLibrary.Graphics.Text)textInstance.RenderableComponent;

        innerTextObject.WrappedText.Count.ShouldBe(3);
        innerTextObject.WrappedText[0].ShouldBe("line1\n");
        innerTextObject.WrappedText[1].ShouldBe("line2\n");
        innerTextObject.WrappedText[2].ShouldBe("line3\n");
    }

    [Fact]
    public void TextBox_ShouldNotRestrictLinesWhenMaxLinesIsNull()
    {
        var textBox = new TextBox();
        textBox.Text = "line1\nline2\nline3\nline4\nline5";
        textBox.MaxNumberOfLines = null;

        var textInstance = textBox.Visual.Find<TextRuntime>("TextInstance")!;

        var innerTextObject = (RenderingLibrary.Graphics.Text)textInstance.RenderableComponent;

        innerTextObject.WrappedText.Count.ShouldBe(5);
        innerTextObject.WrappedText[0].ShouldBe("line1\n");
        innerTextObject.WrappedText[1].ShouldBe("line2\n");
        innerTextObject.WrappedText[2].ShouldBe("line3\n");
        innerTextObject.WrappedText[3].ShouldBe("line4\n");
        innerTextObject.WrappedText[4].ShouldBe("line5");
    }

    [Fact]
    public void TextBox_ShouldPassDownMaxLettersToShowToInnerTextInstance()
    {
        var textBox = new TextBox();

        // Unfortuentely the MaxLettersToShow is only used during the RENDERING 
        // in the Text.DrawWithInlineVariables method.  This means that we
        // have no way to get the actual "Displayed" or "Drawn" text in a unit test (yet?).
        //textBox.Text = "abcdefghijklmnopqrstuvwxyz";
        textBox.MaxLettersToShow = 3;

        var textInstance = textBox.Visual.Find<TextRuntime>("TextInstance")!;

        textInstance.MaxLettersToShow.ShouldBe(3);

        var innerTextObject = (RenderingLibrary.Graphics.Text)textInstance.RenderableComponent;

        innerTextObject.MaxLettersToShow.ShouldBe(3);
    }

    [Fact]
    public void TextWrapping_NoWrap_ShouldRenderCorrectlyWithAcceptsReturn()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.NoWrap;
        textBox.AcceptsReturn = true;
        textBox.IsFocused = true;

        DefaultTextBoxBaseRuntime visual =
            (DefaultTextBoxBaseRuntime)textBox.Visual;

        float originalCaretX = visual.CaretInstance.X;
        float originalCaretY = visual.CaretInstance.Y;

        float textInstanceX = visual.TextInstance.X;


        for(int i = 0; i < 40; i++)
        {
            textBox.HandleCharEntered('a');
        }

        visual.CaretInstance.X.ShouldBeGreaterThan(originalCaretX);
        visual.CaretInstance.Y.ShouldBe(originalCaretY);
        visual.TextInstance.X.ShouldBeLessThan(textInstanceX, "because the text scrolled to the left");

        textBox.HandleCharEntered('\n');
        visual.CaretInstance.X.ShouldBe(originalCaretX, 
            // Add some tolerance because text positioning is based on caret size, and this is different than the
            // starting Text value. Oh well...
            tolerance:2, 
            customMessage:"because the caret should reset to the start of the line after a newline");
        visual.CaretInstance.Y.ShouldBeGreaterThan(originalCaretY, "because the caret should move down after a newline");
        visual.TextInstance.X.ShouldBe(textInstanceX, 
            // See above why we add tolerance:
            tolerance:2,
            customMessage:"because the text should not scroll to the left after a newline in NoWrap mode");
    }

    #region ViewModels

    class TestViewModel : ViewModel
    {
        public string? Text
        {
            get => Get<string>();
            set => Set(value);
        }
    }

    #endregion

    #region Visual
    [Fact]
    public void NativeKeyboardInput_ShouldBypassLocalization_OnTextBox()
    {
        var mockLocalization = new Mock<ILocalizationService>();
        mockLocalization.Setup(x => x.Translate(It.IsAny<string>())).Returns("TRANSLATED");
        CustomSetPropertyOnRenderable.LocalizationService = mockLocalization.Object;

        try
        {
            var textBox = new NativeKeyboardAccessTextBox();
            textBox.InvokeSetTextFromNativeKeyboardInput("user entry");
            textBox.Text.ShouldBe("user entry");
        }
        finally
        {
            CustomSetPropertyOnRenderable.LocalizationService = null;
        }
    }

    [Fact]
    public void NativeKeyboardPasswordMode_ShouldBeFalse_ForTextBox()
    {
        var textBox = new NativeKeyboardAccessTextBox();
        textBox.GetUseNativeKeyboardPasswordMode().ShouldBeFalse();
    }

    [Fact]
    public void ShowNativeKeyboardOnFocus_Default_ShouldMatchPlatform()
    {
        var expected = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
        new TextBox().ShowNativeKeyboardOnFocus.ShouldBe(expected);
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        TextBox sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    #endregion

    class NativeKeyboardAccessTextBox : TextBox
    {
        public void InvokeSetTextFromNativeKeyboardInput(string value)
            => SetTextFromNativeKeyboardInput(value);

        public bool GetUseNativeKeyboardPasswordMode() => UseNativeKeyboardPasswordMode;
    }
}