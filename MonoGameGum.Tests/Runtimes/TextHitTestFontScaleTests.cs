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
/// Text hit testing measures in drawn units, so it follows FontScale and the oversampling
/// compensation scale (#7159). Expected positions come from the raw font measurement times the
/// scale, and the drawn left edge from <see cref="Text.UpdateIpsoForRenderingForTests"/>.
/// </summary>
public class TextHitTestFontScaleTests : BaseTestClass
{
    const string SingleLine = "ABCDEFGHIJ";

    [Fact]
    public void GetCharacterIndexAtPosition_HalfFontScale_ShouldReturnIndexAtLastDrawnCharacter()
    {
        TextRuntime sut = CreateText(SingleLine, fontScale: 0.5f, compensation: 1f, HorizontalAlignment.Left);
        Text contained = (Text)sut.RenderableComponent!;

        int index = contained.GetCharacterIndexAtPosition(
            contained.GetAbsoluteLeft() + contained.WrappedTextWidth - 1, contained.GetAbsoluteTop() + 1);

        index.ShouldBeInRange(SingleLine.Length - 1, SingleLine.Length);
    }

    // Each character is clicked a quarter and three quarters across its drawn width: the nearest edge
    // is the character's start, then its end. 0.5 x 1 and 1 x 0.5 draw the same, so they must hit the same.
    [Theory]
    [InlineData(0.5f, 1f, HorizontalAlignment.Left)]
    [InlineData(1f, 1f, HorizontalAlignment.Left)]
    [InlineData(2f, 1f, HorizontalAlignment.Left)]
    [InlineData(1f, 0.5f, HorizontalAlignment.Left)]
    [InlineData(2f, 0.5f, HorizontalAlignment.Left)]
    [InlineData(0.5f, 1f, HorizontalAlignment.Center)]
    [InlineData(1f, 1f, HorizontalAlignment.Center)]
    [InlineData(2f, 1f, HorizontalAlignment.Center)]
    [InlineData(1f, 0.5f, HorizontalAlignment.Center)]
    [InlineData(0.5f, 1f, HorizontalAlignment.Right)]
    [InlineData(1f, 1f, HorizontalAlignment.Right)]
    [InlineData(2f, 1f, HorizontalAlignment.Right)]
    [InlineData(1f, 0.5f, HorizontalAlignment.Right)]
    public void GetCharacterIndexAtPosition_SingleLine_ShouldReturnCharacterNearestDrawnPoint(
        float fontScale, float compensation, HorizontalAlignment alignment)
    {
        TextRuntime sut = CreateText(SingleLine, fontScale, compensation, alignment);
        Text contained = (Text)sut.RenderableComponent!;
        float scale = fontScale * compensation;
        float drawnLeft = contained.UpdateIpsoForRenderingForTests().X;
        float drawnY = contained.UpdateIpsoForRenderingForTests().Y + 1;

        for (int character = 0; character < SingleLine.Length; character++)
        {
            float start = contained.MeasureString(SingleLine.Substring(0, character)) * scale;
            float end = contained.MeasureString(SingleLine.Substring(0, character + 1)) * scale;
            float width = end - start;

            contained.GetCharacterIndexAtPosition(drawnLeft + start + width * 0.25f, drawnY)
                .ShouldBe(character, $"left quarter of character {character}");
            contained.GetCharacterIndexAtPosition(drawnLeft + start + width * 0.75f, drawnY)
                .ShouldBe(character + 1, $"right quarter of character {character}");
        }
    }

    // Wrapped lines step by the scaled line height and each line aligns on its own drawn width.
    [Theory]
    [InlineData(0.5f, 1f, HorizontalAlignment.Left)]
    [InlineData(2f, 1f, HorizontalAlignment.Left)]
    [InlineData(1f, 0.5f, HorizontalAlignment.Left)]
    [InlineData(0.5f, 1f, HorizontalAlignment.Center)]
    [InlineData(2f, 1f, HorizontalAlignment.Center)]
    [InlineData(1f, 0.5f, HorizontalAlignment.Center)]
    [InlineData(0.5f, 1f, HorizontalAlignment.Right)]
    [InlineData(2f, 1f, HorizontalAlignment.Right)]
    public void GetCharacterIndexAtPosition_MultipleLines_ShouldReturnCharacterOnDrawnLine(
        float fontScale, float compensation, HorizontalAlignment alignment)
    {
        string[] lines = { "ABCDEFGHIJ", "ABCDE", "ABCDEFG" };
        TextRuntime sut = CreateText(string.Join("\n", lines), fontScale, compensation, alignment);
        Text contained = (Text)sut.RenderableComponent!;
        float scale = fontScale * compensation;
        float lineHeight = contained.LineHeightInPixels * contained.LineHeightMultiplier * scale;
        float absoluteLeft = contained.GetAbsoluteLeft();
        float drawnTop = contained.UpdateIpsoForRenderingForTests().Y;
        int startOfLine = 0;

        for (int line = 0; line < lines.Length; line++)
        {
            float lineWidth = contained.MeasureString(lines[line]) * scale;
            float gap = contained.EffectiveWidth - lineWidth;
            float lineLeft = absoluteLeft + alignment switch
            {
                HorizontalAlignment.Center => gap / 2,
                HorizontalAlignment.Right => gap,
                _ => 0
            };
            int character = 3;
            float start = contained.MeasureString(lines[line].Substring(0, character)) * scale;
            float end = contained.MeasureString(lines[line].Substring(0, character + 1)) * scale;
            float y = drawnTop + line * lineHeight + lineHeight / 2;

            contained.GetCharacterIndexAtPosition(lineLeft + start + (end - start) * 0.25f, y)
                .ShouldBe(startOfLine + character, $"line {line}");
            startOfLine += lines[line].Length + 1;
        }
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(1f)]
    [InlineData(2f)]
    public void GetCharacterIndexAtPosition_LineHeightMultiplier_ShouldStepByScaledMultipliedLineHeight(float fontScale)
    {
        TextRuntime sut = CreateText("ABCDE\nABCDE\nABCDE", fontScale, 1f, HorizontalAlignment.Left);
        sut.LineHeightMultiplier = 1.5f;
        Text contained = (Text)sut.RenderableComponent!;
        float lineHeight = contained.LineHeightInPixels * 1.5f * fontScale;
        float drawnTop = contained.UpdateIpsoForRenderingForTests().Y;

        for (int line = 0; line < 3; line++)
        {
            int index = contained.GetCharacterIndexAtPosition(contained.GetAbsoluteLeft() + 0.5f, drawnTop + (line + 0.5f) * lineHeight);

            index.ShouldBe(line * "ABCDE\n".Length, $"line {line}");
        }
    }

    [Theory]
    [InlineData(VerticalAlignment.Center, 0.5f)]
    [InlineData(VerticalAlignment.Bottom, 0.5f)]
    [InlineData(VerticalAlignment.Center, 2f)]
    [InlineData(VerticalAlignment.Bottom, 2f)]
    public void GetCharacterIndexAtPosition_VerticalAlignment_ShouldReturnLineDrawnAtPoint(
        VerticalAlignment verticalAlignment, float fontScale)
    {
        TextRuntime sut = CreateText("ABCDE\nABCDE\nABCDE", fontScale, 1f, HorizontalAlignment.Left);
        sut.Height = 300;
        sut.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        sut.VerticalAlignment = verticalAlignment;
        Text contained = (Text)sut.RenderableComponent!;
        float lineHeight = contained.LineHeightInPixels * fontScale;
        float drawnTop = contained.UpdateIpsoForRenderingForTests().Y;

        for (int line = 0; line < 3; line++)
        {
            int index = contained.GetCharacterIndexAtPosition(contained.GetAbsoluteLeft() + 0.5f, drawnTop + (line + 0.5f) * lineHeight);

            index.ShouldBe(line * "ABCDE\n".Length, $"line {line}");
        }
    }

    // A text box and a plain text with the same font scale answer the same for every point.
    [Theory]
    [InlineData(0.5f)]
    [InlineData(1f)]
    [InlineData(2f)]
    public void GetCharacterIndexAtPosition_ShouldAgreeWithTextBoxCaretHitTest(float fontScale)
    {
        TextBox textBox = new();
        textBox.Width = 400;
        textBox.Height = 200;
        textBox.Visual.X = 20;
        textBox.Visual.Y = 30;
        TextRuntime textInstance = ((TextBoxBaseVisual)textBox.Visual).TextInstance;
        textInstance.FontScale = fontScale;
        textBox.Text = SingleLine;
        Text contained = (Text)textInstance.RenderableComponent!;
        float y = contained.GetAbsoluteTop() + 1;

        for (float x = contained.GetAbsoluteLeft(); x < contained.GetAbsoluteLeft() + contained.WrappedTextWidth + 20; x += 1.5f)
        {
            contained.GetCharacterIndexAtPosition(x, y).ShouldBe(textBox.GetCaretIndexAtPosition(x, y), $"x {x}");
        }
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(2f)]
    public void GetCharacterIndexAtPosition_MultiLine_ShouldAgreeWithTextBoxCaretHitTest(float fontScale)
    {
        TextBox textBox = new();
        textBox.TextWrapping = global::Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;
        textBox.Width = 400;
        textBox.Height = 300;
        textBox.Visual.X = 20;
        textBox.Visual.Y = 30;
        TextRuntime textInstance = ((TextBoxBaseVisual)textBox.Visual).TextInstance;
        textInstance.FontScale = fontScale;
        textBox.Text = "ABCDEFGHIJ\nABCDE\nABCDEFG";
        Text contained = (Text)textInstance.RenderableComponent!;
        float lineHeight = contained.LineHeightInPixels * fontScale;

        for (int line = 0; line < 3; line++)
        {
            float y = contained.GetAbsoluteTop() + (line + 0.5f) * lineHeight;
            for (float x = contained.GetAbsoluteLeft(); x < contained.GetAbsoluteLeft() + contained.WrappedTextWidth + 20; x += 3)
            {
                contained.GetCharacterIndexAtPosition(x, y).ShouldBe(textBox.GetCaretIndexAtPosition(x, y), $"line {line} x {x}");
            }
        }
    }

    // While oversampling pins a measurement font, layout (and the drawn box) is measured at the scale
    // without the compensation, so the hit test follows the layout width and agrees with a text box.
    [Fact]
    public void GetCharacterIndexAtPosition_WithMeasurementFont_ShouldFollowLayoutWidth()
    {
        TextBox textBox = new();
        textBox.Width = 400;
        textBox.Visual.X = 20;
        textBox.Visual.Y = 30;
        TextRuntime textInstance = ((TextBoxBaseVisual)textBox.Visual).TextInstance;
        textBox.Text = SingleLine;
        Text contained = (Text)textInstance.RenderableComponent!;
        contained.MeasurementFont = contained.BitmapFont;
        contained.OversampleCompensationScale = 0.5f;
        float y = contained.GetAbsoluteTop() + 1;

        contained.GetCharacterIndexAtPosition(contained.GetAbsoluteLeft() + contained.WrappedTextWidth - 1, y)
            .ShouldBeInRange(SingleLine.Length - 1, SingleLine.Length);
        for (float x = contained.GetAbsoluteLeft(); x < contained.GetAbsoluteLeft() + contained.WrappedTextWidth + 20; x += 1.5f)
        {
            contained.GetCharacterIndexAtPosition(x, y).ShouldBe(textBox.GetCaretIndexAtPosition(x, y), $"x {x}");
        }
    }

    static TextRuntime CreateText(string text, float fontScale, float compensation, HorizontalAlignment alignment)
    {
        TextRuntime textRuntime = new();
        textRuntime.X = 20;
        textRuntime.Y = 30;
        textRuntime.Width = 400;
        textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        textRuntime.HorizontalAlignment = alignment;
        textRuntime.FontScale = fontScale;
        textRuntime.Text = text;
        ((Text)textRuntime.RenderableComponent!).OversampleCompensationScale = compensation;
        return textRuntime;
    }
}
