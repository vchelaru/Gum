using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// #7086: characters outside the BMP (surrogate pairs) and other multi-code-unit sequences must not
/// drop the text around them from <see cref="Text.WrappedText"/>.
/// </summary>
public class TextEmojiWrappedTextTests
{
    private static Text Make(string content, float width = 400)
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 20;
        text.Width = width;
        text.RawText = content;
        return text;
    }

    [Theory]
    [InlineData("\U0001F600 x")]
    [InlineData("x \U0001F600")]
    [InlineData("\U0001F600")]
    [InlineData("a\U0001F600\U0001F600 b")]
    [InlineData("\U0001F600\r\nx")]
    [InlineData("\u2605 x")]
    [InlineData("\U0001F1FA\U0001F1F8 x")]
    [InlineData("\U0001F468\u200D\U0001F469\u200D\U0001F467 x")]
    public void WrappedText_Emoji_KeepsEveryCharacter(string content)
    {
        Text text = Make(content);

        string.Concat(text.WrappedText).Replace("\n", "").ShouldBe(content.Replace("\r\n", ""));
    }

    [Fact]
    public void WrappedText_EmojiNearWidthLimit_KeepsEveryCharacter()
    {
        string content = "aaaa aaaa \U0001F600 bbbb \U0001F600 cccc";
        Text text = Make(content, width: 90);

        text.WrappedText.Count.ShouldBeGreaterThan(1);
        string.Concat(text.WrappedText).Replace("\n", "").Replace(" ", "").ShouldBe(content.Replace(" ", ""));
    }

    [Fact]
    public void GetCharacterIndexAtPosition_AfterEmoji_IndexesWrappedTextInUtf16Units()
    {
        Text text = Make("\U0001F600AB");
        string concatenated = string.Concat(text.WrappedText);
        float y = text.LineHeightInPixels * 0.5f;

        HashSet<char> found = new();
        for (float x = 0; x < 80; x += 1)
        {
            int index = text.GetCharacterIndexAtPosition(x, y);
            if (index < concatenated.Length)
            {
                char.IsLowSurrogate(concatenated[index]).ShouldBeFalse();
                found.Add(concatenated[index]);
            }
        }

        found.ShouldContain('A');
        found.ShouldContain('B');
    }
}
