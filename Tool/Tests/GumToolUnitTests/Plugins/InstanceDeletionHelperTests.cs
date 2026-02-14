using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Gui.Plugins;
using Gum.Managers;
using Gum.ToolCommands;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace GumToolUnitTests.Plugins;

public class InstanceDeletionHelperTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly InstanceDeletionHelper _helper;
    private readonly Mock<IDeleteLogic> _deleteLogic;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IWireframeCommands> _wireframeCommands;
    private readonly Mock<IFileCommands> _fileCommands;

    public InstanceDeletionHelperTests()
    {
        _mocker = new AutoMocker();
        _deleteLogic = _mocker.GetMock<IDeleteLogic>();
        _guiCommands = _mocker.GetMock<IGuiCommands>();
        _wireframeCommands = _mocker.GetMock<IWireframeCommands>();
        _fileCommands = _mocker.GetMock<IFileCommands>();

        _helper = new InstanceDeletionHelper(
            _deleteLogic.Object,
            _guiCommands.Object,
            _wireframeCommands.Object,
            _fileCommands.Object);
    }

    [Fact]
    public void AnyInstanceHasChildren_NoChildren_ReturnsFalse()
    {
        var screen = CreateScreenWithInstances("Parent1", "Parent2");

        var result = _helper.AnyInstanceHasChildren(screen.Instances);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AnyInstanceHasChildren_EmptyList_ReturnsFalse()
    {
        var result = _helper.AnyInstanceHasChildren(Array.Empty<InstanceSave>());

        result.ShouldBeFalse();
    }

    [Fact]
    public void AnyInstanceHasChildren_MultipleHaveChildren_ReturnsTrue()
    {
        var screen = CreateScreenWithInstances("Parent1", "Parent2");
        AddChild(screen, "Child1", "Parent1");
        AddChild(screen, "Child2", "Parent2");

        var result = _helper.AnyInstanceHasChildren(screen.Instances.Take(2));

        result.ShouldBeTrue();
    }

    [Fact]
    public void AnyInstanceHasChildren_OneHasChildren_ReturnsTrue()
    {
        var screen = CreateScreenWithInstances("Parent1", "Parent2");
        AddChild(screen, "Child1", "Parent1");

        var result = _helper.AnyInstanceHasChildren(screen.Instances.Take(2));

        result.ShouldBeTrue();
    }

    [Fact]
    public void DetachChildrenFromInstance_CallsRemoveParentReferences()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        AddChild(screen, "Child", "Parent");

        _helper.DetachChildrenFromInstance(parent);

        _deleteLogic.Verify(x => x.RemoveParentReferencesToInstance(parent, screen), Times.Once);
    }

    [Fact]
    public void DetachChildrenFromInstance_NullInstance_DoesNotThrow()
    {
        Should.NotThrow(() => _helper.DetachChildrenFromInstance(null));
    }

    [Fact]
    public void DetachChildrenFromInstances_EmptyList_DoesNotThrow()
    {
        Should.NotThrow(() => _helper.DetachChildrenFromInstances(Array.Empty<InstanceSave>()));
    }

    [Fact]
    public void DetachChildrenFromInstances_MixedWithAndWithoutChildren_DetachesAll()
    {
        var screen = CreateScreenWithInstances("Parent1", "Parent2", "Parent3");
        var parent1 = screen.Instances[0];
        var parent2 = screen.Instances[1];
        var parent3 = screen.Instances[2];
        AddChild(screen, "Child1", "Parent1");
        AddChild(screen, "Child3", "Parent3");

        _helper.DetachChildrenFromInstances(new[] { parent1, parent2, parent3 });

        _deleteLogic.Verify(x => x.RemoveParentReferencesToInstance(parent1, screen), Times.Once);
        _deleteLogic.Verify(x => x.RemoveParentReferencesToInstance(parent2, screen), Times.Once);
        _deleteLogic.Verify(x => x.RemoveParentReferencesToInstance(parent3, screen), Times.Once);
    }

    [Fact]
    public void DetachChildrenFromInstances_MultipleInstances_DetachesAll()
    {
        var screen = CreateScreenWithInstances("Parent1", "Parent2");
        var parent1 = screen.Instances[0];
        var parent2 = screen.Instances[1];
        AddChild(screen, "Child1", "Parent1");
        AddChild(screen, "Child2", "Parent2");

        _helper.DetachChildrenFromInstances(new[] { parent1, parent2 });

        _deleteLogic.Verify(x => x.RemoveParentReferencesToInstance(parent1, screen), Times.Once);
        _deleteLogic.Verify(x => x.RemoveParentReferencesToInstance(parent2, screen), Times.Once);
    }

    [Fact]
    public void GetChildrenOf_MultipleChildren_ReturnsAll()
    {
        var screen = CreateScreenWithInstances("Parent");
        var child1 = AddChild(screen, "Child1", "Parent");
        var child2 = AddChild(screen, "Child2", "Parent");
        var child3 = AddChild(screen, "Child3", "Parent");

        var children = _helper.GetChildrenOf(screen.Instances[0]);

        children.Length.ShouldBe(3);
        children.ShouldContain(child1);
        children.ShouldContain(child2);
        children.ShouldContain(child3);
    }

    [Fact]
    public void GetChildrenOf_NoChildren_ReturnsEmpty()
    {
        var screen = CreateScreenWithInstances("Parent");

        var children = _helper.GetChildrenOf(screen.Instances[0]);

        children.ShouldBeEmpty();
    }

    [Fact]
    public void GetChildrenOf_NullInstance_ReturnsEmpty()
    {
        var children = _helper.GetChildrenOf(null);

        children.ShouldBeEmpty();
    }

    [Fact]
    public void GetChildrenOf_OneChild_ReturnsOne()
    {
        var screen = CreateScreenWithInstances("Parent");
        var child = AddChild(screen, "Child", "Parent");

        var children = _helper.GetChildrenOf(screen.Instances[0]);

        children.Length.ShouldBe(1);
        children[0].ShouldBe(child);
    }

    [Fact]
    public void GetChildrenOf_WithDottedParentReference_ReturnsChild()
    {
        var screen = CreateScreenWithInstances("Parent");
        var child = AddChild(screen, "Child", "Parent.Container");

        var children = _helper.GetChildrenOf(screen.Instances[0]);

        children.Length.ShouldBe(1);
        children[0].ShouldBe(child);
    }

    [Fact]
    public void GetChildrenOf_WithGrandchildren_ReturnsOnlyDirectChildren()
    {
        var screen = CreateScreenWithInstances("Parent");
        var child = AddChild(screen, "Child", "Parent");
        AddChild(screen, "Grandchild", "Child");

        var children = _helper.GetChildrenOf(screen.Instances[0]);

        children.Length.ShouldBe(1);
        children[0].ShouldBe(child);
    }

    [Fact]
    public void InstanceHasChildren_NoChildren_ReturnsFalse()
    {
        var screen = CreateScreenWithInstances("Parent");

        var result = _helper.InstanceHasChildren(screen.Instances[0]);

        result.ShouldBeFalse();
    }

    [Fact]
    public void InstanceHasChildren_NullInstance_ReturnsFalse()
    {
        var result = _helper.InstanceHasChildren(null);

        result.ShouldBeFalse();
    }

    [Fact]
    public void InstanceHasChildren_WithChildren_ReturnsTrue()
    {
        var screen = CreateScreenWithInstances("Parent");
        AddChild(screen, "Child", "Parent");

        var result = _helper.InstanceHasChildren(screen.Instances[0]);

        result.ShouldBeTrue();
    }

    [Fact]
    public void InstanceHasChildren_WithGrandchildren_ReturnsTrue()
    {
        var screen = CreateScreenWithInstances("Parent");
        AddChild(screen, "Child", "Parent");
        AddChild(screen, "Grandchild", "Child");

        var result = _helper.InstanceHasChildren(screen.Instances[0]);

        result.ShouldBeTrue();
    }

    [Fact]
    public void RecursivelyDeleteChildrenOf_ProcessesChildrenRecursivelyFirst()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        var child = AddChild(screen, "Child", "Parent");
        var grandchild = AddChild(screen, "Grandchild", "Child");

        var callOrder = new List<string>();

        _deleteLogic.Setup(x => x.RemoveInstance(It.IsAny<InstanceSave>(), It.IsAny<ElementSave>()))
            .Callback<InstanceSave, ElementSave>((inst, elem) => callOrder.Add(inst.Name));

        _helper.RecursivelyDeleteChildrenOf(parent);

        // Verify bottom-up deletion: grandchild deleted before child
        callOrder.Count.ShouldBe(2);
        callOrder[0].ShouldBe("Grandchild");
        callOrder[1].ShouldBe("Child");
    }

    [Fact]
    public void RecursivelyDeleteChildrenOf_MultipleChildren_RemovesAll()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        var child1 = AddChild(screen, "Child1", "Parent");
        var child2 = AddChild(screen, "Child2", "Parent");
        var child3 = AddChild(screen, "Child3", "Parent");

        _helper.RecursivelyDeleteChildrenOf(parent);

        _deleteLogic.Verify(x => x.RemoveInstance(child1, screen), Times.Once);
        _deleteLogic.Verify(x => x.RemoveInstance(child2, screen), Times.Once);
        _deleteLogic.Verify(x => x.RemoveInstance(child3, screen), Times.Once);
    }

    [Fact]
    public void RecursivelyDeleteChildrenOf_NoChildren_DoesNotCallRemove()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];

        _helper.RecursivelyDeleteChildrenOf(parent);

        _deleteLogic.Verify(x => x.RemoveInstance(It.IsAny<InstanceSave>(), It.IsAny<ElementSave>()), Times.Never);
    }

    [Fact]
    public void RecursivelyDeleteChildrenOf_NullInstance_DoesNotThrow()
    {
        Should.NotThrow(() => _helper.RecursivelyDeleteChildrenOf(null));
    }

    [Fact]
    public void RecursivelyDeleteChildrenOf_OneChild_RemovesChild()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        var child = AddChild(screen, "Child", "Parent");

        _helper.RecursivelyDeleteChildrenOf(parent);

        _deleteLogic.Verify(x => x.RemoveInstance(child, screen), Times.Once);
    }

    [Fact]
    public void RecursivelyDeleteChildrenOf_WithGrandchildren_RemovesBottomUp()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        var child = AddChild(screen, "Child", "Parent");
        var grandchild = AddChild(screen, "Grandchild", "Child");

        var deletionOrder = new List<InstanceSave>();
        _deleteLogic.Setup(x => x.RemoveInstance(It.IsAny<InstanceSave>(), It.IsAny<ElementSave>()))
            .Callback<InstanceSave, ElementSave>((inst, elem) => deletionOrder.Add(inst));

        _helper.RecursivelyDeleteChildrenOf(parent);

        deletionOrder.Count.ShouldBe(2);
        deletionOrder[0].ShouldBe(grandchild, "Grandchild should be deleted first (bottom-up)");
        deletionOrder[1].ShouldBe(child, "Child should be deleted second");
    }

    [Fact]
    public void RecursivelyDeleteChildrenOfInstances_EmptyList_OnlyRefreshes()
    {
        var screen = CreateScreenWithInstances("Parent");

        _helper.RecursivelyDeleteChildrenOfInstances(Array.Empty<InstanceSave>(), screen);

        _deleteLogic.Verify(x => x.RemoveInstance(It.IsAny<InstanceSave>(), It.IsAny<ElementSave>()), Times.Never);
        _guiCommands.Verify(x => x.RefreshElementTreeView(screen), Times.Once);
        _wireframeCommands.Verify(x => x.Refresh(It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        _fileCommands.Verify(x => x.TryAutoSaveElement(screen), Times.Once);
    }

    [Fact]
    public void RecursivelyDeleteChildrenOfInstances_MultipleParents_DeletesAllChildren()
    {
        var screen = CreateScreenWithInstances("Parent1", "Parent2");
        var parent1 = screen.Instances[0];
        var parent2 = screen.Instances[1];
        var child1 = AddChild(screen, "Child1", "Parent1");
        var child2 = AddChild(screen, "Child2", "Parent2");

        _helper.RecursivelyDeleteChildrenOfInstances(new[] { parent1, parent2 }, screen);

        _deleteLogic.Verify(x => x.RemoveInstance(child1, screen), Times.Once);
        _deleteLogic.Verify(x => x.RemoveInstance(child2, screen), Times.Once);
    }

    [Fact]
    public void RecursivelyDeleteChildrenOfInstances_RefreshesUI()
    {
        var screen = CreateScreenWithInstances("Parent");
        var parent = screen.Instances[0];
        AddChild(screen, "Child", "Parent");

        _helper.RecursivelyDeleteChildrenOfInstances(new[] { parent }, screen);

        _guiCommands.Verify(x => x.RefreshElementTreeView(screen), Times.Once);
        _wireframeCommands.Verify(x => x.Refresh(It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        _fileCommands.Verify(x => x.TryAutoSaveElement(screen), Times.Once);
    }

    [Fact]
    public void RecursivelyDeleteChildrenOfInstances_WithNestedHierarchy_DeletesBottomUp()
    {
        var screen = CreateScreenWithInstances("Parent1", "Parent2");
        var parent1 = screen.Instances[0];
        var parent2 = screen.Instances[1];
        var child1 = AddChild(screen, "Child1", "Parent1");
        var grandchild1 = AddChild(screen, "Grandchild1", "Child1");
        var child2 = AddChild(screen, "Child2", "Parent2");

        var deletionOrder = new List<InstanceSave>();
        _deleteLogic.Setup(x => x.RemoveInstance(It.IsAny<InstanceSave>(), It.IsAny<ElementSave>()))
            .Callback<InstanceSave, ElementSave>((inst, elem) => deletionOrder.Add(inst));

        _helper.RecursivelyDeleteChildrenOfInstances(new[] { parent1, parent2 }, screen);

        deletionOrder.Count.ShouldBe(3);
        deletionOrder[0].ShouldBe(grandchild1, "Deepest child should be deleted first");
        deletionOrder.ShouldContain(child1);
        deletionOrder.ShouldContain(child2);
    }

    private ScreenSave CreateScreenWithInstances(params string[] instanceNames)
    {
        var screen = new ScreenSave();
        screen.Name = "TestScreen";
        
        var defaultState = new StateSave();
        defaultState.ParentContainer = screen;
        defaultState.Name = "Default";
        screen.States.Add(defaultState);

        foreach (var name in instanceNames)
        {
            var instance = new InstanceSave();
            instance.Name = name;
            instance.ParentContainer = screen;
            screen.Instances.Add(instance);
        }

        return screen;
    }

    private InstanceSave AddChild(ScreenSave screen, string childName, string parentName)
    {
        var child = new InstanceSave();
        child.Name = childName;
        child.ParentContainer = screen;
        screen.Instances.Add(child);

        screen.DefaultState.SetValue($"{childName}.Parent", parentName, "string");

        return child;
    }
}
