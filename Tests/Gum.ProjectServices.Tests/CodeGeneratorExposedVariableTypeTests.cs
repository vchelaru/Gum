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
/// Exposing a standard-element variable generates a property that forwards to the runtime
/// property of the same name. The property must be typed like the runtime property (not like the
/// project's variable type) and must not emit accessors or members the runtime does not have,
/// or the generated code does not compile.
/// </summary>
public class CodeGeneratorExposedVariableTypeTests : BaseTestClass
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

        return new CodeGenerator(
            codeGenNameVerifier,
            new LocalizationService(),
            elementSettingsManager,
            directoryProvider);
    }

    /// <summary>
    /// Generates a component holding one instance of <paramref name="standardName"/> with
    /// <paramref name="variableName"/> exposed as "Exposed", and returns the generated code.
    /// </summary>
    private string GenerateWithExposedVariable(
        string standardName,
        string variableName,
        string variableType,
        OutputLibrary library = OutputLibrary.MonoGameForms)
    {
        GumProjectSave project = Project;

        if (project.StandardElements.All(item => item.Name != standardName))
        {
            StandardElementSave extra = new StandardElementSave { Name = standardName };
            extra.States.Add(new StateSave { Name = "Default", ParentContainer = extra });
            project.StandardElements.Add(extra);
        }

        // The standard element must define the variable for codegen to resolve its root.
        StandardElementSave standard = project.StandardElements.First(item => item.Name == standardName);
        standard.DefaultState.Variables.Add(new VariableSave
        {
            Name = variableName,
            Type = variableType,
            SetsValue = true,
        });

        ComponentSave component = new ComponentSave { Name = "MainComponent", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Instances.Add(new InstanceSave
        {
            Name = "Inst",
            BaseType = standardName,
            ParentContainer = component,
        });
        component.DefaultState.Variables.Add(new VariableSave
        {
            Name = "Inst." + variableName,
            Type = variableType,
            SetsValue = true,
            ExposedAsName = "Exposed",
        });
        project.Components.Add(component);

        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            return CreateCodeGenerator().GetGeneratedCodeForElement(
                component,
                elementSettings: null!,
                new CodeOutputProjectSettings
                {
                    OutputLibrary = library,
                    RootNamespace = "MyGame",
                    // Raylib supports only FindByName; the exposed property is the same for both.
                    ObjectInstantiationType = ObjectInstantiationType.FindByName,
                });
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Theory]
    [InlineData(OutputLibrary.MonoGame)]
    [InlineData(OutputLibrary.MonoGameForms)]
    [InlineData(OutputLibrary.Raylib)]
    public void ExposedBlend_TypesPropertyAsRuntimeBlend(OutputLibrary library)
    {
        string code = GenerateWithExposedVariable("Sprite", "Blend", "Blend", library);

        // The runtime property is Gum.RenderingLibrary.Blend?; a bare "Blend" binds to the unrelated Gum.Blend.
        code.ShouldContain("public global::Gum.RenderingLibrary.Blend? Exposed");
    }

    [Fact]
    public void ExposedFontSize_TypesPropertyAsFloat()
    {
        string code = GenerateWithExposedVariable("Text", "FontSize", "int");

        // TextRuntime.FontSize is a float; an int getter would not compile.
        code.ShouldContain("public float Exposed");
        code.ShouldNotContain("public int Exposed");
    }

    [Fact]
    public void ExposedFontSize_MauiOutput_KeepsProjectIntType()
    {
        // Maui labels take an int font size; only the Gum runtimes use a float.
        string code = GenerateWithExposedVariable("Text", "FontSize", "int", OutputLibrary.Maui);

        code.ShouldContain("public int Exposed");
    }

    [Fact]
    public void ExposedSourceShaderFile_HasNoGetter()
    {
        string code = GenerateWithExposedVariable("Container", "SourceShaderFile", "string");

        // ContainerRuntime.SourceShaderFile is write-only.
        code.ShouldContain("public string Exposed");
        code.ShouldContain("Inst.SourceShaderFile = value;");
        code.ShouldNotContain("get => Inst.SourceShaderFile");
    }

    [Theory]
    [InlineData("Sprite", OutputLibrary.MonoGameForms, "HasEvents", false)]
    [InlineData("Sprite", OutputLibrary.MonoGameForms, "ExposeChildrenEvents", false)]
    [InlineData("Circle", OutputLibrary.MonoGameForms, "HasEvents", false)]
    [InlineData("Rectangle", OutputLibrary.MonoGame, "ExposeChildrenEvents", false)]
    [InlineData("Sprite", OutputLibrary.Skia, "HasEvents", false)]
    // InteractiveGue-based runtimes do have the properties:
    [InlineData("Container", OutputLibrary.MonoGameForms, "HasEvents", true)]
    [InlineData("Text", OutputLibrary.MonoGameForms, "ExposeChildrenEvents", true)]
    [InlineData("Circle", OutputLibrary.Skia, "HasEvents", true)]
    public void ExposedEventVariable_GeneratedOnlyWhenRuntimeHasProperty(
        string standardName, OutputLibrary library, string variableName, bool expectGenerated)
    {
        string code = GenerateWithExposedVariable(standardName, variableName, "bool", library);

        if (expectGenerated)
        {
            code.ShouldContain("public bool Exposed");
        }
        else
        {
            code.ShouldNotContain("public bool Exposed");
        }
    }

    [Fact]
    public void ExposedContainedType_IsNotGenerated()
    {
        string code = GenerateWithExposedVariable("Container", "ContainedType", "string");

        // ContainedType is read when the project loads; ContainerRuntime has no such property.
        code.ShouldNotContain("public string Exposed");
    }
}
