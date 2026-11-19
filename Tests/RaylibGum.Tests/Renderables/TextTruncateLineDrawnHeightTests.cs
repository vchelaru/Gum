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
}
