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
                It.IsAny<HashSet<string>?>()))
            .Returns(new List<InstanceSave> { draggedInstance });

        DropTarget dropTarget = new DropTarget(destinationComponent, null, new DropPosition.InsertAt(1));

        // Act
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, dropTarget);

        // Assert
        destinationComponent.Instances[1].Name.ShouldBe("DraggedInstance");
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
                It.IsAny<HashSet<string>?>()))
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
}
