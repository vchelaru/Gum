using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class BBCodeParserTests : BaseTestClass
{
    [Fact]
    public void KnownTags_ShouldIncludeState()
    {
        BbCodeParser.KnownTags.Contains("state").ShouldBeTrue();
    }

    [Fact]
    public void Parse_ShouldParseOverlappingColors()
    {
        HashSet<string> tags = new() { "Color" };
        List<FoundTag> parsedTags = BbCodeParser.Parse(
            "[Color=Blue]1[Color=Red]2[/Color]3[/Color]", 
            tags);

        parsedTags.Count.ShouldBe(2);
        parsedTags[0].Open.StartStrippedIndex.ShouldBe(0);
        parsedTags[0].Close.StartStrippedIndex.ShouldBe(3);

        parsedTags[1].Open.StartStrippedIndex.ShouldBe(1);
        parsedTags[1].Close.StartStrippedIndex.ShouldBe(2);
    }

    [Theory]
    [InlineData("A[Glyph=57344]B", "A\uE000B")]
    [InlineData("A[Glyph=0xE000]B", "A\uE000B")]
    [InlineData("A[Glyph=0Xe000]B", "A\uE000B")]
    [InlineData("A[glyph=0xE000]B", "A\uE000B")]
    [InlineData("A[GLYPH=57344]B", "A\uE000B")]
    [InlineData("[Glyph=65][Glyph=0x42]", "AB")]
    [InlineData("A[Glyph=0x1F600]B", "A\U0001F600B")]
    [InlineData("A[Glyph=128512]B", "A\U0001F600B")]
    [InlineData("A[Glyph=0x10FFFF]B", "A\U0010FFFFB")]
    public void ReplaceGlyphTags_ShouldInsertCharacterForCodePoint(string text, string expected)
    {
        BbCodeParser.ReplaceGlyphTags(text).ShouldBe(expected);
    }

    [Theory]
    [InlineData("A[Glyph=abc]B")]
    [InlineData("A[Glyph=0xZZ]B")]
    [InlineData("A[Glyph=]B")]
    [InlineData("A[Glyph]B")]
    [InlineData("A[Glyph=0x]B")]
    [InlineData("A[Glyph=-5]B")]
    [InlineData("A[Glyph=0]B")]
    [InlineData("A[Glyph=0x110000]B")]
    [InlineData("A[Glyph=99999999999999]B")]
    [InlineData("A[Glyph=0xD800]B")]
    [InlineData("A[Glyph=55357]B")]
    [InlineData("A[Glyph=0xDFFF]B")]
    public void ReplaceGlyphTags_ShouldRemoveTag_WhenValueIsInvalid(string text)
    {
        BbCodeParser.ReplaceGlyphTags(text).ShouldBe("AB");
    }

    [Theory]
    [InlineData("no tags")]
    [InlineData("[Color=Red]red[/Color]")]
    [InlineData("[Glyph=65")]
    [InlineData("[Glyph=65 and more text")]
    [InlineData("[Glyphs=65]")]
    [InlineData("")]
    public void ReplaceGlyphTags_ShouldLeaveTextUnchanged_WhenThereIsNoCompleteGlyphTag(string text)
    {
        BbCodeParser.ReplaceGlyphTags(text).ShouldBe(text);
    }

    [Fact]
    public void ReplaceGlyphTags_ShouldKeepOtherTagIndexesCorrect()
    {
        string replaced = BbCodeParser.ReplaceGlyphTags("[Color=Red]A[Glyph=0xE000]B[/Color]C");

        List<FoundTag> parsedTags = BbCodeParser.Parse(replaced, BbCodeParser.KnownTags);

        BbCodeParser.RemoveTags(replaced, parsedTags).ShouldBe("A\uE000BC");
        parsedTags.Count.ShouldBe(1);
        parsedTags[0].Open.StartStrippedIndex.ShouldBe(0);
        parsedTags[0].Close.StartStrippedIndex.ShouldBe(3);
    }

    [Theory]
    [InlineData("[Glyph]x[/Glyph]", "x")]
    [InlineData("[Glyph=abc]x[/Glyph]", "x")]
    [InlineData("[Glyph=65]x[/Glyph]", "Ax")]
    [InlineData("x[/Glyph]", "x[/Glyph]")]
    public void ReplaceGlyphTags_ShouldRemoveCloseTagOnlyAfterAnOpenTag(string text, string expected)
    {
        BbCodeParser.ReplaceGlyphTags(text).ShouldBe(expected);
    }

    [Fact]
    public void RemoveTags_ShouldLeaveOrphanCloseTagAsText()
    {
        string text = "x[/Red]";

        List<FoundTag> parsedTags = BbCodeParser.Parse(text, BbCodeParser.KnownTags);

        BbCodeParser.RemoveTags(text, parsedTags).ShouldBe("x[/Red]");
    }
}
