using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class HeadlessErrorCheckerTests : BaseTestClass
{
    private readonly HeadlessErrorChecker _sut;
    private readonly Mock<ITypeResolver> _mockTypeResolver;

    public HeadlessErrorCheckerTests()
    {
        _mockTypeResolver = new Mock<ITypeResolver>();
        _sut = new HeadlessErrorChecker(_mockTypeResolver.Object);
    }

    #region GetAllErrors

    [Fact]
    public void GetAllErrors_ShouldCheckAllElementTypes()
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen", BaseType = "NonExistent" };
        ComponentSave component = new ComponentSave { Name = "TestComponent", BaseType = "AlsoNonExistent" };
        Project.Screens.Add(screen);
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetAllErrors(Project);

        errors.Count.ShouldBeGreaterThanOrEqualTo(2);
        errors.ShouldContain(e => e.ElementName == "TestScreen");
        errors.ShouldContain(e => e.ElementName == "TestComponent");
    }

    [Fact]
    public void GetAllErrors_ShouldReturnEmpty_WhenProjectHasNoElements()
    {
        GumProjectSave emptyProject = new GumProjectSave();

        IReadOnlyList<ErrorResult> errors = _sut.GetAllErrors(emptyProject);

        errors.Count.ShouldBe(0);
    }

    #endregion

    #region Behavior Errors

    [Fact]
    public void GetErrorsFor_ShouldNotReportError_WhenBehaviorExistsAndIsSatisfied()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "IToggle" };
        Project.Behaviors.Add(behavior);

        ComponentSave component = new ComponentSave { Name = "ToggleButton" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "IToggle" });
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.Count.ShouldBe(0);
    }

    [Fact]
    public void GetErrorsFor_ShouldReportError_WhenBehaviorInstanceIsMissing()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "IToggle" };
        behavior.RequiredInstances.Add(new BehaviorInstanceSave { Name = "ToggleSprite", BaseType = "Sprite" });
        Project.Behaviors.Add(behavior);

        ComponentSave component = new ComponentSave { Name = "ToggleButton" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "IToggle" });
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldContain("ToggleSprite");
        errors[0].Message.ShouldContain("IToggle");
        errors[0].ElementName.ShouldBe("ToggleButton");
    }

    [Fact]
    public void GetErrorsFor_ShouldReportError_WhenBehaviorReferenceIsMissing()
    {
        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "NonExistentBehavior" });
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldContain("NonExistentBehavior");
        errors[0].ElementName.ShouldBe("TestComponent");
    }

    [Fact]
    public void GetErrorsFor_ShouldReportError_WhenBehaviorVariableHasWrongType()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "IToggle" };
        behavior.RequiredVariables.Variables.Add(new VariableSave { Name = "IsToggled", Type = "bool" });
        Project.Behaviors.Add(behavior);

        ComponentSave component = new ComponentSave { Name = "ToggleButton" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "IToggle" });
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "IsToggled", Type = "string" });
        component.States.Add(defaultState);
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldContain("wrong type");
    }

    [Fact]
    public void GetErrorsFor_ShouldReportError_WhenBehaviorVariableIsMissing()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "IToggle" };
        behavior.RequiredVariables.Variables.Add(new VariableSave { Name = "IsToggled", Type = "bool" });
        Project.Behaviors.Add(behavior);

        ComponentSave component = new ComponentSave { Name = "ToggleButton" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "IToggle" });
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldContain("IsToggled");
        errors[0].Message.ShouldContain("doesn't exist");
    }

    #endregion

    #region Element BaseType Errors

    [Fact]
    public void GetErrorsFor_ShouldNotReportError_WhenBaseTypeExists()
    {
        ComponentSave baseComponent = new ComponentSave { Name = "BaseButton" };
        ComponentSave derived = new ComponentSave { Name = "FancyButton", BaseType = "BaseButton" };
        Project.Components.Add(baseComponent);
        Project.Components.Add(derived);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(derived, Project);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_ShouldNotReportError_WhenBaseTypeIsStandardElement()
    {
        ComponentSave component = new ComponentSave { Name = "Panel", BaseType = "Container" };
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_ShouldNotReportError_WhenElementHasNoBaseType()
    {
        ScreenSave screen = new ScreenSave { Name = "MainMenu" };
        Project.Screens.Add(screen);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(screen, Project);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_ShouldReportError_WhenComponentHasInvalidBaseType()
    {
        ComponentSave component = new ComponentSave { Name = "DerivedComponent", BaseType = "NonExistentBase" };
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldContain("NonExistentBase");
        errors[0].ElementName.ShouldBe("DerivedComponent");
    }

    #endregion

    #region Instance BaseType Errors

    [Fact]
    public void GetErrorsFor_ShouldNotReportError_WhenInstanceHasValidBaseType()
    {
        ComponentSave component = new ComponentSave { Name = "Label" };
        component.Instances.Add(new InstanceSave { Name = "TextInstance", BaseType = "Text" });
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_ShouldReportError_WhenInstanceHasInvalidBaseType()
    {
        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        component.Instances.Add(new InstanceSave { Name = "BadInstance", BaseType = "NonExistentType" });
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldContain("NonExistentType");
        errors[0].ElementName.ShouldBe("TestComponent");
    }

    [Fact]
    public void GetErrorsFor_ShouldReportMultipleErrors_WhenMultipleInstancesAreInvalid()
    {
        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        component.Instances.Add(new InstanceSave { Name = "Bad1", BaseType = "Missing1" });
        component.Instances.Add(new InstanceSave { Name = "Bad2", BaseType = "Missing2" });
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.Count.ShouldBe(2);
    }

    #endregion

    #region Parent Errors

    [Fact]
    public void GetErrorsFor_ShouldNotReportError_WhenParentReferenceIsValid()
    {
        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        component.Instances.Add(new InstanceSave { Name = "ParentContainer", BaseType = "Container" });
        component.Instances.Add(new InstanceSave { Name = "ChildSprite", BaseType = "Container" });

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Name = "ChildSprite.Parent",
            Value = "ParentContainer",
            Type = "string"
        });
        component.States.Add(defaultState);
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_ShouldReportError_WhenParentReferenceIsInvalid()
    {
        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        component.Instances.Add(new InstanceSave { Name = "ChildSprite", BaseType = "Sprite" });

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Name = "ChildSprite.Parent",
            Value = "NonExistentParent",
            Type = "string"
        });
        component.States.Add(defaultState);
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldContain("NonExistentParent");
        errors[0].Message.ShouldContain("does not exist");
    }

    #endregion

    #region Invalid Variable Type Errors

    [Fact]
    public void GetErrorsFor_ShouldNotReportError_WhenVariableTypeIsKnown()
    {
        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "Width", Type = "float" });
        defaultState.Variables.Add(new VariableSave { Name = "IsVisible", Type = "bool" });
        defaultState.Variables.Add(new VariableSave { Name = "Label", Type = "string" });
        component.States.Add(defaultState);
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_ShouldNotReportError_WhenVariableTypeIsResolvedByTypeResolver()
    {
        _mockTypeResolver.Setup(r => r.GetTypeFromString("HorizontalAlignment"))
            .Returns(typeof(int)); // just needs to return non-null

        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "HAlign", Type = "HorizontalAlignment" });
        component.States.Add(defaultState);
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_ShouldNotReportError_WhenVariableTypeIsStateCategory()
    {
        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        component.Categories.Add(new StateSaveCategory { Name = "ButtonMode" });
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "ButtonModeState", Type = "ButtonModeState" });
        component.States.Add(defaultState);
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_ShouldReportError_WhenVariableTypeIsUnknown()
    {
        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "CustomProp", Type = "CompletelyUnknownType" });
        component.States.Add(defaultState);
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldContain("CompletelyUnknownType");
        errors[0].Message.ShouldContain("unknown type");
    }

    [Fact]
    public void GetErrorsFor_ShouldWarn_WhenVariableNameMissingStateSuffix()
    {
        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        component.Categories.Add(new StateSaveCategory { Name = "ButtonMode" });
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        // Name is "ButtonMode" but should be "ButtonModeState"
        defaultState.Variables.Add(new VariableSave { Name = "ButtonMode", Type = "ButtonMode" });
        component.States.Add(defaultState);
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.Count.ShouldBe(1);
        errors[0].Severity.ShouldBe(ErrorSeverity.Warning);
        errors[0].Message.ShouldContain("State suffix");
    }

    #endregion

    #region Additional Error Sources

    [Fact]
    public void GetErrorsFor_ShouldIncludeAdditionalErrorSourceErrors()
    {
        Mock<IAdditionalErrorSource> mockSource = new Mock<IAdditionalErrorSource>();
        mockSource.Setup(s => s.GetErrors(It.IsAny<ElementSave>(), It.IsAny<GumProjectSave>()))
            .Returns(new[] { new ErrorResult { ElementName = "Test", Message = "Plugin error" } });

        HeadlessErrorChecker sut = new HeadlessErrorChecker(
            _mockTypeResolver.Object,
            new[] { mockSource.Object });

        ScreenSave screen = new ScreenSave { Name = "Test" };
        Project.Screens.Add(screen);

        IReadOnlyList<ErrorResult> errors = sut.GetErrorsFor(screen, Project);

        errors.Count.ShouldBe(1);
        errors[0].Message.ShouldBe("Plugin error");
    }

    #endregion
}
