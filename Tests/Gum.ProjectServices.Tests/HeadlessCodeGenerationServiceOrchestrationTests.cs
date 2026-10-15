using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="HeadlessCodeGenerationService.GenerateCodeForElements"/>, the single
/// orchestrator both <c>gumcli codegen</c> and the tool's Generate All go through (issue #6046).
/// </summary>
public class HeadlessCodeGenerationServiceOrchestrationTests : BaseTestClass
{
    private readonly string _tempDirectory;

    public HeadlessCodeGenerationServiceOrchestrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumHeadlessCodeGenOrchestrationTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    private HeadlessCodeGenerationService CreateService(ICodeFileWriter? fileWriter = null)
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
            codeGenNameVerifier, new LocalizationService(), elementSettingsManager, directoryProvider);
        CustomCodeGenerator customCodeGenerator = new CustomCodeGenerator(codeGenerator, codeGenNameVerifier);
        CodeGenerationFileLocationsService fileLocationsService = new CodeGenerationFileLocationsService(
            codeGenerator, codeGenNameVerifier, directoryProvider);

        return new HeadlessCodeGenerationService(
            codeGenerator, customCodeGenerator, fileLocationsService, elementSettingsManager,
            new NullCodeGenLogger(), fileWriter);
    }

    private CodeOutputProjectSettings CreateProjectSettings() => new CodeOutputProjectSettings
    {
        CodeProjectRoot = _tempDirectory + Path.DirectorySeparatorChar,
        RootNamespace = "MyGame",
        OutputLibrary = OutputLibrary.MonoGame
    };

    private string FallbackFilePath => Path.Combine(_tempDirectory, "StandardElements.Generated.cs");

    private ComponentSave AddComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        Project.Components.Add(component);
        return component;
    }

    private ScreenSave AddScreen(string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        Project.Screens.Add(screen);
        return screen;
    }

    private static ElementCodeGenerator Recording(List<(string Name, bool CheckForMissing)> calls) =>
        (element, settings, checkForMissing) =>
        {
            calls.Add((element.Name, checkForMissing));
            return true;
        };

    [Fact]
    public void GenerateCodeForElements_WithNoSubset_GeneratesScreensThenComponentsAndWritesFallbackFile()
    {
        AddScreen("MainMenu");
        AddComponent("Button");
        List<(string Name, bool CheckForMissing)> calls = new();

        CodeGenerationRunResult result = CreateService().GenerateCodeForElements(
            Project, CreateProjectSettings(), new CodeGenerationRunOptions { ElementGenerator = Recording(calls) });

        calls.ShouldBe(new[] { ("MainMenu", false), ("Button", false) });
        result.GeneratedCount.ShouldBe(2);
        File.Exists(FallbackFilePath).ShouldBeTrue();
    }

    [Fact]
    public void GenerateCodeForElements_WithSubset_GeneratesOnlySubsetChecksForMissingAndSkipsFallbackFile()
    {
        AddScreen("MainMenu");
        ComponentSave button = AddComponent("Button");
        List<(string Name, bool CheckForMissing)> calls = new();

        CodeGenerationRunResult result = CreateService().GenerateCodeForElements(
            Project, CreateProjectSettings(), new CodeGenerationRunOptions
            {
                Elements = new ElementSave[] { button },
                ElementGenerator = Recording(calls)
            });

        calls.ShouldBe(new[] { ("Button", true) });
        result.GeneratedCount.ShouldBe(1);
        File.Exists(FallbackFilePath).ShouldBeFalse();
    }

    [Fact]
    public void GenerateCodeForElements_WithEmptySubset_GeneratesNothingAndSkipsFallbackFile()
    {
        AddComponent("Button");

        CodeGenerationRunResult result = CreateService().GenerateCodeForElements(
            Project, CreateProjectSettings(), new CodeGenerationRunOptions { Elements = Array.Empty<ElementSave>() });

        result.GeneratedCount.ShouldBe(0);
        File.Exists(FallbackFilePath).ShouldBeFalse();
    }

    [Fact]
    public void GenerateCodeForElements_WhenGateRejectsAnElement_SkipsItCountsItBlockedAndStillWritesFallbackFile()
    {
        AddComponent("Good");
        AddComponent("Bad");
        List<(string Name, bool CheckForMissing)> calls = new();

        CodeGenerationRunResult result = CreateService().GenerateCodeForElements(
            Project, CreateProjectSettings(), new CodeGenerationRunOptions
            {
                Gate = element => element.Name != "Bad",
                ElementGenerator = Recording(calls)
            });

        calls.ShouldBe(new[] { ("Good", false) });
        result.GeneratedCount.ShouldBe(1);
        result.BlockedCount.ShouldBe(1);
        File.Exists(FallbackFilePath).ShouldBeTrue();
    }

    [Fact]
    public void GenerateCodeForElements_WhenElementIsNeverGenerate_SkipsItWithoutConsultingGate()
    {
        AddComponent("Skipped");
        Directory.CreateDirectory(Path.Combine(_tempDirectory, "Components"));
        File.WriteAllText(
            Path.Combine(_tempDirectory, "Components", "Skipped.codsj"),
            JsonConvert.SerializeObject(new CodeOutputElementSettings { GenerationBehavior = GenerationBehavior.NeverGenerate }));
        List<string> gated = new();
        List<(string Name, bool CheckForMissing)> calls = new();

        CodeGenerationRunResult result = CreateService().GenerateCodeForElements(
            Project, CreateProjectSettings(), new CodeGenerationRunOptions
            {
                Gate = element => { gated.Add(element.Name); return true; },
                ElementGenerator = Recording(calls)
            });

        calls.ShouldBeEmpty();
        gated.ShouldBeEmpty();
        result.GeneratedCount.ShouldBe(0);
        result.BlockedCount.ShouldBe(0);
    }

    [Fact]
    public void GenerateCodeForElements_WhenElementGeneratorFails_DoesNotCountIt()
    {
        AddComponent("Button");

        CodeGenerationRunResult result = CreateService().GenerateCodeForElements(
            Project, CreateProjectSettings(), new CodeGenerationRunOptions
            {
                ElementGenerator = (element, settings, checkForMissing) => false
            });

        result.GeneratedCount.ShouldBe(0);
    }

    [Fact]
    public void GenerateCodeForElements_WritesFallbackFileThroughTheInjectedWriter()
    {
        Mock<ICodeFileWriter> writer = new Mock<ICodeFileWriter>();
        writer.Setup(x => x.TryWrite(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        CreateService(writer.Object).GenerateCodeForElements(Project, CreateProjectSettings());

        writer.Verify(
            x => x.TryWrite(
                It.Is<string>(path => Path.GetFileName(path) == "StandardElements.Generated.cs"),
                It.Is<string>(contents => contents.Contains("RegisterFallbackStandardElements"))),
            Times.Once);
        File.Exists(FallbackFilePath).ShouldBeFalse();
    }

    [Fact]
    public void GenerateCodeForElements_ForMauiOutputLibrary_DoesNotWriteFallbackFile()
    {
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        projectSettings.OutputLibrary = OutputLibrary.Maui;

        CreateService().GenerateCodeForElements(Project, projectSettings);

        File.Exists(FallbackFilePath).ShouldBeFalse();
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
