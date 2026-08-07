using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Handlers;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins ResizeInputHandler.GetDifferenceToGridForSize, the local Width/Height half of resize
/// grid-snap (issue #4137) — the X/Y half reuses MoveInputHandler.GetDifferenceToGrid, already
/// pinned by MoveInputHandlerGridSnapTests.
/// </summary>
public class ResizeInputHandlerGridSnapTests
{
    [Fact]
    public void GetDifferenceToGridForSize_ShouldRoundToNearestGridLine_WhenPixelBasedAndOffGrid()
    {
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Width = 30,
            Height = 50,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute
        };

        ResizeInputHandler.GetDifferenceToGridForSize(gue, gridSize: 16,
            out float differenceToGridWidth, out float differenceToGridHeight);

        differenceToGridWidth.ShouldBe(2); // 30 -> 32 (nearest), not 16 (floor)
        differenceToGridHeight.ShouldBe(-2); // 50 -> 48 (nearest)
    }

    [Fact]
    public void GetDifferenceToGridForSize_ShouldReturnZero_WhenAlreadyOnGrid()
    {
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Width = 32,
            Height = 64,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute
        };

        ResizeInputHandler.GetDifferenceToGridForSize(gue, gridSize: 16,
            out float differenceToGridWidth, out float differenceToGridHeight);

        differenceToGridWidth.ShouldBe(0);
        differenceToGridHeight.ShouldBe(0);
    }

    [Fact]
    public void GetDifferenceToGridForSize_ShouldSkipAxis_WhenUnitsAreNotPixelBased()
    {
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Width = 30,
            Height = 50,
            WidthUnits = DimensionUnitType.PercentageOfParent,
            HeightUnits = DimensionUnitType.PercentageOfParent
        };

        ResizeInputHandler.GetDifferenceToGridForSize(gue, gridSize: 16,
            out float differenceToGridWidth, out float differenceToGridHeight);

        differenceToGridWidth.ShouldBe(0);
        differenceToGridHeight.ShouldBe(0);
    }
}
