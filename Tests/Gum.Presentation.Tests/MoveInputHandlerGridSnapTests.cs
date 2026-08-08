using System.Numerics;
using Gum.DataTypes;
using Gum.Converters;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Handlers;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins MoveInputHandler.GetDifferenceToGrid, the pure math behind grid-snap-on-move: which axes
/// participate (pixel-based units only, per the maintainer's scoping on issue #4137) and how the
/// world-space anchor position is used so a parented, non-grid-aligned child still lands visually
/// on-grid.
///
/// Snap is computed from grabStartLocal + trueOffsetSinceGrab (the object's position with NO
/// snapping ever applied), not from the live gue.X/Y - reading the live value would read back
/// whatever the previous frame's snap already wrote, so a small drag gets silently reverted every
/// frame instead of accumulating (issue #4137 review: "movement fights you").
/// </summary>
public class MoveInputHandlerGridSnapTests
{
    [Fact]
    public void GetDifferenceToGrid_ShouldReturnOffsetToLowerGridLine_WhenPixelBasedAndOffGrid()
    {
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            X = 20,
            Y = 5,
            XUnits = GeneralUnitType.PixelsFromSmall,
            YUnits = GeneralUnitType.PixelsFromSmall
        };

        MoveInputHandler.GetDifferenceToGrid(gue, gridSize: 16,
            grabStartLocal: new Vector2(20, 5), trueOffsetSinceGrab: Vector2.Zero,
            out float differenceToGridX, out float differenceToGridY);

        differenceToGridX.ShouldBe(-4); // 20 -> 16
        differenceToGridY.ShouldBe(-5); // 5 -> 0
    }

    [Fact]
    public void GetDifferenceToGrid_ShouldReturnZero_WhenAlreadyOnGrid()
    {
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            X = 32,
            Y = 48,
            XUnits = GeneralUnitType.PixelsFromSmall,
            YUnits = GeneralUnitType.PixelsFromSmall
        };

        MoveInputHandler.GetDifferenceToGrid(gue, gridSize: 16,
            grabStartLocal: new Vector2(32, 48), trueOffsetSinceGrab: Vector2.Zero,
            out float differenceToGridX, out float differenceToGridY);

        differenceToGridX.ShouldBe(0);
        differenceToGridY.ShouldBe(0);
    }

    [Fact]
    public void GetDifferenceToGrid_ShouldSkipAxis_WhenUnitsAreNotPixelBased()
    {
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            X = 20,
            Y = 5,
            XUnits = GeneralUnitType.Percentage,
            YUnits = GeneralUnitType.Percentage
        };

        MoveInputHandler.GetDifferenceToGrid(gue, gridSize: 16,
            grabStartLocal: new Vector2(20, 5), trueOffsetSinceGrab: Vector2.Zero,
            out float differenceToGridX, out float differenceToGridY);

        differenceToGridX.ShouldBe(0);
        differenceToGridY.ShouldBe(0);
    }

    [Fact]
    public void GetDifferenceToGrid_ShouldSnapWorldSpaceAnchor_WhenParentIsNotGridAligned()
    {
        // Parent sits at an off-grid world position; the child's own local X (20) happens to land
        // it exactly on a grid line locally, but the maintainer's scoping requires the snap to be
        // computed in world space, so the child should still be nudged to compensate for the
        // parent's offset and land visually on-grid.
        GraphicalUiElement parentGue = new GraphicalUiElement(new InvisibleRenderable())
        {
            X = 5,
            XUnits = GeneralUnitType.PixelsFromSmall
        };
        GraphicalUiElement childGue = new GraphicalUiElement(new InvisibleRenderable())
        {
            X = 20,
            XUnits = GeneralUnitType.PixelsFromSmall,
            Parent = parentGue
        };

        MoveInputHandler.GetDifferenceToGrid(childGue, gridSize: 16,
            grabStartLocal: new Vector2(20, 0), trueOffsetSinceGrab: Vector2.Zero,
            out float differenceToGridX, out float _);

        // World X = 5 + 20 = 25 -> snaps down to 16, a delta of -9.
        differenceToGridX.ShouldBe(-9);
    }

    [Fact]
    public void GetDifferenceToGrid_ShouldSnapFromTrueOffset_NotFromLiveValueAlreadySnappedThisDrag()
    {
        // Simulates the bug: the object was already snapped to X=16 earlier this same drag (its
        // live X no longer reflects raw cursor movement), but the user has since dragged further
        // (trueOffsetSinceGrab keeps accumulating the real, unsnapped movement). The next snap
        // must target the grid line nearest the TRUE position (20 + 10 = 30 -> floors to 16... use
        // a value that crosses into the next cell to prove live X is ignored).
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            X = 16, // already snapped from an earlier frame this drag
            XUnits = GeneralUnitType.PixelsFromSmall,
            YUnits = GeneralUnitType.PixelsFromSmall
        };

        // Grabbed at local X=20; true (unsnapped) cursor movement since grab totals +18 -> true
        // position is 38, which is in the next grid cell up from where the live X (16) sits.
        MoveInputHandler.GetDifferenceToGrid(gue, gridSize: 16,
            grabStartLocal: new Vector2(20, 0), trueOffsetSinceGrab: new Vector2(18, 0),
            out float differenceToGridX, out float _);

        // True world X = 38 -> snaps down to 32. Delta is relative to the LIVE X (16), i.e. 32-16=16,
        // not relative to the true value - GetDifferenceToGrid always returns "how far to move the
        // live object," which callers apply via ModifyVariable.
        differenceToGridX.ShouldBe(16);
    }
}
