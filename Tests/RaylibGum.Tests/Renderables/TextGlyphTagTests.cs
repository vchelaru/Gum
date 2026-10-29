using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Linq;
using Xunit;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

// [Glyph=...] BBCode tag (#6840): inserts the character for a code point when the text is parsed.
public class TextGlyphTagTests : BaseTestClass
{
    [Theory]
    [InlineData("A[Glyph=57344]B", "AB")]
    [InlineData("A[Glyph=0xE000]B", "AB")]
    [InlineData("A[Glyph=abc]B", "AB")]
    public void Text_WithGlyphTag_ReplacesTagWithCharacter(string input, string expected)
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = input;

        Text text = (Text)textRuntime.RenderableComponent!;
        text.RawText.ShouldBe(expected);
    }

    [Fact]
    public void Text_WithGlyphTagInsideColorTag_KeepsColorRunIndexes()
    {
        TextRuntime textRuntime = new();
        textRuntime.Text = "A[Color=Red]B[Glyph=0xE000]C[/Color]D";

        Text text = (Text)textRuntime.RenderableComponent!;
        text.RawText.ShouldBe("ABCD");

        InlineVariable color = text.InlineVariables.Single(v => v.VariableName == "Color");
        color.StartIndex.ShouldBe(1);
        color.CharacterCount.ShouldBe(3);
    }
}
