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
/// RichTextKit multiplies a line's ascent and descent but not its leading, so a multiplied line lands within a
/// couple of percent of base * multiplier * h rather than exactly on it; expectations use that relative tolerance.
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
    /// The known Skia residual, tracked in #6467: RichTextKit multiplies a line's ascent and descent but not its
    /// leading, so a line whose height comes from the multiplied base (plain text at a multiplier other than 1, or
    /// a size run smaller than the multiplied base) lands up to this fraction away from <c>base * mult * h</c>.
    /// Measured at 2%; applies only to such lines, so a wrong size-run formula still fails.
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
    public void PlainText_HeightIsLinesTimesScaledBaseTimesMultiplier(float baseScale, float multiplier)
    {
        float h = PlainLineHeight();
        Text text = Build("a\nb\nc", baseScale, multiplier);

        Line plain = Plain(baseScale, multiplier);
        ShouldMatch(text, h, plain, plain, plain);
    }

    [Theory]
    [InlineData(0.5f, 1f)]
    [InlineData(1f, 1.5f)]
    [InlineData(2f, 0.8f)]
    public void ColourOnlyRuns_AdvanceAndHeightLikePlainText(float baseScale, float multiplier)
    {
        float h = PlainLineHeight();
        Text text = Build("[Color=Red]a[/Color]\nb\n[Color=Blue]c[/Color]", baseScale, multiplier);

        Line plain = Plain(baseScale, multiplier);
        ShouldMatch(text, h, plain, plain, plain);
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

        ShouldMatch(text, h, true, new[] { Plain(1, 1), Plain(1, 1) });
    }

    [Fact]
    public void TrailingNewline_AfterASizeRunLine_AddsAnEmptyLineAtTheMultipliedBase()
    {
        float h = PlainLineHeight();
        Text text = Build("[FontScale=3]a[/FontScale]\n", 2, 1.5f);

        ShouldMatch(text, h, true, new[] { Run(2, 1.5f, 3), Plain(2, 1.5f) });
    }
}
