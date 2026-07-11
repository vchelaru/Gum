using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using System.Linq;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for #3617: the per-corner CornerRadius overrides (<c>CustomRadiusTopLeft</c> etc.) added to
/// the plain Rectangle standard element must round-trip through code generation like any other
/// variable - an explicit value emits a plain assignment, and leaving it at its nullable default
/// (unset/null) emits nothing.
/// </summary>
public class CodeGeneratorCornerRadiusOverrideTests : BaseTestClass
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

    private static void AddVariable(ElementSave element, string name, object? value, string type) =>
        element.DefaultState.Variables.Add(new VariableSave
        {
            Name = name,
            Value = value,
            SetsValue = true,
            Type = type,
        });

    /// <summary>
    /// Mirrors StandardElementsManager defining CornerRadius + CustomRadiusTopLeft on the Rectangle
    /// standard element. Codegen only emits an instance variable whose root is defined on the base
    /// element, so the base must declare it even though this test only sets CustomRadiusTopLeft.
    /// </summary>
    private StandardElementSave RegisterRectangleStandard()
    {
        StandardElementSave element = new StandardElementSave { Name = "Rectangle" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(defaultState);
        AddVariable(element, "CornerRadius", 0.0f, "float");
        AddVariable(element, "CustomRadiusTopLeft", null, "float?");
        Project.StandardElements.Add(element);
        return element;
    }

    [Fact]
    public void GetCodeForInstance_CustomRadiusTopLeftExplicit_EmitsAssignment()
    {
        RegisterRectangleStandard();

        ComponentSave main = CreateComponent("MainComponent", "Container");
        InstanceSave rectangleInstance = new InstanceSave
        {
            Name = "RectInstance",
            BaseType = "Rectangle",
            ParentContainer = main,
        };
        main.Instances.Add(rectangleInstance);
        AddVariable(main, "RectInstance.CustomRadiusTopLeft", 12.5f, "float?");

        Project.Components.Add(main);

        ObjectFinder.Self.GumProjectSave = Project;
        try
        {
            string code = CreateCodeGenerator().GetCodeForInstance(rectangleInstance, main, CreateMonoGame());

            code.ShouldContain("RectInstance.CustomRadiusTopLeft = 12.5");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetCodeForInstance_CustomRadiusTopLeftUnset_EmitsNoAssignment()
    {
        RegisterRectangleStandard();

        ComponentSave main = CreateComponent("MainComponent", "Container");
        InstanceSave rectangleInstance = new InstanceSave
        {
            Name = "RectInstance",
            BaseType = "Rectangle",
            ParentContainer = main,
        };
        main.Instances.Add(rectangleInstance);
        // CustomRadiusTopLeft left unset (its default, null) - should not clutter generated code.

        Project.Components.Add(main);

        ObjectFinder.Self.GumProjectSave = Project;
        try
        {
            string code = CreateCodeGenerator().GetCodeForInstance(rectangleInstance, main, CreateMonoGame());

            code.ShouldNotContain("CustomRadiusTopLeft");
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }
}
