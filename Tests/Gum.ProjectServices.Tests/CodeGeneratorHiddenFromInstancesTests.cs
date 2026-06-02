using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for #3029: instance value-assignment codegen must not emit variables that are hidden from
/// instances (e.g. a behavior tool-only reference like <c>ButtonCategoryState = IsEnabled ? "Enabled"
/// : "Disabled"</c> that the tool materializes into the instance's state for design-time preview).
/// At runtime the Forms control's own setter owns that visual state, so a baked assignment is at best
/// redundant and at worst a stale double-write. Detection walks the BaseType chain so instances of a
/// type derived from a hiding element are handled too.
/// </summary>
public class CodeGeneratorHiddenFromInstancesTests : BaseTestClass
{
    private static CodeGenerator CreateCodeGenerator()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(projectDirectory: null);
        CodeOutputElementSettingsManager elementSettingsManager = new CodeOutputElementSettingsManager(directoryProvider);
        LocalizationService localizationService = new LocalizationService();

        return new CodeGenerator(
            codeGenNameVerifier,
            localizationService,
            elementSettingsManager,
            directoryProvider);
    }

    private static CodeOutputProjectSettings CreateMonoGame() => new CodeOutputProjectSettings
    {
        OutputLibrary = OutputLibrary.MonoGame,
        RootNamespace = "MyGame",
    };

    private static ComponentSave CreateComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        return component;
    }

    private static void AddVariable(ElementSave element, string name, object value, string type) =>
        element.DefaultState.Variables.Add(new VariableSave
        {
            Name = name,
            Value = value,
            SetsValue = true,
            Type = type,
        });

    [Fact]
    public void GetCodeForInstance_VariableHiddenFromInstances_IsNotEmitted()
    {
        GumProjectSave project = Project;

        ComponentSave button = CreateComponent("Button", "Container");
        button.VariablesHiddenFromInstances.Add("ButtonCategoryState");
        AddVariable(button, "ButtonCategoryState", "Enabled", "string");
        AddVariable(button, "Width", 50f, "float");
        project.Components.Add(button);

        ComponentSave main = CreateComponent("MainComponent", "Container");
        InstanceSave buttonInstance = new InstanceSave
        {
            Name = "ButtonInstance",
            BaseType = "Button",
            ParentContainer = main,
        };
        main.Instances.Add(buttonInstance);
        // The tool materializes both: the hidden category state (from the behavior tool-only
        // reference) and a normal user-authored Width.
        AddVariable(main, "ButtonInstance.ButtonCategoryState", "Enabled", "string");
        AddVariable(main, "ButtonInstance.Width", 100f, "float");
        project.Components.Add(main);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetCodeForInstance(buttonInstance, main, CreateMonoGame());

            // Anchor: the non-hidden variable still flows through the instance-assignment pass.
            code.ShouldContain("Width = 100");
            // The hidden-from-instances variable must not be baked into generated code.
            code.ShouldNotContain("ButtonCategoryState");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetCodeForInstance_VariableHiddenOnBaseType_IsNotEmittedForDerivedInstance()
    {
        // The hiding element (Button) is the BASE of the instance's type (FancyButton). Detection
        // must walk BaseType, mirroring ObjectFinder.IsVariableHiddenRecursively, or the redundant
        // assignment leaks back in for instances of derived types.
        GumProjectSave project = Project;

        ComponentSave button = CreateComponent("Button", "Container");
        button.VariablesHiddenFromInstances.Add("ButtonCategoryState");
        AddVariable(button, "ButtonCategoryState", "Enabled", "string");
        AddVariable(button, "Width", 50f, "float");
        project.Components.Add(button);

        // FancyButton derives from Button and does NOT itself list the variable as hidden.
        ComponentSave fancyButton = CreateComponent("FancyButton", "Button");
        AddVariable(fancyButton, "ButtonCategoryState", "Enabled", "string");
        AddVariable(fancyButton, "Width", 50f, "float");
        project.Components.Add(fancyButton);

        ComponentSave main = CreateComponent("MainComponent", "Container");
        InstanceSave fancyInstance = new InstanceSave
        {
            Name = "FancyInstance",
            BaseType = "FancyButton",
            ParentContainer = main,
        };
        main.Instances.Add(fancyInstance);
        AddVariable(main, "FancyInstance.ButtonCategoryState", "Enabled", "string");
        AddVariable(main, "FancyInstance.Width", 100f, "float");
        project.Components.Add(main);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            string code = CreateCodeGenerator().GetCodeForInstance(fancyInstance, main, CreateMonoGame());

            code.ShouldContain("Width = 100");
            code.ShouldNotContain("ButtonCategoryState");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
