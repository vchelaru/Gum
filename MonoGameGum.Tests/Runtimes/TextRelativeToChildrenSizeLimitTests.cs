using Gum.DataTypes;
using Gum.GueDeriving;
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
}
