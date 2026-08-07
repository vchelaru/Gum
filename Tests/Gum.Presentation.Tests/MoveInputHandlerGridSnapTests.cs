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
/// world-space anchor position (AbsoluteX/AbsoluteY) is used so a parented, non-grid-aligned child
/// still lands visually on-grid.
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
            out float differenceToGridX, out float _);

        // World X = 5 + 20 = 25 -> snaps down to 16, a delta of -9.
        differenceToGridX.ShouldBe(-9);
    }
}
