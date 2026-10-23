using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Text = RenderingLibrary.Graphics.Text;
using System;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Text hit testing (#6571): a click inside rotated text, or text under a rotated parent, resolves
/// to the character drawn there. The renderer turns text by its absolute rotation about the
/// renderable's top-left, so each test builds the screen point by turning a local point the same way.
/// </summary>
public class TextHitTestRotationTests : BaseTestClass
{
    // Turns a point in the text's own unrotated frame (origin at its top-left) to the screen,
    // the way the renderer does: counterclockwise in degrees, about the top-left.
    static (float x, float y) LocalToScreen(IRenderableIpso text, float localX, float localY)
    {
        double radians = text.GetAbsoluteRotation() * Math.PI / 180;
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);
        return (
            text.GetAbsoluteLeft() + localX * cos + localY * sin,
            text.GetAbsoluteTop() - localX * sin + localY * cos);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(30)]
    [InlineData(-135)]
    public void GetCharacterIndexAtPosition_RotatedText_ShouldReturnCharacterDrawnAtPoint(float rotation)
    {
        TextRuntime sut = new();
        sut.X = 100;
        sut.Y = 80;
        sut.Width = 200;
        sut.Text = "Line1\nLine2";
        sut.Rotation = rotation;
        Text contained = (Text)sut.RenderableComponent!;
        float lineHeight = contained.LineHeightInPixels * contained.LineHeightMultiplier;
        // Just past the start of the fourth character ('e') on the second line.
        float localX = contained.MeasureString("Lin") + 1;
        float localY = lineHeight * 1.5f;

        (float screenX, float screenY) = LocalToScreen(contained, localX, localY);

        sut.GetCharacterIndexAtPosition(screenX, screenY).ShouldBe("Line1\n".Length + 3);
    }

    [Fact]
    public void GetCharacterIndexAtPosition_UnderRotatedParent_ShouldReturnCharacterDrawnAtPoint()
    {
        ContainerRuntime parent = new();
        parent.X = 150;
        parent.Y = 120;
        parent.Rotation = 60;
        TextRuntime sut = new();
        sut.X = 10;
        sut.Y = 20;
        sut.Width = 200;
        sut.Text = "Line1\nLine2";
        sut.Rotation = 30;
        parent.AddChild(sut);
        Text contained = (Text)sut.RenderableComponent!;
        float lineHeight = contained.LineHeightInPixels * contained.LineHeightMultiplier;
        float localX = contained.MeasureString("Lin") + 1;
        float localY = lineHeight * 1.5f;

        (float screenX, float screenY) = LocalToScreen(contained, localX, localY);

        sut.GetCharacterIndexAtPosition(screenX, screenY).ShouldBe("Line1\n".Length + 3);
    }

    // A fractional line height picks the line under the point; truncating y before dividing put
    // a point just below the first line's bottom back on the first line.
    [Fact]
    public void GetCharacterIndexAtPosition_FractionalLineHeight_ShouldReturnLineUnderPoint()
    {
        TextRuntime sut = new();
        sut.Width = 200;
        sut.Text = "Line1\nLine2";
        Text contained = (Text)sut.RenderableComponent!;
        int baseLineHeight = contained.LineHeightInPixels;
        contained.LineHeightMultiplier = (baseLineHeight + 0.5f) / baseLineHeight;
        float lineHeight = baseLineHeight * contained.LineHeightMultiplier;
        float localX = contained.MeasureString("Lin") + 1;

        (float screenX, float screenY) = LocalToScreen(contained, localX, lineHeight + 0.2f);

        sut.GetCharacterIndexAtPosition(screenX, screenY).ShouldBe("Line1\n".Length + 3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(-45)]
    public void GetCaretIndexAtPosition_RotatedTextBox_ShouldReturnCharacterDrawnAtPoint(float rotation)
    {
        TextBox textBox = new();
        textBox.Visual.X = 100;
        textBox.Visual.Y = 80;
        textBox.Text = "abcdef";
        textBox.Visual.Rotation = rotation;
        TextBoxBaseVisual visual = (TextBoxBaseVisual)textBox.Visual;
        Text contained = (Text)visual.TextInstance.RenderableComponent!;
        // Just past the start of the fourth character ('d'), halfway down the line.
        float localX = contained.MeasureString("abc") + 1;
        float localY = contained.LineHeightInPixels / 2f;

        (float screenX, float screenY) = LocalToScreen(contained, localX, localY);

        textBox.GetCaretIndexAtPosition(screenX, screenY).ShouldBe(3);
    }
    // Up and Down keep the caret's column on the adjacent line whatever the TextBox's rotation (#6607).
    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(-45)]
    public void UpDownArrows_RotatedMultiLineTextBox_ShouldKeepColumnOnAdjacentLine(float rotation)
    {
        TextBox textBox = new();
        textBox.TextWrapping = global::Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;
        textBox.Width = 300;
        textBox.Height = 200;
        textBox.Visual.X = 100;
        textBox.Visual.Y = 80;
        textBox.Text = "abcdef\nghijkl\nmnopqr";
        textBox.Visual.Rotation = rotation;
        textBox.IsFocused = true;
        textBox.CaretIndex = "abcdef\nghi".Length;

        textBox.HandleKeyDown(global::Gum.Forms.Input.Keys.Up, false, false, false);
        textBox.CaretIndex.ShouldBe(3);

        textBox.HandleKeyDown(global::Gum.Forms.Input.Keys.Down, false, false, false);
        textBox.HandleKeyDown(global::Gum.Forms.Input.Keys.Down, false, false, false);
        textBox.CaretIndex.ShouldBe("abcdef\nghijkl\nmno".Length);
    }
}
