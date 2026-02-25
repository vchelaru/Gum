using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
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
                It.IsAny<ISelectedState?>()))
            .Returns(new List<InstanceSave> { draggedInstance });

        // Act
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, 1);

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

        // Act — drop at index 0 (beginning, before A)
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, 0);

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

        // Act — drop at index 3 (after C, which is siblings[2])
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, 3);

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
        folderNode.Setup(x => x.Tag).Returns((object)null);

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
                It.IsAny<ISelectedState?>()))
            .Returns(new List<InstanceSave> { draggedInstance });

        // Act - should not throw even with folder nodes in the list
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, 1);

        // Assert - the tagged instance was still processed correctly despite folder node presence
        destinationComponent.Instances[1].Name.ShouldBe(draggedInstanceName);
    }

    [Fact]
    public void ValidateNodeSorting_NullTarget_ReturnsFalse()
    {
        // Arrange
        Mock<ITreeNode> draggedNode = new Mock<ITreeNode>();
        draggedNode.Setup(x => x.Tag).Returns((object)null);

        // Act
        var result = _dragDropManager.ValidateNodeSorting(
            new[] { draggedNode.Object }, null, 0);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void ValidateNodeSorting_FolderNodeWithNullTag_DoesNotThrow()
    {
        // Arrange - folder node has Tag=null, unlike element/instance nodes
        Mock<ITreeNode> folderNode = new Mock<ITreeNode>();
        folderNode.Setup(x => x.Tag).Returns((object)null);
        folderNode.Setup(x => x.GetFullFilePath()).Returns(new FilePath("C:\\test\\folder\\"));

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns((object)null);
        targetNode.Setup(x => x.GetFullFilePath()).Returns(new FilePath("C:\\test\\other\\"));

        // Act - should not throw; returns false because mocked ITreeNode
        // isn't recognized as a component/screen folder by extension methods
        var result = _dragDropManager.ValidateNodeSorting(
            new[] { folderNode.Object }, targetNode.Object, 0);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void OnNodeSortingDropped_OnlyFolderNodes_DoesNotThrow()
    {
        // Arrange - only folder nodes (Tag=null), no tagged nodes
        Mock<ITreeNode> folderNode = new Mock<ITreeNode>();
        folderNode.Setup(x => x.Tag).Returns((object)null);

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns((object)null);

        List<ITreeNode> draggedNodes = new() { folderNode.Object };

        // Act - should not throw; folder processing gracefully skips
        // because mocked nodes don't pass IsComponentsFolderTreeNode check
        _dragDropManager.OnNodeSortingDropped(draggedNodes, targetNode.Object, 0);

        // Assert - no exception thrown is the success condition
    }
}
