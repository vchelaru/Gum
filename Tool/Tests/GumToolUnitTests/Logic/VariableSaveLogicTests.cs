using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace GumToolUnitTests.Logic;

public class VariableSaveLogicTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _mockSelectedState;
    private readonly Mock<IPluginManager> _mockPluginManager;
    private readonly VariableSaveLogic _logic;

    public VariableSaveLogicTests()
    {
        _mockSelectedState = new Mock<ISelectedState>();
        _mockPluginManager = new Mock<IPluginManager>();
        _mockPluginManager.Setup(p => p.ShouldExclude(It.IsAny<VariableSave>(), It.IsAny<RecursiveVariableFinder>()))
            .Returns(false);
        _logic = new VariableSaveLogic(_mockSelectedState.Object, _mockPluginManager.Object);

        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
    }

    private ComponentSave BuildContainerWithDefaultState()
    {
        ComponentSave container = new ComponentSave();
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = container };
        container.States.Add(defaultState);

        _mockSelectedState.Setup(s => s.SelectedElement).Returns(container);
        _mockSelectedState.Setup(s => s.SelectedStateSave).Returns(container.DefaultState);
        _mockSelectedState.Setup(s => s.SelectedInstance).Returns((InstanceSave?)null);
        _mockSelectedState.Setup(s => s.SelectedScreen).Returns((ScreenSave?)null);

        return container;
    }

    // -----------------------------------------------------------------------
    // GetIfVariableIsActive
    // -----------------------------------------------------------------------

    [Fact]
    public void GetIfVariableIsActive_CustomVariable_ExcludeFromInstances_ReturnsFalse()
    {
        ComponentSave container = BuildContainerWithDefaultState();

        VariableSave variable = new VariableSave
        {
            Name = "MyVar",
            IsCustomVariable = true,
            ExcludeFromInstances = true
        };
        InstanceSave instance = new InstanceSave { Name = "Instance1", DefinedByBase = false };

        bool result = _logic.GetIfVariableIsActive(variable, container, instance);

        result.ShouldBeFalse();
    }

    [Fact]
    public void GetIfVariableIsActive_CustomVariable_ExcludeFromInstances_NullInstance_ReturnsTrue()
    {
        ComponentSave container = BuildContainerWithDefaultState();

        VariableSave variable = new VariableSave
        {
            Name = "MyVar",
            IsCustomVariable = true,
            ExcludeFromInstances = true
        };

        bool result = _logic.GetIfVariableIsActive(variable, container, currentInstance: null);

        result.ShouldBeTrue();
    }

    // -----------------------------------------------------------------------
    // GetShouldIncludeBasedOnBaseType (VariableListSave overload)
    // -----------------------------------------------------------------------

    [Fact]
    public void GetShouldIncludeBasedOnBaseType_ScreenContainer_ReturnsTrue()
    {
        ScreenSave screen = new ScreenSave();
        VariableListSave variableList = new VariableListSave<string> { Name = "SomeList" };

        bool result = _logic.GetShouldIncludeBasedOnBaseType(variableList, screen, rootElementSave: null);

        result.ShouldBeTrue();
    }

    [Fact]
    public void GetShouldIncludeBasedOnBaseType_ComponentWithSourceObject_NoSelectedInstance_ReturnsFalse()
    {
        ComponentSave container = new ComponentSave();
        // SourceObject is derived from the part of Name before the dot
        VariableListSave variableList = new VariableListSave<string> { Name = "ListInstance.Items" };
        _mockSelectedState.Setup(s => s.SelectedInstance).Returns((InstanceSave?)null);

        bool result = _logic.GetShouldIncludeBasedOnBaseType(variableList, container, rootElementSave: null);

        result.ShouldBeFalse();
    }

    // -----------------------------------------------------------------------
    // IsVariableHiddenForInstance
    // -----------------------------------------------------------------------

    [Fact]
    public void IsVariableHiddenForInstance_HiddenListEmpty_ReturnsFalse()
    {
        ComponentSave component = new ComponentSave { Name = "Button" };
        ObjectFinder.Self.GumProjectSave.Components.Add(component);

        InstanceSave instance = new InstanceSave { Name = "ButtonInstance", BaseType = "Button" };

        bool result = _logic.IsVariableHiddenForInstance("ButtonCategoryState", instance);

        result.ShouldBeFalse();
    }

    [Fact]
    public void IsVariableHiddenForInstance_VariableInHiddenList_ReturnsTrue()
    {
        ComponentSave component = new ComponentSave { Name = "Button" };
        component.VariablesHiddenFromInstances.Add("ButtonCategoryState");
        ObjectFinder.Self.GumProjectSave.Components.Add(component);

        InstanceSave instance = new InstanceSave { Name = "ButtonInstance", BaseType = "Button" };

        bool result = _logic.IsVariableHiddenForInstance("ButtonCategoryState", instance);

        result.ShouldBeTrue();
    }

    [Fact]
    public void IsVariableHiddenForInstance_NullElement_ReturnsFalse()
    {
        // Instance whose BaseType doesn't exist in the project
        InstanceSave instance = new InstanceSave { Name = "OrphanInstance", BaseType = "NonExistentComponent" };

        bool result = _logic.IsVariableHiddenForInstance("SomeVar", instance);

        result.ShouldBeFalse();
    }
}
