using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using Gum.Wireframe.Editors.Handlers;
using Gum.Wireframe.Editors.Visuals;
using Moq;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using Shouldly;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;

namespace Gum.Presentation.Tests;

/// <summary>
/// End-to-end coverage (real GraphicalUiElement, driven through HandlePush/HandleDrag) proving the
/// #4385 anchor-flip logic composes correctly with a rotated object. Width/Height are resolved in
/// LOCAL, pre-rotation space (see ResizeInputHandlerFlipTests) and only the resulting X/Y position
/// delta is rotated - via the pre-existing, unchanged MathFunctions.RotateVector step - into world
/// space.
/// </summary>
public class ResizeInputHandlerFlipRotationTests
{
    private class FakeCursorState : IGumCursorState
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float XChange { get; set; }
        public float YChange { get; set; }
        public bool PrimaryDown { get; set; }
        public bool PrimaryPush { get; set; }
        public bool PrimaryClick { get; set; }
        public bool IsInWindow { get; set; } = true;
        public bool PrimaryDoubleClick { get; set; }
        public bool SecondaryPush { get; set; }
        public bool PrimaryDownIgnoringIsInWindow { get; set; }
        public void SetCursor(GumCursorKind kind) { }
    }

    /// <summary>
    /// Builds a ResizeInputHandler wired to a real, grabbed GraphicalUiElement (whole-component
    /// selection, no instance) with the given grab-time X/Y/Width/Height/Rotation, hovering/grabbed
    /// on <paramref name="sideGrabbed"/>. The returned cursor starts at the push position
    /// (100, 100); the caller sets X/XChange/YChange per tick and calls HandleDrag().
    /// </summary>
    private static (ResizeInputHandler sut, FakeCursorState cursor, GraphicalUiElement representation) CreateGrabbedSut(
        float x, float y, float width, float height, float rotation, ResizeSide sideGrabbed)
    {
        var representation = new GraphicalUiElement();
        // The X/Y setters no-op without a contained renderable (see GraphicalUiElement.X's
        // `mContainedObjectAsIpso != null` guard) - attach one so position assignments actually
        // stick, matching how every real runtime (and generated code) constructs a GUE.
        representation.SetContainedObject(new InvisibleRenderable());
        representation.X = x;
        representation.Y = y;
        representation.Width = width;
        representation.Height = height;
        representation.Rotation = rotation;

        var handlesVisual = new Mock<IResizeHandlesVisual>();
        handlesVisual.SetupGet(h => h.Visible).Returns(true);
        handlesVisual.Setup(h => h.GetSideOver(It.IsAny<float>(), It.IsAny<float>())).Returns(sideGrabbed);

        var wireframeObjectManager = new Mock<IWireframeObjectManager>();
        wireframeObjectManager.Setup(w => w.GetRepresentation(It.IsAny<ElementSave>())).Returns(representation);

        var elementCommands = new Mock<IElementCommands>();
        elementCommands
            .Setup(e => e.GetCurrentValueForVariable(It.IsAny<string>(), It.IsAny<InstanceSave>()))
            .Returns((object?)null); // no XOrigin/YOrigin variable set -> defaults to Left/Top
        elementCommands
            .Setup(e => e.ModifyVariable(It.IsAny<string>(), It.IsAny<float>(), It.IsAny<ElementSave>()))
            .Returns((string variableName, float amount, ElementSave elementSave) =>
            {
                switch (variableName)
                {
                    case "X": representation.X += amount; break;
                    case "Y": representation.Y += amount; break;
                    case "Width": representation.Width += amount; break;
                    case "Height": representation.Height += amount; break;
                }
                return 0f;
            });

        var selectedState = new Mock<ISelectedState>();
        var selectedElement = new ScreenSave();
        selectedState.SetupGet(s => s.SelectedElement).Returns(selectedElement);
        selectedState.SetupGet(s => s.SelectedStateSave).Returns(new StateSave());
        selectedState.SetupGet(s => s.SelectedInstances).Returns(Array.Empty<InstanceSave>());
        selectedState.SetupGet(s => s.SelectedInstance).Returns((InstanceSave?)null);
        // Moq's loose mocks intercept default interface methods too (returning null instead of
        // running ISelectedState.GetTopLevelElementStack()'s real body), so this needs an explicit setup.
        selectedState.Setup(s => s.GetTopLevelElementStack())
            .Returns(new List<ElementWithState> { new ElementWithState(selectedElement) });

        var selectionManager = new Mock<ISelectionManager>();
        selectionManager.SetupGet(s => s.HasSelection).Returns(true);
        selectionManager.SetupGet(s => s.SelectedGues).Returns(new List<GraphicalUiElement>());

        var cursor = new FakeCursorState { X = 100, Y = 100, PrimaryPush = true };

        var context = EditorContextTestHelper.Create(
            selectedState: selectedState.Object,
            selectionManager: selectionManager.Object,
            wireframeObjectManager: wireframeObjectManager.Object,
            cursor: cursor,
            elementCommands: elementCommands.Object);

        var sut = new ResizeInputHandler(context, handlesVisual.Object);

        sut.UpdateHover(0f, 0f); // sets _sideOver (cursor.PrimaryPush is true)
        context.GrabbedState.HandlePush(); // captures grab-time ComponentPosition/Size
        sut.HandlePush(0f, 0f); // claims the gesture; _sideGrabbed = sideGrabbed

        cursor.PrimaryDown = true;
        cursor.PrimaryPush = false;

        return (sut, cursor, representation);
    }

    [Fact]
    public void OnDrag_ShouldFlipWidthAndRotatePosition_WhenRightHandleDraggedPastAnchorOnRotatedObject()
    {
        // Arrange - Left-origin, X=5, Width=20 (so the Left edge, at local X=5, is the anchor for
        // a Right-handle drag), rotated 90 degrees.
        var (sut, cursor, representation) = CreateGrabbedSut(
            x: 5, y: 0, width: 20, height: 10, rotation: 90, sideGrabbed: ResizeSide.Right);

        // Act - drag the Right handle left by a total of 30 in one tick - 10 further than the Left
        // anchor (at local X=5, Width=20 means the anchor is exactly at the drag origin's Left edge).
        cursor.X = 70; // > 6px from the push position, satisfies HasMovedEnough
        cursor.XChange = -30;
        cursor.YChange = 0;
        sut.HandleDrag();

        // Assert - Width flips exactly as in the unrotated case (rotation never touches
        // Width/Height - they're local, pre-rotation dimensions): 20 -> 10.
        representation.Width.ShouldBe(10);
        representation.Height.ShouldBe(10); // untouched - Right handle doesn't affect Height

        // The pre-rotation local position delta is (-10, 0) (X: 5 -> -5, matching the unrotated
        // flip case). Rotating that by the object's own 90-degree rotation through the SAME,
        // unchanged MathFunctions.RotateVector pipeline this handler already used before #4385
        // gives the expected world-space delta - computed here the same way the SUT computes it,
        // to independently confirm composition rather than duplicating its flip math.
        var expectedRepositionLocal = new System.Numerics.Vector2(-10, 0);
        expectedRepositionLocal.Y *= -1;
        MathFunctions.RotateVector(ref expectedRepositionLocal, MathHelper.ToRadians(90));
        expectedRepositionLocal.Y *= -1;

        representation.X.ShouldBe(5 + expectedRepositionLocal.X, tolerance: 0.01);
        representation.Y.ShouldBe(0 + expectedRepositionLocal.Y, tolerance: 0.01);
    }

    [Fact]
    public void OnDrag_ShouldNotDriftAcrossTicks_WhenLeftHandleDraggedOnRotatedObject()
    {
        // Regression for the position "shift" the user found manually: a handle that moves BOTH
        // position and size (Left/Top - unlike Right/Bottom, whose position never moves on a
        // Left/Top-origin object) must accumulate correctly across MULTIPLE drag ticks on a rotated
        // object. A single big tick was already covered above and was never broken - the bug only
        // shows up once representation.X/Y (which get overwritten with a ROTATED, world-space delta
        // each tick) get read back as if they were still in the LOCAL, pre-rotation frame that
        // grabStartPositionAxis/trueSizeOffsetSinceGrabAxis are computed in.
        var (sut, cursor, representation) = CreateGrabbedSut(
            x: 5, y: 0, width: 20, height: 10, rotation: 90, sideGrabbed: ResizeSide.Left);

        // Two small ticks, same direction, each moving the cursor right by 3 (shrinking the object
        // from the Left, well short of crossing the anchor at local X=25).
        cursor.X = 110;
        cursor.XChange = 3;
        cursor.YChange = 0;
        sut.HandleDrag();

        cursor.X = 120;
        cursor.XChange = 3;
        cursor.YChange = 0;
        sut.HandleDrag();

        // Local (pre-rotation) X should move by the full 6px total (Left-origin, Left handle keeps
        // the Right edge fixed) and Width should shrink by 6, matching one big 6px tick exactly -
        // this must hold regardless of how the 6px was split across ticks.
        var expectedRepositionLocal = new System.Numerics.Vector2(6, 0);
        expectedRepositionLocal.Y *= -1;
        MathFunctions.RotateVector(ref expectedRepositionLocal, MathHelper.ToRadians(90));
        expectedRepositionLocal.Y *= -1;

        representation.X.ShouldBe(5 + expectedRepositionLocal.X, tolerance: 0.01);
        representation.Y.ShouldBe(0 + expectedRepositionLocal.Y, tolerance: 0.01);
        representation.Width.ShouldBe(14, tolerance: 0.01); // 20 - 6
    }
}
