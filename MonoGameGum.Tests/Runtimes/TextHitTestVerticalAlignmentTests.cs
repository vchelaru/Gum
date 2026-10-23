using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Text hit testing and caret placement follow the vertical alignment the renderer draws (#6608):
/// each test reads the drawn top of the text block from <see cref="Text.UpdateIpsoForRenderingForTests"/>
/// and clicks just inside the top and bottom edges of a drawn line.
/// </summary>
public class TextHitTestVerticalAlignmentTests : BaseTestClass
{
    [Theory]
    [InlineData(VerticalAlignment.Top, "Line1", 0)]
    [InlineData(VerticalAlignment.Center, "Line1", 0)]
    [InlineData(VerticalAlignment.Bottom, "Line1", 0)]
    [InlineData(VerticalAlignment.Top, "Line1\nLine2\nLine3", 1)]
    [InlineData(VerticalAlignment.Center, "Line1\nLine2\nLine3", 1)]
    [InlineData(VerticalAlignment.Bottom, "Line1\nLine2\nLine3", 1)]
    [InlineData(VerticalAlignment.Bottom, "Line1\nLine2\nLine3", 2)]
    public void GetCharacterIndexAtPosition_VerticalAlignment_ShouldReturnLineDrawnAtPoint(
        VerticalAlignment verticalAlignment, string text, int line)
    {
        TextRuntime sut = new();
        sut.X = 100;
        sut.Y = 80;
        sut.Width = 200;
        sut.Height = 100;
        sut.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sut.VerticalAlignment = verticalAlignment;
        sut.Text = text;
        Text contained = (Text)sut.RenderableComponent!;
        float lineHeight = contained.LineHeightInPixels * contained.LineHeightMultiplier;
        float drawnTop = contained.UpdateIpsoForRenderingForTests().Y;
        float x = contained.GetAbsoluteLeft() + contained.MeasureString("Lin") + 1;
        int expected = "Line1\n".Length * line + 3;

        sut.GetCharacterIndexAtPosition(x, drawnTop + line * lineHeight + 0.1f).ShouldBe(expected);
        sut.GetCharacterIndexAtPosition(x, drawnTop + (line + 1) * lineHeight - 0.1f).ShouldBe(expected);
    }

    [Theory]
    [InlineData(VerticalAlignment.Top, 1)]
    [InlineData(VerticalAlignment.Center, 1)]
    [InlineData(VerticalAlignment.Bottom, 1)]
    [InlineData(VerticalAlignment.Bottom, 2)]
    public void GetCaretIndexAtPosition_VerticalAlignment_ShouldReturnLineDrawnAtPoint(
        VerticalAlignment verticalAlignment, int line)
    {
        TextBox textBox = CreateMultiLineTextBox(verticalAlignment);
        Text contained = GetCoreText(textBox);
        float lineHeight = contained.LineHeightInPixels * contained.LineHeightMultiplier;
        float drawnTop = contained.UpdateIpsoForRenderingForTests().Y;
        float x = contained.GetAbsoluteLeft() + contained.MeasureString("abc") + 1;
        int expected = "abcdef\n".Length * line + 3;

        textBox.GetCaretIndexAtPosition(x, drawnTop + line * lineHeight + 0.1f).ShouldBe(expected);
        textBox.GetCaretIndexAtPosition(x, drawnTop + (line + 1) * lineHeight - 0.1f).ShouldBe(expected);
    }

    // The caret sits on the drawn line the same way whatever the vertical alignment: its offset
    // from the drawn top of the text block matches the top-aligned box's.
    [Theory]
    [InlineData(VerticalAlignment.Center)]
    [InlineData(VerticalAlignment.Bottom)]
    public void CaretPosition_VerticalAlignment_ShouldFollowDrawnLine(VerticalAlignment verticalAlignment)
    {
        int caretIndexOnLastLine = "abcdef\nabcdef\nabc".Length;
        float topAlignedOffset = GetCaretCenterOffsetFromDrawnTop(VerticalAlignment.Top, caretIndexOnLastLine);

        float offset = GetCaretCenterOffsetFromDrawnTop(verticalAlignment, caretIndexOnLastLine);

        offset.ShouldBe(topAlignedOffset, tolerance: 0.01f);
    }

    static float GetCaretCenterOffsetFromDrawnTop(VerticalAlignment verticalAlignment, int caretIndex)
    {
        TextBox textBox = CreateMultiLineTextBox(verticalAlignment);
        textBox.IsFocused = true;
        textBox.CaretIndex = caretIndex;
        float drawnTop = GetCoreText(textBox).UpdateIpsoForRenderingForTests().Y;
        return ((TextBoxBaseVisual)textBox.Visual).CaretInstance.GetAbsoluteCenterY() - drawnTop;
    }

    static TextBox CreateMultiLineTextBox(VerticalAlignment verticalAlignment)
    {
        TextBox textBox = new();
        textBox.TextWrapping = global::Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;
        textBox.Width = 300;
        textBox.Height = 200;
        textBox.Visual.X = 100;
        textBox.Visual.Y = 80;
        ((TextBoxBaseVisual)textBox.Visual).TextInstance.VerticalAlignment = verticalAlignment;
        textBox.Text = "abcdef\nabcdef\nabcdef";
        return textBox;
    }

    static Text GetCoreText(TextBox textBox) =>
        (Text)((TextBoxBaseVisual)textBox.Visual).TextInstance.RenderableComponent!;
}
