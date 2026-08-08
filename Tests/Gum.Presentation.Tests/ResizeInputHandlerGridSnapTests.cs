using System.Numerics;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Handlers;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins ResizeInputHandler.GetDifferenceToGridForSize, the Width/Height half of resize grid-snap
/// (issue #4137, refined by #4380) — the X/Y half reuses MoveInputHandler.GetDifferenceToGrid,
/// already pinned by MoveInputHandlerGridSnapTests. Snap targets the dragged edge's absolute
/// (world-space) position directly - AbsoluteLeft/Right for width, AbsoluteTop/Bottom for height -
/// deriving the Width/Height delta from that, rather than rounding Width/Height in isolation.
/// Rounding Width/Height alone only lands on-grid when the opposite (anchor) edge already happens
/// to be grid-aligned; when it isn't (e.g. the object's position is off-grid), the dragged edge can
/// land off-grid even though the size itself is a multiple of the grid. Which edge is "dragged" vs
/// "anchor" is given by the sign of widthMultiplier/heightMultiplier from CalculateMultipliers
/// (negative = Left/Top dragged, positive = Right/Bottom dragged, zero = axis not resized this
/// gesture) - this is independent of XOrigin/YOrigin, since the multiplier system already keeps the
/// opposite edge fixed regardless of origin.
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
            widthMultiplier: 1, heightMultiplier: 1,
            out float differenceToGridWidth, out float differenceToGridHeight);

        differenceToGridWidth.ShouldBe(2); // anchor (left) at 0, right 30 -> 32 (nearest), not 16 (floor)
        differenceToGridHeight.ShouldBe(-2); // anchor (top) at 0, bottom 50 -> 48 (nearest)
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
            widthMultiplier: 1, heightMultiplier: 1,
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
            widthMultiplier: 1, heightMultiplier: 1,
            out float differenceToGridWidth, out float differenceToGridHeight);

        differenceToGridWidth.ShouldBe(0);
        differenceToGridHeight.ShouldBe(0);
    }

    [Fact]
    public void GetDifferenceToGridForSize_ShouldSkipAxis_WhenMultiplierIsZero()
    {
        // Dragging a handle that doesn't affect this axis (e.g. Top/Bottom only) must leave
        // Width untouched, even when the live/true size is off-grid - only the axis actually
        // being dragged this gesture should move.
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Width = 30,
            Height = 50,
            WidthUnits = DimensionUnitType.Absolute,
            HeightUnits = DimensionUnitType.Absolute
        };

        ResizeInputHandler.GetDifferenceToGridForSize(gue, gridSize: 16,
            grabStartSize: new Vector2(30, 50), trueSizeOffsetSinceGrab: Vector2.Zero,
            widthMultiplier: 0, heightMultiplier: 0,
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
            widthMultiplier: 1, heightMultiplier: 0,
            out float differenceToGridWidth, out float _);

        // Anchor (left) is at X=0; true right = 0 + 38 = 38 -> nearest grid line is 32.
        // Delta is relative to the LIVE Width (16).
        differenceToGridWidth.ShouldBe(16);
    }

    [Fact]
    public void GetDifferenceToGridForSize_ShouldSnapDraggedRightEdgeInWorldSpace_WhenAnchorLeftIsOffGrid()
    {
        // Left edge (anchor, fixed by a Right-side drag) sits at X=5, off the 16px grid. Rounding
        // Width alone (the old behavior) would snap the raw width (25 -> nearest multiple 32),
        // landing the dragged right edge at 5+32=37 - off grid. Snapping the right edge itself
        // (5+25=30 -> nearest grid line 32) instead derives a width of 27, landing the right edge
        // exactly on grid at 5+27=32.
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            X = 5,
            Width = 25,
            WidthUnits = DimensionUnitType.Absolute
        };

        ResizeInputHandler.GetDifferenceToGridForSize(gue, gridSize: 16,
            grabStartSize: new Vector2(25, 0), trueSizeOffsetSinceGrab: Vector2.Zero,
            widthMultiplier: 1, heightMultiplier: 0,
            out float differenceToGridWidth, out float _);

        differenceToGridWidth.ShouldBe(2); // 25 -> 27, so AbsoluteRight lands at 5+27=32
    }

    [Fact]
    public void GetDifferenceToGridForSize_ShouldSnapDraggedLeftEdgeInWorldSpace_WhenAnchorRightIsOffGrid()
    {
        // Right edge (anchor, fixed by a Left-side drag) sits at X+Width=15+25=40, off the 16px
        // grid. Rounding Width alone (25 -> nearest multiple 32) would land the dragged left edge
        // at 40-32=8 - off grid. Snapping the left edge itself (40-25=15 -> nearest grid line 16)
        // instead derives a width of 24, landing the left edge exactly on grid at 40-24=16.
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            X = 15,
            Width = 25,
            WidthUnits = DimensionUnitType.Absolute
        };

        ResizeInputHandler.GetDifferenceToGridForSize(gue, gridSize: 16,
            grabStartSize: new Vector2(25, 0), trueSizeOffsetSinceGrab: Vector2.Zero,
            widthMultiplier: -1, heightMultiplier: 0,
            out float differenceToGridWidth, out float _);

        differenceToGridWidth.ShouldBe(-1); // 25 -> 24, so AbsoluteLeft lands at 40-24=16
    }

    [Fact]
    public void GetDifferenceToGridForSize_ShouldSnapDraggedBottomEdgeInWorldSpace_WhenAnchorTopIsOffGrid()
    {
        // Mirrors the Right-edge width case, on the Y axis: Top edge (anchor) sits at Y=5, off
        // grid. Snapping the dragged bottom edge (5+25=30 -> 32) derives a height of 27.
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Y = 5,
            Height = 25,
            HeightUnits = DimensionUnitType.Absolute
        };

        ResizeInputHandler.GetDifferenceToGridForSize(gue, gridSize: 16,
            grabStartSize: new Vector2(0, 25), trueSizeOffsetSinceGrab: Vector2.Zero,
            widthMultiplier: 0, heightMultiplier: 1,
            out float _, out float differenceToGridHeight);

        differenceToGridHeight.ShouldBe(2); // 25 -> 27, so AbsoluteBottom lands at 5+27=32
    }
}
