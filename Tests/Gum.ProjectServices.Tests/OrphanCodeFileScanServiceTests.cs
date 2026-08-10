using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="OrphanCodeFileScanService"/> — the catch-all scan comparing the code output
/// folder (and per-element .codsj settings files) against the project's elements (issue #4422 gap 4).
/// </summary>
public class OrphanCodeFileScanServiceTests : BaseTestClass
{
    private readonly string _tempDirectory;

    public OrphanCodeFileScanServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumOrphanScanTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Scan_ShouldFlagCustomCodeFile_WhenItsGeneratedSiblingIsOrphaned()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("Screens/DeletedScreen.Generated.cs", "DeletedScreen");
        WriteFile("Screens/DeletedScreen.cs", "partial class DeletedScreen { }");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings);

        orphans.Count(item => item.Kind == OrphanCodeFileKind.CustomCode).ShouldBe(1);
        orphans.Single(item => item.Kind == OrphanCodeFileKind.CustomCode)
            .FilePath.ShouldBe(new ToolsUtilities.FilePath(Path.Combine(_tempDirectory, "Screens", "DeletedScreen.cs")));
    }

    [Fact]
    public void Scan_ShouldFlagGeneratedFile_WhenElementRenamedOutFromUnderIt()
    {
        GumProjectSave project = Project;
        project.Screens.Add(CreateScreen("RenamedScreen"));
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("Screens/OldScreen.Generated.cs", "OldScreen");
        WriteGeneratedFile("Screens/RenamedScreen.Generated.cs", "RenamedScreen");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings);

        orphans.Select(item => item.FilePath.FileNameNoPath).ShouldBe(new[] { "OldScreen.Generated.cs" });
    }

    [Fact]
    public void Scan_ShouldFlagGeneratedFile_WhenInNestedFolder()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("Components/Menus/Deleted/DeepComponent.Generated.cs", "Menus/Deleted/DeepComponent");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings);

        orphans.Single().FilePath.ShouldBe(new ToolsUtilities.FilePath(
            Path.Combine(_tempDirectory, "Components", "Menus", "Deleted", "DeepComponent.Generated.cs")));
    }

    [Fact]
    public void Scan_ShouldFlagOrphanedElementSettingsFile()
    {
        GumProjectSave project = Project;
        project.Components.Add(CreateComponent("LiveComponent"));
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteFile("Components/LiveComponent.codsj", "{}");
        WriteFile("Components/DeletedComponent.codsj", "{}");
        WriteFile("ProjectCodeSettings.codsj", "{}");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings);

        orphans.Select(item => item.FilePath.FileNameNoPath).ShouldBe(new[] { "DeletedComponent.codsj" });
    }

    [Fact]
    public void Scan_ShouldNotFlagCustomCodeFile_WhenNoGeneratedSiblingExists()
    {
        // Gum has no knowledge of extra hand-written partials, so orphan detection deliberately
        // only considers a custom .cs file whose .Generated.cs sibling Gum itself wrote.
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteFile("Screens/DeletedScreen.Input.cs", "partial class DeletedScreen { }");
        WriteFile("Screens/MyOwnHelper.cs", "class MyOwnHelper { }");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings);

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldNotFlagFiles_ForElementWithNeverGenerateBehavior()
    {
        GumProjectSave project = Project;
        project.Screens.Add(CreateScreen("HandManagedScreen"));
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteFile("Screens/HandManagedScreen.codsj", "{\"GenerationBehavior\":0}");
        WriteGeneratedFile("Screens/HandManagedScreen.Generated.cs", "HandManagedScreen");
        WriteFile("Screens/HandManagedScreen.cs", "partial class HandManagedScreen { }");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings);

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldNotFlagFiles_ForElementWithMissingSourceFile()
    {
        // A missing .gucx/.gusx is already surfaced by the tree view's "!" indicator; the element is
        // still in the project, so its code files are not orphans.
        GumProjectSave project = Project;
        ScreenSave screen = CreateScreen("BranchSwitchedScreen");
        screen.IsSourceFileMissing = true;
        project.Screens.Add(screen);
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("Screens/BranchSwitchedScreen.Generated.cs", "BranchSwitchedScreen");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings);

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldNotFlagFiles_ForExistingElement()
    {
        GumProjectSave project = Project;
        project.Screens.Add(CreateScreen("LiveScreen"));
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("Screens/LiveScreen.Generated.cs", "LiveScreen");
        WriteFile("Screens/LiveScreen.cs", "partial class LiveScreen { }");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings);

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldNotFlagGeneratedFile_WhenFileNameCaseDiffersFromElementName()
    {
        GumProjectSave project = Project;
        project.Screens.Add(CreateScreen("CaseScreen"));
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("Screens/casescreen.Generated.cs", "CaseScreen");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings);

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldNotFlagGeneratedFile_WhenInBuildOutputFolder()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("bin/Debug/net8.0/StaleCopy.Generated.cs", "StaleCopy");
        WriteGeneratedFile("obj/Debug/StaleCopy.Generated.cs", "StaleCopy");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings);

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldNotFlagGeneratedFile_WhenGumDidNotWriteIt()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteFile("Screens/SomeOtherTool.Generated.cs", "// <auto-generated by something else />");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings);

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldNotFlagStandardElementsFallbackFile()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("StandardElements.Generated.cs", "StandardElements");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings);

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldReturnEmpty_WhenCodeProjectRootIsEmpty()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        projectSettings.CodeProjectRoot = string.Empty;
        WriteGeneratedFile("Screens/DeletedScreen.Generated.cs", "DeletedScreen");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings);

        orphans.ShouldBeEmpty();
    }

    #region Helpers

    private OrphanCodeFileScanService CreateService()
    {
        ObjectFinder.Self.GumProjectSave = Project;

        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider =
            new FixedProjectDirectoryProvider(_tempDirectory + Path.DirectorySeparatorChar);
        CodeOutputElementSettingsManager elementSettingsManager =
            new CodeOutputElementSettingsManager(directoryProvider);
        LocalizationService localizationService = new LocalizationService();
        CodeGenerator codeGenerator = new CodeGenerator(
            codeGenNameVerifier, localizationService, elementSettingsManager, directoryProvider);
        CodeGenerationFileLocationsService fileLocationsService = new CodeGenerationFileLocationsService(
            codeGenerator, codeGenNameVerifier, directoryProvider);

        return new OrphanCodeFileScanService(
            codeGenerator, fileLocationsService, elementSettingsManager, directoryProvider);
    }

    private static CodeOutputProjectSettings CreateProjectSettings() => new CodeOutputProjectSettings
    {
        CodeProjectRoot = "./",
        RootNamespace = "MyGame",
        OutputLibrary = OutputLibrary.MonoGameForms
    };

    private static ScreenSave CreateScreen(string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        StateSave defaultState = new StateSave { Name = "Default" };
        defaultState.ParentContainer = screen;
        screen.States.Add(defaultState);
        return screen;
    }

    private static ComponentSave CreateComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default" };
        defaultState.ParentContainer = component;
        component.States.Add(defaultState);
        return component;
    }

    private void WriteFile(string relativePath, string contents)
    {
        string fullPath = Path.Combine(_tempDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contents);
    }

    private void WriteGeneratedFile(string relativePath, string elementName) =>
        WriteFile(relativePath, $"//Code for {elementName}\r\npublic partial class Whatever {{ }}");

    #endregion

    public override void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        base.Dispose();
    }
}
