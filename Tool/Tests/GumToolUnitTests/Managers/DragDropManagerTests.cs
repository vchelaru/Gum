using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
}
