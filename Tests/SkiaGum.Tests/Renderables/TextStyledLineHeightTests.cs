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
/// Every line, multiplied plain text included, is exact (#6467): the leading RichTextKit leaves unmultiplied is
/// compensated for in the run style's line height.
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

    /// <summary>Float and pixel rounding only, in units of h: every line must match <c>base * multiplier * h</c>
    /// (or the size run's own height) at every multiplier, with no allowance for font leading (#6467).</summary>
    private const float ExactTolerance = 0.005f;

    private static float Plain(float baseScale, float multiplier) => baseScale * multiplier;

    private static float Run(float baseScale, float multiplier, float runScale) =>
        System.Math.Max(baseScale * multiplier, runScale);

    // RichTextKit drops the empty line after a trailing newline, so the text adds it to the reported height itself.
    private static void ShouldMatch(Text text, float h, params float[] expectedAdvances) =>
        ShouldMatch(text, h, false, expectedAdvances);

    private static void ShouldMatch(Text text, float h, bool endsWithEmptyLine, float[] expectedAdvances)
    {
        TextBlock block = text.GetTextBlock();
        block.Lines.Count.ShouldBe(expectedAdvances.Length - (endsWithEmptyLine ? 1 : 0));

        float expectedTop = 0;
        for (int i = 0; i < expectedAdvances.Length; i++)
        {
            if (i < block.Lines.Count)
            {
                block.Lines[i].YCoord.ShouldBe(expectedTop * h, ExactTolerance * h, $"top of line {i}");
            }
            expectedTop += expectedAdvances[i];
        }

        text.WrappedTextHeight.ShouldBe(expectedTop * h, ExactTolerance * h, "the reported height is the drawn extent");
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

    // A size run that wraps over several lines must not get worse than main (#6475 tracks making mixed-size
    // lines exact): the lines hold the base text and the run, each should advance max(base * multiplier, run).
    // Worst top error on main is 0.13h and 0.29h; these pin it from growing (the second is 0.30h with the compensation).
    [Theory]
    [InlineData("Start [FontScale=0.75]The quick brown fox jumps over the lazy dog and again[/FontScale] end", 1f, 120, 0.14f)]
    [InlineData("[FontSize=10]long wrapped text[/FontSize] Wmw", 2f, 200, 0.31f)]
    public void WrappedSizeRun_LineTopsAreNoFurtherFromExpectedThanBeforeTheLeadingCompensation(string markup, float baseScale, float width, float maxErrorInH)
    {
        float h = PlainLineHeight();
        Text text = Build(markup, baseScale, 1);
        text.Width = width;

        TextBlock block = text.GetTextBlock();
        block.Lines.Count.ShouldBeGreaterThan(1);
        for (int i = 0; i < block.Lines.Count; i++)
        {
            block.Lines[i].YCoord.ShouldBe(i * baseScale * h, maxErrorInH * h, $"top of line {i}");
        }
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

        float plain = Plain(baseScale, multiplier);
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

        float plain = Plain(baseScale, multiplier);
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
        float tolerance = ExactTolerance * h;

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
