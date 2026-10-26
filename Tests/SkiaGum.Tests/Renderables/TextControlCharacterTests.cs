using RenderingLibrary.Graphics;
using Shouldly;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// #6835 follow-up: MonoGame draws a control character as the space glyph. A tab is not compared:
/// SkiaGum keeps it as one character with RichTextKit tab stops, so indexes into RawText stay aligned.
/// </summary>
public class TextControlCharacterTests
{
    private static (float Width, int Lines) Measure(string content)
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 20;
        text.RawText = content;
        return (text.WrappedTextWidth, text.WrappedText.Count);
    }

    [Theory]
    [InlineData("a\rb", "a b")]
    [InlineData("a\u0001b", "a b")]
    public void Measure_ControlCharacter_MatchesTheBlankMonoGameUses(string withControl, string blank)
    {
        (float Width, int Lines) actual = Measure(withControl);
        (float Width, int Lines) expected = Measure(blank);

        actual.Lines.ShouldBe(expected.Lines);
        actual.Width.ShouldBe(expected.Width, 0.5f);
    }
}
