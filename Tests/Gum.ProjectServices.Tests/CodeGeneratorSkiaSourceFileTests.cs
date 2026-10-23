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
/// Issue #6510: Skia's string <c>SpriteRuntime.SourceFile</c> is obsolete in favor of
/// <c>SourceFileName</c>, so Skia and MAUI codegen assign a sprite's file through
/// <c>SourceFileName</c>. Skia-only runtimes whose live API is <c>SourceFile</c> (Svg, Lottie) keep it.
/// </summary>
public class CodeGeneratorSkiaSourceFileTests : BaseTestClass
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

    // Codegen only emits an instance variable whose root is defined on the base element.
    private void RegisterStandardWithSourceFile(string name)
    {
        StandardElementSave? element = Project.StandardElements.FirstOrDefault(item => item.Name == name);
        if (element == null)
        {
            element = new StandardElementSave { Name = name };
            element.States.Add(new StateSave { Name = "Default", ParentContainer = element });
            Project.StandardElements.Add(element);
        }
        if (element.DefaultState.GetVariableSave("SourceFile") == null)
        {
            element.DefaultState.Variables.Add(new VariableSave
            {
                Name = "SourceFile",
                Value = null,
                Type = "string",
                IsFile = true,
                SetsValue = true,
            });
        }
    }

    private string GenerateInstanceCode(string baseType, string instanceName, OutputLibrary outputLibrary)
    {
        RegisterStandardWithSourceFile(baseType);

        ComponentSave main = new ComponentSave { Name = "MainComponent", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = main };
        main.States.Add(defaultState);

        InstanceSave instance = new InstanceSave { Name = instanceName, BaseType = baseType, ParentContainer = main };
        main.Instances.Add(instance);
        defaultState.Variables.Add(new VariableSave
        {
            Name = $"{instanceName}.SourceFile",
            Value = "image.png",
            Type = "string",
            IsFile = true,
            SetsValue = true,
        });

        Project.Components.Add(main);

        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            OutputLibrary = outputLibrary,
            RootNamespace = "MyGame",
        };

        ObjectFinder.Self.GumProjectSave = Project;
        try
        {
            return CreateCodeGenerator().GetCodeForInstance(instance, main, settings);
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = null;
        }
    }

    [Theory]
    [InlineData(OutputLibrary.Skia)]
    [InlineData(OutputLibrary.Maui)]
    public void GetCodeForInstance_SpriteSourceFile_AssignsSourceFileName(OutputLibrary outputLibrary)
    {
        string code = GenerateInstanceCode("Sprite", "SpriteInstance", outputLibrary);

        code.ShouldContain("SpriteInstance.SourceFileName = ", customMessage: code);
        code.ShouldNotContain("SpriteInstance.SourceFile = ");
    }

    [Theory]
    [InlineData("Svg", OutputLibrary.Skia)]
    [InlineData("Svg", OutputLibrary.Maui)]
    [InlineData("LottieAnimation", OutputLibrary.Skia)]
    public void GetCodeForInstance_SkiaOnlySourceFile_KeepsSourceFile(string baseType, OutputLibrary outputLibrary)
    {
        string code = GenerateInstanceCode(baseType, "Instance", outputLibrary);

        code.ShouldContain("Instance.SourceFile = ", customMessage: code);
        code.ShouldNotContain("SourceFileName");
    }
}
