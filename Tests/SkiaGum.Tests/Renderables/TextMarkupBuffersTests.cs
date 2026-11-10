using System.Text;
using Gum.Wireframe;
using MonoGameGum.TestsCommon;
using RenderingLibrary.Graphics;
using Shouldly;

namespace SkiaGum.Tests.Renderables;

/// <summary>The buffers a Text reuses while its markup changes (#7846): what they keep, how far they grow, and that the parser and splitter give the same result with them.</summary>
public class TextMarkupBuffersTests : BaseTestClass
{
    private static readonly HashSet<string> Tags = new(StringComparer.OrdinalIgnoreCase)
    {
        "color", "isbold", "fontsize", "custom", "red",
    };

    [Fact]
    public void Release_AfterATextBiggerThanTheBounds_GivesTheArraysBack()
    {
        TextMarkupBuffers buffers = new TextMarkupBuffers();
        Text.StyledTextRun run = new Text.StyledTextRun("a", new Topten.RichTextKit.Style());
        for (int i = 0; i < 500; i++)
        {
            buffers.Runs.Add(run);
            buffers.Tags.Add(new FoundTag());
        }
        buffers.Builder.Append(new string('x', 5000));
        buffers.SortScratch = new TagInfo[500];

        buffers.ReleaseBlockRuns();
        buffers.ReleaseParse();

        buffers.Runs.Count.ShouldBe(0);
        buffers.Runs.Capacity.ShouldBe(0);
        buffers.Builder.Capacity.ShouldBeLessThan(1024);
        buffers.SortScratch.ShouldBeNull();
        buffers.Tags.Count.ShouldBe(500);
    }

    [Fact]
    public void GetInlineVariable_PastThePool_ReturnsANewObjectEachTime()
    {
        TextMarkupBuffers buffers = new TextMarkupBuffers();

        buffers.GetInlineVariable(3).ShouldBeSameAs(buffers.GetInlineVariable(3));
        buffers.GetInlineVariable(1000).ShouldNotBeSameAs(buffers.GetInlineVariable(1000));
    }

    [Fact]
    public void Clone_AfterTheSourceIsParsedAgain_KeepsItsOwnRunsAndTags()
    {
        Text source = new Text();
        source.RawText = "[Color=Red]one[/Color] [IsBold=true]two[/IsBold]";
        source.FontSize = 24;
        string before = TextBbCodeParseOutputTests.Describe(source);
        Text clone = source.Clone();

        source.RawText = "[FontSize=40]other[/FontSize] [Color=Blue]text[/Color] [Color=Green]here[/Color]";

        TextBbCodeParseOutputTests.Describe(clone).ShouldBe(before);
        clone.ParsedBbCodeTags.ShouldNotBeSameAs(source.ParsedBbCodeTags);
    }

    // A reused list of runs allocates only the strings of the runs, not a list, its growth or the run objects.
    [Fact]
    public void FillStyledRuns_ReusedList_AllocatesOnlyTheRunStrings()
    {
        Text text = new Text();
        text.RawText = "[Color=Red]HP[/Color] [IsBold=true]12[/IsBold] [FontSize=24]of[/FontSize] [IsItalic=true]999[/IsItalic]";
        text.FontSize = 24;
        List<Text.StyledTextRun> runs = new List<Text.StyledTextRun>();

        AllocationResult result = AllocationMeasurer.MeasureMinimum(() => text.FillStyledRuns(runs), warmupIterations: 50, measuredIterations: 300);

        runs.Count.ShouldBe(7);
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(400);
    }

    [Fact]
    public void Parse_ReusingTheListsAcrossTexts_MatchesAFreshParse()
    {
        List<FoundTag> reusedTags = new List<FoundTag>();
        List<BbCodeParser.Tag> active = new List<BbCodeParser.Tag>();
        TagInfo[]? sortScratch = null;
        StringBuilder builder = new StringBuilder();

        foreach (string text in TextBbCodeParseOutputTests.Cases.Concat(TextBbCodeParseOutputTests.Cases.Reverse()))
        {
            List<FoundTag> fresh = BbCodeParser.Parse(text, Tags);
            BbCodeParser.Parse(text, Tags, reusedTags, active);

            reusedTags.Count.ShouldBe(fresh.Count, text);
            for (int i = 0; i < fresh.Count; i++)
            {
                reusedTags[i].Name.ShouldBe(fresh[i].Name, text);
                reusedTags[i].Open.ShouldBe(fresh[i].Open, text);
                reusedTags[i].Close.ShouldBe(fresh[i].Close, text);
            }
            BbCodeParser.RemoveTags(text, reusedTags, ref sortScratch, builder).ShouldBe(BbCodeParser.RemoveTags(text, fresh), text);
        }
    }

    private static string FlatTags(int count)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            builder.Append("[Color=Red]a[/Color]");
        }
        return builder.ToString();
    }

    private static string NestedCustomTags(int depth)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < depth; i++)
        {
            builder.Append("[Custom=A]");
        }
        builder.Append("ab");
        for (int i = 0; i < depth; i++)
        {
            builder.Append("[/Custom]");
        }
        return builder.ToString();
    }

    private static void ShouldRetainOnlyTheBounds(Text text)
    {
        TextMarkupBuffers buffers = text.RetainedMarkupBuffers!;
        text.RetainedInlineVariableCapacity.ShouldBeLessThanOrEqualTo(Math.Max(TextMarkupBuffers.MaxRetainedRuns, text.InlineVariableCount));
        // The tags of the text now set are live; only room beyond them is a leftover.
        buffers.RetainedTagCapacity.ShouldBeLessThanOrEqualTo(Math.Max(TextMarkupBuffers.MaxRetainedTags, buffers.RetainedTagCount));
        buffers.RetainedSubstringCapacity.ShouldBeLessThanOrEqualTo(TextMarkupBuffers.MaxRetainedSubstrings);
        buffers.Splitter.RetainedVariableCapacity.ShouldBeLessThanOrEqualTo(StyledSubstringSplitter.MaxRetainedVariables);
    }

    [Fact]
    public void RawText_ChangedFromTenThousandTagsToPlainText_RetainsOnlyTheBounds()
    {
        Text text = new Text();
        text.FontSize = 24;
        text.RawText = FlatTags(10000);
        text.GetStyledRuns();
        text.RetainedMarkupBuffers!.RetainedTagCount.ShouldBe(10000);

        text.RawText = "plain";
        text.GetStyledRuns();

        TextMarkupBuffers buffers = text.RetainedMarkupBuffers!;
        buffers.RetainedTagCount.ShouldBe(0);
        buffers.RetainedSubstringCount.ShouldBe(0);
        buffers.Splitter.RetainedVariableCount.ShouldBe(0);
        ShouldRetainOnlyTheBounds(text);
    }

    [Fact]
    public void RawText_ChangedFromTenThousandTagsToEmptyText_RetainsOnlyTheBounds()
    {
        Text text = new Text();
        text.FontSize = 24;
        text.RawText = FlatTags(10000);
        text.GetCachedTextBlock();

        text.RawText = "";

        TextMarkupBuffers buffers = text.RetainedMarkupBuffers!;
        buffers.RetainedTagCount.ShouldBe(0);
        buffers.RetainedSubstringCount.ShouldBe(0);
        ShouldRetainOnlyTheBounds(text);
    }

    [Fact]
    public void RawText_ChangedFromTenThousandTagsToThreeTags_RetainsOnlyTheBounds()
    {
        Text text = new Text();
        text.FontSize = 24;
        text.RawText = FlatTags(10000);
        text.GetCachedTextBlock();

        text.RawText = FlatTags(3);
        text.GetCachedTextBlock();

        TextMarkupBuffers buffers = text.RetainedMarkupBuffers!;
        buffers.RetainedTagCount.ShouldBe(3);
        buffers.RetainedSubstringCount.ShouldBe(0);
        ShouldRetainOnlyTheBounds(text);
    }

    [Fact]
    public void GetStyledRuns_TenThousandTagsParsed_RetainsOnlyTheBoundsOnceTheRunsAreBuilt()
    {
        Text text = new Text();
        text.FontSize = 24;
        text.RawText = FlatTags(10000);

        text.GetStyledRuns().Count.ShouldBe(10000);

        TextMarkupBuffers buffers = text.RetainedMarkupBuffers!;
        buffers.RetainedSubstringCount.ShouldBe(0);
        buffers.RetainedSubstringCapacity.ShouldBeLessThanOrEqualTo(TextMarkupBuffers.MaxRetainedSubstrings);
    }

    [Fact]
    public void RawText_ChangedFromDeepNestingToPlainText_ReleasesTheVariableLists()
    {
        Text text = new Text();
        text.FontSize = 24;
        text.RawText = NestedCustomTags(3000);
        text.GetStyledRuns();

        text.RawText = "plain";
        text.GetStyledRuns();

        TextMarkupBuffers buffers = text.RetainedMarkupBuffers!;
        buffers.Splitter.RetainedVariableCount.ShouldBe(0);
        ShouldRetainOnlyTheBounds(text);
    }

    [Fact]
    public void GetStyledRuns_AfterDeepNesting_ReleasesTheVariableListsWithoutChangingTheRuns()
    {
        Text text = new Text();
        text.FontSize = 24;
        text.RawText = NestedCustomTags(3000);

        List<Text.StyledTextRun> runs = text.GetStyledRuns();

        string.Concat(runs.Select(run => run.Text)).ShouldBe("ab");
        ShouldRetainOnlyTheBounds(text);
    }

    // A Text that keeps changing a few tags keeps the same lists, which stay big enough for it: nothing is released and rebuilt per change.
    [Fact]
    public void RawText_FourTagsChangedRepeatedly_KeepsTheSameBuffers()
    {
        Text text = new Text();
        text.FontSize = 24;
        text.RawText = "[Color=Red]HP[/Color] [IsBold=true]12[/IsBold] [FontSize=24]of[/FontSize] [IsItalic=true]999[/IsItalic]";
        text.GetStyledRuns();
        TextMarkupBuffers buffers = text.RetainedMarkupBuffers!;
        List<FoundTag> tags = buffers.Tags;
        List<StyledSubstring> substrings = buffers.Substrings;
        int tagCapacity = buffers.RetainedTagCapacity;
        int substringCapacity = buffers.RetainedSubstringCapacity;
        int variableCapacity = buffers.Splitter.RetainedVariableCapacity;

        for (int i = 0; i < 20; i++)
        {
            text.RawText = $"[Color=Red]HP[/Color] [IsBold=true]{i}[/IsBold] [FontSize=24]of[/FontSize] [IsItalic=true]999[/IsItalic]";
            text.GetStyledRuns();
        }

        text.RetainedMarkupBuffers.ShouldBeSameAs(buffers);
        buffers.Tags.ShouldBeSameAs(tags);
        buffers.Substrings.ShouldBeSameAs(substrings);
        buffers.RetainedTagCapacity.ShouldBe(tagCapacity);
        buffers.RetainedSubstringCapacity.ShouldBe(substringCapacity);
        buffers.Splitter.RetainedVariableCapacity.ShouldBe(variableCapacity);
        tagCapacity.ShouldBeGreaterThan(0);
        substringCapacity.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetStyledSubstringsReusing_AcrossLines_MatchesAFreshSplit()
    {
        InlineVariable Variable(string name, int start, int count, object value) =>
            new InlineVariable { VariableName = name, StartIndex = start, CharacterCount = count, Value = value };

        List<InlineVariable> overlapping = new()
        {
            Variable("Color", 0, 6, 1), Variable("Color", 3, 6, 2), Variable("IsBold", 5, 4, true),
            Variable("Custom", 8, 1, "a"), Variable("Custom", 8, 1, "b"), Variable("FontSize", 12, 3, 9f),
        };
        List<InlineVariable> none = new();
        StyledSubstringSplitter reusing = new StyledSubstringSplitter();
        List<StyledSubstring> reused = new List<StyledSubstring>();

        foreach ((string line, int start, List<InlineVariable> variables) in new[]
        {
            ("abcdefghijklmnopqrst", 0, overlapping), ("a", 0, overlapping), ("", 0, overlapping),
            ("plain line", 0, none), ("fghijklmnopqrst", 5, overlapping), ("abcdefghijklmnopqrst", 0, overlapping),
        })
        {
            List<StyledSubstring> fresh = new StyledSubstringSplitter().GetStyledSubstrings(start, line, variables);
            reusing.GetStyledSubstringsReusing(start, line, variables, reused);

            reused.Select(run => run.Substring).ShouldBe(fresh.Select(run => run.Substring), line);
            for (int i = 0; i < fresh.Count; i++)
            {
                reused[i].Variables.ShouldBe(fresh[i].Variables, line);
            }
        }
    }
}
