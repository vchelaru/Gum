using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using static RaylibGum.Tests.Rendering.TextInkRender;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Vertical alignment of multi-line BBCode text whose lines have different sizes on raylib. The text is drawn
/// Top-aligned at its own RelativeToChildren height, then in a box <see cref="ExtraHeight"/> taller: Bottom must
/// move the ink down by the whole extra height and Center by half, which only holds when the alignment uses the
/// same (styled) content height the layout does.
/// </summary>
public class StyledTextVerticalAlignmentDrawTests : BaseTestClass
{
    private const int ExtraHeight = 60;

    [Theory]
    [InlineData("ab\n[FontScale=2]cd[/FontScale]\nef", VerticalAlignment.Bottom, ExtraHeight)]
    [InlineData("ab\n[FontScale=2]cd[/FontScale]\nef", VerticalAlignment.Center, ExtraHeight / 2)]
    [InlineData("[FontScale=2]ab[/FontScale]\ncd", VerticalAlignment.Bottom, ExtraHeight)]
    [InlineData("[FontScale=2]ab[/FontScale]\ncd", VerticalAlignment.Center, ExtraHeight / 2)]
    [InlineData("ab\ncd[FontScale=3]e[/FontScale]", VerticalAlignment.Bottom, ExtraHeight)]
    [InlineData("ab\n[FontScale=0.5]cd[/FontScale]\nef", VerticalAlignment.Bottom, ExtraHeight)]
    [InlineData("[Color=Red]ab[/Color]\ncd", VerticalAlignment.Bottom, ExtraHeight)]
    public void Draw_MixedSizeLines_AlignmentMovesTheTextByTheSpareHeight(string markup, VerticalAlignment alignment, int expectedShift)
    {
        TextRuntime? natural = null;
        Bounds top = InkBounds(Render(markup, left: 10, top: 10, runtime =>
        {
            runtime.FontScale = 1;
            runtime.VerticalAlignment = VerticalAlignment.Top;
            runtime.HeightUnits = DimensionUnitType.RelativeToChildren;
            runtime.Height = 0;
            natural = runtime;
        }));
        float naturalHeight = natural!.GetAbsoluteHeight();

        Bounds aligned = InkBounds(Render(markup, left: 10, top: 10, runtime =>
        {
            runtime.FontScale = 1;
            runtime.VerticalAlignment = alignment;
            runtime.HeightUnits = DimensionUnitType.Absolute;
            runtime.Height = naturalHeight + ExtraHeight;
        }));

        (aligned.Top - top.Top).ShouldBe(expectedShift,
            $"'{markup}' is {naturalHeight} tall in a {naturalHeight + ExtraHeight} box, aligned {alignment}");
    }
}
