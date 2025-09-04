using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.ComponentModel;

namespace GumToolUnitTests.Commands;

public class EditCommandsTests
{
    private readonly AutoMocker _mocker;
    private readonly EditCommands _sut;

    public EditCommandsTests()
    {
        _mocker = new AutoMocker();
        _sut = _mocker.CreateInstance<EditCommands>();
    }


    [Fact]
    public void ShowCreateComponentFromInstancesDialog_ShouldCreateComponentNoChildren_ForSingleInstance()
    {
        InstanceSave instance = new() { Name = "instance" };

        ComponentSave component = new()
        {
            Name = "TestComponent",
            States = [new() { Name = "Default" }],
            Instances = [instance]
        };

        instance.ParentContainer = component;

        _mocker.Setup<ISelectedState, ElementSave>(s => s.SelectedElement!).Returns(component);
        _mocker.Setup<ISelectedState, IEnumerable<InstanceSave>>(s => s.SelectedInstances).Returns([instance]);
        _mocker.SetupWithAny<IDialogService, string>(nameof(IDialogService.GetUserString))
            .Returns("NewComponentName");

        Mock<IProjectCommands> projectCommands = _mocker.GetMock<IProjectCommands>();


        ComponentSave? createdComponent = null;
        projectCommands
            .Setup(pc => pc.AddComponent(It.IsAny<ComponentSave>()))
            .Callback<ComponentSave>(c => createdComponent = c)
            .Verifiable(Times.Once);

        _sut.ShowCreateComponentFromInstancesDialog();

        createdComponent.Instances.Count.ShouldBe(0);
    }

    [Fact]
    public void ShowCreateComponentFromInstancesDialog_ShouldIncludeChildren()
    {
        InstanceSave parentInstance = new() { Name = "ParentInstance" };
        InstanceSave childInstance = new() { Name = "ChildInstance" };
        ComponentSave component = new()
        {
            Name = "TestComponent", 
            States = [new() { Name = "Default" }], 
            Instances = [parentInstance, childInstance]
        };
        component.Instances.ForEach(ins => ins.ParentContainer = component);
        component.DefaultState.SetValue("ChildInstance.Parent", "ParentInstance", "string");
        component.DefaultState.SetValue("ParentInstance.X", 3f, "float");
        component.DefaultState.SetValue("ChildInstance.Y", 5f, "float");
        
        _mocker.SetupWithAny<IDialogService, string>(nameof(IDialogService.GetUserString))
            .Returns("NewComponentName");
        _mocker.Setup<ISelectedState, ElementSave>(s => s.SelectedElement!).Returns(component);
        _mocker.Setup<ISelectedState, IEnumerable<InstanceSave>>(s => s.SelectedInstances).Returns([parentInstance]);

        Mock<IProjectCommands> projectCommands = _mocker.GetMock<IProjectCommands>();

        ComponentSave? result = null;
        projectCommands
            .Setup(pc => pc.AddComponent(It.IsAny<ComponentSave>()))
            .Callback<ComponentSave>(c => result = c)
            .Verifiable(Times.Once);

        // Act
        _sut.ShowCreateComponentFromInstancesDialog();

        // Assert
        projectCommands.Verify();

        result.ShouldNotBeNull();
        result.Instances.Count.ShouldBe(2);

        result.Instances.Count.ShouldBe(1);
        InstanceSave child = result.Instances.Single(x => x.Name == childInstance.Name);

        child.ShouldNotBeNull();

        child.GetParentInstance().ShouldBe(null);

        result.DefaultState.GetValue("X").ShouldBe(3f);
        
        result.DefaultState.GetValue($"{child.Name}.Y").ShouldBe(5f);
    }

}
