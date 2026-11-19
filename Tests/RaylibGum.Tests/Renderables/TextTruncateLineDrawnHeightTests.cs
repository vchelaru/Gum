using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Renderables;
using RenderingLibrary.Graphics;
using Shouldly;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// #8338: TruncateLine keeps only the lines whose drawn heights fit the box, so a line a size run makes taller
/// counts as more than one base line, as on MonoGame and Skia.
/// </summary>
public class TextTruncateLineDrawnHeightTests : BaseTestClass
{
    private static float PlainLineAdvance()
    {
        TextRuntime plain = new();
        plain.Text = "a\nb";
        float advance = ((Text)plain.RenderableComponent!).GetLineTopOffsets()[1];
        advance.ShouldBeGreaterThan(0);
        return advance;
    }

    [Theory]
    [InlineData("a\nb\nc\nd", 2, 2)]
    [InlineData("[FontScale=0.5]a\nb\nc\nd[/FontScale]", 2, 2)]
    [InlineData("[FontScale=2]a\nb\nc\nd[/FontScale]", 2, 1)]
    [InlineData("a\n[FontScale=2]b[/FontScale]\nc\nd", 3, 2)]
    [InlineData("a\n[FontScale=2]b[/FontScale]\nc\nd", 4, 3)]
    public void TruncateLine_KeepsTheLinesWhoseDrawnHeightFits(string markup, float baseLines, int expectedLines)
    {
        float h = PlainLineAdvance();

        TextRuntime runtime = new();
        runtime.HeightUnits = DimensionUnitType.Absolute;
        runtime.Height = h * baseLines;
        runtime.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        runtime.Text = markup;
        runtime.UpdateLayout();

        ((Text)runtime.RenderableComponent!).WrappedText.Count.ShouldBe(expectedLines);
    }

    // Each word is wider than the box, so every line comes from a mid-word break.
    [Theory]
    [InlineData("a [FontScale=2]b[/FontScale]")]
    [InlineData("a b")]
    public void TruncateLine_MidWordBrokenLines_KeepOnlyTheLinesThatFit(string markup)
    {
        float h = PlainLineAdvance();

        TextRuntime runtime = new();
        runtime.HeightUnits = DimensionUnitType.Absolute;
        runtime.Height = h;
        runtime.WidthUnits = DimensionUnitType.Absolute;
        runtime.Width = 5;
        runtime.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        runtime.Text = markup;
        runtime.UpdateLayout();

        Text text = (Text)runtime.RenderableComponent!;
        text.WrappedText.Count.ShouldBe(1);
        text.WrappedText[0].Trim().ShouldBe("a");
    }
}
