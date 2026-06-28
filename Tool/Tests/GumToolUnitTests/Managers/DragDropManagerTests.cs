using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace GumToolUnitTests.Managers;

public class DragDropManagerTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly DragDropManager _dragDropManager;

    private readonly Mock<ICircularReferenceManager> _circularReferenceManager;
    private readonly Mock<ICopyPasteLogic> _copyPasteLogic;


    public DragDropManagerTests()
    {
        _mocker = new AutoMocker();
        _dragDropManager = _mocker.CreateInstance<DragDropManager>();

        Mock<PluginManager> pluginManager = _mocker.GetMock<PluginManager>();
        pluginManager.Object.Plugins = new List<PluginBase>();

        _circularReferenceManager = _mocker.GetMock<ICircularReferenceManager>();
        _copyPasteLogic = _mocker.GetMock<ICopyPasteLogic>();

        _mocker.GetMock<IUndoManager>()
            .Setup(x => x.RequestLock())
            .Returns((UndoLock)null!);
    }

    [Fact]
    public void HandleDroppedStandardElementOnTreeNode_OnInstance_ParentsAndRefreshesWireframe()
    {
        // Issue #973: dropping a Standards-palette chip onto an instance must parent the new
        // instance to that instance AND refresh the wireframe afterward, exactly like dragging a
        // Standard element node does. The chip path passed a null DropTarget, which skipped
        // HandleDroppedElementSave's onto-instance branch (HandleDroppingInstanceOnTarget + the
        // explicit post-parent RefreshAll). AddInstance refreshes BEFORE writing the Parent
        // variable, so the new instance was left visually un-parented until the next drop forced
        // another refresh.
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });

        InstanceSave containerInstance = new InstanceSave { Name = "ContainerInstance", BaseType = "Container", ParentContainer = screen };
        screen.Instances.Add(containerInstance);

        StandardElementSave circleStandard = new StandardElementSave { Name = "Circle" };

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave).Returns(screen.DefaultState);

        _mocker.GetMock<IElementCommands>()
            .Setup(x => x.GetUniqueNameForNewInstance(It.IsAny<ElementSave>(), It.IsAny<ElementSave>()))
            .Returns("CircleInstance");

        // Stand in for the real AddInstance: append the instance and write the bare Parent name,
        // matching the production command, so HandleDroppingInstanceOnTarget runs against real state.
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
                return inst;
            });

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(containerInstance);

        // Act
        _dragDropManager.HandleDroppedStandardElementOnTreeNode(circleStandard, targetNode.Object);

        // Assert: parented in data ...
        string? parentValue = screen.DefaultState.GetValue("CircleInstance.Parent") as string;
        parentValue.ShouldBe("ContainerInstance");

        // ... and the wireframe is refreshed AFTER parenting so the child attaches at drop time.
        _mocker.GetMock<IWireframeObjectManager>()
            .Verify(x => x.RefreshAll(true, false), Times.AtLeastOnce,
                "dropping a chip onto an instance must run the onto-instance parenting branch, which refreshes the wireframe after the parent is set");
    }

    [Fact]
    public void HandleDroppedStandardElementOnTreeNode_OnComponent_AddsInstanceOfStandardType()
    {
        // Arrange: dropping a "Text" chip onto a Component should create a Text instance on it,
        // reusing the same creation path as dragging the Standard element node.
        StandardElementSave textStandard = new StandardElementSave();
        textStandard.Name = "Text";

        ComponentSave targetComponent = new ComponentSave();
        targetComponent.Name = "TargetComponent";
        targetComponent.States.Add(new StateSave());

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(targetComponent);

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        _dragDropManager.HandleDroppedStandardElementOnTreeNode(textStandard, targetNode.Object);

        // Assert
        _mocker.GetMock<IElementCommands>()
            .Verify(x => x.AddInstance(targetComponent, It.IsAny<string>(), "Text", null, (int?)null), Times.Once);
    }

    [Fact]
    public void OnNodeSortingDropped_DropInstance_ShouldInsertAtIndex_OnDifferentElement()
    {
        // Arrange
        ComponentSave parentOfDragged = new ComponentSave();
        parentOfDragged.Name = "ParentOfDragged";
        parentOfDragged.States.Add(new ());
        InstanceSave draggedInstance = new InstanceSave();
        draggedInstance.Name = "DraggedInstance";
        draggedInstance.ParentContainer = parentOfDragged;

        ComponentSave destinationComponent = new ComponentSave();
        destinationComponent.States.Add(new());
        destinationComponent.Instances.Add(new InstanceSave() { Name = "Instance1"});
        destinationComponent.Instances.Add(new InstanceSave() { Name = "Instance2"});

        Mock<ITreeNode> draggedNode = new Mock<ITreeNode>();
        draggedNode.Setup(x => x.Tag).Returns(draggedInstance);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(destinationComponent);

        List<ITreeNode> draggedNodes = new () { draggedNode.Object };

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        _copyPasteLogic
            .Setup(x => x.PasteInstanceSaves(
                It.IsAny<List<InstanceSave>>(),
                It.IsAny<List<StateSave>>(),
                It.IsAny<ElementSave>(),
                It.IsAny<InstanceSave?>(),
                It.IsAny<ISelectedState?>(),
                It.IsAny<List<StateSave>?>(),
                It.IsAny<HashSet<string>?>(),
                It.IsAny<List<InstanceSave>?>()))
            .Returns(new List<InstanceSave> { draggedInstance });

        DropTarget dropTarget = new DropTarget(destinationComponent, null, new DropPosition.InsertAt(1));

        // Act
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, dropTarget);

        // Assert
        destinationComponent.Instances[1].Name.ShouldBe("DraggedInstance");
    }

    [Fact]
    public void OnNodeSortingDropped_BeforeSibling_MultipleInstances_AscendingSortPreservesSourceOrder()
    {
        // Issue #2869 pin: BeforeSibling anchors at a sibling whose flat-list
        // index shifts down every time an earlier item is inserted before it.
        // Processing items in DESCENDING source-index order (the natural
        // "stack at a fixed position" sort) would invert drag order — the
        // higher-source-index item lands at the original anchor, then the
        // lower-source-index item lands in front of it. ASCENDING order is
        // required to preserve drag order. This test guards against a
        // "simplify OnNodeSortingDropped to always-descending" regression.
        //
        // Scenario: [A, B, C, D, E]; drag B(src=1) and D(src=3); drop BeforeSibling(A).
        // Ascending: B → [B, A, C, D, E]; D → [B, D, A, C, E]. B at 0, D at 1.
        // Descending: D → [D, A, B, C, E]; B → [D, B, A, C, E]. D at 0, B at 1. WRONG.
        ComponentSave element = new ComponentSave();
        element.States.Add(new StateSave());

        InstanceSave instanceA = new InstanceSave { Name = "A", ParentContainer = element };
        InstanceSave instanceB = new InstanceSave { Name = "B", ParentContainer = element };
        InstanceSave instanceC = new InstanceSave { Name = "C", ParentContainer = element };
        InstanceSave instanceD = new InstanceSave { Name = "D", ParentContainer = element };
        InstanceSave instanceE = new InstanceSave { Name = "E", ParentContainer = element };
        element.Instances.Add(instanceA);
        element.Instances.Add(instanceB);
        element.Instances.Add(instanceC);
        element.Instances.Add(instanceD);
        element.Instances.Add(instanceE);

        // Arrival order is intentionally NOT source order — sort direction
        // must be derived from source index, not arrival order.
        Mock<ITreeNode> nodeDraggedD = new Mock<ITreeNode>();
        nodeDraggedD.Setup(x => x.Tag).Returns(instanceD);
        Mock<ITreeNode> nodeDraggedB = new Mock<ITreeNode>();
        nodeDraggedB.Setup(x => x.Tag).Returns(instanceB);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(element);

        List<ITreeNode> draggedNodes = new() { nodeDraggedD.Object, nodeDraggedB.Object };

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        DropTarget dropTarget = new DropTarget(element, null, new DropPosition.BeforeSibling(instanceA));

        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, dropTarget);

        element.Instances.IndexOf(instanceB).ShouldBe(0,
            "B (lower source index) must precede D in the result — ascending sort is required for BeforeSibling.");
        element.Instances.IndexOf(instanceD).ShouldBe(1);
    }

    [Fact]
    public void OnNodeSortingDropped_AfterSibling_MultipleInstances_DescendingSortPreservesSourceOrder()
    {
        // Issue #2869 pin: AfterSibling anchors at a sibling whose flat-list
        // index does NOT shift when a later item is inserted after it.
        // Processing items in DESCENDING source-index order preserves drag
        // order — each item stacks at IndexOf(sibling)+1, and the second
        // item displaces the first one further down the list. ASCENDING
        // would invert drag order. This test guards against a "simplify
        // OnNodeSortingDropped to always-ascending" regression.
        //
        // Scenario: [A, B, C, D, E]; drag B(src=1) and D(src=3); drop AfterSibling(C).
        // Descending: D → [A, B, C, D, E]; B → [A, C, B, D, E]. B at 2, D at 3.
        // Ascending: B → [A, C, B, D, E]; D → [A, C, D, B, E]. D at 2, B at 3. WRONG.
        ComponentSave element = new ComponentSave();
        element.States.Add(new StateSave());

        InstanceSave instanceA = new InstanceSave { Name = "A", ParentContainer = element };
        InstanceSave instanceB = new InstanceSave { Name = "B", ParentContainer = element };
        InstanceSave instanceC = new InstanceSave { Name = "C", ParentContainer = element };
        InstanceSave instanceD = new InstanceSave { Name = "D", ParentContainer = element };
        InstanceSave instanceE = new InstanceSave { Name = "E", ParentContainer = element };
        element.Instances.Add(instanceA);
        element.Instances.Add(instanceB);
        element.Instances.Add(instanceC);
        element.Instances.Add(instanceD);
        element.Instances.Add(instanceE);

        Mock<ITreeNode> nodeDraggedD = new Mock<ITreeNode>();
        nodeDraggedD.Setup(x => x.Tag).Returns(instanceD);
        Mock<ITreeNode> nodeDraggedB = new Mock<ITreeNode>();
        nodeDraggedB.Setup(x => x.Tag).Returns(instanceB);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(element);

        // Arrival order is intentionally NOT source order.
        List<ITreeNode> draggedNodes = new() { nodeDraggedB.Object, nodeDraggedD.Object };

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        DropTarget dropTarget = new DropTarget(element, null, new DropPosition.AfterSibling(instanceC));

        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, dropTarget);

        element.Instances.IndexOf(instanceB).ShouldBe(2,
            "B (lower source index) must precede D in the result — descending sort is required for AfterSibling.");
        element.Instances.IndexOf(instanceD).ShouldBe(3);
    }

    [Fact]
    public void OnNodeSortingDropped_MultipleInstances_PreservesRelativeOrder_WhenDroppedAtBeginning()
    {
        // Arrange
        string instanceNameA = "A";
        string instanceNameB = "B";
        string instanceNameC = "C";
        string instanceNameD = "D";
        string instanceNameE = "E";

        ComponentSave element = new ComponentSave();
        element.States.Add(new StateSave());

        InstanceSave instanceA = new InstanceSave { Name = instanceNameA, ParentContainer = element };
        InstanceSave instanceB = new InstanceSave { Name = instanceNameB, ParentContainer = element };
        InstanceSave instanceC = new InstanceSave { Name = instanceNameC, ParentContainer = element };
        InstanceSave instanceD = new InstanceSave { Name = instanceNameD, ParentContainer = element };
        InstanceSave instanceE = new InstanceSave { Name = instanceNameE, ParentContainer = element };

        element.Instances.Add(instanceA);
        element.Instances.Add(instanceB);
        element.Instances.Add(instanceC);
        element.Instances.Add(instanceD);
        element.Instances.Add(instanceE);

        // Tree nodes arrive in reverse order (D first, then B) — simulating the bug
        Mock<ITreeNode> nodeDragged_D = new Mock<ITreeNode>();
        nodeDragged_D.Setup(x => x.Tag).Returns(instanceD);

        Mock<ITreeNode> nodeDragged_B = new Mock<ITreeNode>();
        nodeDragged_B.Setup(x => x.Tag).Returns(instanceB);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(element);

        List<ITreeNode> draggedNodes = new() { nodeDragged_D.Object, nodeDragged_B.Object };

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        // Act — drop at beginning, i.e. before A.
        DropTarget dropTarget = new DropTarget(element, null, new DropPosition.BeforeSibling(instanceA));
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, dropTarget);

        // Assert — B should appear before D (relative order preserved), both at the beginning
        int indexOfB = element.Instances.IndexOf(instanceB);
        int indexOfD = element.Instances.IndexOf(instanceD);

        indexOfB.ShouldBe(0);
        indexOfD.ShouldBe(1);
        indexOfB.ShouldBeLessThan(indexOfD);
    }

    [Fact]
    public void OnNodeSortingDropped_MultipleInstances_PreservesRelativeOrder_WhenDroppedInMiddle()
    {
        // Arrange
        string instanceNameA = "A";
        string instanceNameB = "B";
        string instanceNameC = "C";
        string instanceNameD = "D";
        string instanceNameE = "E";

        ComponentSave element = new ComponentSave();
        element.States.Add(new StateSave());

        InstanceSave instanceA = new InstanceSave { Name = instanceNameA, ParentContainer = element };
        InstanceSave instanceB = new InstanceSave { Name = instanceNameB, ParentContainer = element };
        InstanceSave instanceC = new InstanceSave { Name = instanceNameC, ParentContainer = element };
        InstanceSave instanceD = new InstanceSave { Name = instanceNameD, ParentContainer = element };
        InstanceSave instanceE = new InstanceSave { Name = instanceNameE, ParentContainer = element };

        element.Instances.Add(instanceA);
        element.Instances.Add(instanceB);
        element.Instances.Add(instanceC);
        element.Instances.Add(instanceD);
        element.Instances.Add(instanceE);

        // Tree nodes arrive in reverse order (D first, then B) — simulating the bug
        Mock<ITreeNode> nodeDragged_D = new Mock<ITreeNode>();
        nodeDragged_D.Setup(x => x.Tag).Returns(instanceD);

        Mock<ITreeNode> nodeDragged_B = new Mock<ITreeNode>();
        nodeDragged_B.Setup(x => x.Tag).Returns(instanceB);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(element);

        List<ITreeNode> draggedNodes = new() { nodeDragged_D.Object, nodeDragged_B.Object };

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        // Act — drop after C.
        DropTarget dropTarget = new DropTarget(element, null, new DropPosition.AfterSibling(instanceC));
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, dropTarget);

        // Assert — B should appear before D (relative order preserved), both after A and C
        int indexOfA = element.Instances.IndexOf(instanceA);
        int indexOfC = element.Instances.IndexOf(instanceC);
        int indexOfB = element.Instances.IndexOf(instanceB);
        int indexOfD = element.Instances.IndexOf(instanceD);

        indexOfB.ShouldBe(2);
        indexOfD.ShouldBe(3);
        indexOfB.ShouldBeLessThan(indexOfD);
        indexOfA.ShouldBeLessThan(indexOfB);
        indexOfC.ShouldBeLessThan(indexOfB);
    }

    [Fact]
    public void OnNodeSortingDropped_MixOfFolderAndTaggedNodes_ProcessesTaggedNodesCorrectly()
    {
        // Arrange
        string draggedInstanceName = "DraggedInstance";

        ComponentSave parentOfDragged = new ComponentSave();
        parentOfDragged.Name = "ParentOfDragged";
        parentOfDragged.States.Add(new());
        InstanceSave draggedInstance = new InstanceSave();
        draggedInstance.Name = draggedInstanceName;
        draggedInstance.ParentContainer = parentOfDragged;

        ComponentSave destinationComponent = new ComponentSave();
        destinationComponent.States.Add(new());
        destinationComponent.Instances.Add(new InstanceSave() { Name = "Instance1" });

        // Folder node (Tag = null) - simulates a folder in the tree view
        Mock<ITreeNode> folderNode = new Mock<ITreeNode>();
        folderNode.Setup(x => x.Tag).Returns((object?)null);

        // Instance node
        Mock<ITreeNode> instanceNode = new Mock<ITreeNode>();
        instanceNode.Setup(x => x.Tag).Returns(draggedInstance);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(destinationComponent);

        List<ITreeNode> draggedNodes = new() { folderNode.Object, instanceNode.Object };

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        _copyPasteLogic
            .Setup(x => x.PasteInstanceSaves(
                It.IsAny<List<InstanceSave>>(),
                It.IsAny<List<StateSave>>(),
                It.IsAny<ElementSave>(),
                It.IsAny<InstanceSave?>(),
                It.IsAny<ISelectedState?>(),
                It.IsAny<List<StateSave>?>(),
                It.IsAny<HashSet<string>?>(),
                It.IsAny<List<InstanceSave>?>()))
            .Returns(new List<InstanceSave> { draggedInstance });

        DropTarget dropTarget = new DropTarget(destinationComponent, null, new DropPosition.InsertAt(1));

        // Act - should not throw even with folder nodes in the list
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, dropTarget);

        // Assert - the tagged instance was still processed correctly despite folder node presence
        destinationComponent.Instances[1].Name.ShouldBe(draggedInstanceName);
    }

    [Fact]
    public void ValidateNodeSorting_NullTarget_ReturnsFalse()
    {
        // Arrange
        Mock<ITreeNode> draggedNode = new Mock<ITreeNode>();
        draggedNode.Setup(x => x.Tag).Returns((object?)null);

        // Act
        var result = _dragDropManager.ValidateNodeSorting(
            new[] { draggedNode.Object }, null, dropTarget: null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ValidateNodeSorting_FolderNodeWithNullTag_DoesNotThrow()
    {
        // Arrange - folder node has Tag=null, unlike element/instance nodes
        Mock<ITreeNode> folderNode = new Mock<ITreeNode>();
        folderNode.Setup(x => x.Tag).Returns((object?)null);
        folderNode.Setup(x => x.GetFullFilePath()).Returns(new FilePath("C:\\test\\folder\\"));

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns((object?)null);
        targetNode.Setup(x => x.GetFullFilePath()).Returns(new FilePath("C:\\test\\other\\"));

        // Act - should not throw; returns false because mocked ITreeNode
        // isn't recognized as a component/screen folder by extension methods
        var result = _dragDropManager.ValidateNodeSorting(
            new[] { folderNode.Object }, targetNode.Object, dropTarget: null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsValidExtensionForFileDrop_ShouldAcceptTtfFiles()
    {
        // Act
        bool result = _dragDropManager.IsValidExtensionForFileDrop("fonts/MyFont.ttf");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsValidExtensionForFileDrop_ShouldAcceptPngFiles()
    {
        // Act
        bool result = _dragDropManager.IsValidExtensionForFileDrop("textures/image.png");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsValidExtensionForFileDrop_ShouldRejectUnknownExtensions()
    {
        // Act
        bool result = _dragDropManager.IsValidExtensionForFileDrop("file.xyz");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void OnNodeObjectDroppedInWireframe_ShouldHoldUndoLockWhenAddingInstance()
    {
        // Issue #2658: dropping a component into the wireframe must bundle the
        // instance creation and the cursor-driven X/Y assignment into one undo
        // entry. The DragDropManager must request the undo lock BEFORE calling
        // AddInstance so that AddInstance's plugin-fired RecordUndo is suppressed,
        // and the lock must remain held through the subsequent X/Y SetValue calls.
        // Arrange
        ScreenSave screen = new ScreenSave();
        screen.Name = "TargetScreen";
        screen.States.Add(new Gum.DataTypes.Variables.StateSave { Name = "Default" });

        ComponentSave dragged = new ComponentSave();
        dragged.Name = "DraggedComponent";
        dragged.States.Add(new Gum.DataTypes.Variables.StateSave { Name = "Default" });

        _mocker.GetMock<IWireframeObjectManager>()
            .Setup(x => x.ElementShowing).Returns(screen);

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        bool lockRequested = false;
        bool lockHeldDuringAddInstance = false;

        _mocker.GetMock<IUndoManager>()
            .Setup(x => x.RequestLock())
            .Callback(() => lockRequested = true)
            .Returns((UndoLock)null!);

        InstanceSave? nullInstance = null;
        _mocker.GetMock<IElementCommands>()
            .Setup(x => x.AddInstance(
                It.IsAny<ElementSave>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>()))
            .Callback(() => lockHeldDuringAddInstance = lockRequested)
            .Returns(() => nullInstance!);

        // Act
        _dragDropManager.OnNodeObjectDroppedInWireframe(dragged);

        // Assert
        lockHeldDuringAddInstance.ShouldBeTrue(
            "RequestLock must be called before AddInstance so the drop is recorded as a single undo entry (issue #2658)");
    }

    [Fact]
    public void OnNodeSortingDropped_DropElementOnInstanceWithIndexPastSiblingCount_KeepsNewInstanceAtEnd()
    {
        // Issue #2864 (post-Instances.Count fix): when ProcessDrop returns a
        // flat-list index for the InstanceSave-tag Into case, the index can
        // exceed siblings.Count. The downstream HandleDroppingInstanceOnTarget
        // interprets index as "position among target's children" and falls
        // through to indexToAddAt = 0 when the index is past the end, which
        // moves the just-inserted instance back to the front of the flat list
        // (the user-visible "snap to top"). Out-of-range index must mean
        // "append after the last existing sibling".
        ScreenSave screen = new ScreenSave();
        screen.Name = "MainScreen";
        screen.States.Add(new StateSave { Name = "Default" });

        InstanceSave leftContainer = new InstanceSave { Name = "LeftContainer", ParentContainer = screen };
        screen.Instances.Add(leftContainer);
        for (int i = 0; i < 4; i++)
        {
            string childName = $"Child{i}";
            InstanceSave child = new InstanceSave { Name = childName, ParentContainer = screen };
            screen.Instances.Add(child);
            screen.DefaultState.SetValue($"{childName}.Parent", "LeftContainer", "string");
        }
        // Top-level instances not parented under LeftContainer — required to
        // reproduce the bug, which only triggers when screen.Instances.Count is
        // strictly greater than (siblings of LeftContainer).Count by more than 1.
        for (int i = 0; i < 10; i++)
        {
            screen.Instances.Add(new InstanceSave { Name = $"TopLevel{i}", ParentContainer = screen });
        }

        ComponentSave draggedComponent = new ComponentSave();
        draggedComponent.Name = "DraggedComponent";
        draggedComponent.States.Add(new StateSave { Name = "Default" });

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave).Returns(screen.DefaultState);

        _mocker.GetMock<IElementCommands>()
            .Setup(x => x.GetUniqueNameForNewInstance(It.IsAny<ElementSave>(), It.IsAny<ElementSave>()))
            .Returns("DraggedComponent1");

        // Stand in for ElementCommands.AddInstance — actually mutate the screen
        // (insert at desiredIndex, set Parent variable) and return the new
        // instance, so HandleDroppingInstanceOnTarget runs against the same
        // state ElementCommands would produce.
        _mocker.GetMock<IElementCommands>()
            .Setup(x => x.AddInstance(
                It.IsAny<ElementSave>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>()))
            .Returns((ElementSave element, string name, string? type, string? parentName, int? desiredIndex) =>
            {
                InstanceSave inst = new InstanceSave
                {
                    Name = name,
                    BaseType = type ?? string.Empty,
                    ParentContainer = element
                };
                if (desiredIndex.HasValue)
                {
                    element.Instances.Insert(desiredIndex.Value, inst);
                }
                else
                {
                    element.Instances.Add(inst);
                }
                if (!string.IsNullOrEmpty(parentName))
                {
                    element.DefaultState.SetValue($"{inst.Name}.Parent", parentName, "string");
                }
                return inst;
            });

        Mock<ITreeNode> draggedNode = new Mock<ITreeNode>();
        draggedNode.Setup(x => x.Tag).Returns(draggedComponent);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(leftContainer);

        List<ITreeNode> draggedNodes = new() { draggedNode.Object };

        // ProcessDrop's Into-on-InstanceSave returns DropTarget(screen, leftContainer, Append).
        // Pre-#2869 the int-index path could send Instances.Count downstream and
        // mis-trigger the "snap to top." With the typed DropTarget the consumer
        // resolves position from ParentInstance directly.
        DropTarget dropTarget = new DropTarget(screen, leftContainer, new DropPosition.Append());

        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, dropTarget);

        InstanceSave? newInstance = screen.Instances.FirstOrDefault(i => i.Name == "DraggedComponent1");
        newInstance.ShouldNotBeNull();

        int newIndex = screen.Instances.IndexOf(newInstance);
        int lastExistingChildIndex = screen.Instances.IndexOf(screen.Instances.First(i => i.Name == "Child3"));
        newIndex.ShouldBeGreaterThan(lastExistingChildIndex,
            "the dropped instance must remain after the last existing child of the target container, not snap back to index 0");
    }

    [Fact]
    public void OnNodeSortingDropped_DropElementOnInstanceWithDefaultChild_ParentsToDefaultSlot()
    {
        // Regression: dragging an element from the tree onto an instance whose
        // base component declares a DefaultChildContainer must parent the new
        // instance to "<instance>.<defaultSlot>", matching paste and
        // right-click-add. Commit 66df81010 added an early-out in
        // HandleDroppingInstanceOnTarget guarded by ParentVariableAlreadyMatches,
        // whose loose prefix matching treated the bare "<instance>" parent (the
        // value AddInstance writes) as already matching "<instance>.<slot>", so
        // the slot upgrade was skipped and the child attached to the container
        // root instead of its default slot.
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave listBoxUiContainer = new ComponentSave { Name = "ListBoxUiContainer" };
        StateSave containerDefault = new StateSave { Name = "Default", ParentContainer = listBoxUiContainer };
        listBoxUiContainer.States.Add(containerDefault);
        listBoxUiContainer.Instances.Add(new InstanceSave { Name = "InnerPanel", BaseType = "Container", ParentContainer = listBoxUiContainer });
        containerDefault.SetValue("DefaultChildContainer", "InnerPanel", "string");
        project.Components.Add(listBoxUiContainer);

        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        project.Screens.Add(screen);

        InstanceSave listBoxInstance = new InstanceSave { Name = "ListBoxUiContainerInstance", BaseType = "ListBoxUiContainer", ParentContainer = screen };
        screen.Instances.Add(listBoxInstance);

        // Pin the precondition: the production path relies on this resolving to a
        // non-empty slot, otherwise the test couldn't reproduce the bug.
        ObjectFinder.Self.GetDefaultChildName(listBoxInstance, screen.DefaultState)
            .ShouldBe("InnerPanel", "test setup must produce a default child slot");

        ComponentSave draggedComponent = new ComponentSave { Name = "DraggedComponent" };
        draggedComponent.States.Add(new StateSave { Name = "Default", ParentContainer = draggedComponent });

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave).Returns(screen.DefaultState);

        _mocker.GetMock<IElementCommands>()
            .Setup(x => x.GetUniqueNameForNewInstance(It.IsAny<ElementSave>(), It.IsAny<ElementSave>()))
            .Returns("DraggedComponent1");

        // Stand in for ElementCommands.AddInstance — mutate the screen the way the
        // real command does (append, write the Parent variable to the bare parent
        // name) so HandleDroppingInstanceOnTarget runs against the same state.
        _mocker.GetMock<IElementCommands>()
            .Setup(x => x.AddInstance(
                It.IsAny<ElementSave>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>()))
            .Returns((ElementSave element, string name, string? type, string? parentName, int? desiredIndex) =>
            {
                InstanceSave inst = new InstanceSave
                {
                    Name = name,
                    BaseType = type ?? string.Empty,
                    ParentContainer = element
                };
                if (desiredIndex.HasValue)
                {
                    element.Instances.Insert(desiredIndex.Value, inst);
                }
                else
                {
                    element.Instances.Add(inst);
                }
                if (!string.IsNullOrEmpty(parentName))
                {
                    element.DefaultState.SetValue($"{inst.Name}.Parent", parentName, "string");
                }
                return inst;
            });

        Mock<ITreeNode> draggedNode = new Mock<ITreeNode>();
        draggedNode.Setup(x => x.Tag).Returns(draggedComponent);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(listBoxInstance);

        List<ITreeNode> draggedNodes = new() { draggedNode.Object };

        // ProcessDrop's Into-on-InstanceSave shape (see ProcessDrop line 814).
        DropTarget dropTarget = new DropTarget(screen, listBoxInstance, new DropPosition.Append());

        // Act
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, dropTarget);

        // Assert
        string? parentValue = screen.DefaultState.GetValue("DraggedComponent1.Parent") as string;
        parentValue.ShouldBe("ListBoxUiContainerInstance.InnerPanel",
            "the dropped instance must be parented to the target's default child slot, like paste / add do");
    }

    [Fact]
    public void OnNodeSortingDropped_DropInstanceOnItsCurrentParent_IsNoOp()
    {
        // Dropping an instance onto its current parent must not reorder the
        // flat list. The user's intent for "drop on parent" is "make this a
        // child of parent" — already satisfied when the Parent variable
        // already references that parent. Moving the instance to the end of
        // its sibling group is a side effect of treating Append as
        // "stack at end of siblings" unconditionally, which surprises the
        // user (the visual jumps with no apparent reason).
        //
        // The reorder must still happen for explicit BeforeSibling/AfterSibling
        // drops where the user actively chose a new position.
        ScreenSave screen = new ScreenSave();
        screen.Name = "MainScreen";
        screen.States.Add(new StateSave { Name = "Default" });

        InstanceSave container = new InstanceSave { Name = "Container", ParentContainer = screen };
        screen.Instances.Add(container);

        InstanceSave childA = new InstanceSave { Name = "ChildA", ParentContainer = screen };
        screen.Instances.Add(childA);
        screen.DefaultState.SetValue("ChildA.Parent", "Container", "string");

        InstanceSave childB = new InstanceSave { Name = "ChildB", ParentContainer = screen };
        screen.Instances.Add(childB);
        screen.DefaultState.SetValue("ChildB.Parent", "Container", "string");

        InstanceSave childC = new InstanceSave { Name = "ChildC", ParentContainer = screen };
        screen.Instances.Add(childC);
        screen.DefaultState.SetValue("ChildC.Parent", "Container", "string");

        int originalIndexOfChildA = screen.Instances.IndexOf(childA);

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave).Returns(screen.DefaultState);

        Mock<ITreeNode> draggedNode = new Mock<ITreeNode>();
        draggedNode.Setup(x => x.Tag).Returns(childA);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(container);

        // Drop ChildA onto its current parent (Container).
        DropTarget dropTarget = new DropTarget(screen, container, new DropPosition.Append());
        _dragDropManager.OnNodeSortingDropped(new List<ITreeNode> { draggedNode.Object }, targetNode.Object, dropTarget);

        screen.Instances.IndexOf(childA).ShouldBe(originalIndexOfChildA,
            "ChildA was already a child of Container; dropping on the parent must not reposition.");
    }

    [Fact]
    public void OnNodeSortingDropped_DropTopLevelInstanceOnContainingElement_IsNoOp()
    {
        // Mirror of the parent-reaffirmation case: a top-level instance dropped
        // onto its containing Element (no parent change) must not be reordered.
        ScreenSave screen = new ScreenSave();
        screen.Name = "MainScreen";
        screen.States.Add(new StateSave { Name = "Default" });

        InstanceSave first = new InstanceSave { Name = "First", ParentContainer = screen };
        InstanceSave second = new InstanceSave { Name = "Second", ParentContainer = screen };
        InstanceSave third = new InstanceSave { Name = "Third", ParentContainer = screen };
        screen.Instances.Add(first);
        screen.Instances.Add(second);
        screen.Instances.Add(third);

        int originalIndexOfFirst = screen.Instances.IndexOf(first);

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave).Returns(screen.DefaultState);

        Mock<ITreeNode> draggedNode = new Mock<ITreeNode>();
        draggedNode.Setup(x => x.Tag).Returns(first);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(screen);

        DropTarget dropTarget = new DropTarget(screen, null, new DropPosition.Append());
        _dragDropManager.OnNodeSortingDropped(new List<ITreeNode> { draggedNode.Object }, targetNode.Object, dropTarget);

        screen.Instances.IndexOf(first).ShouldBe(originalIndexOfFirst,
            "First was already a top-level instance; dropping on the element must not reposition.");
    }

    [Fact]
    public void OnNodeSortingDropped_DropElementOnInstance_PreservesFlatListInvariant()
    {
        // Issue #2869: after any drop operation, the flat Instances list must
        // satisfy the invariant "for every instance, all of its children come
        // after it in the list." This drives render z-order — when violated,
        // children render behind their parent (the #2864 user-visible bug).
        ScreenSave screen = new ScreenSave();
        screen.Name = "MainScreen";
        screen.States.Add(new StateSave { Name = "Default" });

        InstanceSave leftContainer = new InstanceSave { Name = "LeftContainer", ParentContainer = screen };
        screen.Instances.Add(leftContainer);
        for (int i = 0; i < 3; i++)
        {
            string childName = $"Child{i}";
            InstanceSave child = new InstanceSave { Name = childName, ParentContainer = screen };
            screen.Instances.Add(child);
            screen.DefaultState.SetValue($"{childName}.Parent", "LeftContainer", "string");
        }
        for (int i = 0; i < 5; i++)
        {
            screen.Instances.Add(new InstanceSave { Name = $"TopLevel{i}", ParentContainer = screen });
        }

        ComponentSave draggedComponent = new ComponentSave();
        draggedComponent.Name = "DraggedComponent";
        draggedComponent.States.Add(new StateSave { Name = "Default" });

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave).Returns(screen.DefaultState);

        _mocker.GetMock<IElementCommands>()
            .Setup(x => x.GetUniqueNameForNewInstance(It.IsAny<ElementSave>(), It.IsAny<ElementSave>()))
            .Returns("DraggedComponent1");

        _mocker.GetMock<IElementCommands>()
            .Setup(x => x.AddInstance(
                It.IsAny<ElementSave>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>()))
            .Returns((ElementSave element, string name, string? type, string? parentName, int? desiredIndex) =>
            {
                InstanceSave inst = new InstanceSave
                {
                    Name = name,
                    BaseType = type ?? string.Empty,
                    ParentContainer = element
                };
                if (desiredIndex.HasValue)
                {
                    element.Instances.Insert(desiredIndex.Value, inst);
                }
                else
                {
                    element.Instances.Add(inst);
                }
                if (!string.IsNullOrEmpty(parentName))
                {
                    element.DefaultState.SetValue($"{inst.Name}.Parent", parentName, "string");
                }
                return inst;
            });

        Mock<ITreeNode> draggedNode = new Mock<ITreeNode>();
        draggedNode.Setup(x => x.Tag).Returns(draggedComponent);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(leftContainer);

        List<ITreeNode> draggedNodes = new() { draggedNode.Object };

        DropTarget dropTarget = new DropTarget(screen, leftContainer, new DropPosition.Append());
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, dropTarget);

        AssertFlatListChildAfterParentInvariant(screen);
    }

    [Fact]
    public void OnNodeSortingDropped_ReorderInstanceWithinSameParent_PreservesFlatListInvariant()
    {
        ScreenSave screen = new ScreenSave();
        screen.Name = "MainScreen";
        screen.States.Add(new StateSave { Name = "Default" });

        InstanceSave container = new InstanceSave { Name = "Container", ParentContainer = screen };
        screen.Instances.Add(container);
        InstanceSave childA = new InstanceSave { Name = "ChildA", ParentContainer = screen };
        screen.Instances.Add(childA);
        screen.DefaultState.SetValue("ChildA.Parent", "Container", "string");
        InstanceSave childB = new InstanceSave { Name = "ChildB", ParentContainer = screen };
        screen.Instances.Add(childB);
        screen.DefaultState.SetValue("ChildB.Parent", "Container", "string");
        InstanceSave childC = new InstanceSave { Name = "ChildC", ParentContainer = screen };
        screen.Instances.Add(childC);
        screen.DefaultState.SetValue("ChildC.Parent", "Container", "string");

        _circularReferenceManager
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave).Returns(screen.DefaultState);

        // Reorder ChildC to be after ChildA (i.e. between A and B among the container's children).
        Mock<ITreeNode> draggedNode = new Mock<ITreeNode>();
        draggedNode.Setup(x => x.Tag).Returns(childC);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(container);

        List<ITreeNode> draggedNodes = new() { draggedNode.Object };

        // Place ChildC before ChildB (the second of the container's children).
        DropTarget dropTarget = new DropTarget(screen, container, new DropPosition.BeforeSibling(childB));
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, dropTarget);

        AssertFlatListChildAfterParentInvariant(screen);
    }

    private static void AssertFlatListChildAfterParentInvariant(ElementSave element)
    {
        // For every instance I in element.Instances, every other instance whose
        // Parent variable references I (or a default-child of I) must appear
        // at a later flat-list index than I. Violating this swaps render order.
        for (int i = 0; i < element.Instances.Count; i++)
        {
            InstanceSave parent = element.Instances[i];
            for (int j = 0; j < element.Instances.Count; j++)
            {
                if (i == j) continue;
                InstanceSave candidate = element.Instances[j];
                object? parentValue = element.DefaultState.GetVariableRecursive(candidate.Name + ".Parent")?.Value;
                if (parentValue is string parentString &&
                    (parentString == parent.Name || parentString.StartsWith(parent.Name + ".")))
                {
                    j.ShouldBeGreaterThan(i,
                        $"Flat-list invariant violated: {candidate.Name} (parented under {parent.Name}) " +
                        $"appears at index {j}, before its parent at index {i}.");
                }
            }
        }
    }

    [Fact]
    public void OnNodeSortingDropped_OnlyFolderNodes_DoesNotThrow()
    {
        // Arrange - only folder nodes (Tag=null), no tagged nodes
        Mock<ITreeNode> folderNode = new Mock<ITreeNode>();
        folderNode.Setup(x => x.Tag).Returns((object?)null);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns((object?)null);

        List<ITreeNode> draggedNodes = new() { folderNode.Object };

        // Act - should not throw; folder processing gracefully skips
        // because mocked nodes don't pass IsComponentsFolderTreeNode check
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, dropTarget: null);

        // Assert - no exception thrown is the success condition
    }

    [Fact]
    public void DecideWireframeDragEffect_FilesAllowed_Accepts()
    {
        // A file drop with a Screen/Component + state selected is allowed, so
        // GetFileDropBlockedReason() returns null and the drop is accepted.
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        StateSave state = new StateSave { Name = "Default" };
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(component);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(state);

        DragAcceptDecision decision = _dragDropManager.DecideWireframeDragEffect(hasFileDrop: true, hasNodes: false);

        decision.Accept.ShouldBeTrue();
        decision.BlockedReason.ShouldBeNull();
    }

    [Fact]
    public void DecideWireframeDragEffect_Neither_Rejects()
    {
        // Neither a file drop nor a node payload: nothing to accept, nothing to
        // surface as a blocked reason.
        DragAcceptDecision decision = _dragDropManager.DecideWireframeDragEffect(hasFileDrop: false, hasNodes: false);

        decision.Accept.ShouldBeFalse();
        decision.BlockedReason.ShouldBeNull();
    }

    [Fact]
    public void DecideWireframeDragEffect_Nodes_Accepts()
    {
        // A node payload is always accepted regardless of selection state — even
        // when a file drop would be blocked, the node drop still wins.
        DragAcceptDecision decision = _dragDropManager.DecideWireframeDragEffect(hasFileDrop: false, hasNodes: true);

        decision.Accept.ShouldBeTrue();
        decision.BlockedReason.ShouldBeNull();
    }

    [Fact]
    public void DecideWireframeDragEffect_FilesBlocked_RejectsAndSurfacesReason()
    {
        // A file drop while a Standard element is selected is blocked (#3128): the
        // drop is rejected and the reason is surfaced so the otherwise-silent
        // failure is diagnosable.
        StandardElementSave standardElement = new StandardElementSave { Name = "Sprite" };
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStandardElement).Returns(standardElement);

        DragAcceptDecision decision = _dragDropManager.DecideWireframeDragEffect(hasFileDrop: true, hasNodes: false);

        decision.Accept.ShouldBeFalse();
        decision.BlockedReason.ShouldNotBeNull();
        decision.BlockedReason.ShouldContain("Sprite");
    }

    [Fact]
    public void GetFileDropBlockedReason_ReturnsNull_WhenComponentAndStateSelected()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        StateSave state = new StateSave { Name = "Default" };
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(component);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(state);

        string? reason = _dragDropManager.GetFileDropBlockedReason();

        reason.ShouldBeNull();
    }

    [Fact]
    public void GetFileDropBlockedReason_ReturnsReason_WhenNoElementSelected()
    {
        // SelectedElement is left null (AutoMocker default).
        string? reason = _dragDropManager.GetFileDropBlockedReason();

        reason.ShouldNotBeNull();
        reason.ShouldContain("no Screen or Component");
    }

    [Fact]
    public void GetFileDropBlockedReason_ReturnsReason_WhenNoStateSelected()
    {
        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(component);
        // SelectedStateSave is left null (AutoMocker default).

        string? reason = _dragDropManager.GetFileDropBlockedReason();

        reason.ShouldNotBeNull();
        reason.ShouldContain("no state");
    }

    [Fact]
    public void GetFileDropBlockedReason_ReturnsReason_WhenStandardElementSelected()
    {
        StandardElementSave standardElement = new StandardElementSave { Name = "Sprite" };
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStandardElement).Returns(standardElement);

        string? reason = _dragDropManager.GetFileDropBlockedReason();

        reason.ShouldNotBeNull();
        reason.ShouldContain("Sprite");
    }
}
