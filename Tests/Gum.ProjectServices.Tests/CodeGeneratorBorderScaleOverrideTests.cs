using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using System.Linq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// #6983: the per-side BorderScale overrides (<c>CustomBorderScaleLeft</c> etc.) on NineSlice round-trip
/// through code generation like any other variable. An explicit value (including 0, which draws no
/// border on that side) emits a plain assignment, and an unset (null) override emits nothing.
/// </summary>
public class CodeGeneratorBorderScaleOverrideTests : BaseTestClass
{
    private static readonly string[] SideVariableNames =
    {
        "CustomBorderScaleLeft", "CustomBorderScaleTop", "CustomBorderScaleRight", "CustomBorderScaleBottom"
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

        return new CodeGenerator(codeGenNameVerifier, localizationService, elementSettingsManager, directoryProvider);
    }

    private static CodeOutputProjectSettings Settings(OutputLibrary library) => new CodeOutputProjectSettings
    {
        OutputLibrary = library,
        RootNamespace = "MyGame",
    };

    private static void AddVariable(ElementSave element, string name, object? value, string type) =>
        element.DefaultState.Variables.Add(new VariableSave { Name = name, Value = value, SetsValue = true, Type = type });

    private string GenerateNineSliceInstanceCode(OutputLibrary library, params (string variableName, float value)[] sideValues)
    {
        // The base test class already registers a bare NineSlice; declare the variables on it.
        StandardElementSave element = Project.StandardElements.Single(item => item.Name == "NineSlice");
        AddVariable(element, "BorderScale", 1f, "float");
        foreach (string sideVariableName in SideVariableNames)
        {
            AddVariable(element, sideVariableName, null, "float?");
        }

        ComponentSave main = new ComponentSave { Name = "MainComponent", BaseType = "Container" };
        main.States.Add(new StateSave { Name = "Default", ParentContainer = main });
        InstanceSave instance = new InstanceSave { Name = "Frame", BaseType = "NineSlice", ParentContainer = main };
        main.Instances.Add(instance);

        foreach ((string variableName, float value) in sideValues)
        {
            AddVariable(main, $"Frame.{variableName}", value, "float?");
        }

        Project.Components.Add(main);

        ObjectFinder.Self.GumProjectSave = Project;
        try
        {
            return CreateCodeGenerator().GetCodeForInstance(instance, main, Settings(library));
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.Skia)]
    public void GetCodeForInstance_CustomBorderScaleLeftExplicit_EmitsAssignment(OutputLibrary library)
    {
        string code = GenerateNineSliceInstanceCode(library, ("CustomBorderScaleLeft", 2.5f));

        code.ShouldContain("Frame.CustomBorderScaleLeft = 2.5");
    }

    [Fact]
    public void GetCodeForInstance_CustomBorderScaleZero_EmitsAssignment()
    {
        string code = GenerateNineSliceInstanceCode(OutputLibrary.MonoGame, ("CustomBorderScaleTop", 0f));

        code.ShouldContain("Frame.CustomBorderScaleTop = 0");
    }

    [Fact]
    public void GetCodeForInstance_CustomBorderScaleUnset_EmitsNoAssignment()
    {
        string code = GenerateNineSliceInstanceCode(OutputLibrary.MonoGame);

        code.ShouldNotContain("CustomBorderScale");
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.Skia)]
    public void GetCodeForInstance_AllFourSides_EmitsAllAssignments(OutputLibrary library)
    {
        string code = GenerateNineSliceInstanceCode(library,
            ("CustomBorderScaleLeft", 1f), ("CustomBorderScaleTop", 2f),
            ("CustomBorderScaleRight", 3f), ("CustomBorderScaleBottom", 4f));

        code.ShouldContain("Frame.CustomBorderScaleLeft = 1");
        code.ShouldContain("Frame.CustomBorderScaleTop = 2");
        code.ShouldContain("Frame.CustomBorderScaleRight = 3");
        code.ShouldContain("Frame.CustomBorderScaleBottom = 4");
    }
}
