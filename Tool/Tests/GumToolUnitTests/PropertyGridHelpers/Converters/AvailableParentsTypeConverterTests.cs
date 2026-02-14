using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.PropertyGridHelpers.Converters;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.ComponentModel;

namespace GumToolUnitTests.PropertyGridHelpers.Converters;

public class AvailableParentsTypeConverterTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly Mock<ISelectedState> _selectedStateMock;
    private AvailableParentsTypeConverter _converter = null!;

    public AvailableParentsTypeConverterTests()
    {
        _mocker = new AutoMocker();
        _selectedStateMock = new Mock<ISelectedState>();
    }

    private void CreateConverter()
    {
        _converter = new AvailableParentsTypeConverter(_selectedStateMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldSetExcludeCurrentInstanceToTrue()
    {
        CreateConverter();

        _converter.ExcludeCurrentInstance.ShouldBeTrue();
    }

    #endregion

    #region GetStandardValuesSupported Tests

    [Fact]
    public void GetStandardValuesSupported_ShouldReturnTrue()
    {
        CreateConverter();

        var result = _converter.GetStandardValuesSupported(null);

        result.ShouldBeTrue();
    }

    [Fact]
    public void GetStandardValuesSupported_ShouldReturnTrue_WithContext()
    {
        CreateConverter();
        var context = _mocker.GetMock<ITypeDescriptorContext>().Object;

        var result = _converter.GetStandardValuesSupported(context);

        result.ShouldBeTrue();
    }

    #endregion

    #region GetStandardValuesExclusive Tests

    [Fact]
    public void GetStandardValuesExclusive_ShouldReturnTrue()
    {
        CreateConverter();

        var result = _converter.GetStandardValuesExclusive(null);

        result.ShouldBeTrue();
    }

    [Fact]
    public void GetStandardValuesExclusive_ShouldReturnTrue_WithContext()
    {
        CreateConverter();
        var context = _mocker.GetMock<ITypeDescriptorContext>().Object;

        var result = _converter.GetStandardValuesExclusive(context);

        result.ShouldBeTrue();
    }

    #endregion

    #region GetStandardValues Tests - Basic Scenarios

    [Fact]
    public void GetStandardValues_ShouldReturnNoneOnly_WhenNoElementSelected()
    {
        _selectedStateMock.Setup(x => x.SelectedElement).Returns((ElementSave)null);
        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ShouldBe("<NONE>");
    }

    [Fact]
    public void GetStandardValues_ShouldReturnNoneOnly_WhenElementHasNoInstances()
    {
        var element = new ComponentSave();
        element.States.Add(new StateSave());
        element.DefaultState.ParentContainer = element;

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(element);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);
        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].ShouldBe("<NONE>");
    }

    [Fact]
    public void GetStandardValues_ShouldIncludeAllInstances_WhenNoInstanceSelected()
    {
        var element = new ComponentSave();
        element.States.Add(new StateSave());
        element.DefaultState.ParentContainer = element;

        var instance1 = new InstanceSave { Name = "Instance1", BaseType = "Container" };
        var instance2 = new InstanceSave { Name = "Instance2", BaseType = "Container" };
        element.Instances.Add(instance1);
        element.Instances.Add(instance2);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(element);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("Instance1");
        result[2].ShouldBe("Instance2");
    }

    [Fact]
    public void GetStandardValues_ShouldExcludeCurrentInstance_WhenInstanceSelected()
    {
        var element = new ComponentSave();
        element.States.Add(new StateSave());
        element.DefaultState.ParentContainer = element;

        var instance1 = new InstanceSave { Name = "Instance1", BaseType = "Container" };
        var instance2 = new InstanceSave { Name = "Instance2", BaseType = "Container" };
        var instance3 = new InstanceSave { Name = "Instance3", BaseType = "Container" };
        element.Instances.Add(instance1);
        element.Instances.Add(instance2);
        element.Instances.Add(instance3);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(element);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns(instance2);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("Instance1");
        result[2].ShouldBe("Instance3");
        result.Cast<string>().ShouldNotContain("Instance2");
    }

    #endregion

    #region GetStandardValues Tests - DefaultChildContainer

    [Fact]
    public void GetStandardValues_ShouldIncludeDefaultChildContainer_WhenInstanceIsComponent()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var childComponent = new ComponentSave();
        childComponent.Name = "ChildComponent";
        childComponent.States.Add(new StateSave());
        childComponent.DefaultState.ParentContainer = childComponent;
        childComponent.DefaultState.SetValue("DefaultChildContainer", "InnerContainer");

        var instance = new InstanceSave { Name = "MyChild", BaseType = "ChildComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(childComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
        result[2].ShouldBe("MyChild.InnerContainer");
    }

    [Fact]
    public void GetStandardValues_ShouldNotIncludeDefaultChildContainer_WhenEmpty()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var childComponent = new ComponentSave();
        childComponent.Name = "ChildComponent";
        childComponent.States.Add(new StateSave());
        childComponent.DefaultState.ParentContainer = childComponent;
        childComponent.DefaultState.SetValue("DefaultChildContainer", "");

        var instance = new InstanceSave { Name = "MyChild", BaseType = "ChildComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(childComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
    }

    [Fact]
    public void GetStandardValues_ShouldNotIncludeDefaultChildContainer_WhenNotSet()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var childComponent = new ComponentSave();
        childComponent.Name = "ChildComponent";
        childComponent.States.Add(new StateSave());
        childComponent.DefaultState.ParentContainer = childComponent;

        var instance = new InstanceSave { Name = "MyChild", BaseType = "ChildComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(childComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
    }

    #endregion

    #region GetStandardValues Tests - Slots

    [Fact]
    public void GetStandardValues_ShouldIncludeSlotInstances_WhenComponentHasSlots()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var childComponent = new ComponentSave();
        childComponent.Name = "ChildComponent";
        childComponent.States.Add(new StateSave());
        childComponent.DefaultState.ParentContainer = childComponent;

        var slotInstance = new InstanceSave { Name = "SlotInstance", BaseType = "Container", IsSlot = true };
        childComponent.Instances.Add(slotInstance);

        var instance = new InstanceSave { Name = "MyChild", BaseType = "ChildComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(childComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
        result[2].ShouldBe("MyChild.SlotInstance");
    }

    [Fact]
    public void GetStandardValues_ShouldIncludeMultipleSlots_WhenComponentHasMultipleSlots()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var childComponent = new ComponentSave();
        childComponent.Name = "ChildComponent";
        childComponent.States.Add(new StateSave());
        childComponent.DefaultState.ParentContainer = childComponent;

        var slot1 = new InstanceSave { Name = "Slot1", BaseType = "Container", IsSlot = true };
        var slot2 = new InstanceSave { Name = "Slot2", BaseType = "Container", IsSlot = true };
        var nonSlot = new InstanceSave { Name = "NotASlot", BaseType = "Container", IsSlot = false };

        childComponent.Instances.Add(slot1);
        childComponent.Instances.Add(slot2);
        childComponent.Instances.Add(nonSlot);

        var instance = new InstanceSave { Name = "MyChild", BaseType = "ChildComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave?)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(childComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(4);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
        result[2].ShouldBe("MyChild.Slot1");
        result[3].ShouldBe("MyChild.Slot2");
        result.Cast<string>().ShouldNotContain("MyChild.NotASlot");
    }

    [Fact]
    public void GetStandardValues_ShouldIncludeSlotsFromBaseComponent_WhenComponentInheritsSlots()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var baseComponent = new ComponentSave();
        baseComponent.Name = "BaseComponent";
        baseComponent.States.Add(new StateSave());
        baseComponent.DefaultState.ParentContainer = baseComponent;

        var baseSlot = new InstanceSave { Name = "BaseSlot", BaseType = "Container", IsSlot = true };
        baseComponent.Instances.Add(baseSlot);

        var derivedComponent = new ComponentSave();
        derivedComponent.Name = "DerivedComponent";
        derivedComponent.BaseType = "BaseComponent";
        derivedComponent.States.Add(new StateSave());
        derivedComponent.DefaultState.ParentContainer = derivedComponent;

        var derivedSlot = new InstanceSave { Name = "DerivedSlot", BaseType = "Container", IsSlot = true };
        derivedComponent.Instances.Add(derivedSlot);

        var instance = new InstanceSave { Name = "MyChild", BaseType = "DerivedComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(baseComponent);
        project.Components.Add(derivedComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(4);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
        result[2].ShouldBe("MyChild.DerivedSlot");
        result[3].ShouldBe("MyChild.BaseSlot");
    }

    [Fact]
    public void GetStandardValues_ShouldIncludeSlotMarkedInBaseElement_WhenInstanceDefinedInDerived()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var baseComponent = new ComponentSave();
        baseComponent.Name = "BaseComponent";
        baseComponent.States.Add(new StateSave());
        baseComponent.DefaultState.ParentContainer = baseComponent;

        var baseSlot = new InstanceSave { Name = "SlotInstance", BaseType = "Container", IsSlot = true };
        baseComponent.Instances.Add(baseSlot);

        var derivedComponent = new ComponentSave();
        derivedComponent.Name = "DerivedComponent";
        derivedComponent.BaseType = "BaseComponent";
        derivedComponent.States.Add(new StateSave());
        derivedComponent.DefaultState.ParentContainer = derivedComponent;

        var instance = new InstanceSave { Name = "MyChild", BaseType = "DerivedComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(baseComponent);
        project.Components.Add(derivedComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
        result[2].ShouldBe("MyChild.SlotInstance");
    }

    #endregion

    #region GetStandardValues Tests - Combined Scenarios

    [Fact]
    public void GetStandardValues_ShouldIncludeBothDefaultChildContainerAndSlots_WhenBothExist()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var childComponent = new ComponentSave();
        childComponent.Name = "ChildComponent";
        childComponent.States.Add(new StateSave());
        childComponent.DefaultState.ParentContainer = childComponent;
        childComponent.DefaultState.SetValue("DefaultChildContainer", "InnerContainer");

        var slot1 = new InstanceSave { Name = "Slot1", BaseType = "Container", IsSlot = true };
        var slot2 = new InstanceSave { Name = "Slot2", BaseType = "Container", IsSlot = true };
        childComponent.Instances.Add(slot1);
        childComponent.Instances.Add(slot2);

        var instance = new InstanceSave { Name = "MyChild", BaseType = "ChildComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(childComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(5);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
        result[2].ShouldBe("MyChild.InnerContainer");
        result[3].ShouldBe("MyChild.Slot1");
        result[4].ShouldBe("MyChild.Slot2");
    }

    [Fact]
    public void GetStandardValues_ShouldHandleMultipleInstances_WithMixedConfigurations()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var componentWithSlots = new ComponentSave();
        componentWithSlots.Name = "ComponentWithSlots";
        componentWithSlots.States.Add(new StateSave());
        componentWithSlots.DefaultState.ParentContainer = componentWithSlots;
        var slot = new InstanceSave { Name = "Slot", BaseType = "Container", IsSlot = true };
        componentWithSlots.Instances.Add(slot);

        var componentWithDefaultChild = new ComponentSave();
        componentWithDefaultChild.Name = "ComponentWithDefaultChild";
        componentWithDefaultChild.States.Add(new StateSave());
        componentWithDefaultChild.DefaultState.ParentContainer = componentWithDefaultChild;
        componentWithDefaultChild.DefaultState.SetValue("DefaultChildContainer", "Inner");

        var simpleComponent = new ComponentSave();
        simpleComponent.Name = "SimpleComponent";
        simpleComponent.States.Add(new StateSave());
        simpleComponent.DefaultState.ParentContainer = simpleComponent;

        var instance1 = new InstanceSave { Name = "Instance1", BaseType = "ComponentWithSlots" };
        var instance2 = new InstanceSave { Name = "Instance2", BaseType = "ComponentWithDefaultChild" };
        var instance3 = new InstanceSave { Name = "Instance3", BaseType = "SimpleComponent" };

        parentElement.Instances.Add(instance1);
        parentElement.Instances.Add(instance2);
        parentElement.Instances.Add(instance3);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(componentWithSlots);
        project.Components.Add(componentWithDefaultChild);
        project.Components.Add(simpleComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(6);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("Instance1");
        result[2].ShouldBe("Instance1.Slot");
        result[3].ShouldBe("Instance2");
        result[4].ShouldBe("Instance2.Inner");
        result[5].ShouldBe("Instance3");
    }

    #endregion

    #region GetStandardValues Tests - Edge Cases

    [Fact]
    public void GetStandardValues_ShouldHandleNullComponent_WhenObjectFinderReturnsNull()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var instance = new InstanceSave { Name = "MyChild", BaseType = "NonExistentComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder with empty project
        var project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
    }

    [Fact]
    public void GetStandardValues_ShouldThrowException_WhenComponentHasNullDefaultState()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var childComponent = new ComponentSave();
        childComponent.Name = "ChildComponent";
        // No states added - DefaultState will be null

        var instance = new InstanceSave { Name = "MyChild", BaseType = "ChildComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave?)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(childComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        // Should throw an exception when encountering a component with null DefaultState
        Should.Throw<InvalidOperationException>(() => _converter.GetStandardValues(null))
            .Message.ShouldContain("DefaultState");
    }

    [Fact]
    public void GetStandardValues_ShouldHandleNonStringDefaultChildContainer()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var childComponent = new ComponentSave();
        childComponent.Name = "ChildComponent";
        childComponent.States.Add(new StateSave());
        childComponent.DefaultState.ParentContainer = childComponent;
        childComponent.DefaultState.SetValue("DefaultChildContainer", 123); // Non-string value

        var instance = new InstanceSave { Name = "MyChild", BaseType = "ChildComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(childComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
    }

    [Fact]
    public void GetStandardValues_ShouldHandleScreen_AsElementType()
    {
        var screen = new ScreenSave();
        screen.Name = "TestScreen";
        screen.States.Add(new StateSave());
        screen.DefaultState.ParentContainer = screen;

        var instance1 = new InstanceSave { Name = "Instance1", BaseType = "Container" };
        var instance2 = new InstanceSave { Name = "Instance2", BaseType = "Container" };
        screen.Instances.Add(instance1);
        screen.Instances.Add(instance2);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(screen);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave?)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("Instance1");
        result[2].ShouldBe("Instance2");
    }

    #endregion

    #region GetStandardValues Tests - Deep Inheritance

    [Fact]
    public void GetStandardValues_ShouldHandleDeepInheritance_WhenMultipleLevelsOfBaseTypes()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var level1 = new ComponentSave();
        level1.Name = "Level1";
        level1.States.Add(new StateSave());
        level1.DefaultState.ParentContainer = level1;
        var slot1 = new InstanceSave { Name = "Slot1", BaseType = "Container", IsSlot = true };
        level1.Instances.Add(slot1);

        var level2 = new ComponentSave();
        level2.Name = "Level2";
        level2.BaseType = "Level1";
        level2.States.Add(new StateSave());
        level2.DefaultState.ParentContainer = level2;
        var slot2 = new InstanceSave { Name = "Slot2", BaseType = "Container", IsSlot = true };
        level2.Instances.Add(slot2);

        var level3 = new ComponentSave();
        level3.Name = "Level3";
        level3.BaseType = "Level2";
        level3.States.Add(new StateSave());
        level3.DefaultState.ParentContainer = level3;
        var slot3 = new InstanceSave { Name = "Slot3", BaseType = "Container", IsSlot = true };
        level3.Instances.Add(slot3);

        var instance = new InstanceSave { Name = "MyChild", BaseType = "Level3" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(level1);
        project.Components.Add(level2);
        project.Components.Add(level3);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(5);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
        result[2].ShouldBe("MyChild.Slot3");
        result[3].ShouldBe("MyChild.Slot2");
        result[4].ShouldBe("MyChild.Slot1");
    }

    [Fact]
    public void GetStandardValues_ShouldNotDuplicateSlots_WhenSameNameInBaseAndDerived()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var baseComponent = new ComponentSave();
        baseComponent.Name = "BaseComponent";
        baseComponent.States.Add(new StateSave());
        baseComponent.DefaultState.ParentContainer = baseComponent;
        var baseSlot = new InstanceSave { Name = "SharedSlot", BaseType = "Container", IsSlot = true };
        baseComponent.Instances.Add(baseSlot);

        var derivedComponent = new ComponentSave();
        derivedComponent.Name = "DerivedComponent";
        derivedComponent.BaseType = "BaseComponent";
        derivedComponent.States.Add(new StateSave());
        derivedComponent.DefaultState.ParentContainer = derivedComponent;
        // Same name in derived (this is how inheritance works in Gum)
        var derivedSlot = new InstanceSave { Name = "SharedSlot", BaseType = "Container", IsSlot = false };
        derivedComponent.Instances.Add(derivedSlot);

        var instance = new InstanceSave { Name = "MyChild", BaseType = "DerivedComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(baseComponent);
        project.Components.Add(derivedComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        // Should not include the slot since it's not marked as slot in derived
        result.Count.ShouldBe(2);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
    }

    #endregion

    #region GetStandardValues Tests - Null/Empty Element Cases

    [Fact]
    public void GetStandardValues_ShouldHandleNullBaseType_InInheritanceChain()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var childComponent = new ComponentSave();
        childComponent.Name = "ChildComponent";
        childComponent.BaseType = null;
        childComponent.States.Add(new StateSave());
        childComponent.DefaultState.ParentContainer = childComponent;
        var slot = new InstanceSave { Name = "Slot", BaseType = "Container", IsSlot = true };
        childComponent.Instances.Add(slot);

        var instance = new InstanceSave { Name = "MyChild", BaseType = "ChildComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(childComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
        result[2].ShouldBe("MyChild.Slot");
    }

    [Fact]
    public void GetStandardValues_ShouldHandleEmptyBaseType_InInheritanceChain()
    {
        var parentElement = new ComponentSave();
        parentElement.States.Add(new StateSave());
        parentElement.DefaultState.ParentContainer = parentElement;

        var childComponent = new ComponentSave();
        childComponent.Name = "ChildComponent";
        childComponent.BaseType = "";
        childComponent.States.Add(new StateSave());
        childComponent.DefaultState.ParentContainer = childComponent;
        var slot = new InstanceSave { Name = "Slot", BaseType = "Container", IsSlot = true };
        childComponent.Instances.Add(slot);

        var instance = new InstanceSave { Name = "MyChild", BaseType = "ChildComponent" };
        parentElement.Instances.Add(instance);

        _selectedStateMock.Setup(x => x.SelectedElement).Returns(parentElement);
        _selectedStateMock.Setup(x => x.SelectedInstance).Returns((InstanceSave)null);

        // Setup ObjectFinder
        var project = new GumProjectSave();
        project.Components.Add(childComponent);
        ObjectFinder.Self.GumProjectSave = project;

        CreateConverter();

        var result = _converter.GetStandardValues(null);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result[0].ShouldBe("<NONE>");
        result[1].ShouldBe("MyChild");
        result[2].ShouldBe("MyChild.Slot");
    }

    #endregion
}
