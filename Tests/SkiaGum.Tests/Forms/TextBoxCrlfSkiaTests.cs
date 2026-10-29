using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Shouldly;
using SkiaGum.Tests.Forms.Linked;

namespace SkiaGum.Tests.Forms;

/// <summary>
/// A "\r\n" in Text is one character for the caret and selection on Skia too (#6996).
/// </summary>
public class TextBoxCrlfSkiaTests : SkiaFormsTestBase
{
    static TextBox CreateTextBox(string text)
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;
        textBox.Width = 300;
        textBox.Height = 200;
        textBox.Visual.X = 100;
        textBox.Visual.Y = 80;
        textBox.Text = text;
        textBox.IsFocused = true;
        return textBox;
    }

    [Theory]
    [InlineData(4, 5)]
    [InlineData(6, 7)]
    [InlineData(10, 11)]
    public void CaretPosition_Crlf_ShouldMatchLf(int lfIndex, int crlfIndex)
    {
        TextBox lf = CreateTextBox("one\ntwo\nthree");
        TextBox crlf = CreateTextBox("one\r\ntwo\r\nthree");
        lf.CaretIndex = lfIndex;
        crlf.CaretIndex = crlfIndex + (lfIndex == 10 ? 1 : 0);

        GraphicalUiElement lfCaret = ((TextBoxBaseVisual)lf.Visual).CaretInstance;
        GraphicalUiElement crlfCaret = ((TextBoxBaseVisual)crlf.Visual).CaretInstance;
        crlfCaret.AbsoluteLeft.ShouldBe(lfCaret.AbsoluteLeft, tolerance: 0.01f);
        crlfCaret.AbsoluteTop.ShouldBe(lfCaret.AbsoluteTop, tolerance: 0.01f);
    }

    [Fact]
    public void Selection_Crlf_ShouldMatchLf()
    {
        TextBox lf = CreateTextBox("one\ntwo\nthree");
        TextBox crlf = CreateTextBox("one\r\ntwo\r\nthree");
        lf.SelectionStart = 1;
        lf.SelectionLength = 6;
        crlf.SelectionStart = 1;
        crlf.SelectionLength = 7;

        GraphicalUiElement[] expected = Selections(lf);
        GraphicalUiElement[] actual = Selections(crlf);
        actual.Length.ShouldBe(expected.Length);
        for (int i = 0; i < actual.Length; i++)
        {
            actual[i].AbsoluteLeft.ShouldBe(expected[i].AbsoluteLeft, tolerance: 0.01f);
            actual[i].AbsoluteWidth.ShouldBe(expected[i].AbsoluteWidth, tolerance: 0.01f);
        }
    }

    static GraphicalUiElement[] Selections(TextBox textBox) =>
        ((TextBoxBaseVisual)textBox.Visual).ClipContainer.Children
            .OfType<GraphicalUiElement>()
            .Where(child => child.Name == "SelectionInstance" && child.Visible)
            .ToArray();
}
