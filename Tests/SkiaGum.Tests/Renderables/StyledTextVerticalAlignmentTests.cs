using Gum.DataTypes;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Vertical alignment of multi-line BBCode text whose lines have different sizes on Skia. The text is drawn
/// Top-aligned at its own RelativeToChildren height, then in a box <see cref="ExtraHeight"/> taller: Bottom must
/// move the ink down by the whole extra height and Center by half.
/// </summary>
public class StyledTextVerticalAlignmentTests : BaseTestClass
{
    private const int ExtraHeight = 60;

    [Theory]
    [InlineData("ab\n[FontScale=2]cd[/FontScale]\nef", VerticalAlignment.Bottom, ExtraHeight)]
    [InlineData("ab\n[FontScale=2]cd[/FontScale]\nef", VerticalAlignment.Center, ExtraHeight / 2)]
    [InlineData("[FontScale=2]ab[/FontScale]\ncd", VerticalAlignment.Bottom, ExtraHeight)]
    [InlineData("[FontScale=2]ab[/FontScale]\ncd", VerticalAlignment.Center, ExtraHeight / 2)]
    [InlineData("ab\ncd[FontScale=3]e[/FontScale]", VerticalAlignment.Bottom, ExtraHeight)]
    [InlineData("ab\n[FontScale=0.5]cd[/FontScale]\nef", VerticalAlignment.Bottom, ExtraHeight)]
    [InlineData("ab\n[FontSize=30]cd[/FontSize]\nef", VerticalAlignment.Bottom, ExtraHeight)]
    [InlineData("[Color=Red]ab[/Color]\ncd", VerticalAlignment.Bottom, ExtraHeight)]
    public void Render_MixedSizeLines_AlignmentMovesTheTextByTheSpareHeight(string markup, VerticalAlignment alignment, int expectedShift)
    {
        int topInk = InkTop(markup, VerticalAlignment.Top, height: null, out float naturalHeight);
        int alignedInk = InkTop(markup, alignment, naturalHeight + ExtraHeight, out _);

        ((float)(alignedInk - topInk)).ShouldBe(expectedShift, tolerance: 1,
            $"'{markup}' is {naturalHeight} tall in a {naturalHeight + ExtraHeight} box, aligned {alignment}");
    }

    private static int InkTop(string markup, VerticalAlignment alignment, float? height, out float layoutHeight)
    {
        TextRuntime runtime = new();
        runtime.X = 10;
        runtime.Y = 10;
        runtime.Text = markup;
        runtime.VerticalAlignment = alignment;
        if (height == null)
        {
            runtime.HeightUnits = DimensionUnitType.RelativeToChildren;
            runtime.Height = 0;
        }
        else
        {
            runtime.HeightUnits = DimensionUnitType.Absolute;
            runtime.Height = height.Value;
        }
        runtime.UpdateLayout();
        layoutHeight = runtime.GetAbsoluteHeight();

        using SKSurface surface = SKSurface.Create(new SKImageInfo(240, 320));
        surface.Canvas.Clear(SKColors.Transparent);
        SystemManagers managers = new() { Canvas = surface.Canvas };
        ((IRenderable)runtime.RenderableComponent).Render(managers);

        using SKImage image = surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(image);
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).Alpha > 128)
                {
                    return y;
                }
            }
        }
        throw new ShouldAssertException("the text drew no pixels");
    }
}
