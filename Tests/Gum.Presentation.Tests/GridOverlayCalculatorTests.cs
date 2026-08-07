using Gum.Wireframe.Editors;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins GridOverlayCalculator, which sizes/positions the grid overlay to cover the camera's
/// visible world rect without visually shifting as the camera pans (issue #4137).
/// </summary>
public class GridOverlayCalculatorTests
{
    [Fact]
    public void Calculate_ShouldAlignOriginToGridLineAtOrBeforeVisibleLeftAndTop()
    {
        GridOverlayCalculator.Calculate(
            visibleLeft: 20, visibleTop: 40, visibleRight: 100, visibleBottom: 100, gridSize: 16,
            out float originX, out float originY, out int columnCount, out int rowCount);

        originX.ShouldBe(16);
        originY.ShouldBe(32);
    }

    [Fact]
    public void Calculate_ShouldCoverVisibleRectWithOneExtraLine()
    {
        // Visible width is exactly 80 (5 grid cells) starting from an aligned origin -
        // still needs a trailing line to cover the right/bottom edge.
        GridOverlayCalculator.Calculate(
            visibleLeft: 0, visibleTop: 0, visibleRight: 80, visibleBottom: 80, gridSize: 16,
            out float originX, out float originY, out int columnCount, out int rowCount);

        columnCount.ShouldBe(6);
        rowCount.ShouldBe(6);
    }

    [Fact]
    public void Calculate_ShouldAlignOrigin_WhenVisibleRectIsInNegativeSpace()
    {
        GridOverlayCalculator.Calculate(
            visibleLeft: -20, visibleTop: -5, visibleRight: 10, visibleBottom: 10, gridSize: 16,
            out float originX, out float originY, out int columnCount, out int rowCount);

        originX.ShouldBe(-32);
        originY.ShouldBe(-16);
    }

    [Fact]
    public void Calculate_ShouldReturnNoCells_WhenGridSizeIsZeroOrNegative()
    {
        GridOverlayCalculator.Calculate(
            visibleLeft: 0, visibleTop: 0, visibleRight: 100, visibleBottom: 100, gridSize: 0,
            out float originX, out float originY, out int columnCount, out int rowCount);

        columnCount.ShouldBe(0);
        rowCount.ShouldBe(0);
    }
}
