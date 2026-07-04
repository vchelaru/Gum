using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using System;
using System.IO;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="HeadlessCodeGenerationService.GenerateStandardElementsFallbackFile"/> and its
/// hook into <see cref="HeadlessCodeGenerationService.GenerateCodeForAllElements"/>: the headless/CLI
/// equivalent of the tool's "Generate All" path writing the per-project
/// <c>StandardElements.Generated.cs</c> fallback-registration file (issue #3505).
/// </summary>
public class HeadlessCodeGenerationServiceStandardElementsFallbackTests : BaseTestClass
{
    private readonly string _tempDirectory;

    public HeadlessCodeGenerationServiceStandardElementsFallbackTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumHeadlessCodeGenFallbackTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    private static HeadlessCodeGenerationService CreateService()
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
        CodeGenerator codeGenerator = new CodeGenerator(
            codeGenNameVerifier, localizationService, elementSettingsManager, directoryProvider);
        CustomCodeGenerator customCodeGenerator = new CustomCodeGenerator(codeGenerator, codeGenNameVerifier);
        CodeGenerationFileLocationsService fileLocationsService = new CodeGenerationFileLocationsService(
            codeGenerator, codeGenNameVerifier, directoryProvider);

        return new HeadlessCodeGenerationService(
            codeGenerator, customCodeGenerator, fileLocationsService, elementSettingsManager, new NullCodeGenLogger());
    }

    [Fact]
    public void GenerateCodeForAllElements_WritesStandardElementsFallbackFile()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = new CodeOutputProjectSettings
        {
            CodeProjectRoot = _tempDirectory + Path.DirectorySeparatorChar,
            RootNamespace = "MyGame",
            OutputLibrary = OutputLibrary.MonoGame
        };

        CreateService().GenerateCodeForAllElements(project, projectSettings);

        string generatedPath = Path.Combine(_tempDirectory, "StandardElements.Generated.cs");
        File.Exists(generatedPath).ShouldBeTrue();
        string contents = File.ReadAllText(generatedPath);
        contents.ShouldContain("[ModuleInitializer]");
        contents.ShouldContain("ObjectFinder.Self.RegisterFallbackStandardElements(");
    }

    [Fact]
    public void GenerateCodeForAllElements_ForMauiOutputLibrary_DoesNotWriteStandardElementsFallbackFile()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = new CodeOutputProjectSettings
        {
            CodeProjectRoot = _tempDirectory + Path.DirectorySeparatorChar,
            RootNamespace = "MyGame",
            OutputLibrary = OutputLibrary.Maui
        };

        CreateService().GenerateCodeForAllElements(project, projectSettings);

        string generatedPath = Path.Combine(_tempDirectory, "StandardElements.Generated.cs");
        File.Exists(generatedPath).ShouldBeFalse();
    }

    public override void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        base.Dispose();
    }

    private class NullCodeGenLogger : ICodeGenLogger
    {
        public void PrintOutput(string message) { }
        public void PrintError(string message) { }
    }
}
