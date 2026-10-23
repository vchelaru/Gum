using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

public class LetterCellScaleTests
{
    // Cell at (10, 20), 8 wide and 12 tall, baseline 9 below its top, scaled by (2, 3).
    [Theory]
    [InlineData(HorizontalAlignment.Left, VerticalAlignment.Top, 10f, 20f)]
    [InlineData(HorizontalAlignment.Center, VerticalAlignment.Center, 6f, 8f)]
    [InlineData(HorizontalAlignment.Right, VerticalAlignment.Bottom, 2f, -4f)]
    [InlineData(HorizontalAlignment.Left, VerticalAlignment.TextBaseline, 10f, 2f)]
    public void GetScaledTopLeft_KeepsTheOriginPointFixed(HorizontalAlignment xOrigin, VerticalAlignment yOrigin,
        float expectedX, float expectedY)
    {
        System.Numerics.Vector2 topLeft = LetterCellScale.GetScaledTopLeft(10, 20, 8, 12, 9, 2, 3, xOrigin, yOrigin);

        topLeft.X.ShouldBe(expectedX, tolerance: 0.0001f);
        topLeft.Y.ShouldBe(expectedY, tolerance: 0.0001f);
    }

    [Fact]
    public void GetScaledTopLeft_ScaleOfOne_IsTheCellTopLeft()
    {
        System.Numerics.Vector2 topLeft = LetterCellScale.GetScaledTopLeft(10, 20, 8, 12, 9, 1, 1,
            HorizontalAlignment.Right, VerticalAlignment.Bottom);

        topLeft.X.ShouldBe(10);
        topLeft.Y.ShouldBe(20);
    }
}
