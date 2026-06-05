using Gum.Content.AnimationChain;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.ProjectServices;
using GumRuntime;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;
using ToolsUtilities;

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
    public void GetAllErrors_ShouldReturnError_WhenComponentInstanceHasInvalidBaseType()
    {
        ComponentSave component = new ComponentSave { Name = "BrokenComponent", BaseType = "Container" };
        component.Instances.Add(new InstanceSave { Name = "BadChild", BaseType = "NonExistentType" });
        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetAllErrors(Project);

        ErrorResult error = errors.ShouldHaveSingleItem();
        error.ElementName.ShouldBe("BrokenComponent");
        error.Message.ShouldContain("NonExistentType");
        error.Severity.ShouldBe(ErrorSeverity.Error);
    }

    [Fact]
    public void GetAllErrors_ShouldReturnError_WhenScreenInstanceHasInvalidBaseType()
    {
        ScreenSave screen = new ScreenSave { Name = "MainMenu" };
        screen.Instances.Add(new InstanceSave { Name = "BadChild", BaseType = "NonExistentType" });
        Project.Screens.Add(screen);

        IReadOnlyList<ErrorResult> errors = _sut.GetAllErrors(Project);

        ErrorResult error = errors.ShouldHaveSingleItem();
        error.ElementName.ShouldBe("MainMenu");
        error.Severity.ShouldBe(ErrorSeverity.Error);
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
        errors[0].Code.ShouldBe("GUM0001");
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

    #region ACHX Origin Errors

    [Fact]
    public void GetErrorsFor_ShouldNotWarn_WhenAchxHasNoOffsets()
    {
        string achxPath = WriteTempAchx(includeOffset: false);
        try
        {
            ComponentSave component = BuildComponentWithSpriteUsingSourceFile(
                achxPath,
                xOrigin: HorizontalAlignment.Left,
                yOrigin: VerticalAlignment.Top);

            IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

            errors.ShouldBeEmpty();
        }
        finally
        {
            File.Delete(achxPath);
        }
    }

    [Fact]
    public void GetErrorsFor_ShouldNotWarn_WhenAchxHasOffsetsAndOriginIsCenter()
    {
        string achxPath = WriteTempAchx(includeOffset: true);
        try
        {
            ComponentSave component = BuildComponentWithSpriteUsingSourceFile(
                achxPath,
                xOrigin: HorizontalAlignment.Center,
                yOrigin: VerticalAlignment.Center);

            IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

            errors.ShouldBeEmpty();
        }
        finally
        {
            File.Delete(achxPath);
        }
    }

    [Fact]
    public void GetErrorsFor_ShouldNotWarn_WhenSourceFileIsNotAchx()
    {
        ComponentSave component = BuildComponentWithSpriteUsingSourceFile(
            "image.png",
            xOrigin: HorizontalAlignment.Left,
            yOrigin: VerticalAlignment.Top);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_ShouldWarn_WhenAchxHasOffsetsAndOriginNotCenter()
    {
        string achxPath = WriteTempAchx(includeOffset: true);
        try
        {
            ComponentSave component = BuildComponentWithSpriteUsingSourceFile(
                achxPath,
                xOrigin: HorizontalAlignment.Left,
                yOrigin: VerticalAlignment.Top);

            IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

            ErrorResult warning = errors.ShouldHaveSingleItem();
            warning.Severity.ShouldBe(ErrorSeverity.Warning);
            warning.Message.ShouldContain("Center");
            warning.Message.ShouldContain("AnimatedSprite");
        }
        finally
        {
            File.Delete(achxPath);
        }
    }

    private ComponentSave BuildComponentWithSpriteUsingSourceFile(
        string sourceFile,
        HorizontalAlignment xOrigin,
        VerticalAlignment yOrigin)
    {
        ComponentSave component = new ComponentSave { Name = "AnimatedThing" };
        component.Instances.Add(new InstanceSave { Name = "AnimatedSprite", BaseType = "Sprite" });

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Name = "AnimatedSprite.SourceFile",
            Value = sourceFile,
            Type = "string",
        });
        defaultState.Variables.Add(new VariableSave
        {
            Name = "AnimatedSprite.XOrigin",
            Value = xOrigin,
            Type = nameof(HorizontalAlignment),
        });
        defaultState.Variables.Add(new VariableSave
        {
            Name = "AnimatedSprite.YOrigin",
            Value = yOrigin,
            Type = nameof(VerticalAlignment),
        });
        component.States.Add(defaultState);
        Project.Components.Add(component);
        return component;
    }

    private static string WriteTempAchx(bool includeOffset)
    {
        AnimationChainListSave achx = new AnimationChainListSave();
        AnimationChainSave chain = new AnimationChainSave { Name = "Chain1" };
        AnimationFrameSave frame = new AnimationFrameSave
        {
            TextureName = "tex.png",
            FrameLength = 0.1f,
        };
        if (includeOffset)
        {
            frame.RelativeY = -3f;
        }
        chain.Frames.Add(frame);
        achx.AnimationChains.Add(chain);

        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".achx");
        FileManager.XmlSerialize(achx, path);
        return path;
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

    #region GUM0002 — VariableReference value disagrees with explicit set

    [Fact]
    public void GetErrorsFor_ShouldPassLeftTypeToCustomEvaluateExpression_SoTypeAwareEvaluatorsResolve()
    {
        // Repro for the bug seen in the live Gum tool: GumExpressionService's
        // EvaluateExpression returns null when desiredType is null (CastTo(null)
        // returns false). The check must pass the LHS type so the Roslyn-based
        // evaluator can resolve cross-element / literal RHSes.
        bool sawNonNullDesiredType = false;
        ElementSaveExtensions.CustomEvaluateExpression = (state, expr, desiredType) =>
        {
            if (desiredType == null)
            {
                return null;
            }
            sawNonNullDesiredType = true;
            return 100f;
        };
        try
        {
            ComponentSave component = new ComponentSave { Name = "TypeAware", BaseType = "Container" };
            StateSave state = new StateSave { Name = "Default", ParentContainer = component };
            component.States.Add(state);
            state.Variables.Add(new VariableSave
            {
                Name = "X",
                Type = "float",
                Value = 14f,
                SetsValue = true
            });

            VariableListSave<string> refs = new VariableListSave<string>
            {
                Name = "VariableReferences",
                Type = "string"
            };
            refs.Value.Add("X = SomeOther.X");
            state.VariableLists.Add(refs);

            Project.Components.Add(component);

            IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

            sawNonNullDesiredType.ShouldBeTrue(
                "because the evaluator must receive the LHS type so type-coercion can succeed.");
            errors.ShouldContain(e => e.Code == "GUM0002",
                customMessage: "and the resulting non-null evaluated value must reach the conflict check.");
        }
        finally
        {
            ElementSaveExtensions.CustomEvaluateExpression = null;
        }
    }


    [Fact]
    public void GetErrorsFor_ShouldReportGum0002_WhenLocalVariableReferenceDisagreesWithMaterializedScalar()
    {
        // A state has BOTH a VariableReferences row that resolves X=100 AND an
        // explicit scalar X=14. The local explicit value wins at lookup time, so the
        // reference is silently ineffective — the author probably didn't realize.
        ComponentSave component = new ComponentSave { Name = "BadComponent", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);

        state.Variables.Add(new VariableSave
        {
            Name = "SourceX",
            Type = "float",
            Value = 100f,
            SetsValue = true
        });
        state.Variables.Add(new VariableSave
        {
            Name = "X",
            Type = "float",
            Value = 14f,
            SetsValue = true
        });

        VariableListSave<string> refs = new VariableListSave<string>
        {
            Name = "VariableReferences",
            Type = "string"
        };
        refs.Value.Add("X = SourceX");
        state.VariableLists.Add(refs);

        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        ErrorResult error = errors.ShouldHaveSingleItem();
        error.Code.ShouldBe("GUM0002");
        error.ElementName.ShouldBe("BadComponent");
        error.Severity.ShouldBe(ErrorSeverity.Warning);
        error.Message.ShouldContain("X");
    }

    [Fact]
    public void GetErrorsFor_ShouldNotReportGum0002_WhenScalarMatchesEvaluatedReference()
    {
        // Regression guard: when the materialized scalar matches the evaluated
        // right side, the state is consistent — no warning.
        ComponentSave component = new ComponentSave { Name = "GoodComponent", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);

        state.Variables.Add(new VariableSave
        {
            Name = "SourceX",
            Type = "float",
            Value = 100f,
            SetsValue = true
        });
        state.Variables.Add(new VariableSave
        {
            Name = "X",
            Type = "float",
            Value = 100f,
            SetsValue = true
        });

        VariableListSave<string> refs = new VariableListSave<string>
        {
            Name = "VariableReferences",
            Type = "string"
        };
        refs.Value.Add("X = SourceX");
        state.VariableLists.Add(refs);

        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.ShouldNotContain(e => e.Code == "GUM0002");
    }

    [Fact]
    public void GetErrorsFor_ShouldReportGum0002_WhenInheritedCategorizedStateReferenceDisagreesWithLocalScalar()
    {
        // The UpgradeButton repro:
        // - Label (base) defines TextCategory.Title with TextInstance.VariableReferences
        //   that resolves FontSize to 100 (via SourceFontSize on the same state).
        //   (We use a variable-to-variable RHS so RecursiveVariableFinder can evaluate
        //   without GumExpressionService wired.)
        // - UpgradeButton derives, has an instance "TextInstance" that's DefinedByBase,
        //   and sets BOTH TextInstance.TextCategoryState = "Title" (which implies
        //   FontSize = 100 via the categorized state's reference) AND a local
        //   TextInstance.FontSize = 14 that conflicts.
        ComponentSave label = new ComponentSave { Name = "LabelForGum0002", BaseType = "Container" };
        StateSave labelDefault = new StateSave { Name = "Default", ParentContainer = label };
        label.States.Add(labelDefault);

        StateSaveCategory textCategory = new StateSaveCategory { Name = "TextCategory" };
        StateSave titleState = new StateSave { Name = "Title", ParentContainer = label };
        titleState.Variables.Add(new VariableSave
        {
            Name = "SourceFontSize",
            Type = "float",
            Value = 100f,
            SetsValue = true
        });
        VariableListSave<string> titleRefs = new VariableListSave<string>
        {
            Name = "VariableReferences",
            Type = "string"
        };
        titleRefs.Value.Add("FontSize = SourceFontSize");
        titleState.VariableLists.Add(titleRefs);
        textCategory.States.Add(titleState);
        label.Categories.Add(textCategory);

        Project.Components.Add(label);

        ComponentSave button = new ComponentSave { Name = "UpgradeButtonForGum0002", BaseType = "Container" };
        StateSave buttonDefault = new StateSave { Name = "Default", ParentContainer = button };
        button.States.Add(buttonDefault);

        InstanceSave textInstance = new InstanceSave
        {
            Name = "TextInstance",
            BaseType = "LabelForGum0002",
            ParentContainer = button
        };
        button.Instances.Add(textInstance);

        // Activate the Title categorized state on the instance.
        buttonDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.TextCategoryState",
            Type = "TextCategory",
            Value = "Title",
            SetsValue = true
        });
        // Local explicit override that conflicts with what the active state would imply.
        buttonDefault.Variables.Add(new VariableSave
        {
            Name = "TextInstance.FontSize",
            Type = "float",
            Value = 14f,
            SetsValue = true
        });

        Project.Components.Add(button);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(button, Project);

        ErrorResult error = errors.ShouldHaveSingleItem();
        error.Code.ShouldBe("GUM0002");
        error.ElementName.ShouldBe("UpgradeButtonForGum0002");
        error.Severity.ShouldBe(ErrorSeverity.Warning);
        error.Message.ShouldContain("TextInstance.FontSize");
    }

    [Fact]
    public void GetErrorsFor_ShouldNotReportGum0002_WhenLocalReferencesRowShadowsInheritedCategorizedRefs()
    {
        // User-reported false positive: Screen has LabelInstance1 with a local
        // VariableReferences row that intentionally excludes FontSize (the user
        // wants the explicit local FontSize=44 to apply). The instance also has
        // TextCategoryState=Title set, and Title's refs row includes FontSize.
        // Per GetVariableListRecursive's local-wins lookup, the local refs row
        // entirely shadows the categorized state's refs — so Title's FontSize
        // ref never fires at apply time. GUM0002 must NOT flag this as a
        // conflict; the user's intent is for the local scalar to win.
        ComponentSave label = new() { Name = "LabelForShadow", BaseType = "Container" };
        StateSave labelDefault = new() { Name = "Default", ParentContainer = label };
        label.States.Add(labelDefault);
        StateSaveCategory textCategory = new() { Name = "TextCategory" };
        StateSave titleState = new() { Name = "Title", ParentContainer = label };
        titleState.Variables.Add(new VariableSave
        {
            Name = "SourceFontSize",
            Type = "int",
            Value = 28,
            SetsValue = true
        });
        VariableListSave<string> titleRefs = new()
        {
            Name = "VariableReferences",
            Type = "string"
        };
        titleRefs.ValueAsIList.Add("FontSize = SourceFontSize");
        titleState.VariableLists.Add(titleRefs);
        textCategory.States.Add(titleState);
        label.Categories.Add(textCategory);
        Project.Components.Add(label);

        ScreenSave screen = new() { Name = "ScreenForShadow" };
        StateSave screenDefault = new() { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        InstanceSave instance = new()
        {
            Name = "LabelInstance1",
            BaseType = "LabelForShadow",
            ParentContainer = screen
        };
        screen.Instances.Add(instance);

        // Local explicit override the user authored.
        screenDefault.Variables.Add(new VariableSave
        {
            Name = "LabelInstance1.FontSize",
            Type = "int",
            Value = 44,
            SetsValue = true
        });
        // Categorized state assignment that, by itself, would point at Title's refs.
        screenDefault.Variables.Add(new VariableSave
        {
            Name = "LabelInstance1.TextCategoryState",
            Type = "TextCategory",
            Value = "Title",
            SetsValue = true
        });
        // The user edited the local refs row to exclude FontSize on purpose — they
        // want the explicit local FontSize to win without ref interference.
        VariableListSave<string> localRefs = new()
        {
            Name = "LabelInstance1.VariableReferences",
            Type = "string"
        };
        localRefs.ValueAsIList.Add("Red = SourceRed");
        screenDefault.VariableLists.Add(localRefs);
        Project.Screens.Add(screen);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(screen, Project);

        errors.ShouldNotContain(
            e => e.Code == "GUM0002" && e.Message.Contains("FontSize"),
            customMessage: "the local LabelInstance1.VariableReferences row shadows Title's refs entirely; " +
                "Title's FontSize ref is never going to fire, so flagging it as a conflict is a false positive.");
    }

    #endregion

    #region GUM0003 — Category state sets its own category's selector

    [Fact]
    public void GetErrorsFor_ShouldReportGum0003_WhenCategoryStateSetsItsOwnCategorySelector()
    {
        // Repro #3055: a behavior tool-only reference materialized TextBoxCategoryState into
        // a TextBoxCategory state, so the state selects a state within its own category - a
        // circular reference that re-drives the category when the state is applied, clobbering
        // its authored values. This is never valid and must be surfaced.
        ComponentSave component = new ComponentSave { Name = "Controls/TextBox", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });

        StateSaveCategory textBoxCategory = new StateSaveCategory { Name = "TextBoxCategory" };
        StateSave focusedState = new StateSave { Name = "Focused", ParentContainer = component };
        focusedState.Variables.Add(new VariableSave
        {
            Name = "TextBoxCategoryState",
            Type = "TextBoxCategory",
            Value = "Enabled",
            SetsValue = true
        });
        textBoxCategory.States.Add(focusedState);
        component.Categories.Add(textBoxCategory);

        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        ErrorResult error = errors.ShouldHaveSingleItem();
        error.Code.ShouldBe("GUM0003");
        error.ElementName.ShouldBe("Controls/TextBox");
        error.Message.ShouldContain("TextBoxCategory");
        error.Message.ShouldContain("Focused");
        error.Severity.ShouldBe(ErrorSeverity.Warning);
    }

    [Fact]
    public void GetErrorsFor_ShouldNotReportGum0003_WhenCategoryStateSetsChildCategorySelector()
    {
        // The normal cascade: a TextBoxCategory state setting a *child* instance's category
        // selector (Border.ColorCategoryState) is the intended mechanism and must not be flagged.
        // It is distinguished by having a SourceObject (the child), unlike the element's own selector.
        ComponentSave component = new ComponentSave { Name = "Controls/TextBox", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });

        StateSaveCategory textBoxCategory = new StateSaveCategory { Name = "TextBoxCategory" };
        StateSave focusedState = new StateSave { Name = "Focused", ParentContainer = component };
        focusedState.Variables.Add(new VariableSave
        {
            Name = "Border.ColorCategoryState",
            Type = "ColorCategory",
            Value = "Primary",
            SetsValue = true
        });
        textBoxCategory.States.Add(focusedState);
        component.Categories.Add(textBoxCategory);

        Project.Components.Add(component);

        IReadOnlyList<ErrorResult> errors = _sut.GetErrorsFor(component, Project);

        errors.ShouldNotContain(e => e.Code == "GUM0003");
    }

    #endregion
}
