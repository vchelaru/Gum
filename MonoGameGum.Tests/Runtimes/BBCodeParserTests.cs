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

    [Fact]
    public void Parse_ShouldLeaveUnclosedOpenTagAsText_WhenFollowedByNestedTag()
    {
        string text = "[Color=Red]a[Color=Blue]b[/Color]";

        List<FoundTag> parsedTags = BbCodeParser.Parse(text, BbCodeParser.KnownTags);

        parsedTags.Count.ShouldBe(1);
        parsedTags[0].Open.Argument.ShouldBe("Blue");
        BbCodeParser.RemoveTags(text, parsedTags).ShouldBe("[Color=Red]ab");
        parsedTags[0].Open.StartStrippedIndex.ShouldBe(12);
        parsedTags[0].Close.StartStrippedIndex.ShouldBe(13);
    }

    // #7094: a closing tag pairs with its opener ignoring case, the way tag names are recognized.
    [Theory]
    [InlineData("[Color=Red]a[/color]b", "ab")]
    [InlineData("[Color=Red]a[/COLOR]b", "ab")]
    [InlineData("[link=x]a[/LINK]", "a")]
    [InlineData("[Color=Red]a[Link=x]b[/LINK]c[/color]", "abc")]
    [InlineData("[Color=Red]a[color=Blue]b[/Color]c[/COLOR]", "abc")]
    public void Parse_ShouldPairCloseTagIgnoringCase(string text, string expectedStripped)
    {
        List<FoundTag> parsedTags = BbCodeParser.Parse(text, BbCodeParser.KnownTags);

        BbCodeParser.RemoveTags(text, parsedTags).ShouldBe(expectedStripped);
    }

    [Fact]
    public void Parse_ShouldKeepOpeningTagNameForBothTags_WhenCloseCaseDiffers()
    {
        // ApplyFontVariables pushes on the open tag's Name and pops on the close tag's Name.
        List<FoundTag> parsedTags = BbCodeParser.Parse("[Color=Red]a[/color]", BbCodeParser.KnownTags);

        parsedTags.Count.ShouldBe(1);
        parsedTags[0].Name.ShouldBe("Color");
        parsedTags[0].Open.Name.ShouldBe("Color");
        parsedTags[0].Close.Name.ShouldBe("Color");
        parsedTags[0].Open.Argument.ShouldBe("Red");
        parsedTags[0].Open.StartStrippedIndex.ShouldBe(0);
        parsedTags[0].Close.StartStrippedIndex.ShouldBe(1);
    }

    // Guards the #7094 change: when every tag in a string is written in one case, pairing must be exactly
    // what a case-sensitive nearest-opener match gives. The oracle below is that matching, written against
    // the generated token list rather than by scanning the text.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Parse_ShouldMatchCaseSensitiveOracle_ForStringsWithOneCaseStyle(int caseStyle)
    {
        string[] names = { "Color", "Link", "Red", "FontScale" };
        Random random = new Random(7094 + caseStyle);

        for (int iteration = 0; iteration < 800; iteration++)
        {
            StringBuilder text = new StringBuilder();
            // (isOpening, name, startIndex, count)
            List<(bool IsOpening, string Name, int Start, int Count)> tokens = new();
            int tokenCount = random.Next(1, 9);
            for (int t = 0; t < tokenCount; t++)
            {
                if (random.Next(3) == 0)
                {
                    text.Append((char)('a' + random.Next(26)));
                    continue;
                }

                string name = names[random.Next(names.Length)];
                name = caseStyle == 0 ? name.ToLowerInvariant() : caseStyle == 1 ? name.ToUpperInvariant() : name;
                bool isOpening = random.Next(2) == 0;
                string tag = isOpening ? $"[{name}=1]" : $"[/{name}]";
                tokens.Add((isOpening, name, text.Length, tag.Length));
                text.Append(tag);
            }

            string s = text.ToString();
            List<FoundTag> parsed = BbCodeParser.Parse(s, BbCodeParser.KnownTags);

            // Oracle: nearest earlier unmatched opener with the identical name.
            List<(string Name, int Open, int OpenCount, int Close, int CloseCount)> expected = new();
            List<(string Name, int Start, int Count)> stack = new();
            foreach (var token in tokens)
            {
                if (token.IsOpening)
                {
                    stack.Add((token.Name, token.Start, token.Count));
                }
                else
                {
                    int found = stack.FindLastIndex(item => item.Name == token.Name);
                    if (found >= 0)
                    {
                        expected.Add((token.Name, stack[found].Start, stack[found].Count, token.Start, token.Count));
                        stack.RemoveAt(found);
                    }
                }
            }

            parsed.Count.ShouldBe(expected.Count, s);
            var actual = parsed
                .Select(item => (item.Name, item.Open.StartIndex, item.Open.Count, item.Close.StartIndex, item.Close.Count))
                .OrderBy(item => item.Item2).ThenBy(item => item.Item4).ToList();
            var sortedExpected = expected.OrderBy(item => item.Item2).ThenBy(item => item.Item4).ToList();
            for (int i = 0; i < actual.Count; i++)
            {
                actual[i].ShouldBe(sortedExpected[i], s);
            }

            // Stripping removes exactly the matched tags' text.
            StringBuilder oracleStripped = new StringBuilder(s);
            foreach (var pair in expected.SelectMany(item => new[] { (item.Open, item.OpenCount), (item.Close, item.CloseCount) })
                .OrderByDescending(item => item.Item1))
            {
                oracleStripped.Remove(pair.Item1, pair.Item2);
            }
            BbCodeParser.RemoveTags(s, parsed).ShouldBe(oracleStripped.ToString(), s);
        }
    }

    private static string RemoveTagsWithOrderBy(string text, List<FoundTag> tags)
    {
        var allTags = new List<TagInfo>(tags.Count * 2);
        for (int i = 0; i < tags.Count; i++)
        {
            allTags.Add(tags[i].Open);
            allTags.Add(tags[i].Close);
        }
        var sorted = allTags.OrderBy(item => item.StartStrippedIndex).ToArray();
        var sb = new StringBuilder(text);
        foreach (var tag in sorted) sb.Remove(tag.StartStrippedIndex, tag.Count);
        return sb.ToString();
    }

    private static readonly string[] SortTestTagNames = { "Color", "IsBold", "FontSize", "IsItalic" };
    private static readonly HashSet<string> SortTestAvailableTags = new(SortTestTagNames);

    private static string RandomBbCode(Random r)
    {
        var sb = new StringBuilder();
        var open = new Stack<string>();
        int pieces = r.Next(1, 12);
        for (int p = 0; p < pieces; p++)
        {
            int kind = r.Next(5);
            if (kind == 0) { string n = SortTestTagNames[r.Next(SortTestTagNames.Length)]; sb.Append('[').Append(n).Append('=').Append(r.Next(1, 40)).Append(']'); open.Push(n); }
            else if (kind == 1 && open.Count > 0) sb.Append("[/").Append(open.Pop()).Append(']');
            else if (kind == 2) sb.Append("[/").Append(SortTestTagNames[r.Next(SortTestTagNames.Length)]).Append(']');
            else sb.Append(new string((char)('a' + r.Next(26)), r.Next(0, 4)));
        }
        return sb.ToString();
    }

    [Fact]
    public void Parse_RandomMarkup_IsSortedLikeOrderByAndRemoveTagsMatchesOrderBy()
    {
        var r = new Random(7840);
        for (int c = 0; c < 500; c++)
        {
            string text = RandomBbCode(r);
            List<FoundTag> parsed = BbCodeParser.Parse(text, SortTestAvailableTags);
            parsed.Select(t => t.ToString()).ShouldBe(parsed.OrderBy(t => t.Open.StartIndex).Select(t => t.ToString()), text);
            BbCodeParser.RemoveTags(text, parsed).ShouldBe(RemoveTagsWithOrderBy(text, parsed), text);
        }
    }

    [Fact]
    public void RemoveTags_RandomTagListsWithEqualKeys_MatchesOrderBy()
    {
        var r = new Random(18);
        for (int c = 0; c < 500; c++)
        {
            int len = r.Next(10, 60);
            string text = new string(Enumerable.Range(0, len).Select(i => (char)('a' + r.Next(26))).ToArray());
            int n = r.Next(0, 8);
            var tags = new List<FoundTag>();
            int budget = len;
            for (int i = 0; i < n; i++)
            {
                TagInfo Make() { int start = r.Next(0, 4); int count = r.Next(0, 3); return new TagInfo { StartStrippedIndex = start, Count = count, Name = "T" }; }
                tags.Add(new FoundTag { Name = "T", Open = Make(), Close = Make() });
            }
            if (tags.Sum(t => t.Open.Count + t.Close.Count) + 4 > len) continue;
            BbCodeParser.RemoveTags(text, tags).ShouldBe(RemoveTagsWithOrderBy(text, tags));
        }
    }
}
