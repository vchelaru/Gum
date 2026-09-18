using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.Numerics;

namespace Gum.Presentation.Tests.Managers;

/// <summary>
/// Covers #4834: dropping an element from the tree view onto the canvas should attach the new
/// instance to the currently-selected instance only when the drop point BOTH hit-tests onto an
/// instance (the <c>instanceUnderCursor</c> caller-supplied argument) AND that instance is the
/// current selection — never on hit-test or selection alone. Also covers the follow-up: when the
/// new instance IS parented, its X/Y must be computed relative to the parent's own absolute
/// position (which isn't necessarily the canvas origin), not the world drop position as-is.
/// </summary>
public class DragDropManagerWireframeDropParentingTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly DragDropManager _dragDropManager;

    private PositionUnitType _newInstanceXUnits = PositionUnitType.PixelsFromLeft;
    private PositionUnitType _newInstanceYUnits = PositionUnitType.PixelsFromTop;

    public DragDropManagerWireframeDropParentingTests()
    {
        _mocker = new AutoMocker();
        _dragDropManager = _mocker.CreateInstance<DragDropManager>();

        _mocker.GetMock<ICircularReferenceManager>()
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        _mocker.GetMock<IUndoManager>()
            .Setup(x => x.RequestLock())
            .Returns((UndoLock)null!);

        _mocker.GetMock<IElementCommands>()
            .Setup(x => x.GetUniqueNameForNewInstance(It.IsAny<ElementSave>(), It.IsAny<ElementSave>()))
            .Returns("NewInstance");

        // SetInstanceToPosition (called after AddInstance in production) reads the project's
        // default canvas size unconditionally, even when no Component is selected.
        _mocker.GetMock<IProjectState>()
            .Setup(x => x.GumProjectSave)
            .Returns(new GumProjectSave { DefaultCanvasWidth = 800, DefaultCanvasHeight = 600 });

        // Stand in for the real AddInstance: append the instance and write the bare Parent name to
        // DefaultState, matching the production command exactly (including its DefaultState-only write).
        _mocker.GetMock<IElementCommands>()
            .Setup(x => x.AddInstance(
                It.IsAny<ElementSave>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()))
            .Returns((ElementSave element, string name, string? type, string? parentName, int? desiredIndex) =>
            {
                InstanceSave inst = new InstanceSave { Name = name, BaseType = type ?? string.Empty, ParentContainer = element };
                element.Instances.Add(inst);
                if (!string.IsNullOrEmpty(parentName))
                {
                    element.DefaultState.SetValue($"{inst.Name}.Parent", parentName, "string");
                }

                // SetInstanceToPosition (called after AddInstance in production) reads these units
                // recursively; set them directly so the test doesn't depend on the dragged type's
                // base-element chain resolving through an unregistered ObjectFinder project.
                element.DefaultState.SetValue($"{inst.Name}.XUnits", _newInstanceXUnits, "PositionUnitType");
                element.DefaultState.SetValue($"{inst.Name}.YUnits", _newInstanceYUnits, "PositionUnitType");

                return inst;
            });
    }

    /// <summary>
    /// A standalone (parent-less) GraphicalUiElement whose absolute left/top equal <paramref name="x"/>/
    /// <paramref name="y"/> directly — X/Y's setters skip the real layout pass for a parent-less element
    /// with the default XUnits/YUnits/XOrigin/YOrigin, so this is safe to construct headlessly.
    /// </summary>
    private static GraphicalUiElement CreatePositionedRepresentation(float x, float y, float width = 0, float height = 0)
    {
        GraphicalUiElement gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
        };
        return gue;
    }

    private (ScreenSave screen, InstanceSave container, InstanceSave sibling) BuildScreenWithTwoInstances()
    {
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default" });

        InstanceSave container = new InstanceSave { Name = "ContainerInstance", BaseType = "Container", ParentContainer = screen };
        screen.Instances.Add(container);

        InstanceSave sibling = new InstanceSave { Name = "SiblingInstance", BaseType = "Container", ParentContainer = screen };
        screen.Instances.Add(sibling);

        _mocker.GetMock<IWireframeObjectManager>().Setup(x => x.ElementShowing).Returns(screen);
        _mocker.GetMock<IPluginManager>().Setup(x => x.GetWorldCursorPosition()).Returns(new Vector2(10, 10));

        // SetInstanceToPosition reads SelectedStateSave to convert the drop's world position into
        // the new instance's X/Y units — needs a non-null default here; overridden per-test below.
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(screen.DefaultState);

        return (screen, container, sibling);
    }

    private static ComponentSave BuildDraggedComponent()
    {
        ComponentSave dragged = new ComponentSave { Name = "DraggedComponent" };
        dragged.States.Add(new StateSave { Name = "Default" });
        return dragged;
    }

    [Fact]
    public void DropOnSelectedInstance_ParentsNewInstanceToSelection_AndPositionsRelativeToParentNotWorldOrigin()
    {
        // Issue #4834 follow-up: the parent isn't necessarily at the canvas origin the way the
        // top-level root is, so a naive "X/Y = world drop position" is wrong once the instance is
        // parented — it must be relative to the parent's own absolute position (100, 50 here).
        var (screen, container, _) = BuildScreenWithTwoInstances();

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns(container);
        _mocker.GetMock<IWireframeObjectManager>()
            .Setup(x => x.GetRepresentation(container, It.IsAny<List<ElementWithState>>()))
            .Returns(CreatePositionedRepresentation(x: 100, y: 50, width: 200, height: 150));

        // BuildScreenWithTwoInstances sets the drop's world position to (10, 10); this test needs a
        // point actually inside the parent's bounds, so it overrides it to (120, 70).
        _mocker.GetMock<IPluginManager>().Setup(x => x.GetWorldCursorPosition()).Returns(new Vector2(120, 70));

        _dragDropManager.OnNodeObjectDroppedInWireframe(BuildDraggedComponent(), instanceUnderCursor: container);

        (screen.DefaultState.GetValue("NewInstance.Parent") as string).ShouldBe("ContainerInstance");
        (screen.DefaultState.GetValue("NewInstance.X") as float?).ShouldBe(20f);
        (screen.DefaultState.GetValue("NewInstance.Y") as float?).ShouldBe(20f);
    }

    [Fact]
    public void DropWithoutAParent_PositionsAtTheWorldDropPointDirectly()
    {
        // Baseline/regression pin for the pre-existing (non-parenting) behavior: with no parent, the
        // container is the element root at the canvas origin, so X/Y equal the world drop point as-is.
        var (screen, container, _) = BuildScreenWithTwoInstances();

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns((InstanceSave?)null);

        _dragDropManager.OnNodeObjectDroppedInWireframe(BuildDraggedComponent(), instanceUnderCursor: null);

        (screen.DefaultState.GetValue("NewInstance.Parent") as string).ShouldBeNull();
        (screen.DefaultState.GetValue("NewInstance.X") as float?).ShouldBe(10f);
        (screen.DefaultState.GetValue("NewInstance.Y") as float?).ShouldBe(10f);
    }

    [Fact]
    public void DropOnSelection_ParentRepresentationNotFound_FallsBackToWorldDropPoint()
    {
        // Defensive fallback: parentInstance is set (so Parent still gets written), but if its
        // rendered representation can't be found for some reason, positioning falls back to the same
        // root-relative behavior as the no-parent case rather than crashing or silently mispositioning.
        var (screen, container, _) = BuildScreenWithTwoInstances();

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns(container);
        // GetRepresentation(container, ...) is deliberately left unmocked, so it returns null.

        _dragDropManager.OnNodeObjectDroppedInWireframe(BuildDraggedComponent(), instanceUnderCursor: container);

        (screen.DefaultState.GetValue("NewInstance.Parent") as string).ShouldBe("ContainerInstance");
        (screen.DefaultState.GetValue("NewInstance.X") as float?).ShouldBe(10f);
        (screen.DefaultState.GetValue("NewInstance.Y") as float?).ShouldBe(10f);
    }

    [Fact]
    public void DropOnSelection_PercentageUnits_UsesParentSizeNotCanvasSize()
    {
        // The parent's WIDTH/HEIGHT (not the top-level canvas size) must back a percentage-unit
        // instance's position too, not just its pixel offset.
        var (screen, container, _) = BuildScreenWithTwoInstances();

        _newInstanceXUnits = PositionUnitType.PercentageWidth;
        _newInstanceYUnits = PositionUnitType.PercentageHeight;

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns(container);
        _mocker.GetMock<IWireframeObjectManager>()
            .Setup(x => x.GetRepresentation(container, It.IsAny<List<ElementWithState>>()))
            .Returns(CreatePositionedRepresentation(x: 100, y: 50, width: 400, height: 200));
        _mocker.GetMock<IPluginManager>().Setup(x => x.GetWorldCursorPosition()).Returns(new Vector2(200, 150));

        _dragDropManager.OnNodeObjectDroppedInWireframe(BuildDraggedComponent(), instanceUnderCursor: container);

        // differenceX = 200 - 100 = 100 -> 100 * 100 / 400 = 25%
        (screen.DefaultState.GetValue("NewInstance.X") as float?).ShouldBe(25f);
        // differenceY = 150 - 50 = 100 -> 100 * 100 / 200 = 50%
        (screen.DefaultState.GetValue("NewInstance.Y") as float?).ShouldBe(50f);
    }

    [Fact]
    public void DropOnUnselectedInstance_DoesNotParentToSelection()
    {
        // A selection exists, but the drop hit-tests onto a DIFFERENT instance than the selection —
        // hit-test alone must not be enough to parent.
        var (screen, container, sibling) = BuildScreenWithTwoInstances();

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns(container);

        _dragDropManager.OnNodeObjectDroppedInWireframe(BuildDraggedComponent(), instanceUnderCursor: sibling);

        (screen.DefaultState.GetValue("NewInstance.Parent") as string).ShouldBeNull();
    }

    [Fact]
    public void DropMissesEveryInstance_DoesNotParentToSelection()
    {
        // A selection exists, but the drop point doesn't land on any instance (e.g. empty canvas).
        var (screen, container, _) = BuildScreenWithTwoInstances();

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns(container);

        _dragDropManager.OnNodeObjectDroppedInWireframe(BuildDraggedComponent(), instanceUnderCursor: null);

        (screen.DefaultState.GetValue("NewInstance.Parent") as string).ShouldBeNull();
    }

    [Fact]
    public void HitTestFindsInstanceButNothingSelected_DoesNotParent()
    {
        // Selection alone (or hit-test alone) is not enough — without a current selection, landing
        // on an instance's bounds must not parent the new instance to it.
        var (screen, container, _) = BuildScreenWithTwoInstances();

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns((InstanceSave?)null);

        _dragDropManager.OnNodeObjectDroppedInWireframe(BuildDraggedComponent(), instanceUnderCursor: container);

        (screen.DefaultState.GetValue("NewInstance.Parent") as string).ShouldBeNull();
    }

    [Fact]
    public void DropOnSelectionWhileCategorizedStateSelected_WritesParentOnDefaultState()
    {
        // The Parent property can only be assigned on the Default state, even when a categorized
        // state is the one currently selected in the tool. SelectedElement is deliberately left
        // unmocked (unrelated to this behavior) so the pre-existing "must be on Default state to
        // add instances" guard in GetDropElementErrorMessage doesn't block the drop before this
        // assertion gets to run — that guard's own behavior isn't part of this issue.
        var (screen, container, _) = BuildScreenWithTwoInstances();

        StateSaveCategory category = new StateSaveCategory { Name = "VisibilityCategory" };
        StateSave categorizedState = new StateSave { Name = "Enabled", ParentContainer = screen };
        category.States.Add(categorizedState);
        screen.Categories.Add(category);

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(categorizedState);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns(container);

        _dragDropManager.OnNodeObjectDroppedInWireframe(BuildDraggedComponent(), instanceUnderCursor: container);

        (screen.DefaultState.GetValue("NewInstance.Parent") as string).ShouldBe("ContainerInstance");
        categorizedState.GetValue("NewInstance.Parent").ShouldBeNull();
    }
}
