using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Moq;
using Shouldly;

namespace GumToolUnitTests.Logic;

public class InheritanceLogicTests : BaseTestClass
{
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly InheritanceLogic _sut;

    public InheritanceLogicTests()
    {
        _fileCommands = new Mock<IFileCommands>();
        _guiCommands = new Mock<IGuiCommands>();

        _sut = new InheritanceLogic(
            _fileCommands.Object,
            _guiCommands.Object,
            StandardElementsManagerGumTool.Self);
    }

    [Fact]
    public void HandleInstanceRenamed_WithNullContainer_DoesNotThrow()
    {
        // Behavior instances fire InstanceRename with a null container (BehaviorSave is
        // not an ElementSave). Before the fix, this caused an ArgumentNullException
        // inside ObjectFinder.GetElementsInheritingFrom.
        var project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        var behaviorInstance = new BehaviorInstanceSave { Name = "NewName" };

        Should.NotThrow(() => _sut.HandleInstanceRenamed(
            container: null,
            instance: behaviorInstance,
            oldName: "OldName"));
    }

    [Fact]
    public void HandleInstanceRenamed_WithNullContainer_DoesNotSaveOrRefresh()
    {
        var project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        var behaviorInstance = new BehaviorInstanceSave { Name = "NewName" };

        _sut.HandleInstanceRenamed(container: null, instance: behaviorInstance, oldName: "OldName");

        _fileCommands.Verify(f => f.TryAutoSaveElement(It.IsAny<ElementSave>()), Times.Never);
        _guiCommands.Verify(g => g.RefreshElementTreeView(It.IsAny<IInstanceContainer>()), Times.Never);
    }

    [Fact]
    public void HandleInstanceRenamed_WithValidContainer_RenamesInstanceInDerivedElements()
    {
        var project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        var baseComponent = new ComponentSave { Name = "Base" };
        baseComponent.InitializeDefaultAndComponentVariables();
        var baseInstance = new InstanceSave { Name = "NewName", BaseType = "Sprite" };
        baseComponent.Instances.Add(baseInstance);
        project.Components.Add(baseComponent);

        var derivedComponent = new ComponentSave { Name = "Derived", BaseType = "Base" };
        derivedComponent.InitializeDefaultAndComponentVariables();
        var derivedInstance = new InstanceSave { Name = "OldName", BaseType = "Sprite" };
        derivedComponent.Instances.Add(derivedInstance);
        project.Components.Add(derivedComponent);

        _sut.HandleInstanceRenamed(container: baseComponent, instance: baseInstance, oldName: "OldName");

        derivedInstance.Name.ShouldBe("NewName");
    }
}
