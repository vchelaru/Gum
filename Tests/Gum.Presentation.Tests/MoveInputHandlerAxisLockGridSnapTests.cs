using System.Collections.Generic;
using System.Linq;
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
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// #5484: a Shift (axis-locked) drag moves along the dominant axis only, leaving the other axis at
/// its grab position in both the state and the visual, with or without Snap to Grid.
/// </summary>
public class MoveInputHandlerAxisLockGridSnapTests
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

    private class Harness
    {
        public MoveInputHandler Handler = null!;
        public FakeCursorState Cursor = null!;
        public GraphicalUiElement Gue = null!;
        public StateSave State = null!;
        public string VariablePrefix = "";

        public float StateX => (float)State.GetValue(VariablePrefix + "X")!;
        public float StateY => (float)State.GetValue(VariablePrefix + "Y")!;

        public void Push()
        {
            Handler.HandlePush(0f, 0f).ShouldBeTrue();
            Cursor.PrimaryDown = true;
            Cursor.PrimaryPush = false;
        }

        public void Drag(float x, float y)
        {
            Cursor.X += x;
            Cursor.Y += y;
            Cursor.XChange = x;
            Cursor.YChange = y;
            Handler.HandleDrag();
        }
    }

    /// <summary>
    /// Builds a MoveInputHandler over real ElementCommands with Shift held. When
    /// <paramref name="selectInstance"/> is false, the component itself is selected and dragged.
    /// </summary>
    private static Harness CreateHarness(bool selectInstance, bool snapToGrid, int gridSize)
    {
        ComponentSave component = new ComponentSave { Name = "AxisLockComponent" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);

        InstanceSave instance = new InstanceSave { Name = "ContainerInstance", BaseType = "Container", ParentContainer = component };
        component.Instances.Add(instance);

        StandardElementSave containerStandard = new StandardElementSave { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(containerStandard);
        project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = project;

        string prefix = selectInstance ? "ContainerInstance." : "";
        defaultState.Variables.Add(new VariableSave { Name = prefix + "X", Value = 0f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = prefix + "Y", Value = 0f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = prefix + "XUnits", Value = PositionUnitType.PixelsFromLeft, Type = "PositionUnitType", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = prefix + "YUnits", Value = PositionUnitType.PixelsFromTop, Type = "PositionUnitType", SetsValue = true });

        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
            { Name = selectInstance ? "ContainerInstance" : "AxisLockComponent", X = 0, Y = 0 };

        Mock<ISelectedState> selectedState = new();
        selectedState.SetupGet(x => x.SelectedElement).Returns(component);
        selectedState.SetupGet(x => x.SelectedStateSave).Returns(defaultState);
        selectedState.SetupGet(x => x.CustomCurrentStateSave).Returns((StateSave?)null);
        selectedState.Setup(x => x.GetTopLevelElementStack())
            .Returns(new List<ElementWithState> { new ElementWithState(component) });

        Mock<IWireframeObjectManager> wireframeObjectManager = new();
        wireframeObjectManager.Setup(x => x.GetSelectedRepresentation()).Returns(gue);

        Mock<ISelectionManager> selectionManager = new();
        selectionManager.SetupGet(x => x.HasSelection).Returns(true);
        selectionManager.SetupGet(x => x.IsOverBody).Returns(true);
        selectionManager.SetupGet(x => x.IsOverSelectedBody).Returns(true);
        selectionManager.SetupGet(x => x.SelectedGues).Returns(new List<GraphicalUiElement> { gue });
        selectionManager.SetupGet(x => x.SelectedGue).Returns(gue);

        if (selectInstance)
        {
            gue.Tag = instance;
            selectedState.SetupGet(x => x.SelectedInstances).Returns(new[] { instance });
            selectedState.SetupGet(x => x.SelectedInstance).Returns(instance);
            wireframeObjectManager.Setup(x => x.GetRepresentation(instance, It.IsAny<List<ElementWithState>?>())).Returns(gue);
        }
        else
        {
            gue.Tag = component;
            selectedState.SetupGet(x => x.SelectedInstances).Returns(Enumerable.Empty<InstanceSave>());
            selectedState.SetupGet(x => x.SelectedComponent).Returns(component);
            wireframeObjectManager.Setup(x => x.GetRepresentation(component)).Returns(gue);
        }

        ElementCommands elementCommands = new ElementCommands(
            selectedState.Object,
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<IVariableInCategoryPropagationLogic>(),
            wireframeObjectManager.Object,
            Mock.Of<IPluginManager>(),
            Mock.Of<IProjectManager>(),
            Mock.Of<IProjectState>());

        KeyCombination shift = KeyCombination.Shift();
        Mock<IHotkeyManager> hotkeyManager = new();
        hotkeyManager.SetupGet(h => h.MultiSelect).Returns(shift);
        hotkeyManager.SetupGet(h => h.LockMovementToAxis).Returns(shift);
        hotkeyManager.Setup(h => h.IsPressedInControl(shift)).Returns(true);

        FakeCursorState cursor = new() { X = 100, Y = 100, PrimaryPush = true };

        EditorContext context = EditorContextTestHelper.Create(
            selectedState: selectedState.Object,
            selectionManager: selectionManager.Object,
            wireframeObjectManager: wireframeObjectManager.Object,
            hotkeyManager: hotkeyManager.Object,
            cursor: cursor,
            elementCommands: elementCommands);
        context.SnapToGrid = snapToGrid;
        context.GridSize = gridSize;
        context.GrabbedState.HandlePush();

        return new Harness
        {
            Handler = new MoveInputHandler(context),
            Cursor = cursor,
            Gue = gue,
            State = defaultState,
            VariablePrefix = prefix
        };
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Drag_WithAxisLockAndGridSnap_ShouldKeepLockedAxisAtGrabPositionAndSnapTheOther(bool selectInstance)
    {
        Harness harness = CreateHarness(selectInstance, snapToGrid: true, gridSize: 16);
        harness.Push();

        // Mostly-horizontal drag, 5 frames of (20, 7): X dominates, so Y is the locked axis. The
        // raw Y crosses the 16px grid line on frame 3.
        for (int i = 0; i < 5; i++)
        {
            harness.Drag(20, 7);

            harness.Gue.Y.ShouldBe(0f, $"visual Y after frame {i + 1}");
            harness.StateY.ShouldBe(0f, $"state Y after frame {i + 1}");
        }

        // 100px of raw X movement lands on the grid line at or below it.
        harness.Gue.X.ShouldBe(96f);
        harness.StateX.ShouldBe(96f);
    }

    [Fact]
    public void Drag_WithAxisLockAndNoGridSnap_ShouldNotMoveLockedAxisInState()
    {
        Harness harness = CreateHarness(selectInstance: true, snapToGrid: false, gridSize: 16);
        harness.Push();

        for (int i = 0; i < 3; i++)
        {
            harness.Drag(20, 7);

            harness.Gue.Y.ShouldBe(0f, $"visual Y after frame {i + 1}");
            harness.StateY.ShouldBe(0f, $"state Y after frame {i + 1}");
        }

        harness.Gue.X.ShouldBe(60f);
        harness.StateX.ShouldBe(60f);
    }

    [Fact]
    public void Release_WithAxisLock_ShouldReturnAxisThatStoppedDominatingAfterTheLastDragFrame()
    {
        Harness harness = CreateHarness(selectInstance: true, snapToGrid: false, gridSize: 16);
        harness.Push();
        harness.Drag(20, 0);
        harness.Drag(20, 0);

        // The cursor moves without a drag frame (for example off the body), making Y dominant.
        harness.Cursor.Y += 60;
        harness.Cursor.PrimaryDown = false;
        harness.Handler.HandleRelease();

        harness.Gue.X.ShouldBe(0f);
        harness.StateX.ShouldBe(0f);
    }

    [Fact]
    public void Drag_WithAxisLockAndGridSnap_ShouldFollowTheDominantAxisWhenItSwitches()
    {
        Harness harness = CreateHarness(selectInstance: true, snapToGrid: true, gridSize: 16);
        harness.Push();

        // Cursor offset (40, 0): X dominant, true X 40 snaps to 32.
        harness.Drag(20, 0);
        harness.Drag(20, 0);
        harness.Gue.X.ShouldBe(32f);
        harness.StateX.ShouldBe(32f);

        // Cursor offset (40, 60): Y dominant, so X returns to where it was grabbed and Y snaps 60 to 48.
        harness.Drag(0, 60);
        harness.Gue.X.ShouldBe(0f);
        harness.StateX.ShouldBe(0f);
        harness.Gue.Y.ShouldBe(48f);
        harness.StateY.ShouldBe(48f);

        // Cursor offset (80, 60): X dominant again, so X picks up all 80px and Y returns to 0.
        harness.Drag(40, 0);
        harness.Gue.X.ShouldBe(80f);
        harness.StateX.ShouldBe(80f);
        harness.Gue.Y.ShouldBe(0f);
        harness.StateY.ShouldBe(0f);
    }
}
