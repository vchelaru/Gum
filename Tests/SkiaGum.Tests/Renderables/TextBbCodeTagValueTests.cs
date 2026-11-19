using Shouldly;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// What a tag's value does to the run it wraps: a value that parses styles the run, and a value that does not
/// parse leaves the run as if the tag were absent (the base style, or the style of an enclosing tag of the same
/// name). The MonoGame (TextRuntimeBbCodeTagValueTests) and raylib classes of the same name run the same rows.
/// </summary>
public class TextBbCodeTagValueTests : BaseTestClass
{
    private static Text MakeText(string markup)
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 18;
        text.Width = 1000;
        text.Color = SKColors.Black;
        text.RawText = markup;
        return text;
    }

    // One entry per displayed character: the style that draws it.
    private static List<string> StylePerCharacter(Text text, bool includeColor)
    {
        List<string> result = new();
        foreach (Text.StyledTextRun run in text.GetStyledRuns())
        {
            string style = $"{run.Style.FontFamily}/{run.Style.FontSize}/{run.Style.FontWeight}/{run.Style.FontItalic}/{run.Style.HaloWidth}"
                + (includeColor ? $"/{run.Style.TextColor}" : "");
            for (int i = 0; i < run.Text.Length; i++)
            {
                result.Add(style);
            }
        }
        return result;
    }

    [Theory]
    [InlineData("[Color=NotAColor]a[/Color]")]
    [InlineData("[Color=]a[/Color]")]
    [InlineData("[Color=0xZZ]a[/Color]")]
    public void Text_ColorTagWithUnparseableValue_KeepsBaseColor(string markup)
    {
        Text text = MakeText(markup);

        text.GetStyledRuns().Single().Text.ShouldBe("a");
        text.GetStyledRuns().Single().Style.TextColor.ShouldBe(SKColors.Black);
    }

    [Theory]
    [InlineData("[Color=red]a[/Color]", 255, 255, 0, 0)]
    [InlineData("[Color=CornflowerBlue]a[/Color]", 255, 100, 149, 237)]
    [InlineData("[Color=0xFF00FF00]a[/Color]", 255, 0, 255, 0)]
    [InlineData("[Color=0x8000FF00]a[/Color]", 128, 0, 255, 0)]
    public void Text_ColorTagWithValidValue_AppliesColor(string markup, int a, int r, int g, int b)
    {
        Text text = MakeText(markup);

        text.GetStyledRuns().Single(run => run.Text == "a").Style.TextColor
            .ShouldBe(new SKColor((byte)r, (byte)g, (byte)b, (byte)a));
    }

    [Theory]
    [InlineData("[Red=300]a[/Red]")]
    [InlineData("[Green=-1]a[/Green]")]
    [InlineData("[Blue=1.5]a[/Blue]")]
    [InlineData("[FontScale=abc]a[/FontScale]")]
    [InlineData("[FontScale=]a[/FontScale]")]
    public void Text_ColorOrScaleTagWithUnparseableValue_KeepsBaseStyle(string markup)
    {
        Text text = MakeText(markup);

        StylePerCharacter(text, includeColor: true).ShouldBe(StylePerCharacter(MakeText("a"), includeColor: true));
    }

    [Theory]
    [InlineData("[FontScale=1.5]a[/FontScale]", 27f)]
    [InlineData("[FontScale=.5]a[/FontScale]", 9f)]
    public void Text_FontScaleTagWithDecimalValue_AppliesScale(string markup, float expectedSize)
    {
        Text text = MakeText(markup);

        text.GetStyledRuns().Single(run => run.Text == "a").Style.FontSize.ShouldBe(expectedSize);
    }

    [Theory]
    [InlineData("[FontSize=40]a[FontSize=abc]b[/FontSize]c[/FontSize]")]
    [InlineData("[FontSize=40]a[FontSize=]b[/FontSize]c[/FontSize]")]
    [InlineData("[Font=Courier New]a[Font=]b[/Font]c[/Font]")]
    [InlineData("[IsBold=true]a[IsBold=maybe]b[/IsBold]c[/IsBold]")]
    [InlineData("[IsItalic=true]a[IsItalic=]b[/IsItalic]c[/IsItalic]")]
    [InlineData("[OutlineThickness=2]a[OutlineThickness=x]b[/OutlineThickness]c[/OutlineThickness]")]
    public void Text_FontTagWithUnparseableValue_InsideValidTag_KeepsEnclosingFont(string markup)
    {
        Text text = MakeText(markup);

        List<string> styles = StylePerCharacter(text, includeColor: false);
        styles.Count.ShouldBe(3);
        styles[0].ShouldNotBe(StylePerCharacter(MakeText("a"), includeColor: false)[0],
            "Because the valid outer tag must change the style of 'a'");
        styles[1].ShouldBe(styles[0], "Because an unparseable inner value must not undo the enclosing tag");
        styles[2].ShouldBe(styles[0], "Because closing the unparseable inner tag must not close the enclosing tag too");
    }

    [Fact]
    public void Text_CustomTagAfterEmoji_ReceivesUtf16IndexWithTagsRemoved()
    {
        List<(int index, string block)> calls = new();
        Text.Customizations["TagValueIndex"] = (index, block) =>
        {
            calls.Add((index, block));
            return new LetterCustomization();
        };

        Text text = MakeText("\U0001F600[Color=Red]a[/Color][Custom=TagValueIndex]bc[/Custom]");
        text.GetStyledRuns();

        calls.Select(c => c.index).Distinct().OrderBy(i => i).ShouldBe(new[] { 3, 4 });
        calls.ShouldAllBe(c => c.block == "bc");
    }
}
