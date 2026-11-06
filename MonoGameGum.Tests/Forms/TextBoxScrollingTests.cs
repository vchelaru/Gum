using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Forms.Input;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Linq;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The text of a TextBox scrolls so the caret stays inside the box: horizontally for a single line,
/// vertically for multi-line text, and the caret lines up with the drawn text of aligned lines.
/// </summary>
public class TextBoxScrollingTests : BaseTestClass
{
    const string LongText = "0123456789 0123456789 0123456789 0123456789 0123456789 0123456789";

    static TextBox CreateSingleLine(string text)
    {
        TextBox textBox = new();
        textBox.Width = 120;
        textBox.Visual.X = 20;
        textBox.Visual.Y = 30;
        textBox.Text = text;
        textBox.IsFocused = true;
        return textBox;
    }

    static TextBox CreateMultiLine(string text, float height = 60, float width = 200)
    {
        TextBox textBox = new();
        textBox.TextWrapping = global::Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;
        textBox.Width = width;
        textBox.Height = height;
        textBox.Visual.X = 20;
        textBox.Visual.Y = 30;
        textBox.Text = text;
        textBox.IsFocused = true;
        return textBox;
    }

    static TextBoxBaseVisual V(TextBox textBox) => (TextBoxBaseVisual)textBox.Visual;

    static void AssertCaretInsideHorizontally(TextBox textBox)
    {
        GraphicalUiElement clip = V(textBox).ClipContainer;
        float caretX = V(textBox).CaretInstance.AbsoluteLeft;
        caretX.ShouldBeGreaterThanOrEqualTo(clip.AbsoluteLeft - 0.5f);
        caretX.ShouldBeLessThanOrEqualTo(clip.AbsoluteLeft + clip.GetAbsoluteWidth() + 0.5f);
    }

    [Fact]
    public void SingleLine_CaretMovedToTheEnd_ScrollsTheTextLeftAndKeepsTheCaretInside()
    {
        TextBox textBox = CreateSingleLine(LongText);
        float restingX = V(textBox).TextInstance.X;

        textBox.CaretIndex = LongText.Length;

        V(textBox).TextInstance.X.ShouldBeLessThan(restingX);
        AssertCaretInsideHorizontally(textBox);
    }

    [Fact]
    public void SingleLine_HomeAfterEnd_RestoresTheRestingPosition()
    {
        TextBox textBox = CreateSingleLine(LongText);
        float restingX = V(textBox).TextInstance.X;
        textBox.HandleKeyDown(Keys.End, false, false, false);

        textBox.HandleKeyDown(Keys.Home, false, false, false);

        V(textBox).TextInstance.X.ShouldBe(restingX, tolerance: 0.01f);
        AssertCaretInsideHorizontally(textBox);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(45)]
    public void SingleLine_CaretSetToAnyIndex_StaysInsideTheBox(int index)
    {
        TextBox textBox = CreateSingleLine(LongText);
        textBox.CaretIndex = LongText.Length;

        textBox.CaretIndex = index;

        AssertCaretInsideHorizontally(textBox);
    }

    [Fact]
    public void SingleLine_CtrlLeftBackAcrossTheScrolledText_KeepsTheCaretInside()
    {
        TextBox textBox = CreateSingleLine(LongText);
        textBox.HandleKeyDown(Keys.End, false, false, false);

        for (int i = 0; i < 12; i++)
        {
            textBox.HandleKeyDown(Keys.Left, false, false, true);
            AssertCaretInsideHorizontally(textBox);
        }
    }

    [Fact]
    public void SingleLine_TypingPastTheRightEdge_KeepsTheCaretInside()
    {
        TextBox textBox = CreateSingleLine("");

        foreach (char character in LongText)
        {
            textBox.HandleCharEntered(character);
            AssertCaretInsideHorizontally(textBox);
        }
    }

    [Fact]
    public void SingleLine_TextShorterThanTheBox_NeverScrolls()
    {
        TextBox textBox = CreateSingleLine("short");
        float restingX = V(textBox).TextInstance.X;

        textBox.HandleKeyDown(Keys.End, false, false, false);
        textBox.HandleKeyDown(Keys.Home, true, false, false);

        V(textBox).TextInstance.X.ShouldBe(restingX, tolerance: 0.01f);
    }

    [Fact]
    public void SingleLine_ShiftEndOnLongText_ExtendsTheSelectionToTheEndAndScrolls()
    {
        TextBox textBox = CreateSingleLine(LongText);
        textBox.CaretIndex = 0;

        textBox.HandleKeyDown(Keys.End, true, false, false);

        textBox.SelectionStart.ShouldBe(0);
        textBox.SelectionLength.ShouldBe(LongText.Length);
        AssertCaretInsideHorizontally(textBox);
    }

    [Fact]
    public void MultiLine_DownThroughTheText_KeepsTheCaretInsideVertically()
    {
        string text = string.Join("\n", Enumerable.Range(1, 12).Select(i => "line" + i));
        TextBox textBox = CreateMultiLine(text);
        textBox.CaretIndex = 0;
        GraphicalUiElement clip = V(textBox).ClipContainer;

        for (int i = 0; i < 12; i++)
        {
            textBox.HandleKeyDown(Keys.Down, false, false, false);
            GraphicalUiElement caret = V(textBox).CaretInstance;
            caret.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(clip.AbsoluteTop - 1f);
            (caret.AbsoluteTop + caret.GetAbsoluteHeight()).ShouldBeLessThanOrEqualTo(clip.AbsoluteTop + clip.GetAbsoluteHeight() + 1f);
        }
    }

    [Fact]
    public void MultiLine_Wrapped_NeverScrollsHorizontally()
    {
        TextBox textBox = CreateMultiLine(LongText, height: 120);
        float restingX = V(textBox).TextInstance.X;

        textBox.HandleKeyDown(Keys.End, false, false, true);
        textBox.CaretIndex = LongText.Length;

        V(textBox).TextInstance.X.ShouldBe(restingX, tolerance: 0.01f);
    }

    // ----- Aligned lines -----

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void MultiLine_Aligned_CaretAtTheEndOfEachLineSitsAtTheLinesDrawnEnd(HorizontalAlignment alignment)
    {
        string[] lines = { "short", "a much longer second line", "mid length" };
        TextBox textBox = CreateMultiLine(string.Join("\n", lines), height: 200, width: 700);
        TextRuntime textInstance = V(textBox).TextInstance;
        textInstance.HorizontalAlignment = alignment;
        Text core = (Text)textInstance.RenderableComponent!;
        float boxWidth = textInstance.GetAbsoluteWidth();
        int lineStart = 0;

        foreach (string line in lines)
        {
            textBox.CaretIndex = lineStart + line.Length;
            float lineWidth = core.MeasureDrawnString(line, HorizontalMeasurementStyle.Full);
            float lineLeft = alignment switch
            {
                HorizontalAlignment.Center => (boxWidth - lineWidth) / 2f,
                HorizontalAlignment.Right => boxWidth - lineWidth,
                _ => 0f,
            };

            (V(textBox).CaretInstance.AbsoluteLeft - textInstance.AbsoluteLeft)
                .ShouldBe(lineLeft + lineWidth, tolerance: 1.01f, "end of line " + line);
            lineStart += line.Length + 1;
        }
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    public void MultiLine_Aligned_ClickPastTheEndOfALinePlacesTheCaretAtThatLinesEnd(HorizontalAlignment alignment)
    {
        string[] lines = { "short", "a much longer second line", "mid length" };
        TextBox textBox = CreateMultiLine(string.Join("\n", lines), height: 200, width: 700);
        TextRuntime textInstance = V(textBox).TextInstance;
        textInstance.HorizontalAlignment = alignment;
        Text core = (Text)textInstance.RenderableComponent!;
        float lineHeight = core.WrappedTextHeight / core.WrappedText.Count;
        int lineStart = 0;

        for (int row = 0; row < lines.Length; row++)
        {
            float y = textInstance.AbsoluteTop + (row + 0.5f) * lineHeight;
            float farRight = textInstance.AbsoluteLeft + textInstance.GetAbsoluteWidth() + 50;

            textBox.GetCaretIndexAtPosition(farRight, y).ShouldBe(lineStart + lines[row].Length, "row " + row);
            lineStart += lines[row].Length + 1;
        }
    }
}
