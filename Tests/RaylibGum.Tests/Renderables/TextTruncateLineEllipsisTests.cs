using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// #8340: whenever TruncateLine cuts lines (a hard newline, a dropped tall line), the ellipsis goes on the last
/// kept line, as on MonoGame and Skia.
/// </summary>
public class TextTruncateLineEllipsisTests : BaseTestClass
{
    [Theory]
    [InlineData("a\nb\nc\nd", 2, true, new[] { "a\n", "b..." })]
    [InlineData("a\n[FontScale=2]b[/FontScale]", 1, true, new[] { "a..." })]
    [InlineData("a\nb", 2, true, new[] { "a\n", "b" })]
    [InlineData("a\nb\nc\nd", 2, false, new[] { "a\n", "b\n" })]
    public void TruncateLine_CutLines_EndWithEllipsisOnTheLastKeptLine(string markup, float baseLines, bool ellipsis, string[] expectedLines)
    {
        TextRuntime plain = new();
        plain.Text = "a\nb";
        float lineAdvance = ((Text)plain.RenderableComponent!).GetLineTopOffsets()[1];

        TextRuntime runtime = new();
        runtime.HeightUnits = DimensionUnitType.Absolute;
        runtime.Height = lineAdvance * baseLines;
        runtime.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        runtime.IsTruncatingWithEllipsisOnLastLine = ellipsis;
        runtime.Text = markup;
        runtime.UpdateLayout();

        ((Text)runtime.RenderableComponent!).WrappedText.ShouldBe(expectedLines);
    }

    [Fact]
    public void TruncateLine_EllipsisReplacingASizeRun_IsDrawnInThePrecedingStyle()
    {
        TextRuntime plain = new();
        plain.Text = "a\nb";
        float lineAdvance = ((Text)plain.RenderableComponent!).GetLineTopOffsets()[1];

        TextRuntime runtime = new();
        runtime.HeightUnits = DimensionUnitType.Absolute;
        runtime.Height = lineAdvance;
        runtime.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        runtime.IsTruncatingWithEllipsisOnLastLine = true;
        runtime.Text = "First line\n[FontScale=2]Tall[/FontScale]";
        runtime.UpdateLayout();

        Text text = (Text)runtime.RenderableComponent!;
        text.WrappedText.ShouldBe(new[] { "First line..." });
        text.GetCachedStyledSubstrings(0, 0, text.WrappedText[0])
            .ShouldAllBe(run => run.Variables.Count == 0);
    }
}
