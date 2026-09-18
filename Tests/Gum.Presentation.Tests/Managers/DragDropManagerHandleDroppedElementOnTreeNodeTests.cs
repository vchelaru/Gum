using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Moq.AutoMock;

namespace Gum.Presentation.Tests.Managers;

/// <summary>
/// Covers #4837: <c>HandleDroppedElementOnTreeNode</c> widened from Standards-only to any
/// <see cref="ElementSave"/> so Ctrl+Shift-click on a top-level Component/Screen node can reuse it
/// too. The Standards-only cases stay pinned in <c>GumToolUnitTests.Managers.DragDropManagerTests</c>.
/// </summary>
public class DragDropManagerHandleDroppedElementOnTreeNodeTests : BaseTestClass
{
    [Fact]
    public void HandleDroppedElementOnTreeNode_ComponentOnComponent_AddsInstanceOfComponentType()
    {
        AutoMocker mocker = new AutoMocker();
        DragDropManager dragDropManager = mocker.CreateInstance<DragDropManager>();

        mocker.GetMock<IUndoManager>()
            .Setup(x => x.RequestLock())
            .Returns((UndoLock)null!);

        mocker.GetMock<ICircularReferenceManager>()
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);

        ComponentSave draggedComponent = new ComponentSave { Name = "Button" };
        ComponentSave targetComponent = new ComponentSave { Name = "TargetComponent" };
        targetComponent.States.Add(new Gum.DataTypes.Variables.StateSave());

        Mock<ITreeNode> targetNode = new Mock<ITreeNode>();
        targetNode.Setup(x => x.Tag).Returns(targetComponent);

        dragDropManager.HandleDroppedElementOnTreeNode(draggedComponent, targetNode.Object);

        mocker.GetMock<IElementCommands>()
            .Verify(x => x.AddInstance(targetComponent, It.IsAny<string>(), "Button", null, (int?)null), Times.Once);
    }
}
