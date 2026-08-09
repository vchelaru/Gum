using System;
using System.Collections.Generic;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using Gum.Wireframe.Editors;
using Gum.Wireframe.Editors.Handlers;
using Gum.Wireframe.Editors.Visuals;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// End-to-end regression for #4395's follow-up: GrabbedState used to capture an instance's grab-time
/// size from GraphicalUiElement.Width/Height, which is the RAW unit-specific stored value (e.g. a
/// Ratio weight like 1), not its rendered pixel size. ResizeInputHandler.ResolveResizeAxis mixes that
/// grab-time size directly against real cursor pixel deltas to decide when the dragged edge crosses
/// the anchor and should flip (see ResizeInputHandlerFlipTests) - so once the raw stored number was
/// smaller than the pixel distance dragged, it flipped immediately, even though the object's actual
/// rendered size was nowhere near zero. Reported as: shrinking a Ratio-Width rectangle below a raw
/// Width of ~1 made it "increase" and the object jump left, out of its container.
/// </summary>
public class ResizeInputHandlerNonPixelUnitTests
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
    public void OnDrag_ShrinkingRatioWidthRectangleFarBelowItsRawRatioValue_ShouldNotFlip()
    {
        // Container 400px wide, two Ratio=1 children (200px each) - matches the RectScreen-style
        // repro. RectangleInstance's raw Width (1) is far smaller than the real cursor distance
        // being dragged (190px), which is exactly the scenario that used to falsely trigger a flip.
        ScreenSave screen = new ScreenSave { Name = "RatioScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);

        InstanceSave firstInstance = new InstanceSave { Name = "RectangleInstance", BaseType = "Container", ParentContainer = screen };
        InstanceSave secondInstance = new InstanceSave { Name = "RectangleInstance1", BaseType = "Container", ParentContainer = screen };
        screen.Instances.Add(firstInstance);
        screen.Instances.Add(secondInstance);

        StandardElementSave containerStandard = new StandardElementSave { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(containerStandard);
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;

        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.Width", Value = 1f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.WidthUnits", Value = DimensionUnitType.Ratio, Type = "DimensionUnitType", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance1.Width", Value = 1f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance1.WidthUnits", Value = DimensionUnitType.Ratio, Type = "DimensionUnitType", SetsValue = true });

        GraphicalUiElement rootGue = new GraphicalUiElement(new InvisibleRenderable());
        GraphicalUiElement firstGue = new GraphicalUiElement(new InvisibleRenderable())
            { Name = "RectangleInstance", Width = 1f, WidthUnits = DimensionUnitType.Ratio, Tag = firstInstance };
        GraphicalUiElement secondGue = new GraphicalUiElement(new InvisibleRenderable())
            { Name = "RectangleInstance1", Width = 1f, WidthUnits = DimensionUnitType.Ratio, Tag = secondInstance };
        firstGue.Parent = rootGue;
        secondGue.Parent = rootGue;
        // Sets the rendered pixel size directly, bypassing the real Ratio layout pass - the same
        // controlled setup ElementCommandsTests uses for its Ratio-resize tests.
        ((IPositionedSizedObject)firstGue).Width = 200f;
        ((IPositionedSizedObject)secondGue).Width = 200f;

        Mock<ISelectedState> selectedState = new();
        selectedState.SetupGet(x => x.SelectedElement).Returns(screen);
        selectedState.SetupGet(x => x.SelectedStateSave).Returns(defaultState);
        selectedState.SetupGet(x => x.CustomCurrentStateSave).Returns((StateSave)null);
        selectedState.SetupGet(x => x.SelectedInstances).Returns(new[] { firstInstance });
        selectedState.SetupGet(x => x.SelectedInstance).Returns(firstInstance);
        selectedState.Setup(x => x.GetTopLevelElementStack())
            .Returns(new List<ElementWithState> { new ElementWithState(screen) });

        Mock<IWireframeObjectManager> wireframeObjectManager = new();
        wireframeObjectManager.Setup(x => x.GetRepresentation(firstInstance, It.IsAny<List<ElementWithState>>())).Returns(firstGue);
        wireframeObjectManager.SetupGet(x => x.RootGue).Returns(rootGue);

        ElementCommands elementCommands = new ElementCommands(
            selectedState.Object,
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<IVariableInCategoryPropagationLogic>(),
            wireframeObjectManager.Object,
            Mock.Of<IPluginManager>(),
            Mock.Of<IProjectManager>(),
            Mock.Of<IProjectState>());

        Mock<IResizeHandlesVisual> handlesVisual = new();
        handlesVisual.SetupGet(x => x.Visible).Returns(true);
        handlesVisual.Setup(x => x.GetSideOver(It.IsAny<float>(), It.IsAny<float>())).Returns(ResizeSide.Right);

        Mock<ISelectionManager> selectionManager = new();
        selectionManager.SetupGet(x => x.HasSelection).Returns(true);
        selectionManager.SetupGet(x => x.SelectedGues).Returns(new List<GraphicalUiElement>());

        FakeCursorState cursor = new() { X = 100, Y = 100, PrimaryPush = true };

        EditorContext context = EditorContextTestHelper.Create(
            selectedState: selectedState.Object,
            selectionManager: selectionManager.Object,
            wireframeObjectManager: wireframeObjectManager.Object,
            cursor: cursor,
            elementCommands: elementCommands);

        ResizeInputHandler sut = new ResizeInputHandler(context, handlesVisual.Object);

        sut.UpdateHover(0f, 0f);
        context.GrabbedState.HandlePush(); // captures grab-time InstanceSizes - must use AbsoluteWidth
        sut.HandlePush(0f, 0f);

        cursor.PrimaryDown = true;
        cursor.PrimaryPush = false;

        // Drag the Right handle left by 190px in one tick - far more than the raw Ratio value (1),
        // but well short of the real 200px pixel width reaching zero.
        cursor.X = 70;
        cursor.XChange = -190;
        cursor.YChange = 0;
        sut.HandleDrag();

        // No flip: a Right-handle drag on a Left-origin object never moves X. Under the bug, the
        // confused grab-time size (1, the raw ratio) was exceeded almost immediately by the real
        // pixel drag distance, triggering a flip that shifted X left even though X should be static
        // for a Right-handle drag - exactly the reported "object moves left, out of its container."
        firstGue.X.ShouldBe(0f, tolerance: 0.01f);

        // The raw Ratio value shrank towards (200px - 190px = 10px of) the original 200px, i.e. it
        // decreased proportionally - not increased, which was the other half of the reported symptom
        // ("instead of the ratio continuing to go down... it seems to increase").
        float newRatioValue = (float)defaultState.GetValue("RectangleInstance.Width");
        newRatioValue.ShouldBeLessThan(1f);
        newRatioValue.ShouldBeGreaterThan(0f);
        newRatioValue.ShouldBe(1f * (10f / 200f), tolerance: 0.01f);
    }
}
