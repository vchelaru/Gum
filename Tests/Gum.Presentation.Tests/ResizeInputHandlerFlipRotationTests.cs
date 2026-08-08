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
/// space. This test drags a Right handle far enough to cross the anchor on a 90-degree-rotated
/// object, and checks both that Width still flips correctly (rotation-agnostic) AND that the
/// resulting position delta lands where the (unchanged) rotation math says it should.
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

    [Fact]
    public void OnDrag_ShouldFlipWidthAndRotatePosition_WhenRightHandleDraggedPastAnchorOnRotatedObject()
    {
        // Arrange - a real, 90-degree-rotated GraphicalUiElement: Left-origin, X=5, Width=20 (so
        // the Left edge, at local X=5, is the anchor for a Right-handle drag).
        var representation = new GraphicalUiElement();
        // The X/Y setters no-op without a contained renderable (see GraphicalUiElement.X's
        // `mContainedObjectAsIpso != null` guard) - attach one so position assignments actually
        // stick, matching how every real runtime (and generated code) constructs a GUE.
        representation.SetContainedObject(new InvisibleRenderable());
        representation.X = 5;
        representation.Y = 0;
        representation.Width = 20;
        representation.Height = 10;
        representation.Rotation = 90;

        var handlesVisual = new Mock<IResizeHandlesVisual>();
        handlesVisual.SetupGet(h => h.Visible).Returns(true);
        handlesVisual.Setup(h => h.GetSideOver(It.IsAny<float>(), It.IsAny<float>())).Returns(ResizeSide.Right);

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

        // Act
        sut.UpdateHover(0f, 0f); // sets _sideOver = Right (cursor.PrimaryPush is true)

        context.GrabbedState.HandlePush(); // captures grab-time ComponentPosition/Size (X=5, Width=20)
        sut.HandlePush(0f, 0f); // claims the gesture; _sideGrabbed = Right

        // Drag the Right handle left by a total of 30 - 10 further than the Left anchor (at
        // local X=5, Width=20 means the anchor is exactly at the drag origin's Left edge).
        cursor.PrimaryDown = true;
        cursor.PrimaryPush = false;
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

        context.HasChangedAnythingSinceLastPush.ShouldBeTrue();
    }
}
