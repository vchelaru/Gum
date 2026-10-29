using Gum.Forms.Controls;
using Gum.Forms.Input;
using Shouldly;
using SkiaGum.Tests.Forms.Linked;

namespace SkiaGum.Tests.Forms;

/// <summary>
/// An emoji (a surrogate pair) is one character for the caret and deletes on Skia too (#7091).
/// </summary>
public class TextBoxSurrogatePairSkiaTests : SkiaFormsTestBase
{
    const string Emoji = "\U0001F600";

    static TextBox CreateTextBox(string text)
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.Width = 300;
        textBox.Height = 200;
        textBox.Visual.X = 100;
        textBox.Visual.Y = 80;
        textBox.Text = text;
        textBox.IsFocused = true;
        return textBox;
    }

    [Fact]
    public void Backspace_AfterEmoji_ShouldRemoveWholePair()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        textBox.CaretIndex = 3;

        textBox.HandleBackspace();

        textBox.Text.ShouldBe("ab");
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void Delete_BeforeEmoji_ShouldRemoveWholePair()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        textBox.CaretIndex = 1;

        textBox.HandleKeyDown(Keys.Delete, false, false, false);

        textBox.Text.ShouldBe("ab");
    }

    [Fact]
    public void Arrows_OverEmoji_ShouldStepOverWholePair()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");
        textBox.CaretIndex = 1;

        textBox.HandleKeyDown(Keys.Right, false, false, false);
        textBox.CaretIndex.ShouldBe(3);

        textBox.HandleKeyDown(Keys.Left, false, false, false);
        textBox.CaretIndex.ShouldBe(1);
    }

    [Fact]
    public void ClickToCaret_AcrossAnEmoji_ShouldNeverLandBetweenTheHalves()
    {
        TextBox textBox = CreateTextBox("a" + Emoji + "b");

        for (float x = 90; x < 400; x += 1)
        {
            textBox.GetCaretIndexAtPosition(x, 90).ShouldNotBe(2, $"x={x}");
        }
    }
}
