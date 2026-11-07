using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// An element built on a standard type the engine profile does not support is dormant: the orchestrator writes
/// no code file for it (#7724).
/// </summary>
public class HeadlessCodeGenerationBlockedElementTests : BaseTestClass
{
    private readonly string _tempDirectory;

    public HeadlessCodeGenerationBlockedElementTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumBlockedElementTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public override void Dispose()
    {
        Directory.Delete(_tempDirectory, recursive: true);
        base.Dispose();
    }

    private HeadlessCodeGenerationService CreateService(IVariableRestrictions? restrictions)
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string? whyNotValid = null;
        CommonValidationError error = default;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(_tempDirectory + Path.DirectorySeparatorChar);
        CodeOutputElementSettingsManager elementSettingsManager = new CodeOutputElementSettingsManager(directoryProvider);
        CodeGenerator codeGenerator = new CodeGenerator(
            codeGenNameVerifier, new LocalizationService(), elementSettingsManager, directoryProvider,
            variableRestrictions: restrictions);
        CustomCodeGenerator customCodeGenerator = new CustomCodeGenerator(codeGenerator, codeGenNameVerifier);
        CodeGenerationFileLocationsService fileLocationsService = new CodeGenerationFileLocationsService(
            codeGenerator, codeGenNameVerifier, directoryProvider);

        return new HeadlessCodeGenerationService(
            codeGenerator, customCodeGenerator, fileLocationsService, elementSettingsManager, Mock.Of<ICodeGenLogger>());
    }

    private void AddComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        Project.Components.Add(component);
    }

    private CodeGenerationRunResult Run(IVariableRestrictions? restrictions)
    {
        AddComponent("Hero", "Sprite");
        AddComponent("SuperHero", "Hero");
        AddComponent("Panel", "Container");
        ObjectFinder.Self.GumProjectSave = Project;
        CodeOutputProjectSettings settings = new CodeOutputProjectSettings
        {
            CodeProjectRoot = _tempDirectory + Path.DirectorySeparatorChar,
            RootNamespace = "MyGame",
            OutputLibrary = OutputLibrary.MonoGame,
        };
        return CreateService(restrictions).GenerateCodeForElements(Project, settings);
    }

    private bool HasGeneratedFile(string name) =>
        Directory.GetFiles(_tempDirectory, name + "*.Generated.cs", SearchOption.AllDirectories).Length > 0;

    [Fact]
    public void GenerateCodeForElements_NoRestrictions_WritesEveryComponent()
    {
        CodeGenerationRunResult result = Run(null);

        result.GeneratedCount.ShouldBe(3);
        HasGeneratedFile("Hero").ShouldBeTrue();
    }

    [Fact]
    public void GenerateCodeForElements_ComponentOnBlockedStandardType_WritesNoFileForItOrItsDerivatives()
    {
        CodeGenerationRunResult result = Run(new EngineProfileVariableRestrictions(
            new EngineProfile { SupportedStandardTypes = new List<string> { "Container" } }));

        result.GeneratedCount.ShouldBe(1);
        HasGeneratedFile("Panel").ShouldBeTrue();
        HasGeneratedFile("Hero").ShouldBeFalse();
        HasGeneratedFile("SuperHero").ShouldBeFalse();
        Directory.GetFiles(_tempDirectory, "Hero*.cs", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    [Fact]
    public void GenerateCodeForElements_DisableComponents_WritesNoComponentFilesButStillWritesScreens()
    {
        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        Project.Screens.Add(screen);

        CodeGenerationRunResult result = Run(new EngineProfileVariableRestrictions(
            new EngineProfile { DisableComponents = true }));

        result.GeneratedCount.ShouldBe(1);
        HasGeneratedFile("MainScreen").ShouldBeTrue();
        HasGeneratedFile("Panel").ShouldBeFalse();
        HasGeneratedFile("Hero").ShouldBeFalse();
        Directory.GetFiles(_tempDirectory, "Panel*.cs", SearchOption.AllDirectories).ShouldBeEmpty();
    }
}