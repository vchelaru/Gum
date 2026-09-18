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
using Shouldly;
using System.Numerics;

namespace Gum.Presentation.Tests.Managers;

/// <summary>
/// Covers #4834: dropping an element from the tree view onto the canvas should attach the new
/// instance to the currently-selected instance only when the drop point BOTH hit-tests onto an
/// instance (the <c>instanceUnderCursor</c> caller-supplied argument) AND that instance is the
/// current selection — never on hit-test or selection alone.
/// </summary>
public class DragDropManagerWireframeDropParentingTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly DragDropManager _dragDropManager;

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
                element.DefaultState.SetValue($"{inst.Name}.XUnits", PositionUnitType.PixelsFromLeft, "PositionUnitType");
                element.DefaultState.SetValue($"{inst.Name}.YUnits", PositionUnitType.PixelsFromTop, "PositionUnitType");

                return inst;
            });
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
    public void DropOnSelectedInstance_ParentsNewInstanceToSelection()
    {
        var (screen, container, _) = BuildScreenWithTwoInstances();

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns(container);

        _dragDropManager.OnNodeObjectDroppedInWireframe(BuildDraggedComponent(), instanceUnderCursor: container);

        (screen.DefaultState.GetValue("NewInstance.Parent") as string).ShouldBe("ContainerInstance");
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
