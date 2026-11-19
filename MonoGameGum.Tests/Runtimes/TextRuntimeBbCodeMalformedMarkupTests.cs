using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Markup that is not a complete, well-formed tag pair: what the TextRuntime displays. An open tag with no
// close, a close with no open, and anything that is not a known tag stay in the text as literal
// characters; a matched pair is removed wherever it sits. The SkiaGum and raylib classes of the same name
// run the same rows.
public class TextRuntimeBbCodeMalformedMarkupTests : BaseTestClass
{
    [Theory]
    [InlineData("a[/Color]b", "a[/Color]b")]
    [InlineData("[Color=Red]a", "[Color=Red]a")]
    [InlineData("[Color=Red]a[/IsBold]b[/Color]", "a[/IsBold]b")]
    [InlineData("[[Color=Red]a[/Color]", "[a")]
    [InlineData("a[Color=Red]b[/Color]]", "ab]")]
    [InlineData("[Color=Red][/Color]a", "a")]
    [InlineData("a[Color=Red]b[/Color", "a[Color=Red]b[/Color")]
    [InlineData("[ Color=Red]a[/Color]", "[ Color=Red]a[/Color]")]
    [InlineData("[Color =Red]a[/Color]", "[Color =Red]a[/Color]")]
    [InlineData("[Unknown=1]a[/Unknown]", "[Unknown=1]a[/Unknown]")]
    [InlineData("[]a[/]", "[]a[/]")]
    [InlineData("[=Red]a", "[=Red]a")]
    [InlineData("[Color]a[/Color]", "a")]
    [InlineData("[Color=Red]a[Color=Blue]b[/Color]c[/Color]", "abc")]
    [InlineData("[IsBold=true]a[Color=Red]b[/IsBold]c[/Color]", "abc")]
    [InlineData("[Color=Red]a[/Color][Color=Blue]b[/Color]", "ab")]
    [InlineData("[Color=Red]a[Color=Blue]b[/Color]", "[Color=Red]ab")]
    [InlineData("[Color=Red]a\nb[/Color]", "a\nb")]
    [InlineData("[Color=Red]a\r\nb[/Color]", "a\nb")]
    [InlineData("[Color=Red]a[/Color]\n", "a\n")]
    [InlineData("[Color=Light Blue]a[/Color]", "a")]
    [InlineData("a [ b ] c", "a [ b ] c")]
    public void Text_WithMarkup_DisplaysExpectedText(string markup, string expectedDisplayed)
    {
        TextRuntime textRuntime = new();

        textRuntime.Text = markup;

        ((Text)textRuntime.RenderableComponent!).RawText.ShouldBe(expectedDisplayed);
    }
}
