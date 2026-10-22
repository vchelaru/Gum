using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// #6459: a styled text's reported Height must equal the extent it draws, on every runtime. Each line advances
/// by <c>max(base * multiplier, run)</c> (a size run is absolute and never multiplied; a line with no size run
/// is the multiplied base), and the Height is the sum of those advances, last line included. MonoGame pins the
/// same numbers in TextRuntimeStyledLineHeightTests and Raylib in TextRunLineHeightMultiplierTests. h is one
/// plain line at scale 1 and multiplier 1, read from a control text so no font metric is hard-coded.
/// Text without a size run is exact (#6467): RichTextKit multiplies a line's ascent and descent but not its
/// leading, and the Skia text compensates for that. Text with a size run keeps the uncompensated line heights, so a
/// multiplied line there lands within a couple of percent of base * multiplier * h; tracked in #6475, and pinned
/// exactly by SizeRunText_LineTopsAreUnchanged so that it cannot get worse.
/// </summary>
public class TextStyledLineHeightTests
{
    private static Text Build(string markup, float baseScale, float multiplier)
    {
        Text text = new();
        text.FontName = "Arial";
        text.FontSize = 20;
        text.Width = 1000;
        text.FontScale = baseScale;
        text.LineHeightMultiplier = multiplier;
        text.RawText = markup;
        return text;
    }

    private static float PlainLineHeight()
    {
        TextBlock control = Build("a\nb", 1, 1).GetTextBlock();
        float h = control.Lines[1].YCoord - control.Lines[0].YCoord;
        h.ShouldBeGreaterThan(0);
        return h;
    }

    private static string Scale(float scale) => scale.ToString(CultureInfo.InvariantCulture);

    /// <summary>Float and pixel rounding only, in units of h per line: a line whose height comes from a size run
    /// at least as tall as the multiplied base, or from plain text at multiplier 1, must match exactly.</summary>
    private const float ExactTolerance = 0.01f;

    /// <summary>
    /// The known Skia residual of text that has a size run, tracked in #6475: RichTextKit multiplies a line's
    /// ascent and descent but not its leading, so a line whose height comes from the multiplied base lands up to
    /// this fraction away from <c>base * mult * h</c>. Applies only to such lines, so a wrong size-run formula
    /// still fails. Text without a size run is exact and does not use it.
    /// </summary>
    private const float LeadingResidualFraction = 0.025f;

    private readonly record struct Line(float Advance, float Tolerance);

    private static Line Plain(float baseScale, float multiplier)
    {
        float advance = baseScale * multiplier;
        return new Line(advance, multiplier == 1 ? ExactTolerance : advance * LeadingResidualFraction + ExactTolerance);
    }

    private static Line Run(float baseScale, float multiplier, float runScale)
    {
        float multipliedBase = baseScale * multiplier;
        return runScale >= multipliedBase
            ? new Line(runScale, ExactTolerance)
            : new Line(multipliedBase, multipliedBase * LeadingResidualFraction + ExactTolerance);
    }

    // RichTextKit drops the empty line after a trailing newline, so the text adds it to the reported height itself.
    private static void ShouldMatch(Text text, float h, params Line[] expectedLines) =>
        ShouldMatch(text, h, false, expectedLines);

    private static void ShouldMatch(Text text, float h, bool endsWithEmptyLine, Line[] expectedLines)
    {
        TextBlock block = text.GetTextBlock();
        block.Lines.Count.ShouldBe(expectedLines.Length - (endsWithEmptyLine ? 1 : 0));

        float expectedTop = 0;
        float topTolerance = 0;
        for (int i = 0; i < expectedLines.Length; i++)
        {
            if (i < block.Lines.Count)
            {
                block.Lines[i].YCoord.ShouldBe(expectedTop * h, topTolerance * h, $"top of line {i}");
            }
            expectedTop += expectedLines[i].Advance;
            topTolerance += expectedLines[i].Tolerance;
        }

        text.WrappedTextHeight.ShouldBe(expectedTop * h, topTolerance * h, "the reported height is the drawn extent");
    }

    /// <summary>Text without a size run: every line advances exactly <c>base * multiplier * h</c>, 0.005h at most
    /// off at each top and at the reported height.</summary>
    private static void ShouldMatchExactly(Text text, float h, float advance, int lineCount, bool endsWithEmptyLine = false)
    {
        TextBlock block = text.GetTextBlock();
        block.Lines.Count.ShouldBe(lineCount - (endsWithEmptyLine ? 1 : 0));
        for (int i = 0; i < block.Lines.Count; i++)
        {
            block.Lines[i].YCoord.ShouldBe(i * advance * h, 0.005f * h, $"top of line {i}");
        }

        text.WrappedTextHeight.ShouldBe(lineCount * advance * h, 0.005f * h, "the reported height is the drawn extent");
    }

    public static IEnumerable<object[]> BaseRunMultiplier()
    {
        foreach (float baseScale in new[] { 0.5f, 1f, 2f })
        {
            foreach (float runScale in new[] { 0.75f, 1.5f, 3f })
            {
                foreach (float multiplier in new[] { 1f, 1.5f, 0.8f })
                {
                    yield return new object[] { baseScale, runScale, multiplier };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(BaseRunMultiplier))]
    public void SizeRunBetweenPlainLines_AdvancesAndHeightFollowMaxOfScaledBaseAndRun(float baseScale, float runScale, float multiplier)
    {
        float h = PlainLineHeight();
        Text text = Build($"a\n[FontScale={Scale(runScale)}]b[/FontScale]\nc", baseScale, multiplier);

        ShouldMatch(text, h, Plain(baseScale, multiplier), Run(baseScale, multiplier, runScale), Plain(baseScale, multiplier));
    }

    [Theory]
    [MemberData(nameof(BaseRunMultiplier))]
    public void SizeRunAsLastLine_HeightIncludesTheRunLinesOwnAdvance(float baseScale, float runScale, float multiplier)
    {
        float h = PlainLineHeight();
        Text text = Build($"a\n[FontScale={Scale(runScale)}]b[/FontScale]", baseScale, multiplier);

        ShouldMatch(text, h, Plain(baseScale, multiplier), Run(baseScale, multiplier, runScale));
    }

    [Fact]
    public void ConsecutiveSizeRunLines_HeightIsTheSumOfEachLinesOwnMax()
    {
        float h = PlainLineHeight();
        Text text = Build("[FontScale=3]a[/FontScale]\n[FontScale=1.5]b[/FontScale]\nc", 2, 1.5f);

        ShouldMatch(text, h, Run(2, 1.5f, 3), Run(2, 1.5f, 1.5f), Plain(2, 1.5f));
    }

    // Text with a size run keeps the uncompensated line heights (#6475), so its line tops are whatever they were
    // before the leading compensation; pinned in units of h (read from the code before the change) so any change
    // to a size-run text, better or worse, is visible and has to be made on purpose.
    [Theory]
    [InlineData("Start [FontScale=0.75]The quick brown fox jumps over the lazy dog and again[/FontScale] end", 2f, 1.5f, 100f, "0.000,2.972,5.908,8.844,11.780,14.716,18.046,21.018")]
    [InlineData("Start [FontScale=0.75]The quick brown fox jumps over the lazy dog and again[/FontScale] end", 2f, 1.5f, 200f, "0.000,3.331,6.267,9.597")]
    [InlineData("Start [FontScale=0.75]The quick brown fox jumps over the lazy dog and again[/FontScale] end", 2f, 3f, 100f, "0.000,5.886,11.737,17.587,23.438,29.289,35.534,41.420")]
    [InlineData("Start [FontScale=0.75]The quick brown fox jumps over the lazy dog and again[/FontScale] end", 2f, 3f, 200f, "0.000,6.245,12.096,18.341")]
    [InlineData("Start [FontScale=0.75]The quick brown fox jumps over the lazy dog and again[/FontScale] end", 1f, 1f, 120f, "0.000,1.072,2.065,3.058,4.129")]
    [InlineData("[FontSize=10]long wrapped text[/FontSize] Wmw", 2f, 1.5f, 100f, "0.000,2.943,5.886,9.145,12.117")]
    [InlineData("[FontSize=10]long wrapped text[/FontSize] Wmw", 2f, 1.5f, 200f, "0.000,3.259,6.230")]
    [InlineData("[FontSize=10]long wrapped text[/FontSize] Wmw", 2f, 3f, 100f, "0.000,5.858,11.716,17.889,23.775")]
    [InlineData("[FontSize=10]long wrapped text[/FontSize] Wmw", 2f, 3f, 200f, "0.000,6.173,12.060")]
    [InlineData("[FontSize=10]long wrapped text[/FontSize] Wmw", 2f, 1f, 200f, "0.000,2.287,4.287")]
    [InlineData("a[FontScale=2]b[/FontScale]c", 2f, 1.5f, 100f, "0.000,2.972")]
    [InlineData("a[FontScale=2]b[/FontScale]c", 2f, 1.5f, 200f, "0.000,2.972")]
    [InlineData("a[FontScale=2]b[/FontScale]c", 2f, 3f, 100f, "0.000,5.886")]
    [InlineData("a[FontScale=2]b[/FontScale]c", 2f, 3f, 200f, "0.000,5.886")]
    [InlineData("abc[FontScale=0.5]d[/FontScale]", 2f, 1.5f, 100f, "0.000,3.402")]
    [InlineData("abc[FontScale=0.5]d[/FontScale]", 2f, 1.5f, 200f, "0.000,3.402")]
    [InlineData("abc[FontScale=0.5]d[/FontScale]", 2f, 3f, 100f, "0.000,6.317")]
    [InlineData("abc[FontScale=0.5]d[/FontScale]", 2f, 3f, 200f, "0.000,6.317")]
    [InlineData("The [FontScale=2]big[/FontScale] brown fox jumps over the lazy dog again and again", 2f, 1.5f, 100f, "0.000,2.972,5.943,8.915,11.886,14.858,17.829,20.801,23.772,26.744,29.716,32.687,35.659,38.630")]
    [InlineData("The [FontScale=2]big[/FontScale] brown fox jumps over the lazy dog again and again", 2f, 1.5f, 200f, "0.000,2.972,5.943,8.915,11.886,14.858,17.829")]
    [InlineData("The [FontScale=2]big[/FontScale] brown fox jumps over the lazy dog again and again", 2f, 3f, 100f, "0.000,5.886,11.772,17.659,23.545,29.431,35.317,41.203,47.090,52.976,58.862,64.748,70.634,76.521")]
    [InlineData("The [FontScale=2]big[/FontScale] brown fox jumps over the lazy dog again and again", 2f, 3f, 200f, "0.000,5.886,11.772,17.659,23.545,29.431,35.317")]
    [InlineData("[FontScale=3]Huge[/FontScale] line then normal text that wraps around the width", 2f, 1.5f, 100f, "0.000,3.000,6.287,9.259,12.230,15.202,18.173,21.145,24.117,27.088,30.060,33.031,36.003,38.974,41.946")]
    [InlineData("[FontScale=3]Huge[/FontScale] line then normal text that wraps around the width", 2f, 1.5f, 200f, "0.000,3.287,6.259,9.230,12.202,15.173,18.145")]
    [InlineData("[FontScale=3]Huge[/FontScale] line then normal text that wraps around the width", 2f, 3f, 100f, "0.000,5.915,12.117,18.003,23.889,29.775,35.661,41.548,47.434,53.320,59.206,65.092,70.979,76.865,82.751")]
    [InlineData("[FontScale=3]Huge[/FontScale] line then normal text that wraps around the width", 2f, 3f, 200f, "0.000,6.202,12.088,17.974,23.861,29.747,35.633")]
    [InlineData("small [FontScale=0.5]tiny words that wrap around a few lines here[/FontScale] after", 2f, 1.5f, 100f, "0.000,2.972,5.900,8.829,12.232,15.203")]
    [InlineData("small [FontScale=0.5]tiny words that wrap around a few lines here[/FontScale] after", 2f, 1.5f, 200f, "0.000,3.402,6.805")]
    [InlineData("small [FontScale=0.5]tiny words that wrap around a few lines here[/FontScale] after", 2f, 3f, 100f, "0.000,5.886,11.730,17.573,23.890,29.777")]
    [InlineData("small [FontScale=0.5]tiny words that wrap around a few lines here[/FontScale] after", 2f, 3f, 200f, "0.000,6.317,12.634")]
    [InlineData("one\n[FontScale=2]two[/FontScale]\nthree four five six seven eight", 2f, 1.5f, 100f, "0.000,2.972,5.943,8.915,11.886,14.858,17.829,20.801,23.772,26.744")]
    [InlineData("one\n[FontScale=2]two[/FontScale]\nthree four five six seven eight", 2f, 1.5f, 200f, "0.000,2.972,5.943,8.915,11.886,14.858,17.829")]
    [InlineData("one\n[FontScale=2]two[/FontScale]\nthree four five six seven eight", 2f, 3f, 100f, "0.000,5.886,11.772,17.659,23.545,29.431,35.317,41.203,47.090,52.976")]
    [InlineData("one\n[FontScale=2]two[/FontScale]\nthree four five six seven eight", 2f, 3f, 200f, "0.000,5.886,11.772,17.659,23.545,29.431,35.317")]
    [InlineData("[FontSize=30]Large[/FontSize] and [FontSize=10]small[/FontSize] mixed in wrapped text example", 2f, 1.5f, 100f, "0.000,3.000,6.287,9.259,12.518,15.489,18.461,21.432,24.404,27.375,30.347,33.318")]
    [InlineData("[FontSize=30]Large[/FontSize] and [FontSize=10]small[/FontSize] mixed in wrapped text example", 2f, 1.5f, 200f, "0.000,3.287,6.546,9.518,12.489,15.461,18.432")]
    [InlineData("[FontSize=30]Large[/FontSize] and [FontSize=10]small[/FontSize] mixed in wrapped text example", 2f, 3f, 100f, "0.000,5.915,12.117,18.003,24.176,30.062,35.949,41.835,47.721,53.607,59.493,65.380")]
    [InlineData("[FontSize=30]Large[/FontSize] and [FontSize=10]small[/FontSize] mixed in wrapped text example", 2f, 3f, 200f, "0.000,6.202,12.375,18.262,24.148,30.034,35.920")]
    [InlineData("[Color=Red]red[/Color] [FontScale=1.5]bigger red words[/FontScale] [Color=Blue]blue[/Color] words that go on and on", 2f, 1.5f, 100f, "0.000,2.972,5.929,8.886,12.001,14.973,17.944,20.916,23.888,26.859,29.831,32.802")]
    [InlineData("[Color=Red]red[/Color] [FontScale=1.5]bigger red words[/FontScale] [Color=Blue]blue[/Color] words that go on and on", 2f, 1.5f, 200f, "0.000,3.115,6.230,9.202,12.173,15.145")]
    [InlineData("[Color=Red]red[/Color] [FontScale=1.5]bigger red words[/FontScale] [Color=Blue]blue[/Color] words that go on and on", 2f, 3f, 100f, "0.000,5.886,11.758,17.630,23.660,29.546,35.432,41.319,47.205,53.091,58.977,64.863")]
    [InlineData("[Color=Red]red[/Color] [FontScale=1.5]bigger red words[/FontScale] [Color=Blue]blue[/Color] words that go on and on", 2f, 3f, 200f, "0.000,6.030,12.060,17.946,23.832,29.718")]
    [InlineData("ab[FontScale=1]c[/FontScale]d wraps when there is enough text to wrap here", 2f, 1.5f, 100f, "0.000,3.259,6.230,9.202,12.173,15.145,18.117,21.088,24.060,27.031,30.003,32.974,35.946")]
    [InlineData("ab[FontScale=1]c[/FontScale]d wraps when there is enough text to wrap here", 2f, 1.5f, 200f, "0.000,3.259,6.230,9.202,12.173,15.145")]
    [InlineData("ab[FontScale=1]c[/FontScale]d wraps when there is enough text to wrap here", 2f, 3f, 100f, "0.000,6.173,12.060,17.946,23.832,29.718,35.604,41.491,47.377,53.263,59.149,65.035,70.922")]
    [InlineData("ab[FontScale=1]c[/FontScale]d wraps when there is enough text to wrap here", 2f, 3f, 200f, "0.000,6.173,12.060,17.946,23.832,29.718")]
    [InlineData("[FontScale=0.75]small start[/FontScale] then base text that continues on for a while", 2f, 1.5f, 100f, "0.000,3.331,6.302,9.274,12.245,15.217,18.188,21.160,24.131,27.103,30.075")]
    [InlineData("[FontScale=0.75]small start[/FontScale] then base text that continues on for a while", 2f, 1.5f, 200f, "0.000,3.331,6.302,9.274,12.245,15.217,18.188")]
    [InlineData("[FontScale=0.75]small start[/FontScale] then base text that continues on for a while", 2f, 3f, 100f, "0.000,6.245,12.131,18.018,23.904,29.790,35.676,41.562,47.449,53.335,59.221")]
    [InlineData("[FontScale=0.75]small start[/FontScale] then base text that continues on for a while", 2f, 3f, 200f, "0.000,6.245,12.131,18.018,23.904,29.790,35.676")]
    public void SizeRunText_LineTopsAreUnchanged(string markup, float baseScale, float multiplier, float width, string expectedTopsAndHeightInH)
    {
        float h = PlainLineHeight();
        Text text = Build(markup, baseScale, multiplier);
        text.Width = width;

        float[] expected = expectedTopsAndHeightInH.Split(',').Select(value => float.Parse(value, CultureInfo.InvariantCulture)).ToArray();
        TextBlock block = text.GetTextBlock();
        block.Lines.Count.ShouldBe(expected.Length - 1);
        for (int i = 0; i < block.Lines.Count; i++)
        {
            block.Lines[i].YCoord.ShouldBe(expected[i] * h, 0.002f * h, $"top of line {i}");
        }

        text.WrappedTextHeight.ShouldBe(expected[expected.Length - 1] * h, 0.002f * h, "the reported height");
    }

    [Fact]
    public void TrailingNewline_OnMultipliedPlainText_AddsOneEmptyLineAtTheMultipliedBase()
    {
        float h = PlainLineHeight();
        Text text = Build("a\nb\n", 2, 1.5f);

        ShouldMatchExactly(text, h, 3, lineCount: 3, endsWithEmptyLine: true);
    }

    // A font-family run ([Font=...]) mixed with the base font on one line has the same problem as a size run:
    // the line's top and bottom come from different fonts, so compensating each run alone overshoots (#6475). Such
    // text keeps main's line heights, pinned in units of h from main like SizeRunText_LineTopsAreUnchanged.
    [Theory]
    [InlineData("a[Font=Courier New]b[/Font]\nc\nd", 1f, 1.5f, 1000f, "0.000,1.552,3.038,4.523")]
    [InlineData("Hello [Font=Courier New]monospaced words in the middle[/Font] and back to Arial text that wraps", 1f, 1.5f, 100f, "0.000,1.486,2.963,4.441,5.919,7.471,8.956,10.442,11.928")]
    [InlineData("Hello [Font=Courier New]monospaced words in the middle[/Font] and back to Arial text that wraps", 1f, 1.5f, 200f, "0.000,1.552,3.030,4.581,6.067")]
    [InlineData("Hello [Font=Courier New]monospaced words in the middle[/Font] and back to Arial text that wraps", 2f, 3f, 100f, "0.000,5.886,11.797,17.708,23.619,29.530,35.440,41.351,47.262,53.301,59.187,65.073,70.959,76.845,82.732,88.618,94.504,100.390")]
    [InlineData("Hello [Font=Courier New]monospaced words in the middle[/Font] and back to Arial text that wraps", 2f, 3f, 200f, "0.000,5.886,11.797,17.708,23.619,29.657,35.544,41.430,47.316")]
    [InlineData("[Font=Courier New]All courier text here that wraps around[/Font] then arial", 1f, 1.5f, 100f, "0.000,1.478,2.955,4.433,5.911,7.389,8.866,10.418,11.904")]
    [InlineData("[Font=Courier New]All courier text here that wraps around[/Font] then arial", 1f, 1.5f, 200f, "0.000,1.478,2.955,4.507")]
    [InlineData("[Font=Courier New]All courier text here that wraps around[/Font] then arial", 2f, 3f, 100f, "0.000,5.911,11.822,17.732,23.643,29.554,35.465,41.376,47.287,53.197,59.236,65.122,71.008")]
    [InlineData("[Font=Courier New]All courier text here that wraps around[/Font] then arial", 2f, 3f, 200f, "0.000,5.911,11.822,17.732,23.643,29.554,35.465,41.504,47.390")]
    [InlineData("x[Font=Times New Roman]y[/Font]z", 1f, 1.5f, 100f, "0.000,1.492")]
    [InlineData("x[Font=Times New Roman]y[/Font]z", 1f, 1.5f, 200f, "0.000,1.492")]
    [InlineData("x[Font=Times New Roman]y[/Font]z", 2f, 3f, 100f, "0.000,5.886")]
    [InlineData("x[Font=Times New Roman]y[/Font]z", 2f, 3f, 200f, "0.000,5.886")]
    [InlineData("one two [Font=Courier New]three[/Font] four five six seven eight nine ten", 1f, 1.5f, 100f, "0.000,1.486,3.038,4.523,6.009,7.495,8.981")]
    [InlineData("one two [Font=Courier New]three[/Font] four five six seven eight nine ten", 1f, 1.5f, 200f, "0.000,1.552,3.038,4.523")]
    [InlineData("one two [Font=Courier New]three[/Font] four five six seven eight nine ten", 2f, 3f, 100f, "0.000,5.886,11.772,17.683,23.722,29.608,35.494,41.380,47.267,53.153,59.039,64.925,70.811")]
    [InlineData("one two [Font=Courier New]three[/Font] four five six seven eight nine ten", 2f, 3f, 200f, "0.000,5.886,11.925,17.811,23.697,29.583,35.470")]
    [InlineData("[Font=Courier New]a[/Font]\n[Font=Times New Roman]b[/Font]\nc d e f g h i j k", 1f, 1.5f, 100f, "0.000,1.478,2.959,4.445,5.931")]
    [InlineData("[Font=Courier New]a[/Font]\n[Font=Times New Roman]b[/Font]\nc d e f g h i j k", 1f, 1.5f, 200f, "0.000,1.478,2.959,4.445")]
    [InlineData("[Font=Courier New]a[/Font]\n[Font=Times New Roman]b[/Font]\nc d e f g h i j k", 2f, 3f, 100f, "0.000,5.911,11.763,17.649,23.535,29.422")]
    [InlineData("[Font=Courier New]a[/Font]\n[Font=Times New Roman]b[/Font]\nc d e f g h i j k", 2f, 3f, 200f, "0.000,5.911,11.763,17.649,23.535")]
    [InlineData("plain text then [Color=Red]red [Font=Courier New]red courier[/Font][/Color] and more words to wrap", 1f, 1.5f, 100f, "0.000,1.486,2.972,4.449,6.001,7.487,8.973,10.458")]
    [InlineData("plain text then [Color=Red]red [Font=Courier New]red courier[/Font][/Color] and more words to wrap", 1f, 1.5f, 200f, "0.000,1.486,3.038,4.523")]
    [InlineData("plain text then [Color=Red]red [Font=Courier New]red courier[/Font][/Color] and more words to wrap", 2f, 3f, 100f, "0.000,5.886,11.772,17.659,23.545,29.456,35.366,41.405,47.291,53.177,59.064,64.950,70.836")]
    [InlineData("plain text then [Color=Red]red [Font=Courier New]red courier[/Font][/Color] and more words to wrap", 2f, 3f, 200f, "0.000,5.886,11.772,17.683,23.722,29.608,35.494,41.380")]
    [InlineData("[Font=Courier New]mono[/Font] [IsBold]bold[/IsBold] words that are many and wrap around the area", 1f, 1.5f, 100f, "0.000,1.552,3.038,4.523,6.009,7.495,8.981")]
    [InlineData("[Font=Courier New]mono[/Font] [IsBold]bold[/IsBold] words that are many and wrap around the area", 1f, 1.5f, 200f, "0.000,1.552,3.038,4.523")]
    [InlineData("[Font=Courier New]mono[/Font] [IsBold]bold[/IsBold] words that are many and wrap around the area", 2f, 3f, 100f, "0.000,6.039,11.925,17.811,23.697,29.583,35.470,41.356,47.242,53.128,59.014,64.901,70.787")]
    [InlineData("[Font=Courier New]mono[/Font] [IsBold]bold[/IsBold] words that are many and wrap around the area", 2f, 3f, 200f, "0.000,6.039,11.925,17.811,23.697,29.583,35.470")]
    public void FontRunText_LineTopsAreUnchanged(string markup, float baseScale, float multiplier, float width, string expectedTopsAndHeightInH)
    {
        float h = PlainLineHeight();
        Text text = Build(markup, baseScale, multiplier);
        text.Width = width;

        float[] expected = expectedTopsAndHeightInH.Split(',').Select(value => float.Parse(value, CultureInfo.InvariantCulture)).ToArray();
        TextBlock block = text.GetTextBlock();
        block.Lines.Count.ShouldBe(expected.Length - 1);
        for (int i = 0; i < block.Lines.Count; i++)
        {
            block.Lines[i].YCoord.ShouldBe(expected[i] * h, 0.002f * h, $"top of line {i}");
        }

        text.WrappedTextHeight.ShouldBe(expected[expected.Length - 1] * h, 0.002f * h, "the reported height");
    }

    [Fact]
    public void TextReassignedWithoutAFontRun_IsCompensatedAgain()
    {
        float h = PlainLineHeight();
        Text text = Build("a[Font=Courier New]b[/Font]\nc\nd", 1, 1.5f);
        text.GetTextBlock();

        text.RawText = "a\nb\nc";

        ShouldMatchExactly(text, h, 1.5f, lineCount: 3);
    }

    [Fact]
    public void SizeRunLargerThanMultipliedBase_IsNotMultiplied()
    {
        float h = PlainLineHeight();
        // The #6459 case: base 2, run 3, multiplier 1.5, two lines. Drawn 3h + 3h = 6h.
        Text text = Build("[FontScale=3]a[/FontScale]\nb", 2, 1.5f);

        ShouldMatch(text, h, Run(2, 1.5f, 3), Plain(2, 1.5f));
    }

    [Theory]
    [InlineData(0.5f, 1f)]
    [InlineData(1f, 1.5f)]
    [InlineData(2f, 0.8f)]
    [InlineData(1f, 0.1f)]
    [InlineData(1f, 0.25f)]
    [InlineData(1f, 0.5f)]
    [InlineData(1f, 2f)]
    [InlineData(1f, 3f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(0.5f, 2f)]
    [InlineData(2f, 0.5f)]
    [InlineData(2f, 2f)]
    public void PlainText_HeightIsLinesTimesScaledBaseTimesMultiplier(float baseScale, float multiplier)
    {
        float h = PlainLineHeight();
        Text text = Build("a\nb\nc", baseScale, multiplier);

        ShouldMatchExactly(text, h, baseScale * multiplier, lineCount: 3);
    }

    [Theory]
    [InlineData(0.5f, 1f)]
    [InlineData(1f, 1.5f)]
    [InlineData(2f, 0.8f)]
    public void ColourOnlyRuns_AdvanceAndHeightLikePlainText(float baseScale, float multiplier)
    {
        float h = PlainLineHeight();
        Text text = Build("[Color=Red]a[/Color]\nb\n[Color=Blue]c[/Color]", baseScale, multiplier);

        ShouldMatchExactly(text, h, baseScale * multiplier, lineCount: 3);
    }

    [Fact]
    public void TrailingNewline_AddsAnEmptyLastLineAtTheMultipliedBase()
    {
        float h = PlainLineHeight();
        // MonoGame and Raylib count the empty line after a trailing newline: a, the size run, then an empty line.
        Text text = Build("a\n[FontScale=3]b\n", 2, 1.5f);

        ShouldMatch(text, h, true, new[] { Plain(2, 1.5f), Run(2, 1.5f, 3), Plain(2, 1.5f) });
    }

    [Fact]
    public void TrailingNewline_OnPlainText_AddsOneEmptyLine()
    {
        float h = PlainLineHeight();
        Text text = Build("a\n", 1, 1);

        ShouldMatchExactly(text, h, 1, lineCount: 2, endsWithEmptyLine: true);
    }

    [Fact]
    public void TrailingNewline_AfterASizeRunLine_AddsAnEmptyLineAtTheMultipliedBase()
    {
        float h = PlainLineHeight();
        Text text = Build("[FontScale=3]a[/FontScale]\n", 2, 1.5f);

        ShouldMatch(text, h, true, new[] { Run(2, 1.5f, 3), Plain(2, 1.5f) });
    }

    [Theory]
    [InlineData("a\n", 2, 1f)]
    [InlineData("a\n", 2, 1.5f)]
    [InlineData("a\nb\n", 3, 1f)]
    [InlineData("a\nb\n", 3, 1.5f)]
    public void TrailingNewline_VerticalAlignmentCentersAndBottomsTheCountedEmptyLine(string markup, int lineCount, float multiplier)
    {
        float h = PlainLineHeight();
        const float boxHeight = 400;
        // MonoGame and Raylib place the block using every counted line, the empty last line included.
        float drawn = lineCount * multiplier * h;
        float tolerance = 0.005f * h;

        Text center = Build(markup, 1, multiplier);
        center.Height = boxHeight;
        center.VerticalAlignment = VerticalAlignment.Center;
        center.GetVerticalAlignmentOffset(center.GetTextBlock()).ShouldBe((boxHeight - drawn) / 2, tolerance);

        Text bottom = Build(markup, 1, multiplier);
        bottom.Height = boxHeight;
        bottom.VerticalAlignment = VerticalAlignment.Bottom;
        bottom.GetVerticalAlignmentOffset(bottom.GetTextBlock()).ShouldBe(boxHeight - drawn, tolerance);
    }
    [Fact]
    public void FontRegisteredAgainUnderTheSameFamily_DoesNotReuseTheOldFontsMetrics()
    {
        const string family = "LeadingCompensationReRegisteredFamily";
        FontMapper previousMapper = FontMapper.Default;
        FontMapper.Default = new SkiaGum.Content.Fonts.GumFontMapper();
        try
        {
            byte[] orbitron = System.IO.File.ReadAllBytes(System.IO.Path.Combine(System.AppContext.BaseDirectory, "Assets", "Fonts", "TestFont.ttf"));
            using SkiaSharp.SKTypeface arial = SkiaSharp.SKTypeface.FromFamilyName("Arial");
            using SkiaSharp.SKData arialData = SkiaSharp.SKData.Create(arial.OpenStream());
            byte[] arialBytes = arialData.ToArray();

            SkiaGum.Content.Fonts.GumFontMapper.RegisterFont(family, orbitron);
            AssertMultipliedLineIsExact(family);

            SkiaGum.Content.Fonts.GumFontMapper.RegisterFont(family, arialBytes);
            AssertMultipliedLineIsExact(family);
        }
        finally
        {
            FontMapper.Default = previousMapper;
        }

        static void AssertMultipliedLineIsExact(string fontName)
        {
            Text plain = Build("a\nb", 1, 1);
            plain.FontName = fontName;
            TextBlock control = plain.GetTextBlock();
            float h = control.Lines[1].YCoord - control.Lines[0].YCoord;

            Text multiplied = Build("a\nb", 1, 1.5f);
            multiplied.FontName = fontName;
            multiplied.GetTextBlock().Lines[1].YCoord.ShouldBe(1.5f * h, ExactTolerance * h);
        }
    }

    [Fact]
    public void RebuildingTheSameTextWithAMultiplier_ProbesTheFontOnce()
    {
        Text text = Build("[Color=Red]a[/Color] [Color=Blue]b[/Color]", 1, 1.5f);
        text.GetTextBlock();

        int probesBefore = Text.FontLineMetricsProbeCount;
        for (int i = 0; i < 10; i++)
        {
            text.RawText = "[Color=Red]a" + i + "[/Color] [Color=Blue]b[/Color]";
            text.GetTextBlock();
        }

        Text.FontLineMetricsProbeCount.ShouldBe(probesBefore);
    }
}
