using Gum.Wireframe.Editors;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins GridSnapper's floor-based snap math, including negative-coordinate handling.
/// </summary>
public class GridSnapperTests
{
    [Fact]
    public void Snap_ShouldReturnLowerGridLine_WhenValueIsBetweenTwoGridLines()
    {
        float result = GridSnapper.Snap(value: 20, gridSize: 16);

        result.ShouldBe(16);
    }

    [Fact]
    public void Snap_ShouldReturnSameValue_WhenValueIsAlreadyOnGridLine()
    {
        float result = GridSnapper.Snap(value: 32, gridSize: 16);

        result.ShouldBe(32);
    }

    [Fact]
    public void Snap_ShouldFloorTowardNegativeInfinity_WhenValueIsNegative()
    {
        // -5 sits between the -16 and 0 grid lines; floor-based snapping picks -16, not 0.
        float result = GridSnapper.Snap(value: -5, gridSize: 16);

        result.ShouldBe(-16);
    }

    [Fact]
    public void Snap_ShouldReturnSameValue_WhenValueIsNegativeAndAlreadyOnGridLine()
    {
        float result = GridSnapper.Snap(value: -32, gridSize: 16);

        result.ShouldBe(-32);
    }

    [Fact]
    public void Snap_ShouldReturnZero_WhenValueIsZero()
    {
        float result = GridSnapper.Snap(value: 0, gridSize: 16);

        result.ShouldBe(0);
    }

    [Fact]
    public void Snap_ShouldReturnOriginalValue_WhenGridSizeIsZeroOrNegative()
    {
        float result = GridSnapper.Snap(value: 23, gridSize: 0);

        result.ShouldBe(23);
    }
}
