using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.Globalization;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// #6459: a styled text's reported Height must equal the extent it draws, on every runtime. Each line advances
/// by <c>max(base * multiplier, run)</c> (a size run is absolute and never multiplied; a line with no size run
/// is the multiplied base), and the Height is the sum of those advances, last line included. Raylib pins the
/// same numbers in TextRunLineHeightMultiplierTests and Skia in TextStyledLineHeightTests. h is one plain line
/// at scale 1 and multiplier 1, read from the control text so no font metric is hard-coded.
/// </summary>
public class TextRuntimeStyledLineHeightTests : BaseTestClass
{
    private static float PlainLineHeight() => ((Text)new TextRuntime().RenderableComponent!).LineHeightInPixels;

    private static Text Build(string markup, float baseScale, float multiplier)
    {
        TextRuntime runtime = new();
        runtime.FontScale = baseScale;
        runtime.LineHeightMultiplier = multiplier;
        runtime.Text = markup;
        return (Text)runtime.RenderableComponent!;
    }

    // The advance DrawWithInlineVariables takes past each line: ComputeStyledLineHeight for a styled line,
    // the multiplied base for a plain text with no inline variables.
    private static List<float> DrawnAdvances(Text text, float baseScale, float multiplier)
    {
        List<float> advances = new();
        int startOfLineIndex = 0;
        for (int i = 0; i < text.WrappedText.Count; i++)
        {
            string line = text.WrappedText[i];
            if (text.InlineVariables.Count == 0)
            {
                advances.Add(text.BitmapFont!.EffectiveLineHeight(baseScale, multiplier));
            }
            else
            {
                List<StyledSubstring> substrings = text.GetCachedStyledSubstrings(i, startOfLineIndex, line);
                advances.Add(text.ComputeStyledLineHeight(text.BitmapFont!, substrings, out _));
            }
            startOfLineIndex += line.Length;
        }
        return advances;
    }

    private static string Scale(float scale) => scale.ToString(CultureInfo.InvariantCulture);

    private static float Sum(IEnumerable<float> values)
    {
        float total = 0;
        foreach (float value in values) total += value;
        return total;
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

        float plain = h * baseScale * multiplier;
        float run = h * System.Math.Max(baseScale * multiplier, runScale);
        DrawnAdvances(text, baseScale, multiplier).ShouldBe(new[] { plain, run, plain }, tolerance: 0.01f);
        // The reported height is stored as whole pixels in base units, so allow rounding at scale 2.
        text.WrappedTextHeight.ShouldBe(2 * plain + run, tolerance: 1.01f);
    }

    [Theory]
    [MemberData(nameof(BaseRunMultiplier))]
    public void SizeRunAsLastLine_HeightIncludesTheRunLinesOwnAdvance(float baseScale, float runScale, float multiplier)
    {
        float h = PlainLineHeight();
        Text text = Build($"a\n[FontScale={Scale(runScale)}]b[/FontScale]", baseScale, multiplier);

        float plain = h * baseScale * multiplier;
        float run = h * System.Math.Max(baseScale * multiplier, runScale);
        text.WrappedTextHeight.ShouldBe(plain + run, tolerance: 1.01f);
    }

    [Fact]
    public void ConsecutiveSizeRunLines_HeightIsTheSumOfEachLinesOwnMax()
    {
        float h = PlainLineHeight();
        // Base 2, multiplier 1.5: the 3 run beats the multiplied base 3 only by tying, the 1.5 run loses to it.
        Text text = Build("[FontScale=3]a[/FontScale]\n[FontScale=1.5]b[/FontScale]\nc", 2, 1.5f);

        DrawnAdvances(text, 2, 1.5f).ShouldBe(new[] { 3 * h, 3 * h, 3 * h }, tolerance: 0.01f);
        text.WrappedTextHeight.ShouldBe(9 * h, tolerance: 1.01f);
    }

    [Fact]
    public void SizeRunLargerThanMultipliedBase_IsNotMultiplied()
    {
        float h = PlainLineHeight();
        // The #6459 case: base 2, run 3, multiplier 1.5, two lines. Drawn 3h + 3h = 6h; it used to report 7.5h.
        Text text = Build("[FontScale=3]a[/FontScale]\nb", 2, 1.5f);

        DrawnAdvances(text, 2, 1.5f).ShouldBe(new[] { 3 * h, 3 * h }, tolerance: 0.01f);
        text.WrappedTextHeight.ShouldBe(6 * h, tolerance: 1.01f);
    }

    [Theory]
    [InlineData(0.5f, 1f)]
    [InlineData(1f, 1.5f)]
    [InlineData(2f, 0.8f)]
    public void PlainText_HeightIsLinesTimesScaledBaseTimesMultiplier(float baseScale, float multiplier)
    {
        float h = PlainLineHeight();
        Text text = Build("a\nb\nc", baseScale, multiplier);

        text.WrappedTextHeight.ShouldBe(3 * h * baseScale * multiplier, tolerance: 1.01f);
    }

    [Theory]
    [InlineData(0.5f, 1f)]
    [InlineData(1f, 1.5f)]
    [InlineData(2f, 0.8f)]
    public void ColourOnlyRuns_AdvanceAndHeightLikePlainText(float baseScale, float multiplier)
    {
        float h = PlainLineHeight();
        Text text = Build("[Color=Red]a[/Color]\nb\n[Color=Blue]c[/Color]", baseScale, multiplier);

        float plain = h * baseScale * multiplier;
        DrawnAdvances(text, baseScale, multiplier).ShouldBe(new[] { plain, plain, plain }, tolerance: 0.01f);
        text.WrappedTextHeight.ShouldBe(3 * plain, tolerance: 1.01f);
    }

    [Fact]
    public void TrailingNewline_CountsTheEmptyLastLineAtTheMultipliedBase()
    {
        float h = PlainLineHeight();
        // a, the size run, then the empty line after the trailing newline: 3h + 3h + 3h.
        Text text = Build("a\n[FontScale=3]b\n", 2, 1.5f);

        text.WrappedText.Count.ShouldBe(3);
        text.WrappedTextHeight.ShouldBe(9 * h, tolerance: 1.01f);
    }
}
