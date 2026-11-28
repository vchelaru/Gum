using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Linq;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A RelativeToChildren Text reports the full size of its lines, however large, so siblings and
/// parents lay out after all of the drawn text (#8506).
/// </summary>
public class TextRelativeToChildrenSizeLimitTests : BaseTestClass
{
    private static TextRuntime MakeRelative(string text)
    {
        TextRuntime textRuntime = new();
        textRuntime.WidthUnits = DimensionUnitType.RelativeToChildren;
        textRuntime.Width = 0;
        textRuntime.HeightUnits = DimensionUnitType.RelativeToChildren;
        textRuntime.Height = 0;
        textRuntime.Text = text;
        return textRuntime;
    }

    private static string RepeatLines(string line, int count) =>
        string.Join("\n", Enumerable.Repeat(line, count));

    [Theory]
    [InlineData("Line plain")]
    [InlineData("Line [Color=Orange]styled[/Color]")]
    public void Height_RelativeToChildren_ManyLines_IsNotCappedAt4096(string line)
    {
        float halfHeight = MakeRelative(RepeatLines(line, 150)).AbsoluteHeight;

        float fullHeight = MakeRelative(RepeatLines(line, 300)).AbsoluteHeight;

        fullHeight.ShouldBeGreaterThan(4096);
        fullHeight.ShouldBe(halfHeight * 2, tolerance: 1);
    }

    [Fact]
    public void Width_RelativeToChildren_LongLine_IsNotCappedAt4096()
    {
        string line = new string('m', 600);
        TextRuntime text = MakeRelative(line);
        float measuredWidth = ((Text)text.RenderableComponent!).BitmapFont!.MeasureString(line);

        float width = text.AbsoluteWidth;

        width.ShouldBeGreaterThan(4096);
        width.ShouldBe(measuredWidth);
    }

    // RenderTarget mode draws a texture that stops at 4096 pixels, so it is drawn at that size (times the
    // font scale, and the line-height multiplier for height) and the rest is cut off, rather than stretched
    // over the full measured size.
    [Theory]
    [InlineData(1f, 1f, 4096f)]
    [InlineData(0.5f, 1f, 2048f)]
    [InlineData(2f, 1f, 8192f)]
    [InlineData(1f, 0.5f, 2048f)]
    [InlineData(1f, 1.5f, 6144f)]
    public void RenderTargetMode_TextTallerThan4096_DrawsAtCappedHeight(float fontScale, float lineHeightMultiplier, float expectedHeight)
    {
        string manyLines = RepeatLines("mmmmmmmmmm", 300);

        IRenderableIpso drawn = GetDrawnRectInRenderTargetMode(manyLines, fontScale, lineHeightMultiplier, out TextRuntime text);

        text.AbsoluteHeight.ShouldBeGreaterThan(expectedHeight);
        drawn.Height.ShouldBe(expectedHeight);
    }

    [Theory]
    [InlineData(1f, 1f, 4096f)]
    [InlineData(0.5f, 1f, 2048f)]
    [InlineData(2f, 1f, 8192f)]
    [InlineData(1f, 1.5f, 4096f)]
    public void RenderTargetMode_TextWiderThan4096_DrawsAtCappedWidth(float fontScale, float lineHeightMultiplier, float expectedWidth)
    {
        string longLine = new string('m', 600);

        IRenderableIpso drawn = GetDrawnRectInRenderTargetMode(longLine, fontScale, lineHeightMultiplier, out TextRuntime text);

        text.AbsoluteWidth.ShouldBeGreaterThan(expectedWidth);
        drawn.Width.ShouldBe(expectedWidth);
    }

    private static IRenderableIpso GetDrawnRectInRenderTargetMode(string value, float fontScale,
        float lineHeightMultiplier, out TextRuntime text)
    {
        TextRenderingMode originalMode = Text.TextRenderingMode;
        Text.TextRenderingMode = TextRenderingMode.RenderTarget;
        try
        {
            text = MakeRelative(value);
            text.FontScale = fontScale;
            text.LineHeightMultiplier = lineHeightMultiplier;
            return ((Text)text.RenderableComponent!).UpdateIpsoForRenderingForTests();
        }
        finally
        {
            Text.TextRenderingMode = originalMode;
        }
    }
}
