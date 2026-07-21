using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.PropertyGridHelpers.Converters;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

public class ElementCommandsTests : BaseTestClass
{
    private readonly ElementCommands _sut;

    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IVariableInCategoryPropagationLogic> _variableInCategoryPropagationLogic;
    private readonly Mock<IWireframeObjectManager> _wireframeObjectManager;
    private readonly Mock<IPluginManager> _pluginManager;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly Mock<IProjectState> _projectState;

    private ObjectFinder ObjectFinder => ObjectFinder.Self;

    public ElementCommandsTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _variableInCategoryPropagationLogic = new Mock<IVariableInCategoryPropagationLogic>();
        _wireframeObjectManager = new Mock<IWireframeObjectManager>();
        _pluginManager = new Mock<IPluginManager>();
        _projectManager = new Mock<IProjectManager>();
        _projectState = new Mock<IProjectState>();

        _sut = new ElementCommands(
            _selectedState.Object,
            _guiCommands.Object,
            _fileCommands.Object,
            _variableInCategoryPropagationLogic.Object,
            _wireframeObjectManager.Object,
            _pluginManager.Object,
            _projectManager.Object,
            _projectState.Object);
    }

    [Fact]
    public void AddCategory_ShouldAddCategoryStateVariable_WithAvailableStatesConverter()
    {
        ComponentSave component = new();
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });

        StateSaveCategory category = _sut.AddCategory(component, "MyCategory");

        VariableSave? stateVariable = component.DefaultState.Variables
            .FirstOrDefault(item => item.Name == "MyCategoryState");

        stateVariable.ShouldNotBeNull();
        stateVariable.CustomTypeConverter.ShouldBeOfType<AvailableStatesConverter>();
        ((AvailableStatesConverter)stateVariable.CustomTypeConverter).CategoryName.ShouldBe("MyCategory");
    }

    [Fact]
    public void AddInstance_AddsInstanceToProject()
    {
        ComponentSave component = new();

        _sut.AddInstance(component, "NewInstanceName", "Sprite");

        component.Instances.Count.ShouldBe(1);
        component.Instances[0].Name.ShouldBe("NewInstanceName");
        component.Instances[0].BaseType.ShouldBe("Sprite");
    }

    [Fact]
    public void AddInstance_ShouldNotifyPlugins()
    {
        ComponentSave component = new();
        _sut.AddInstance(component, "NewInstanceName", "Sprite");
        _pluginManager.Verify(
            x => x.InstanceAdd(
                It.IsAny<ElementSave>(),
                It.Is<InstanceSave>(i => i.Name == "NewInstanceName" && i.BaseType == "Sprite")),
            Times.Once);
    }

    [Fact]
    public void GetUniqueNameForNewInstance_ShouldReturnDefaultName()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.GumProjectSave = project;

        StandardElementSave text = new()
        {
            Name = "Text"
        };
        ObjectFinder.GumProjectSave.StandardElements.Add(text);

        ComponentSave component = new();

        // act
        string name = _sut.GetUniqueNameForNewInstance(text, component);

        // assert
        name.ShouldBe("TextInstance");
    }

    [Fact]
    public void GetUniqueNameForNewInstance_ShouldIncrement_OnMatchingName()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.GumProjectSave = project;

        StandardElementSave text = new()
        {
            Name = "Text"
        };
        ObjectFinder.GumProjectSave.StandardElements.Add(text);

        ComponentSave component = new();
        component.Instances.Add(new InstanceSave
        {
            Name = "TextInstance"
        });

        // act
        string name = _sut.GetUniqueNameForNewInstance(text, component);

        // assert
        name.ShouldBe("TextInstance1");
    }

    [Fact]
    public void GetUniqueNameForNewInstance_WithBehaviorSave_ShouldReturnDefaultName()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.GumProjectSave = project;

        StandardElementSave text = new()
        {
            Name = "Text"
        };
        ObjectFinder.GumProjectSave.StandardElements.Add(text);

        BehaviorSave behavior = new();

        // act
        string name = _sut.GetUniqueNameForNewInstance(text, behavior);

        // assert
        name.ShouldBe("TextInstance");
    }

    [Fact]
    public void GetUniqueNameForNewInstance_WithBehaviorSave_ShouldIncrement_OnMatchingName()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.GumProjectSave = project;

        StandardElementSave text = new()
        {
            Name = "Text"
        };
        ObjectFinder.GumProjectSave.StandardElements.Add(text);

        BehaviorSave behavior = new();
        behavior.RequiredInstances.Add(new BehaviorInstanceSave
        {
            Name = "TextInstance"
        });

        // act
        string name = _sut.GetUniqueNameForNewInstance(text, behavior);

        // assert
        name.ShouldBe("TextInstance1");
    }
}
