using System.Numerics;
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
/// pinned by MoveInputHandlerGridSnapTests. Snap is computed from grabStartSize + trueSizeOffset
/// (the size with no snapping ever applied), not from the live gue.Width/Height, for the same
/// "movement fights you" reason documented on GetDifferenceToGrid.
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
            grabStartSize: new Vector2(30, 50), trueSizeOffsetSinceGrab: Vector2.Zero,
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
            grabStartSize: new Vector2(32, 64), trueSizeOffsetSinceGrab: Vector2.Zero,
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
            grabStartSize: new Vector2(30, 50), trueSizeOffsetSinceGrab: Vector2.Zero,
            out float differenceToGridWidth, out float differenceToGridHeight);

        differenceToGridWidth.ShouldBe(0);
        differenceToGridHeight.ShouldBe(0);
    }

    [Fact]
    public void GetDifferenceToGridForSize_ShouldSnapFromTrueOffset_NotFromLiveValueAlreadySnappedThisDrag()
    {
        // Live Width (16) was already snapped earlier this drag, but the true (unsnapped) size has
        // since grown further via trueSizeOffsetSinceGrab, crossing into the next grid cell.
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Width = 16,
            WidthUnits = DimensionUnitType.Absolute
        };

        // Grabbed at Width=20; true growth since grab totals +18 -> true width is 38.
        ResizeInputHandler.GetDifferenceToGridForSize(gue, gridSize: 16,
            grabStartSize: new Vector2(20, 0), trueSizeOffsetSinceGrab: new Vector2(18, 0),
            out float differenceToGridWidth, out float _);

        // True width 38 -> nearest grid line is 32. Delta is relative to the LIVE Width (16).
        differenceToGridWidth.ShouldBe(16);
    }
}
