using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.ToolCommands;

public class ElementCommandsTests
{
    ElementCommands _sut;

    Mock<ISelectedState> _selectedState;
    Mock<IGuiCommands> _guiCommands;
    Mock<IFileCommands> _fileCommands;
    Mock<IVariableInCategoryPropagationLogic> _variableInCategoryPropagationLogic;
    Mock<IWireframeObjectManager> _wireframeObjectManager;
    Mock<PluginManager> _pluginManager;
    Mock<IProjectManager> _projectManager;
    Mock<IProjectState> _projectState;

    ObjectFinder _objectFinder => ObjectFinder.Self;

    public ElementCommandsTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _variableInCategoryPropagationLogic = new Mock<IVariableInCategoryPropagationLogic>();
        _wireframeObjectManager = new Mock<IWireframeObjectManager>();
        _pluginManager = new Mock<PluginManager>();
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
        _objectFinder.GumProjectSave = project;

        StandardElementSave text = new()
        {
            Name = "Text"
        };
        _objectFinder.GumProjectSave.StandardElements.Add(text);

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
        _objectFinder.GumProjectSave = project;

        StandardElementSave text = new()
        {
            Name = "Text"
        };
        _objectFinder.GumProjectSave.StandardElements.Add(text);

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
}
