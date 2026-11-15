using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Content.Fonts;
using SkiaSharp;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// The widths Skia's <c>Text</c> keeps for repeated measures (#7872): a character's advance, and the width of a
/// string up to 128 characters, are each measured once per font and then looked up. Each case compares the answer
/// to a fresh <c>TextBlock</c>, on the first ask and on the repeats, so a stale or mixed-up entry shows.
/// </summary>
public class TextAdvanceCacheTests
{
    private static float FreshWidth(Text text, string value)
    {
        TextBlock block = new TextBlock();
        block.AddText(value, new Style()
        {
            FontFamily = text.FontName,
            FontSize = text.FontSize * (float)Text.GlobalTextScale,
            TextColor = text.Color,
            FontItalic = text.IsItalic,
            FontWeight = (int)(400 * text.BoldWeight),
        });
        return block.MeasuredWidth;
    }

    private static readonly string Characters = "aAiWm fT.,\t0éש中";

    [Fact]
    public void GetCharacterAdvance_EveryCharacterAndFont_IsTheFreshWidthOfThatCharacter()
    {
        foreach (string name in new[] { "Arial", "Times New Roman" })
        foreach (float size in new[] { 12f, 33.5f })
        foreach (bool italic in new[] { false, true })
        {
            Text text = new Text { FontName = name, FontSize = size, IsItalic = italic };
            for (int pass = 0; pass < 2; pass++)
            {
                foreach (char character in Characters)
                {
                    ((IFormsText)text).GetCharacterAdvance(character).ShouldBe(FreshWidth(text, character.ToString()),
                        $"{name} {size} italic {italic} pass {pass} U+{(int)character:X4}");
                }
            }
        }
    }

    [Fact]
    public void GetCharacterAdvance_LoneSurrogateHalves_AreMeasuredLikeAnyOtherCharacter()
    {
        Text text = new Text();
        string emoji = "\U0001F600";

        for (int pass = 0; pass < 2; pass++)
        {
            ((IFormsText)text).GetCharacterAdvance(emoji[0]).ShouldBe(FreshWidth(text, emoji[0].ToString()));
            ((IFormsText)text).GetCharacterAdvance(emoji[1]).ShouldBe(FreshWidth(text, emoji[1].ToString()));
        }
    }

    [Fact]
    public void MeasureString_ShortAndLongStringsAskedTwice_AreTheFreshWidth()
    {
        Text text = new Text { FontSize = 20 };
        string atTheLimit = new string('m', 128);
        string pastTheLimit = new string('m', 129);

        foreach (string value in new[] { "hello", "AV To fi", atTheLimit, pastTheLimit })
        {
            for (int pass = 0; pass < 2; pass++)
            {
                text.MeasureString(value).ShouldBe(FreshWidth(text, value), $"{value.Length} chars, pass {pass}");
            }
        }
    }

    [Fact]
    public void MeasureString_FontChangedAfterTheStringWasKept_UsesTheNewFont()
    {
        Text text = new Text { FontSize = 12 };
        text.MeasureString("hello world").ShouldBe(FreshWidth(text, "hello world"));
        ((IFormsText)text).GetCharacterAdvance('e').ShouldBe(FreshWidth(text, "e"));

        text.FontSize = 40;
        text.IsItalic = true;
        text.BoldWeight = 1.5f;

        text.MeasureString("hello world").ShouldBe(FreshWidth(text, "hello world"));
        ((IFormsText)text).GetCharacterAdvance('e').ShouldBe(FreshWidth(text, "e"));
    }

    [Fact]
    public void MeasureString_MoreDistinctStringsThanAreKept_StaysExact()
    {
        Text text = new Text { FontSize = 14 };

        // Past the 512 widths kept, the entries start over; no answer may come from the wrong one.
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < 700; i++)
            {
                string value = "line " + i + new string('x', i % 7);
                text.MeasureString(value).ShouldBe(FreshWidth(text, value), $"{value} pass {pass}");
            }
        }
    }

    [Fact]
    public void MeasureString_FontRegisteredAgainUnderTheSameFamily_DoesNotReuseTheOldFontsWidths()
    {
        const string family = "AdvanceCacheReRegisteredFamily";
        FontMapper previousMapper = FontMapper.Default;
        FontMapper.Default = new GumFontMapper();
        try
        {
            byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "TestFont.ttf"));
            using SKTypeface arial = SKTypeface.FromFamilyName("Arial");
            using SKData arialData = SKData.Create(arial.OpenStream());
            byte[] arialBytes = arialData.ToArray();
            Text text = new Text { FontName = family, FontSize = 24 };

            GumFontMapper.RegisterFont(family, fixture);
            text.MeasureString("Wide text here").ShouldBe(FreshWidth(text, "Wide text here"));
            ((IFormsText)text).GetCharacterAdvance('W').ShouldBe(FreshWidth(text, "W"));
            float first = text.MeasureString("Wide text here");

            GumFontMapper.RegisterFont(family, arialBytes);
            text.MeasureString("Wide text here").ShouldBe(FreshWidth(text, "Wide text here"));
            ((IFormsText)text).GetCharacterAdvance('W').ShouldBe(FreshWidth(text, "W"));
            text.MeasureString("Wide text here").ShouldNotBe(first);
        }
        finally
        {
            FontMapper.Default = previousMapper;
        }
    }
}
