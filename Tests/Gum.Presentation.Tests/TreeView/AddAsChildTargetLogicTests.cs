using Gum.Controls;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Moq;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

public class AddAsChildTargetLogicTests
{
    private readonly Mock<IAddDestinationTracker> _tracker = new();
    private readonly Mock<IElementTreeRoots> _roots = new();
    private readonly ComponentSave _component = new ComponentSave { Name = "MyComponent" };
    private readonly InstanceSave _container;
    private readonly GumTreeNode _elementNode;
    private readonly GumTreeNode _containerNode;
    private readonly GumTreeNode _childNode;

    public AddAsChildTargetLogicTests()
    {
        _container = new InstanceSave { Name = "Container", ParentContainer = _component };
        InstanceSave child = new InstanceSave { Name = "Child", ParentContainer = _component };
        _component.Instances.Add(_container);
        _component.Instances.Add(child);

        GumTreeNode componentsRoot = new GumTreeNode("Components");
        _elementNode = (GumTreeNode)componentsRoot.AddChild("MyComponent");
        _elementNode.SetTag(_component);
        _containerNode = (GumTreeNode)_elementNode.AddChild("Container");
        _containerNode.SetTag(_container);
        _childNode = (GumTreeNode)_containerNode.AddChild("Child");
        _childNode.SetTag(child);

        _roots.Setup(x => x.Components).Returns(componentsRoot);
    }

    [Fact]
    public void GetTarget_DestinationRemembered_ReturnsItsNodeInsteadOfSelection()
    {
        // #4846: after a Ctrl+Shift-click add, the new child is selected, but the next add must
        // still land under the container the user originally picked.
        _tracker.Setup(x => x.Destination).Returns(_container);
        AddAsChildTargetLogic logic = new AddAsChildTargetLogic(_tracker.Object);

        ITreeNode? target = logic.GetTarget(_roots.Object, _childNode);

        target.ShouldBeSameAs(_containerNode);
    }

    [Fact]
    public void GetTarget_DestinationIsElement_ReturnsElementNode()
    {
        _tracker.Setup(x => x.Destination).Returns(_component);
        AddAsChildTargetLogic logic = new AddAsChildTargetLogic(_tracker.Object);

        ITreeNode? target = logic.GetTarget(_roots.Object, _childNode);

        target.ShouldBeSameAs(_elementNode);
    }

    [Fact]
    public void GetTarget_NoDestination_ReturnsSelection()
    {
        _tracker.Setup(x => x.Destination).Returns((object?)null);
        AddAsChildTargetLogic logic = new AddAsChildTargetLogic(_tracker.Object);

        ITreeNode? target = logic.GetTarget(_roots.Object, _childNode);

        target.ShouldBeSameAs(_childNode);
    }

    [Fact]
    public void GetTarget_DestinationNoLongerInTree_ReturnsSelection()
    {
        InstanceSave removed = new InstanceSave { Name = "Removed", ParentContainer = _component };
        _tracker.Setup(x => x.Destination).Returns(removed);
        AddAsChildTargetLogic logic = new AddAsChildTargetLogic(_tracker.Object);

        ITreeNode? target = logic.GetTarget(_roots.Object, _childNode);

        target.ShouldBeSameAs(_childNode);
    }
}
