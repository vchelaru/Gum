using Gum.DataTypes;
using Gum.GueDeriving;
using Shouldly;
using System;
using System.Collections.Generic;
using Xunit;
using Text = Gum.Renderables.Text;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Pins which words land on which line of <see cref="Text.WrappedText"/> for BBCode-styled text on Raylib
/// (issue #6492). The expected lines are written by hand. The wrap width is derived from the plain base-font
/// glyph advances (<see cref="Text.MeasureString(string)"/>, one character at a time), never from the wrap
/// loop or the styled measurement under test: a [FontScale=s] character is s times its plain advance, and a
/// [FontSize=N] character (no font creator wired) is N / BaseSize times it, because Raylib measures with
/// spacing 0 and scales advances linearly. From those advances each test takes the widest expected line
/// (trailing space and newline excluded, which the wrap ignores) and the narrowest overflow (a wrapped line
/// plus the next line's first word), requires a gap of at least <see cref="MinimumGap"/> pixels between them,
/// and wraps at the midpoint, so no assertion depends on a pixel of glyph measurement (#6491).
/// </summary>
public class TextStyledWrapLineTests : BaseTestClass
{
    const float MinimumGap = 8;

    [Fact]
    public void WrappedText_ShouldMatchPlainWrap_WhenRunIsColorOnly()
    {
        string markup = "[Color=Red]AA BB[/Color] CC DD";
        string[] expected = { "AA BB ", "CC DD" };

        float width = GetWrapWidth("AA BB CC DD", _ => 1, expected);

        GetWrappedLines(markup, width).ShouldBe(expected);
        GetWrappedLines("AA BB CC DD", width).ShouldBe(expected);
    }

    [Fact]
    public void WrappedText_ShouldWrapAtScaledWidth_WhenFontScaleRunEnlargesWord()
    {
        // BB at 2x pushes CC to the second line; at 1x all three words would fit (checked below).
        string markup = "AA [FontScale=2]BB[/FontScale] CC DD";
        string stripped = "AA BB CC DD";
        string[] expected = { "AA BB ", "CC DD" };

        float width = GetWrapWidth(stripped, ScaleOver(stripped, "BB", 2), expected);

        GetWrappedLines(markup, width).ShouldBe(expected);
        GetWrappedLines(stripped, width).ShouldBe(new[] { "AA BB CC ", "DD" });
    }

    [Fact]
    public void WrappedText_ShouldWrapAtScaledWidth_WhenFontSizeRunEnlargesWord()
    {
        TextRuntime probe = new();
        float baseSize = ((Text)probe.RenderableComponent!).Font.BaseSize;
        float runSize = baseSize * 2;

        string markup = $"AA [FontSize={runSize}]BB[/FontSize] CC DD";
        string stripped = "AA BB CC DD";
        string[] expected = { "AA BB ", "CC DD" };

        float width = GetWrapWidth(stripped, ScaleOver(stripped, "BB", runSize / baseSize), expected);

        GetWrappedLines(markup, width).ShouldBe(expected);
    }

    [Fact]
    public void WrappedText_ShouldMeasureContinuationAtRunSize_WhenRunSpansWrap()
    {
        // The run covers BB and CC; CC opens the second line and must still measure at 2x, which
        // leaves no room for EE (at 1x, CC DD EE would fit).
        string markup = "AA [FontScale=2]BB CC[/FontScale] DD EE";
        string stripped = "AA BB CC DD EE";
        string[] expected = { "AA BB ", "CC DD ", "EE" };

        float width = GetWrapWidth(stripped, ScaleOver(stripped, "BB CC", 2), expected);

        GetWrappedLines(markup, width).ShouldBe(expected);
    }

    [Fact]
    public void WrappedText_ShouldKeepRunIndexAfterNewline_WhenNewlineIsInsideRun()
    {
        // The newline breaks inside the 2x run; BB (still in the run) opens line two at 2x, which
        // leaves no room for DD.
        string markup = "[FontScale=2]AA\nBB[/FontScale] CC DD";
        string stripped = "AA\nBB CC DD";
        string[] expected = { "AA\n", "BB CC ", "DD" };

        float width = GetWrapWidth(stripped, ScaleOver(stripped, "AA\nBB", 2), expected);

        GetWrappedLines(markup, width).ShouldBe(expected);
    }

    [Fact]
    public void WrappedText_ShouldKeepTrailingSpacesOnWrappedLine_WhenRunEndsInSpaces()
    {
        // Both spaces stay at the end of line one, so line two starts after them, outside the run:
        // BB and CC measure at 1x and fit together.
        string markup = "[FontScale=2]AA  [/FontScale]BB CC";
        string stripped = "AA  BB CC";
        string[] expected = { "AA  ", "BB CC" };

        float width = GetWrapWidth(stripped, ScaleOver(stripped, "AA  ", 2), expected);

        GetWrappedLines(markup, width).ShouldBe(expected);
    }

    static List<string> GetWrappedLines(string text, float width)
    {
        TextRuntime textRuntime = new();
        textRuntime.WidthUnits = DimensionUnitType.Absolute;
        textRuntime.Width = width;
        textRuntime.Text = text;
        return new List<string>(((Text)textRuntime.RenderableComponent!).WrappedText);
    }

    /// <summary>Scale per stripped-text index: <paramref name="scale"/> inside the first occurrence of
    /// <paramref name="run"/>, 1 elsewhere.</summary>
    static Func<int, float> ScaleOver(string stripped, string run, float scale)
    {
        int start = stripped.IndexOf(run, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0);
        int end = start + run.Length;
        return index => index >= start && index < end ? scale : 1;
    }

    /// <summary>
    /// The midpoint between the widest expected line and the narrowest overflow (see the class summary),
    /// after checking the gap between them is at least <see cref="MinimumGap"/>.
    /// </summary>
    static float GetWrapWidth(string stripped, Func<int, float> scaleAt, string[] expectedLines)
    {
        string.Concat(expectedLines).ShouldBe(stripped);

        TextRuntime measurer = new();
        Text measuringText = (Text)measurer.RenderableComponent!;

        float Measure(int start, int length)
        {
            float total = 0;
            for (int i = start; i < start + length; i++)
            {
                total += measuringText.MeasureString(stripped[i].ToString()) * scaleAt(i);
            }
            return total;
        }

        float widestLine = 0;
        float narrowestOverflow = float.MaxValue;
        int lineStart = 0;
        for (int lineIndex = 0; lineIndex < expectedLines.Length; lineIndex++)
        {
            string line = expectedLines[lineIndex];
            string fitted = line.TrimEnd(' ', '\n');
            widestLine = Math.Max(widestLine, Measure(lineStart, fitted.Length));

            bool isLastLine = lineIndex == expectedLines.Length - 1;
            if (!isLastLine && !line.EndsWith('\n'))
            {
                string nextLine = expectedLines[lineIndex + 1];
                int nextWordLength = nextLine.IndexOfAny(new[] { ' ', '\n' });
                if (nextWordLength < 0)
                {
                    nextWordLength = nextLine.Length;
                }
                narrowestOverflow = Math.Min(narrowestOverflow, Measure(lineStart, line.Length + nextWordLength));
            }
            lineStart += line.Length;
        }

        (narrowestOverflow - widestLine).ShouldBeGreaterThanOrEqualTo(MinimumGap,
            $"widest line {widestLine}, narrowest overflow {narrowestOverflow}");
        return (widestLine + narrowestOverflow) / 2;
    }
}
