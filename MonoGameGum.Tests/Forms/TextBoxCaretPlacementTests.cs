using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The default TextBox visual's caret is centered on the line the renderer draws (#6624), read from
/// <see cref="Text.UpdateIpsoForRenderingForTests"/>.
/// </summary>
public class TextBoxCaretPlacementTests : BaseTestClass
{
    [Theory]
    [InlineData(VerticalAlignment.Top)]
    [InlineData(VerticalAlignment.Center)]
    [InlineData(VerticalAlignment.Bottom)]
    public void CaretCenter_MultiLine_ShouldMatchDrawnLineCenter(VerticalAlignment verticalAlignment)
    {
        TextBox textBox = CreateTextBox(verticalAlignment, multiLine: true);
        Text contained = GetCoreText(textBox);
        float drawnLineCenter = contained.UpdateIpsoForRenderingForTests().Y + contained.LineHeightInPixels / 2.0f;

        GetCaret(textBox).GetAbsoluteCenterY().ShouldBe(drawnLineCenter, tolerance: 0.01f);
    }

    // Single-line mode leaves the caret where the visual places it; the default visual centers it
    // on the box, which is within half a pixel of the drawn line whatever the alignment.
    [Theory]
    [InlineData(VerticalAlignment.Top)]
    [InlineData(VerticalAlignment.Center)]
    [InlineData(VerticalAlignment.Bottom)]
    public void CaretCenter_SingleLine_ShouldBeWithinHalfPixelOfDrawnLineCenter(VerticalAlignment verticalAlignment)
    {
        TextBox textBox = CreateTextBox(verticalAlignment, multiLine: false);
        Text contained = GetCoreText(textBox);
        float drawnLineCenter = contained.UpdateIpsoForRenderingForTests().Y + contained.LineHeightInPixels / 2.0f;

        GetCaret(textBox).GetAbsoluteCenterY().ShouldBe(drawnLineCenter, tolerance: 0.51f);
    }

    static TextBox CreateTextBox(VerticalAlignment verticalAlignment, bool multiLine)
    {
        TextBox textBox = new();
        if (multiLine)
        {
            textBox.TextWrapping = global::Gum.Forms.TextWrapping.Wrap;
            textBox.AcceptsReturn = true;
            textBox.Height = 200;
        }
        textBox.Width = 300;
        textBox.Visual.X = 100;
        textBox.Visual.Y = 80;
        ((TextBoxBaseVisual)textBox.Visual).TextInstance.VerticalAlignment = verticalAlignment;
        textBox.Text = "abc";
        textBox.IsFocused = true;
        textBox.CaretIndex = 1;
        return textBox;
    }

    static Text GetCoreText(TextBox textBox) =>
        (Text)((TextBoxBaseVisual)textBox.Visual).TextInstance.RenderableComponent!;

    static Gum.GueDeriving.SpriteRuntime GetCaret(TextBox textBox) =>
        ((TextBoxBaseVisual)textBox.Visual).CaretInstance;
}
