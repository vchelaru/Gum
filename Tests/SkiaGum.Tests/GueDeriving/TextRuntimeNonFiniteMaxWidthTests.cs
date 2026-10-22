using Gum.DataTypes;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum;
using Gum.GueDeriving;

namespace SkiaGum.Tests.GueDeriving;

/// <summary>
/// A NaN or infinite MaxWidth is no limit (#6473): a Skia text wraps exactly as it does with no MaxWidth, whether its
/// width is fixed or grows to its content, and whether the bound is set before or after the text.
/// </summary>
public class TextRuntimeNonFiniteMaxWidthTests
{
    private const string LongText = "a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a";

    public TextRuntimeNonFiniteMaxWidthTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
    }

    private static (float Width, float Height, int Lines) Layout(float? maxWidth, bool boundFirst, bool growsToContent)
    {
        TextRuntime text = new();
        text.Width = growsToContent ? 0 : 50;
        text.WidthUnits = growsToContent ? DimensionUnitType.RelativeToChildren : DimensionUnitType.Absolute;
        if (boundFirst)
        {
            text.MaxWidth = maxWidth;
        }
        text.Text = LongText;
        if (!boundFirst)
        {
            text.MaxWidth = maxWidth;
        }
        text.UpdateLayout();
        Text inner = (Text)text.RenderableComponent!;
        return (text.AbsoluteWidth, text.AbsoluteHeight, inner.WrappedText.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FiniteMaxWidth_StillWrapsAGrowingText(bool boundFirst)
    {
        Layout(60, boundFirst, growsToContent: true).Lines.ShouldBeGreaterThan(Layout(null, boundFirst, growsToContent: true).Lines);
    }

    [Theory]
    [InlineData(float.PositiveInfinity, true, true)]
    [InlineData(float.PositiveInfinity, false, true)]
    [InlineData(float.NegativeInfinity, true, true)]
    [InlineData(float.NegativeInfinity, false, true)]
    [InlineData(float.NaN, true, true)]
    [InlineData(float.NaN, false, true)]
    [InlineData(float.PositiveInfinity, true, false)]
    [InlineData(float.PositiveInfinity, false, false)]
    [InlineData(float.NegativeInfinity, true, false)]
    [InlineData(float.NegativeInfinity, false, false)]
    [InlineData(float.NaN, true, false)]
    [InlineData(float.NaN, false, false)]
    public void NonFiniteMaxWidth_WrapsAsWithNoMaxWidth(float maxWidth, bool boundFirst, bool growsToContent)
    {
        Layout(maxWidth, boundFirst, growsToContent).ShouldBe(Layout(null, boundFirst, growsToContent));
    }
}
