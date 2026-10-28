using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// #6991: text whose RawText is used verbatim as the layout text (no BBCode, or a '[' that is not a
/// tag) must count a CRLF as one character, as MonoGame and Raylib do.
/// </summary>
public class TextCrlfWrappedTextTests
{
    private static Text Make(string content)
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 20;
        text.Width = 400;
        text.RawText = content;
        return text;
    }

    [Theory]
    [InlineData("AB\r\nCD", "AB\n", "CD")]
    [InlineData("AB\r\n[x]CD", "AB\n", "[x]CD")]
    // Baseline (already passed before #6991): the BBCode path always normalized.
    [InlineData("AB\r\n[Color=Red]CD[/Color]", "AB\n", "CD")]
    public void WrappedText_Crlf_DropsTheCarriageReturn(string content, string first, string second)
    {
        Text text = Make(content);

        text.WrappedText.ShouldBe(new List<string> { first, second });
    }

    [Fact]
    public void GetCharacterIndexAtPosition_Crlf_MapsThroughWrappedText()
    {
        Text text = Make("AB\r\nCD");
        float y = text.LineHeightInPixels * 1.5f;

        List<char> found = new();
        foreach (float x in new float[] { 3, 12 })
        {
            int index = text.GetCharacterIndexAtPosition(x, y);
            foreach (string line in text.WrappedText)
            {
                if (line.Length > index)
                {
                    found.Add(line[index]);
                    break;
                }
                index -= line.Length;
            }
        }

        found.ShouldBe(new List<char> { 'C', 'D' });
    }

    // MonoGame/Raylib only turn "\r\n" into "\n"; every other '\r' stays one character that draws as
    // a blank, so Skia keeps one character per '\r' (as a space) and never loses or adds one.
    [Theory]
    [InlineData("AB\rCD", "AB CD")]
    [InlineData("\r", " ")]
    [InlineData("\r\r\n", " \n")]
    [InlineData("A\n\rB", "A\n B")]
    public void WrappedText_OtherCarriageReturns_KeepOneCharacterEach(string content, string expected)
    {
        Text text = Make(content);

        string.Concat(text.WrappedText).ShouldBe(expected);
    }

    [Fact]
    public void GetVisibleWrappedText_Crlf_RevealsLettersInOrder()
    {
        Text text = Make("AB\r\nCD");
        text.MaxLettersToShow = 4;

        string.Concat(text.GetVisibleWrappedText()).ShouldBe("AB\nC");
    }
}
