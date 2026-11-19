using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// #8338: TruncateLine keeps only the lines whose drawn heights fit the box, so a line a size run makes taller
/// counts as more than one base line. Skia does the same (SkiaGum.Tests TextTruncateLineDrawnHeightTests).
/// </summary>
public class TextTruncateLineDrawnHeightTests : BaseTestClass
{
    private static Text Make(string markup, float baseLines, float? width = null, bool ellipsis = false)
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
    [InlineData("a\nb\nc\nd", 2, 2)]
    [InlineData("a\nb\nc\nd", 2.5f, 2)]
    [InlineData("[Color=Red]a[/Color]\nb\nc\nd", 2, 2)]
    [InlineData("[FontScale=0.5]a\nb\nc\nd[/FontScale]", 2, 2)]
    [InlineData("[FontScale=2]a\nb\nc\nd[/FontScale]", 2, 1)]
    [InlineData("[FontScale=2]a\nb\nc\nd[/FontScale]", 1.5f, 1)]
    [InlineData("[FontScale=5]hi[/FontScale]", 1, 1)]
    [InlineData("a\nb", 0.5f, 0)]
    [InlineData("a\n[FontScale=2]b[/FontScale]\nc\nd", 3, 2)]
    [InlineData("a\n[FontScale=2]b[/FontScale]\nc\nd", 4, 3)]
    [InlineData("a\n[FontScale=2]b[/FontScale]\nc\nd", 10, 4)]
    public void TruncateLine_KeepsTheLinesWhoseDrawnHeightFits(string markup, float baseLines, int expectedLines)
    {
        Text text = Make(markup, baseLines);

        text.WrappedText.Count.ShouldBe(expectedLines);
        if (expectedLines > 1)
        {
            text.GetDrawnLineTop(text.WrappedText.Count).ShouldBeLessThanOrEqualTo(text.LineHeightInPixels * baseLines + 0.001f);
        }
    }

    // Each word is wider than the box, so every line comes from a mid-word break.
    [Theory]
    [InlineData("a [FontScale=2]b[/FontScale]")]
    [InlineData("a b")]
    public void TruncateLine_MidWordBrokenLines_KeepOnlyTheLinesThatFit(string markup)
    {
        Text text = Make(markup, 1, width: 5);

        text.WrappedText.Count.ShouldBe(1);
        text.WrappedText[0].Trim().ShouldBe("a");
    }

    [Fact]
    public void TruncateLine_TallRuns_PutTheEllipsisOnTheLastLineThatFits()
    {
        Text text = Make("[FontScale=2]one two three four five six[/FontScale]", 2, width: 150, ellipsis: true);

        text.WrappedText.Count.ShouldBe(1);
        text.WrappedText[0].ShouldEndWith("...");
    }

    [Fact]
    public void TruncateLine_TallRuns_RespectsMaxNumberOfLines()
    {
        TextRuntime runtime = new();
        Text renderable = (Text)runtime.RenderableComponent!;
        runtime.HeightUnits = DimensionUnitType.Absolute;
        runtime.Height = renderable.LineHeightInPixels * 10;
        runtime.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        runtime.MaxNumberOfLines = 2;
        runtime.Text = "[FontScale=2]a\nb\nc\nd[/FontScale]";
        runtime.UpdateLayout();

        renderable.WrappedText.Count.ShouldBe(2);
    }
}
