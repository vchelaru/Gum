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
/// (unset/null) emits nothing. Covered for both MonoGame and Skia output, and for a single overridden
/// corner as well as all four at once.
/// </summary>
public class CodeGeneratorCornerRadiusOverrideTests : BaseTestClass
{
    private static readonly string[] CornerVariableNames =
    {
        "CustomRadiusTopLeft", "CustomRadiusTopRight", "CustomRadiusBottomLeft", "CustomRadiusBottomRight"
    };

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

    private static CodeOutputProjectSettings CreateSkia() => new CodeOutputProjectSettings
    {
        OutputLibrary = OutputLibrary.Skia,
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
    /// Mirrors StandardElementsManager defining CornerRadius + the four CustomRadius* overrides on
    /// the Rectangle standard element. Codegen only emits an instance variable whose root is defined
    /// on the base element, so the base must declare every variable a test sets on an instance.
    /// </summary>
    private void RegisterRectangleStandard()
    {
        StandardElementSave element = new StandardElementSave { Name = "Rectangle" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(defaultState);
        AddVariable(element, "CornerRadius", 0.0f, "float");
        foreach (string cornerVariableName in CornerVariableNames)
        {
            AddVariable(element, cornerVariableName, null, "float?");
        }
        Project.StandardElements.Add(element);
    }

    /// <summary>
    /// Builds a component containing a single Rectangle instance and returns the generated code for
    /// that instance, with the given corner-radius variables set (root name, value).
    /// </summary>
    private string GenerateRectangleInstanceCode(
        CodeOutputProjectSettings settings,
        params (string variableName, float value)[] cornerValues)
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

        foreach ((string variableName, float value) in cornerValues)
        {
            AddVariable(main, $"RectInstance.{variableName}", value, "float?");
        }

        Project.Components.Add(main);

        ObjectFinder.Self.GumProjectSave = Project;
        try
        {
            return CreateCodeGenerator().GetCodeForInstance(rectangleInstance, main, settings);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Fact]
    public void GetCodeForInstance_CustomRadiusTopLeftExplicit_MonoGame_EmitsAssignment()
    {
        string code = GenerateRectangleInstanceCode(CreateMonoGame(), ("CustomRadiusTopLeft", 12.5f));

        code.ShouldContain("RectInstance.CustomRadiusTopLeft = 12.5");
    }

    [Fact]
    public void GetCodeForInstance_CustomRadiusTopLeftExplicit_Skia_EmitsAssignment()
    {
        string code = GenerateRectangleInstanceCode(CreateSkia(), ("CustomRadiusTopLeft", 12.5f));

        code.ShouldContain("RectInstance.CustomRadiusTopLeft = 12.5");
    }

    [Fact]
    public void GetCodeForInstance_CustomRadiusTopLeftUnset_EmitsNoAssignment()
    {
        // CustomRadiusTopLeft left unset (its default, null) - should not clutter generated code.
        string code = GenerateRectangleInstanceCode(CreateMonoGame());

        code.ShouldNotContain("CustomRadiusTopLeft");
    }

    [Fact]
    public void GetCodeForInstance_AllFourCustomRadiusCorners_MonoGame_EmitsAllAssignments()
    {
        string code = GenerateRectangleInstanceCode(
            CreateMonoGame(),
            ("CustomRadiusTopLeft", 1f),
            ("CustomRadiusTopRight", 2f),
            ("CustomRadiusBottomLeft", 3f),
            ("CustomRadiusBottomRight", 4f));

        code.ShouldContain("RectInstance.CustomRadiusTopLeft = 1");
        code.ShouldContain("RectInstance.CustomRadiusTopRight = 2");
        code.ShouldContain("RectInstance.CustomRadiusBottomLeft = 3");
        code.ShouldContain("RectInstance.CustomRadiusBottomRight = 4");
    }

    [Fact]
    public void GetCodeForInstance_AllFourCustomRadiusCorners_Skia_EmitsAllAssignments()
    {
        string code = GenerateRectangleInstanceCode(
            CreateSkia(),
            ("CustomRadiusTopLeft", 1f),
            ("CustomRadiusTopRight", 2f),
            ("CustomRadiusBottomLeft", 3f),
            ("CustomRadiusBottomRight", 4f));

        code.ShouldContain("RectInstance.CustomRadiusTopLeft = 1");
        code.ShouldContain("RectInstance.CustomRadiusTopRight = 2");
        code.ShouldContain("RectInstance.CustomRadiusBottomLeft = 3");
        code.ShouldContain("RectInstance.CustomRadiusBottomRight = 4");
    }
}
