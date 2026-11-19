using Gum.Forms.Controls;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// TextBox holding text that looks like BBCode. A TextBox edits characters, so its Text round-trips exactly and
/// its caret moves over every typed character. Linked into SkiaGum.Tests so both runtimes run the same cases.
/// </summary>
// Red until #8329: every test here except the "[b]" case fails on MonoGame, and the inner-text tests fail on Skia.
public class TextBoxBbCodeTests : BaseTestClass
{
    private static IFormsText CoreText(TextBox textBox) =>
        (IFormsText)textBox.Visual.GetGraphicalUiElementByName("TextInstance")!.RenderableComponent;

    [Theory]
    [InlineData("a[Color=Red]b[/Color]c")]
    [InlineData("[FontScale=2]big[/FontScale]")]
    [InlineData("[Glyph=0x42]")]
    [InlineData("x[color=red]y[/COLOR]")]
    public void Text_SetToTaggedText_RoundTripsExactly(string markup)
    {
        TextBox textBox = new();

        textBox.Text = markup;

        textBox.Text.ShouldBe(markup);
    }

    [Theory]
    [InlineData("[b]")]
    [InlineData("[Color=Red]x[/Color]")]
    public void HandleCharEntered_TypingTagText_KeepsEveryCharacter(string typed)
    {
        TextBox textBox = new();
        textBox.IsFocused = true;

        foreach (char c in typed)
        {
            textBox.HandleCharEntered(c);
        }

        textBox.Text.ShouldBe(typed);
        textBox.CaretIndex.ShouldBe(typed.Length);
    }

    [Theory]
    [InlineData("a[Color=Red]b[/Color]c")]
    [InlineData("[FontScale=2]big[/FontScale]")]
    public void Text_SetToTaggedText_InnerTextMatchesOnEveryRuntime(string markup)
    {
        TextBox textBox = new();

        textBox.Text = markup;

        // Whatever the inner text displays, the caret indexes into it, so it must hold every character of Text.
        string.Concat(CoreText(textBox).WrappedText).ShouldBe(markup);
    }

    [Fact]
    public void Text_MultiLineTaggedText_InnerTextHoldsEveryLine()
    {
        TextBox textBox = new();
        textBox.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        textBox.AcceptsReturn = true;

        textBox.Text = "a[Color=Red]b\nc[/Color]d";

        textBox.Text.ShouldBe("a[Color=Red]b\nc[/Color]d");
        IFormsText text = CoreText(textBox);
        text.WrappedText.Count.ShouldBe(2);
        string.Concat(text.WrappedText).ShouldBe("a[Color=Red]b\nc[/Color]d");
    }
}
