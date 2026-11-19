using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using Topten.RichTextKit;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// #8338: pins Skia's TruncateLine, the reference MonoGame and raylib match: a line a size run makes taller
/// counts as more than one base line, and a smaller run does not shrink its line.
/// </summary>
public class TextTruncateLineDrawnHeightTests : BaseTestClass
{
    [Theory]
    [InlineData("a\nb\nc\nd", 2, 2)]
    [InlineData("[FontScale=0.5]a\nb\nc\nd[/FontScale]", 2, 2)]
    [InlineData("[FontScale=2]a\nb\nc\nd[/FontScale]", 2, 1)]
    public void TruncateLine_KeepsTheLinesWhoseDrawnHeightFits(string markup, float baseLines, int expectedLines)
    {
        TextRuntime plain = new();
        plain.Text = "a\nb";
        TextBlock plainBlock = ((SkiaGum.Text)plain.RenderableComponent!).GetTextBlock();
        float h = plainBlock.Lines[1].YCoord - plainBlock.Lines[0].YCoord;

        TextRuntime runtime = new();
        runtime.HeightUnits = DimensionUnitType.Absolute;
        runtime.Height = h * baseLines;
        runtime.TextOverflowVerticalMode = TextOverflowVerticalMode.TruncateLine;
        runtime.Text = markup;
        runtime.UpdateLayout();

        ((SkiaGum.Text)runtime.RenderableComponent!).GetTextBlock().Lines.Count.ShouldBe(expectedLines);
    }
}
