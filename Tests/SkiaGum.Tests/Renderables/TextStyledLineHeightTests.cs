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

    private static float Within(float expected) => 0.035f * expected;

    private static void ShouldMatch(Text text, float h, params float[] expectedLineAdvances)
    {
        TextBlock block = text.GetTextBlock();
        block.Lines.Count.ShouldBe(expectedLineAdvances.Length);

        float expectedTop = 0;
        for (int i = 0; i < expectedLineAdvances.Length; i++)
        {
            block.Lines[i].YCoord.ShouldBe(expectedTop * h, Within(expectedTop * h) + 0.01f, $"top of line {i}");
            expectedTop += expectedLineAdvances[i];
        }

        text.WrappedTextHeight.ShouldBe(expectedTop * h, Within(expectedTop * h), "the reported height is the drawn extent");
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

        float plain = baseScale * multiplier;
        ShouldMatch(text, h, plain, System.Math.Max(plain, runScale), plain);
    }

    [Theory]
    [MemberData(nameof(BaseRunMultiplier))]
    public void SizeRunAsLastLine_HeightIncludesTheRunLinesOwnAdvance(float baseScale, float runScale, float multiplier)
    {
        float h = PlainLineHeight();
        Text text = Build($"a\n[FontScale={Scale(runScale)}]b[/FontScale]", baseScale, multiplier);

        float plain = baseScale * multiplier;
        ShouldMatch(text, h, plain, System.Math.Max(plain, runScale));
    }

    [Fact]
    public void ConsecutiveSizeRunLines_HeightIsTheSumOfEachLinesOwnMax()
    {
        float h = PlainLineHeight();
        Text text = Build("[FontScale=3]a[/FontScale]\n[FontScale=1.5]b[/FontScale]\nc", 2, 1.5f);

        ShouldMatch(text, h, 3, 3, 3);
    }

    [Fact]
    public void SizeRunLargerThanMultipliedBase_IsNotMultiplied()
    {
        float h = PlainLineHeight();
        // The #6459 case: base 2, run 3, multiplier 1.5, two lines. Drawn 3h + 3h = 6h.
        Text text = Build("[FontScale=3]a[/FontScale]\nb", 2, 1.5f);

        ShouldMatch(text, h, 3, 3);
    }

    [Theory]
    [InlineData(0.5f, 1f)]
    [InlineData(1f, 1.5f)]
    [InlineData(2f, 0.8f)]
    public void PlainText_HeightIsLinesTimesScaledBaseTimesMultiplier(float baseScale, float multiplier)
    {
        float h = PlainLineHeight();
        Text text = Build("a\nb\nc", baseScale, multiplier);

        float plain = baseScale * multiplier;
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

        float plain = baseScale * multiplier;
        ShouldMatch(text, h, plain, plain, plain);
    }
}
