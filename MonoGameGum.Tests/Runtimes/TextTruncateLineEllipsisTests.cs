using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// #8340: whenever TruncateLine cuts lines, for any reason (a hard newline, a dropped tall line, wrapping), the
/// ellipsis goes on the last kept line, as on Skia: the line loses its newline and trailing spaces, then its last
/// characters until the ellipsis fits the width.
/// </summary>
public class TextTruncateLineEllipsisTests : BaseTestClass
{
    private static Text Make(string markup, float baseLines, bool ellipsis, float? width = null)
    {
        TextRuntime runtime = new();
        Text renderable = (Text)runtime.RenderableComponent!;
        runtime.HeightUnits = DimensionUnitType.Absolute;
        runtime.Height = renderable.LineHeightInPixels * baseLines;
        if (width != null)
        {
            runtime.WidthUnits = DimensionUnitType.Absolute;
            runtime.Width = width.Value;
        }
        runtime.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        runtime.IsTruncatingWithEllipsisOnLastLine = ellipsis;
        runtime.Text = markup;
        runtime.UpdateLayout();
        return renderable;
    }

    [Theory]
    // Cut at a hard newline.
    [InlineData("a\nb\nc\nd", 2, true, new[] { "a\n", "b..." })]
    [InlineData("[Color=Red]a[/Color]\nb\nc\nd", 2, true, new[] { "a\n", "b..." })]
    [InlineData("a \nb", 1, true, new[] { "a..." })]
    [InlineData("a\n\nc", 2, true, new[] { "a\n", "..." })]
    // A later line a size run makes too tall is dropped.
    [InlineData("a\n[FontScale=2]b[/FontScale]", 1, true, new[] { "a..." })]
    [InlineData("a\nb\n[FontScale=3]c[/FontScale]", 2, true, new[] { "a\n", "b..." })]
    // Nothing cut, or no ellipsis wanted: the lines are left alone.
    [InlineData("a\nb", 2, true, new[] { "a\n", "b" })]
    [InlineData("a\nb\nc", 5, true, new[] { "a\n", "b\n", "c" })]
    [InlineData("a\n", 1, true, new[] { "a\n" })]
    [InlineData("a\nb\nc\nd", 2, false, new[] { "a\n", "b\n" })]
    [InlineData("a\n[FontScale=2]b[/FontScale]", 1, false, new[] { "a\n" })]
    public void TruncateLine_CutLines_EndWithEllipsisOnTheLastKeptLine(string markup, float baseLines, bool ellipsis, string[] expectedLines)
    {
        Text text = Make(markup, baseLines, ellipsis);

        text.WrappedText.ShouldBe(expectedLines);
    }

    [Fact]
    public void TruncateLine_CutAtHardNewline_DropsLastCharactersUntilTheEllipsisFits()
    {
        Text measure = Make("x", 1, false);
        float width = measure.MeasureString("aa...") - 1;

        Text text = Make("aaaa\nb", 1, true, width);

        text.WrappedText.ShouldBe(new[] { "a..." });
    }

    [Fact]
    public void TruncateLine_WordTooWideForAnyLetterBesideTheEllipsis_ShowsTheEllipsisAlone()
    {
        Text measure = Make("x", 1, false);
        float width = measure.MeasureString("a...") - 1;

        Text text = Make("aaaa bbbb", 1, true, width);

        text.WrappedText.ShouldBe(new[] { "..." });
    }

    // The "..." replaced cut text that a size run covers; it is drawn like the character before it, so neither the
    // dots nor the kept line grow to the dropped run's size.
    [Theory]
    [InlineData("First line\n[FontScale=2]Tall[/FontScale]", null, "First line...")]
    [InlineData("aaa [FontScale=2]bbb[/FontScale]", "aaa...", "aaa...")]
    public void TruncateLine_EllipsisReplacingASizeRun_IsDrawnInThePrecedingStyle(string markup, string? widthOf, string expectedLine)
    {
        Text measure = Make("x", 1, false);
        float? width = widthOf == null ? null : measure.MeasureString(widthOf) + 2;
        Text plain = Make(expectedLine, 1, false, width);

        Text text = Make(markup, 1, true, width);

        text.WrappedText.ShouldBe(new[] { expectedLine });
        text.GetCachedStyledSubstrings(0, 0, text.WrappedText[0])
            .ShouldAllBe(run => run.Variables.Count == 0);
        text.GetDrawnLineTop(1).ShouldBe(plain.GetDrawnLineTop(1));
    }
}
