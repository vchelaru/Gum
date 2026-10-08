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
using System.Threading;

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
    public void EnumerateGeneratedFiles_SkipsADirectoryItCannotRead_AndKeepsWalkingTheRest()
    {
        // A root-owned folder under the code root (Linux, macOS) must not end the scan.
        Dictionary<string, string[]> files = new Dictionary<string, string[]>
        {
            ["root"] = new[] { "root/A.Generated.cs" },
            ["root/ok"] = new[] { "root/ok/B.Generated.cs" },
        };
        Dictionary<string, string[]> subdirectories = new Dictionary<string, string[]>
        {
            ["root"] = new[] { "root/locked", "root/ok" },
            ["root/ok"] = Array.Empty<string>(),
        };
        Func<string, IEnumerable<string>> enumerateFiles = directory =>
            directory == "root/locked" ? throw new UnauthorizedAccessException(directory) : files[directory];
        Func<string, IEnumerable<string>> enumerateDirectories = directory => subdirectories[directory];

        OrphanCodeFileScanService.GeneratedFileWalk walk = OrphanCodeFileScanService.WalkGeneratedFiles(
            "root", enumerateFiles, enumerateDirectories, maxDirectories: 100, CancellationToken.None);

        walk.Files.ShouldBe(new[] { "root/A.Generated.cs", "root/ok/B.Generated.cs" });
    }

    [Fact]
    public void WalkGeneratedFiles_SkipsHiddenAndPackageFolders()
    {
        // .git, .vs and node_modules are never code output, and can hold thousands of folders.
        Dictionary<string, string[]> files = new Dictionary<string, string[]>
        {
            ["root"] = Array.Empty<string>(),
            ["root/Screens"] = new[] { "root/Screens/A.Generated.cs" },
        };
        Dictionary<string, string[]> subdirectories = new Dictionary<string, string[]>
        {
            ["root"] = new[] { "root/.git", "root/.vs", "root/node_modules", "root/Screens" },
            ["root/Screens"] = Array.Empty<string>(),
        };

        OrphanCodeFileScanService.GeneratedFileWalk walk = OrphanCodeFileScanService.WalkGeneratedFiles(
            "root", directory => files[directory], directory => subdirectories[directory],
            maxDirectories: 100, CancellationToken.None);

        walk.Files.ShouldBe(new[] { "root/Screens/A.Generated.cs" });
    }

    [Fact]
    public void WalkGeneratedFiles_StopsAtDirectoryBudget_AndReportsTruncation()
    {
        // A CodeProjectRoot pointing at a huge folder (a drive root, %LOCALAPPDATA%) must not walk forever.
        Dictionary<string, string[]> files = new Dictionary<string, string[]>
        {
            ["root"] = new[] { "root/A.Generated.cs" },
            ["root/a"] = new[] { "root/a/B.Generated.cs" },
            ["root/a/b"] = new[] { "root/a/b/C.Generated.cs" },
        };
        Dictionary<string, string[]> subdirectories = new Dictionary<string, string[]>
        {
            ["root"] = new[] { "root/a" },
            ["root/a"] = new[] { "root/a/b" },
            ["root/a/b"] = Array.Empty<string>(),
        };

        OrphanCodeFileScanService.GeneratedFileWalk walk = OrphanCodeFileScanService.WalkGeneratedFiles(
            "root", directory => files[directory], directory => subdirectories[directory],
            maxDirectories: 2, CancellationToken.None);

        walk.Files.ShouldBe(new[] { "root/A.Generated.cs", "root/a/B.Generated.cs" });
        walk.IsTruncated.ShouldBeTrue();
    }

    [Fact]
    public void WalkGeneratedFiles_IsNotTruncated_WhenTreeFitsTheBudget()
    {
        Dictionary<string, string[]> subdirectories = new Dictionary<string, string[]>
        {
            ["root"] = new[] { "root/a" },
            ["root/a"] = Array.Empty<string>(),
        };

        OrphanCodeFileScanService.GeneratedFileWalk walk = OrphanCodeFileScanService.WalkGeneratedFiles(
            "root", _ => Array.Empty<string>(), directory => subdirectories[directory],
            maxDirectories: 2, CancellationToken.None);

        walk.IsTruncated.ShouldBeFalse();
    }

    [Fact]
    public void Execute_ShouldThrow_WhenCancelled()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("Screens/DeletedScreen.Generated.cs", "DeletedScreen");
        OrphanCodeFileScanService service = CreateService();
        OrphanCodeFileScanPlan plan = service.CreatePlan(project, projectSettings);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Should.Throw<OperationCanceledException>(() => service.Execute(plan, cancellation.Token));
    }

    [Fact]
    public void Execute_ShouldFindOrphans_FromAPlanCreatedEarlier_AfterTheProjectChanges()
    {
        // The tool builds the plan on the UI thread and walks the disk on a worker; the walk must not
        // read the live project, which the user may be editing meanwhile.
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("Screens/DeletedScreen.Generated.cs", "DeletedScreen");
        OrphanCodeFileScanService service = CreateService();
        OrphanCodeFileScanPlan plan = service.CreatePlan(project, projectSettings);
        project.Screens.Add(CreateScreen("DeletedScreen"));

        OrphanCodeFileScanResult result = service.Execute(plan, CancellationToken.None);

        result.Orphans.Select(item => item.FilePath.FileNameNoPath).ShouldBe(new[] { "DeletedScreen.Generated.cs" });
    }

    [Fact]
    public void Scan_ShouldFlagCustomCodeFile_WhenItsGeneratedSiblingIsOrphaned()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("Screens/DeletedScreen.Generated.cs", "DeletedScreen");
        WriteFile("Screens/DeletedScreen.cs", "partial class DeletedScreen { }");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

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

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.Select(item => item.FilePath.FileNameNoPath).ShouldBe(new[] { "OldScreen.Generated.cs" });
    }

    [Fact]
    public void Scan_ShouldFlagGeneratedFile_WhenInNestedFolder()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("Components/Menus/Deleted/DeepComponent.Generated.cs", "Menus/Deleted/DeepComponent");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

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

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

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

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

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

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

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

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

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

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldNotFlagGeneratedFile_WhenFileNameCaseDiffersFromElementName()
    {
        GumProjectSave project = Project;
        project.Screens.Add(CreateScreen("CaseScreen"));
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("Screens/casescreen.Generated.cs", "CaseScreen");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldNotFlagGeneratedFile_WhenInBuildOutputFolder()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("bin/Debug/net8.0/StaleCopy.Generated.cs", "StaleCopy");
        WriteGeneratedFile("obj/Debug/StaleCopy.Generated.cs", "StaleCopy");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldNotFlagGeneratedFile_WhenCodeProjectRootIsMixedCaseBuildOutputFolder()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        projectSettings.CodeProjectRoot = "./Bin/";
        WriteGeneratedFile("Bin/Screens/StaleCopy.Generated.cs", "StaleCopy");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldNotFlagGeneratedFile_WhenGumDidNotWriteIt()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteFile("Screens/SomeOtherTool.Generated.cs", "// <auto-generated by something else />");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldNotFlagStandardElementsFallbackFile()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("StandardElements.Generated.cs", "StandardElements");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_ShouldFlagOrphans_InsideGeneratedCodeFolder_AndNotFlagLiveElements()
    {
        GumProjectSave project = Project;
        project.Screens.Add(CreateScreen("LiveScreen"));
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        projectSettings.GeneratedCodeFolder = "Gum/Generated/";
        WriteGeneratedFile("Gum/Generated/Screens/LiveScreen.Generated.cs", "LiveScreen");
        WriteGeneratedFile("Gum/Generated/Screens/DeletedScreen.Generated.cs", "DeletedScreen");
        WriteGeneratedFile("Gum/Generated/StandardElements.Generated.cs", "StandardElements");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.Select(item => item.FilePath).ShouldBe(new[]
        {
            new ToolsUtilities.FilePath(Path.Combine(_tempDirectory, "Gum", "Generated", "Screens", "DeletedScreen.Generated.cs"))
        });
    }

    [Fact]
    public void Scan_ShouldFlagFilesLeftAtTheCodeProjectRoot_AfterGeneratedCodeFolderIsSet()
    {
        // Left behind, the old pair compiles alongside the new one as a duplicate partial class.
        GumProjectSave project = Project;
        project.Screens.Add(CreateScreen("LiveScreen"));
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        projectSettings.GeneratedCodeFolder = "Gum/Generated";
        WriteGeneratedFile("Gum/Generated/Screens/LiveScreen.Generated.cs", "LiveScreen");
        WriteGeneratedFile("Screens/LiveScreen.Generated.cs", "LiveScreen");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.Select(item => item.FilePath).ShouldBe(new[]
        {
            new ToolsUtilities.FilePath(Path.Combine(_tempDirectory, "Screens", "LiveScreen.Generated.cs"))
        });
    }

    [Theory]
    [InlineData("GumCodeGen/", "GumCodeGen/Screens")]
    [InlineData("Gum", "GumScreens")]
    public void Scan_ShouldFlagOrphans_InsidePrefixedFolders_AndNotFlagLiveElements(string prefix, string screensFolder)
    {
        GumProjectSave project = Project;
        project.Screens.Add(CreateScreen("LiveScreen"));
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        projectSettings.GeneratedCodeFolderPrefix = prefix;
        WriteGeneratedFile(screensFolder + "/LiveScreen.Generated.cs", "LiveScreen");
        WriteGeneratedFile(screensFolder + "/DeletedScreen.Generated.cs", "DeletedScreen");
        WriteGeneratedFile("StandardElements.Generated.cs", "StandardElements");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.Select(item => item.FilePath).ShouldBe(new[]
        {
            new ToolsUtilities.FilePath(Path.Combine(_tempDirectory, Path.Combine(screensFolder.Split('/')), "DeletedScreen.Generated.cs"))
        });
    }

    [Theory]
    [InlineData("GumCodeGen/", "GumCodeGen/Screens")]
    [InlineData("Gum", "GumScreens")]
    public void Scan_ShouldFlagFilesLeftInTheUnprefixedFolders_AfterAPrefixIsSet(string prefix, string screensFolder)
    {
        // Left behind, the old pair compiles alongside the new one as a duplicate partial class.
        GumProjectSave project = Project;
        project.Screens.Add(CreateScreen("LiveScreen"));
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        projectSettings.GeneratedCodeFolderPrefix = prefix;
        WriteGeneratedFile(screensFolder + "/LiveScreen.Generated.cs", "LiveScreen");
        WriteGeneratedFile("Screens/LiveScreen.Generated.cs", "LiveScreen");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.Select(item => item.FilePath).ShouldBe(new[]
        {
            new ToolsUtilities.FilePath(Path.Combine(_tempDirectory, "Screens", "LiveScreen.Generated.cs"))
        });
    }

    [Fact]
    public void Scan_ShouldFlagFilesLeftInThePreviousPrefixFolder_AfterThePrefixIsCleared()
    {
        GumProjectSave project = Project;
        project.Screens.Add(CreateScreen("LiveScreen"));
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        WriteGeneratedFile("Screens/LiveScreen.Generated.cs", "LiveScreen");
        WriteGeneratedFile("GumCodeGen/Screens/LiveScreen.Generated.cs", "LiveScreen");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.Select(item => item.FilePath).ShouldBe(new[]
        {
            new ToolsUtilities.FilePath(Path.Combine(_tempDirectory, "GumCodeGen", "Screens", "LiveScreen.Generated.cs"))
        });
    }

    [Fact]
    public void Scan_ShouldFlagFilesLeftInThePreviousFolder_WhenGeneratedCodeFolderAndPrefixBothChange()
    {
        GumProjectSave project = Project;
        project.Screens.Add(CreateScreen("LiveScreen"));
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        projectSettings.GeneratedCodeFolder = "Gum/Generated";
        projectSettings.GeneratedCodeFolderPrefix = "My";
        WriteGeneratedFile("Gum/Generated/MyScreens/LiveScreen.Generated.cs", "LiveScreen");
        WriteGeneratedFile("Gum/Generated/Screens/LiveScreen.Generated.cs", "LiveScreen");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.Select(item => item.FilePath).ShouldBe(new[]
        {
            new ToolsUtilities.FilePath(Path.Combine(_tempDirectory, "Gum", "Generated", "Screens", "LiveScreen.Generated.cs"))
        });
    }

    [Fact]
    public void Scan_ShouldWalkTheGeneratedCodeFolder_WhenItIsOutsideTheCodeProjectRoot()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        projectSettings.CodeProjectRoot = "Game/";
        projectSettings.GeneratedCodeFolder = "../Shared/Generated";
        Directory.CreateDirectory(Path.Combine(_tempDirectory, "Game"));
        WriteGeneratedFile("Shared/Generated/Screens/DeletedScreen.Generated.cs", "DeletedScreen");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.Select(item => item.FilePath).ShouldBe(new[]
        {
            new ToolsUtilities.FilePath(Path.Combine(_tempDirectory, "Shared", "Generated", "Screens", "DeletedScreen.Generated.cs"))
        });
    }

    [Fact]
    public void Scan_ShouldReturnEmpty_WhenCodeProjectRootIsEmpty()
    {
        GumProjectSave project = Project;
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        projectSettings.CodeProjectRoot = string.Empty;
        WriteGeneratedFile("Screens/DeletedScreen.Generated.cs", "DeletedScreen");

        IReadOnlyList<OrphanCodeFile> orphans = CreateService().Scan(project, projectSettings).Orphans;

        orphans.ShouldBeEmpty();
    }

    [Fact]
    public void Scan_WithPreviousSettings_AlsoFindsFilesUnderTheOldCodeRoot()
    {
        // The Code Project Root moved, so the old files sit outside the folder the scan walks.
        GumProjectSave project = Project;
        project.Components.Add(CreateComponent("Card"));
        CodeOutputProjectSettings previous = CreateProjectSettings();
        previous.CodeProjectRoot = "OldCode/";
        CodeOutputProjectSettings current = CreateProjectSettings();
        current.CodeProjectRoot = "NewCode/";
        Directory.CreateDirectory(Path.Combine(_tempDirectory, "NewCode"));
        WriteGeneratedFile("OldCode/Components/Card.Generated.cs", "Card");
        WriteFile("OldCode/Components/Card.cs", "partial class Card { }");

        IReadOnlyList<OrphanCodeFile> withPrevious = CreateService().Scan(project, current, previous).Orphans;
        IReadOnlyList<OrphanCodeFile> withoutPrevious = CreateService().Scan(project, current).Orphans;

        withPrevious.Select(orphan => orphan.FilePath.FileNameNoPath).ShouldBe(new[] { "Card.Generated.cs", "Card.cs" });
        withoutPrevious.ShouldBeEmpty();
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
