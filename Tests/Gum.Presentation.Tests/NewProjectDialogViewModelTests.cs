using Gum.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gum.Dialogs;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The New Project dialog's rules: the live gumcli command, the warnings, validation, the folder tree and the
/// create-then-restore run that happens while the dialog stays open.
/// </summary>
public class NewProjectDialogViewModelTests
{
    private static readonly string Folder = Path.Combine(Path.GetTempPath(), "GumNewProjectDialogTests");

    private readonly Mock<INewProjectService> _service = new();
    private readonly Mock<IExistingCodeProjectDetector> _detector = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly Mock<IProjectState> _projectState = new();

    public NewProjectDialogViewModelTests()
    {
        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard", "Bubblegum" });
        _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");
        _service.Setup(x => x.IsSourceLinkedAvailable).Returns(true);
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(Valid());
    }

    private static NewProjectValidation Valid() => new NewProjectValidation
    {
        Resolved = new ResolvedNewProject { Name = "MyGame", CsprojPath = Path.Combine(Folder, "MyGame", "MyGame.csproj") },
    };

    private NewProjectDialogViewModel Create(Action<NewProjectDialogViewModel>? configure = null)
    {
        NewProjectDialogViewModel viewModel = new NewProjectDialogViewModel(
            new ThemeSelectionViewModel(_formsFileService.Object, _projectState.Object),
            _service.Object, _detector.Object, _dialogService.Object, new Mock<INewProjectLocationSettings>().Object)
        {
            ProjectName = "MyGame",
            Location = Folder,
        };
        configure?.Invoke(viewModel);
        return viewModel;
    }

    private static readonly string Sep = Path.DirectorySeparatorChar.ToString();

    private static readonly char OtherSeparator = Path.DirectorySeparatorChar == (char)92 ? (char)47 : (char)92;

    private static IEnumerable<string> AllDisplayNames(NewProjectLayoutNode node) =>
        new[] { node.DisplayName }.Concat(node.Children.SelectMany(AllDisplayNames));

    private static string WithOtherSeparator(string path) => path.Replace(Path.DirectorySeparatorChar, OtherSeparator);

    private static string GumOnlyPath(string name = "MyGame", string extension = ".gumj") => Path.Combine(Folder, name, name + extension);

    #region Command line

    [Fact]
    public void CommandLine_ForTheDefaults_QuotesTheGumProjectPath()
    {
        NewProjectDialogViewModel viewModel = Create();

        viewModel.CommandLine.ShouldBe($"gumcli new \"{GumOnlyPath()}\"");
    }

    [Theory]
    [InlineData("monogame", false, true, true, "--platform monogame")]
    [InlineData("raylib", false, true, true, "--platform raylib")]
    [InlineData("stride", false, true, true, "--platform stride")]
    [InlineData("silknet", false, true, true, "--platform silknet")]
    [InlineData("kni", true, true, true, "--platform kni --source-linked")]
    [InlineData("kni", false, true, false, "--platform kni --no-restore")]
    [InlineData("monogame", true, false, false, "--platform monogame --template empty --source-linked --no-restore")]
    [InlineData("kniskia", false, true, true, "--platform kniskia --source-linked")]
    public void CommandLine_WithACodeProject_OrdersTheOptions(
        string platform, bool sourceLinked, bool forms, bool restore, string expectedOptions)
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = platform;
            vm.SourceLinked = sourceLinked;
            vm.IsIncludeFormsControls = forms;
            vm.RunRestore = restore;
        });

        viewModel.CommandLine.ShouldBe($"gumcli new \"{Path.Combine(Folder, "MyGame")}\" {expectedOptions}");
    }

    [Fact]
    public void CommandLine_AddsTheTheme_OnlyWhenItIsNotStandard()
    {
        NewProjectDialogViewModel viewModel = Create();

        viewModel.ThemeSelection.SelectedTheme = "Bubblegum";

        viewModel.CommandLine.ShouldEndWith(" --theme Bubblegum");
        viewModel.ThemeSelection.SelectedTheme = "Standard";
        viewModel.CommandLine.ShouldNotContain("--theme");
    }

    [Fact]
    public void CommandLine_DropsTheTheme_WhenFormsAreOff()
    {
        NewProjectDialogViewModel viewModel = Create();
        viewModel.ThemeSelection.SelectedTheme = "Bubblegum";

        viewModel.IsIncludeFormsControls = false;

        viewModel.CommandLine.ShouldEndWith("--template empty");
        viewModel.CommandLine.ShouldNotContain("--theme");
    }

    [Fact]
    public void CommandLine_ForXml_PutsTheExtensionInThePath_AndNotesIt()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.SelectedFileFormat = NewProjectDialogViewModel.XmlFormat;
            vm.IsIncludeDemoScreenGum = true;
        });

        viewModel.CommandLine.ShouldBe($"gumcli new \"{GumOnlyPath(extension: ".gumx")}\"");
        viewModel.CommandLineNote.ShouldBe("The extension in the path picks the file format.");
    }

    [Fact]
    public void CommandLineNote_SaysDemoScreenCannotBeLeftOut_WhenFormsAreOnAndTheDemoIsUnchecked()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IsIncludeDemoScreenGum = false);

        viewModel.CommandLineNote.ShouldBe("gumcli new has no option to leave out DemoScreenGum yet.");
        viewModel.IsIncludeFormsControls = false;
        viewModel.CommandLineNote.ShouldBeEmpty();
    }

    #endregion

    #region Options

    [Fact]
    public void Defaults_AreJsonFormsDemoAndNoCodeProject()
    {
        NewProjectDialogViewModel viewModel = Create();

        viewModel.SelectedFileFormat.ShouldBe(NewProjectDialogViewModel.JsonFormat);
        viewModel.UseXmlFormat.ShouldBeFalse();
        viewModel.IsIncludeFormsControls.ShouldBeTrue();
        viewModel.IsIncludeDemoScreenGum.ShouldBeFalse();
        viewModel.IncludeCodeProject.ShouldBeFalse();
        viewModel.RunRestore.ShouldBeTrue();
    }

    [Fact]
    public void Platforms_AreTheMainNames_WithStrideMarkedWindowsOnly()
    {
        NewProjectDialogViewModel viewModel = Create();

        viewModel.Platforms.Select(platform => platform.Name).ShouldBe(NewProjectPlatforms.MainNames.Select(entry => entry.Name));
        viewModel.Platforms.Single(platform => platform.Name == "stride").Label.ShouldBe("Stride (Windows only)");
        viewModel.Platforms.Single(platform => platform.Name == "kniskia").Label.ShouldBe("KNI with Skia");
    }

    [Fact]
    public void SourceLinked_IsForcedOnAndLocked_ForKniSkia()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "kniskia";
        });

        viewModel.SourceLinked.ShouldBeTrue();
        viewModel.IsSourceLinkedEnabled.ShouldBeFalse();
        viewModel.PlatformNote.ShouldBe("Gum.KniSkia is not on NuGet yet, so the local Gum source is required.");
    }

    [Fact]
    public void SourceLinked_IsOffAndDisabledWithANote_WhenNoGumCheckoutExists()
    {
        _service.Setup(x => x.IsSourceLinkedAvailable).Returns(false);

        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.SourceLinked = true;
        });

        viewModel.SourceLinked.ShouldBeFalse();
        viewModel.IsSourceLinkedEnabled.ShouldBeFalse();
    }

    [Fact]
    public void KniSkia_WithoutAGumCheckout_ExplainsWhyOkIsOff()
    {
        _service.Setup(x => x.IsSourceLinkedAvailable).Returns(false);

        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "kniskia";
        });

        viewModel.ValidationMessage.ShouldBe("Use local Gum source needs the Gum source code on this computer, and none was found.");
        viewModel.CanExecuteAffirmative().ShouldBeFalse();
    }

    [Fact]
    public void Browse_PutsThePickedFolderInLocation()
    {
        _dialogService.Setup(x => x.OpenFolder(It.IsAny<OpenFolderDialogOptions?>())).Returns(Path.Combine(Folder, "Picked"));
        NewProjectDialogViewModel viewModel = Create();

        viewModel.BrowseCommand.Execute(null);

        viewModel.Location.ShouldBe(Path.Combine(Folder, "Picked"));
    }

    [Fact]
    public void Browse_KeepsLocation_WhenThePickerIsCancelled()
    {
        _dialogService.Setup(x => x.OpenFolder(It.IsAny<OpenFolderDialogOptions?>())).Returns((string?)null);
        NewProjectDialogViewModel viewModel = Create();

        viewModel.BrowseCommand.Execute(null);

        viewModel.Location.ShouldBe(Folder);
    }

    [Theory]
    [InlineData("stride", NewProjectDialogText.StridePlatformNote)]
    [InlineData("silknet", NewProjectDialogText.SilkNetPlatformNote)]
    [InlineData("monogame", "")]
    public void PlatformNote_ShowsOnlyForAPlatformWithOne_AndOnlyWithACodeProject(string platform, string expected)
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.PlatformName = platform);
        viewModel.PlatformNote.ShouldBeEmpty();

        viewModel.IncludeCodeProject = true;
        viewModel.PlatformNote.ShouldBe(expected);

        viewModel.IncludeCodeProject = false;
        viewModel.PlatformNote.ShouldBeEmpty();
    }

    [Fact]
    public void GumProjectPath_WithACodeProject_IsTheProjectInsideContentGumProject()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.SelectedFileFormat = NewProjectDialogViewModel.XmlFormat;
        });

        viewModel.GumProjectPath.ShouldBe(Path.Combine(Folder, "MyGame", "Content", "GumProject", "GumProject.gumx"));
        viewModel.CommandLineNote.ShouldContain(NewProjectDialogText.NoXmlWithCodeProjectNote);
    }

    [Fact]
    public void ProjectName_IsTrimmed_InThePathsAndTheCommandLine()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.ProjectName = "  Spaced  ");

        viewModel.GumProjectPath.ShouldBe(GumOnlyPath("Spaced"));
        viewModel.CommandLine.ShouldBe($"gumcli new \"{GumOnlyPath("Spaced")}\"");
        viewModel.ValidationMessage.ShouldBeEmpty();
    }

    [Fact]
    public void CopyCommandLine_AsksTheViewToCopyTheCurrentCommand()
    {
        NewProjectDialogViewModel viewModel = Create();
        List<string> copied = new List<string>();
        viewModel.CommandLineCopyRequested += copied.Add;
        viewModel.ProjectName = "Other";

        viewModel.CopyCommandLineCommand.Execute(null);

        copied.ShouldBe(new[] { viewModel.CommandLine });
        copied.Single().ShouldContain("Other");
    }

    #endregion

    #region Validation

    [Theory]
    [InlineData("", false, "Enter a project name.")]
    [InlineData("   ", false, "Enter a project name.")]
    [InlineData("My Game", false, "")]
    [InlineData("My Game", true, "Not a valid C# name.")]
    [InlineData("1Game", true, "Not a valid C# name.")]
    [InlineData("My_Game2", true, "")]
    [InlineData("a|b", false, "The name has characters a folder name can't have.")]
    [InlineData("CON", false, "The name has characters a folder name can't have.")]
    [InlineData("a.", false, "The name has characters a folder name can't have.")]
    public void ValidationMessage_FollowsTheNameRules(string name, bool codeProject, string expected)
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = codeProject;
            vm.ProjectName = name;
        });

        viewModel.ValidationMessage.ShouldBe(expected);
        viewModel.CanExecuteAffirmative().ShouldBe(expected.Length == 0);
    }

    [Fact]
    public void ValidationMessage_AsksForALocation_WhenItIsEmpty()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.Location = "  ");

        viewModel.ValidationMessage.ShouldBe("Choose a location.");
    }

    [Fact]
    public void ValidationMessage_RejectsInvalidPathCharacters()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.Location = Folder + "\0");

        viewModel.ValidationMessage.ShouldBe("The location has characters a path can't have.");
    }

    [Theory]
    [InlineData(125, "")]
    [InlineData(126, "The name is too long.")]
    public void ValidationMessage_CountsTheNameInBytes_NotCharacters(int length, string expected)
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.ProjectName = new string('\u00e9', length));

        viewModel.ValidationMessage.ShouldBe(expected);
    }

    [Fact]
    public void ValidationMessage_UpdatesTheNameLimit_WhenTheCodeProjectIsToggled()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.ProjectName = new string('a', 250));
        viewModel.ValidationMessage.ShouldBeEmpty();

        viewModel.IncludeCodeProject = true;
        viewModel.ValidationMessage.ShouldBe("The name is too long.");

        viewModel.IncludeCodeProject = false;
        viewModel.ValidationMessage.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Projects")]
    [InlineData("~/Projects")]
    [InlineData("../Projects")]
    [InlineData("  Projects  ")]
    [InlineData("C:foo")]
    public void ValidationMessage_RejectsALocationThatIsNotAnAbsolutePath(string location)
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.Location = location);

        viewModel.ValidationMessage.ShouldBe("The location must be a full path.");
        viewModel.CanExecuteAffirmative().ShouldBeFalse();
    }

    [Theory]
    [InlineData(250, false, "")]
    [InlineData(251, false, "The name is too long.")]
    [InlineData(248, true, "")]
    [InlineData(249, true, "The name is too long.")]
    public void ValidationMessage_LimitsTheNameToWhatFitsAsAFileNameWithItsExtension(int length, bool codeProject, string expected)
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = codeProject;
            vm.ProjectName = new string('a', length);
        });

        viewModel.ValidationMessage.ShouldBe(expected);
        viewModel.CanExecuteAffirmative().ShouldBe(expected.Length == 0);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("  ", "  ")]
    public void Location_TrailingSeparatorAndSurroundingSpaces_DoNotChangeThePaths(string before, string after)
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.Location = before + Folder + Path.DirectorySeparatorChar + after);

        viewModel.GumProjectPath.ShouldBe(GumOnlyPath());
        viewModel.CommandLine.ShouldBe($"gumcli new \"{GumOnlyPath()}\"");
        viewModel.ValidationMessage.ShouldBeEmpty();
    }

    #endregion

    [Theory]
    [InlineData("MyGame", true)]
    [InlineData("My Game", true)]
    [InlineData("My.Game", true)]
    [InlineData("a|b", false)]
    [InlineData("a:b", false)]
    [InlineData("a?b", false)]
    [InlineData("a*b", false)]
    [InlineData("a<b", false)]
    [InlineData("a>b", false)]
    [InlineData("a\"b", false)]
    [InlineData("a/b", false)]
    [InlineData("a\\b", false)]
    [InlineData("a\tb", false)]
    [InlineData("name.", false)]
    [InlineData("name ", false)]
    [InlineData("con", false)]
    [InlineData("NUL", false)]
    [InlineData("Aux.txt", false)]
    [InlineData("COM1", false)]
    [InlineData("lpt9", false)]
    [InlineData("COM10", true)]
    [InlineData("Console", true)]
    public void NameRules_AreTheSameOnEveryOs(string name, bool valid)
    {
        NewProjectNameRules.IsValidFolderName(name).ShouldBe(valid);
    }

    #region Warnings

    [Fact]
    public async Task ExistingCodeProjectWarning_Shows_WhenACodeProjectIsIncluded_AndOneIsFound()
    {
        string found = Path.Combine(Folder, "MyOtherGame.csproj");
        _detector.Setup(x => x.FindExistingCodeProject(It.IsAny<string>())).Returns(found);
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        await viewModel.ChecksRefresh;

        viewModel.IsExistingCodeProjectWarningVisible.ShouldBeTrue();
        viewModel.ExistingCodeProjectText.ShouldBe($"Found MyOtherGame.csproj in {Folder}.");
        viewModel.ExistingCodeProjectTip.ShouldContain("Program.cs, Game1.cs");
        viewModel.ExistingCodeProjectTip.ShouldContain(Path.Combine(Folder, "MyGame"));
        viewModel.ExistingCodeProjectTip.ShouldContain("uncheck Create C# project");
    }

    [Fact]
    public async Task ExistingCodeProjectWarning_IsHidden_WithoutACodeProject()
    {
        _detector.Setup(x => x.FindExistingCodeProject(It.IsAny<string>())).Returns(Path.Combine(Folder, "Other.csproj"));
        NewProjectDialogViewModel viewModel = Create();

        await viewModel.ChecksRefresh;

        viewModel.IsExistingCodeProjectWarningVisible.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistingCodeProjectTip_LeavesOutGame1_ForRaylib()
    {
        _detector.Setup(x => x.FindExistingCodeProject(It.IsAny<string>())).Returns(Path.Combine(Folder, "Other.csproj"));
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "raylib";
        });

        await viewModel.ChecksRefresh;

        viewModel.ExistingCodeProjectTip.ShouldContain("(Program.cs)");
    }

    [Fact]
    public async Task TargetNotEmptyWarning_Shows_WhenTheNewFolderHasFiles()
    {
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(Folder, "MyGame")).Returns(true);
        NewProjectDialogViewModel viewModel = Create();

        await viewModel.ChecksRefresh;

        viewModel.IsTargetNotEmptyWarningVisible.ShouldBeTrue();
        viewModel.TargetNotEmptyText.ShouldBe($"{Path.Combine(Folder, "MyGame")} already has files. Files with the same names may be overwritten.");
    }

    [Fact]
    public async Task Warnings_DoNotBlockOk()
    {
        _detector.Setup(x => x.FindExistingCodeProject(It.IsAny<string>())).Returns(Path.Combine(Folder, "Other.csproj"));
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        await viewModel.ChecksRefresh;

        viewModel.CanExecuteAffirmative().ShouldBeTrue();
    }

    #endregion

    #region Folder tree

    [Fact]
    public void FolderTree_ForAGumOnlyProject_IsTheRootWithTheStandardFoldersAndTheProjectFile()
    {
        NewProjectDialogViewModel viewModel = Create();

        NewProjectLayoutNode root = viewModel.FolderTree.Single();

        root.DisplayName.ShouldBe(NewProjectDisplayPath.AsFolder(Path.Combine(Folder, "MyGame")));
        root.Children.Select(child => child.DisplayName).ShouldBe(
            new[] { "Behaviors" + Sep, "Components" + Sep, "Fonts" + Sep, "Screens" + Sep, "Standards" + Sep, "MyGame.gumj" });
        root.Children.Single(child => child.Name == "MyGame.gumj").IsMainFile.ShouldBeTrue();
    }

    [Fact]
    public void FolderTree_ForACodeProject_NestsTheGumProjectUnderContent_AndMarksItsFile()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        NewProjectLayoutNode root = viewModel.FolderTree.Single();

        root.Children.Select(child => child.Name).ShouldBe(new[] { "Content" });
        NewProjectLayoutNode gumProject = root.Children.Single().Children.Single(child => child.Name == "GumProject");
        gumProject.Children.Single(child => child.Name == "GumProject.gumj").IsMainFile.ShouldBeTrue();
        gumProject.Children.Select(child => child.Name).ShouldContain("Screens");
    }

    [Fact]
    public void FolderTree_FollowsTheInputs()
    {
        NewProjectDialogViewModel viewModel = Create();

        viewModel.ProjectName = "Renamed";
        viewModel.SelectedFileFormat = NewProjectDialogViewModel.XmlFormat;

        NewProjectLayoutNode root = viewModel.FolderTree.Single();
        root.DisplayName.ShouldBe(NewProjectDisplayPath.AsFolder(Path.Combine(Folder, "Renamed")));
        root.Children.Select(child => child.Name).ShouldContain("Renamed.gumx");
    }

    [Theory]
    [InlineData("monogame", "Game1.cs")]
    [InlineData("raylib", null)]
    public void FolderTree_DoesNotListFiles_ExceptTheGumProjectFile(string platform, string? game1)
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = platform;
        });

        List<string> files = Flatten(viewModel.FolderTree.Single()).Where(node => !node.IsFolder).Select(node => node.Name).ToList();

        files.ShouldBe(new[] { "GumProject.gumj" });
        files.ShouldNotContain(game1 ?? "Game1.cs");
    }

    [Fact]
    public void FolderTree_StartsFoldersCollapsed_ExceptTheRoot_AndExpandAllOpensEverything()
    {
        NewProjectDialogViewModel viewModel = Create();
        NewProjectLayoutNode root = viewModel.FolderTree.Single();

        root.IsExpanded.ShouldBeTrue();
        root.Children.Single(child => child.Name == "Fonts").IsExpanded.ShouldBeFalse();

        viewModel.SetFolderTreeExpanded(true);
        root.Children.Single(child => child.Name == "Fonts").IsExpanded.ShouldBeTrue();
        viewModel.SetFolderTreeExpanded(false);
        root.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void FolderTree_IsEmpty_ForAPlatformTheDialogDoesNotKnow()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "fna";
        });

        viewModel.FolderTree.ShouldBeEmpty();
    }

    private static IEnumerable<NewProjectLayoutNode> Flatten(NewProjectLayoutNode node) =>
        new[] { node }.Concat(node.Children.SelectMany(Flatten));

    #endregion

    #region Checks

    [Fact]
    public async Task Checks_AskTheServiceAboutTheCurrentChoices()
    {
        System.Collections.Concurrent.ConcurrentQueue<NewProjectRequest> requests = new();
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Callback<NewProjectRequest>(request => requests.Enqueue(request))
            .Returns(Valid());
        NewProjectDialogViewModel viewModel = Create();

        viewModel.IncludeCodeProject = true;
        viewModel.PlatformName = "raylib";
        viewModel.IsIncludeFormsControls = false;
        viewModel.SelectedFileFormat = NewProjectDialogViewModel.XmlFormat;
        await viewModel.ChecksRefresh;

        // Earlier refreshes may finish in any order; one of them carries all of the final choices.
        requests.ShouldContain(request => request.Platform == "raylib" && request.Template == "empty" && request.UseXmlFormat
            && request.Path == Path.Combine(Folder, "MyGame"));
    }

    [Fact]
    public async Task Checks_DoNotBlockTyping()
    {
        using ManualResetEventSlim release = new ManualResetEventSlim();
        _detector.Setup(x => x.FindExistingCodeProject(It.IsAny<string>())).Returns(() =>
        {
            release.Wait(TimeSpan.FromSeconds(30));
            return Path.Combine(Folder, "Other.csproj");
        });
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        viewModel.ProjectName = "Typed";

        viewModel.ChecksRefresh.IsCompleted.ShouldBeFalse();
        viewModel.CommandLine.ShouldContain("Typed");
        release.Set();
        await viewModel.ChecksRefresh;
        viewModel.IsExistingCodeProjectWarningVisible.ShouldBeTrue();
    }

    [Fact]
    public async Task Checks_KeepOnlyTheLatestResult()
    {
        using ManualResetEventSlim release = new ManualResetEventSlim();
        int calls = 0;
        _detector.Setup(x => x.FindExistingCodeProject(It.IsAny<string>())).Returns(() =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                release.Wait(TimeSpan.FromSeconds(30));
                return Path.Combine(Folder, "Stale.csproj");
            }
            return Path.Combine(Folder, "Fresh.csproj");
        });
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);
        Task first = viewModel.ChecksRefresh;

        viewModel.ProjectName = "Second";
        await viewModel.ChecksRefresh;
        release.Set();
        await first;

        viewModel.ExistingCodeProjectText.ShouldContain("Fresh.csproj");
    }

    [Fact]
    public async Task Checks_Survive_AServiceAndADetectorThatThrow()
    {
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Throws(new InvalidOperationException("validate"));
        _detector.Setup(x => x.FindExistingCodeProject(It.IsAny<string>())).Throws(new UnauthorizedAccessException());
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        await viewModel.ChecksRefresh;

        viewModel.IsExistingCodeProjectWarningVisible.ShouldBeFalse();
        viewModel.IsRefused.ShouldBeFalse();
        viewModel.CanExecuteAffirmative().ShouldBeTrue();
    }

    #endregion

    #region Separators

    [Fact]
    public void FolderTree_ShowsOnlyTheOsSeparator_WhateverTheLocationUses()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.Location = WithOtherSeparator(Folder);
            vm.IncludeCodeProject = true;
        });

        NewProjectLayoutNode root = viewModel.FolderTree.Single();
        List<string> names = AllDisplayNames(root).ToList();
        names.ShouldNotContain(name => name.Contains(OtherSeparator));
        root.DisplayName.ShouldBe(NewProjectDisplayPath.AsFolder(Path.Combine(Folder, "MyGame")));
        root.Children.Select(child => child.DisplayName).ShouldContain("Content" + Sep);
        names.ShouldContain("Behaviors" + Sep);
        names.ShouldContain("GumProject.gumj");
    }

    [Fact]
    public void CommandLine_UsesTheOsSeparatorInThePath_EvenWhenLocationMixesThem()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.Location = WithOtherSeparator(Folder));

        viewModel.CommandLine.ShouldBe($"gumcli new \"{GumOnlyPath()}\"");
        viewModel.CommandLine.ShouldNotContain(OtherSeparator.ToString());
    }

    [Fact]
    public async Task Banners_ShowOnlyTheOsSeparator()
    {
        _detector.Setup(x => x.FindExistingCodeProject(It.IsAny<string>())).Returns(WithOtherSeparator(Path.Combine(Folder, "Other.csproj")));
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.Location = WithOtherSeparator(Folder);
            vm.IncludeCodeProject = true;
        });

        await viewModel.ChecksRefresh;

        viewModel.ExistingCodeProjectText.ShouldNotContain(OtherSeparator.ToString());
        viewModel.ExistingCodeProjectTip.ShouldNotContain(OtherSeparator.ToString());
        viewModel.TargetNotEmptyText.ShouldNotContain(OtherSeparator.ToString());
    }

    [Fact]
    public void Browse_NormalizesThePickedFolder()
    {
        _dialogService.Setup(x => x.OpenFolder(It.IsAny<OpenFolderDialogOptions?>())).Returns(WithOtherSeparator(Path.Combine(Folder, "Picked")));
        NewProjectDialogViewModel viewModel = Create();

        viewModel.BrowseCommand.Execute(null);

        viewModel.Location.ShouldBe(Path.Combine(Folder, "Picked"));
    }

    #endregion

    #region Existing target

    [Fact]
    public async Task EmptyOrMissingFolder_ShowsNoBanner_AndOkStaysOn()
    {
        NewProjectDialogViewModel viewModel = Create();

        await viewModel.ChecksRefresh;

        viewModel.IsTargetNotEmptyWarningVisible.ShouldBeFalse();
        viewModel.IsRefused.ShouldBeFalse();
        viewModel.CanExecuteAffirmative().ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NonEmptyFolder_WithoutAConflictingProjectFile_WarnsButOkStaysOn(bool codeProject)
    {
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(Folder, "MyGame")).Returns(true);
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = codeProject);

        await viewModel.ChecksRefresh;

        viewModel.IsTargetNotEmptyWarningVisible.ShouldBeTrue();
        viewModel.IsRefused.ShouldBeFalse();
        viewModel.CanExecuteAffirmative().ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProjectFileAlreadyThere_ShowsTheRefusal_AndTurnsOkOff(bool codeProject)
    {
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(Folder, "MyGame")).Returns(true);
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation
        {
            Error = new NewProjectError(NewProjectErrorKind.ProjectAlreadyExists, "Project already exists: " + GumOnlyPath()),
        });
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = codeProject);

        await viewModel.ChecksRefresh;

        viewModel.IsRefused.ShouldBeTrue();
        viewModel.RefusalText.ShouldBe("Project already exists: " + GumOnlyPath());
        viewModel.IsTargetNotEmptyWarningVisible.ShouldBeTrue();
        viewModel.CanExecuteAffirmative().ShouldBeFalse();
    }

    [Fact]
    public async Task OtherValidationErrors_DoNotTurnOkOffUpFront()
    {
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation
        {
            Error = new NewProjectError(NewProjectErrorKind.UnknownTheme, "Unknown theme"),
        });
        NewProjectDialogViewModel viewModel = Create();

        await viewModel.ChecksRefresh;

        viewModel.IsRefused.ShouldBeFalse();
        viewModel.CanExecuteAffirmative().ShouldBeTrue();
    }

    #endregion

    #region Running

    private static (NewProjectDialogViewModel ViewModel, List<bool> Closed) WithCloseLog(NewProjectDialogViewModel viewModel)
    {
        List<bool> closed = new List<bool>();
        viewModel.RequestClose += (_, affirmed) => closed.Add(affirmed);
        return (viewModel, closed);
    }

    private static NewProjectFilesResult FilesOk() => new NewProjectFilesResult
    {
        Success = true,
        GumProjectPath = Path.Combine(Folder, "MyGame", "Content", "GumProject", "GumProject.gumj"),
        CsprojPath = Path.Combine(Folder, "MyGame", "MyGame.csproj"),
    };

    private static RestoreResult RestoreOk() => new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 };

    private static RestoreResult RestoreFailed() => new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = "NU1101: unable to find package" };

    private void SetUpRestore(params RestoreResult[] results)
    {
        Queue<RestoreResult> queue = new Queue<RestoreResult>(results);
        _service.Setup(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(queue.Dequeue()));
    }

    [Fact]
    public void Ok_ForAGumOnlyProject_ClosesAcceptedWithoutCreatingAnything()
    {
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create());

        viewModel.AffirmativeCommand.Execute(null);

        closed.ShouldBe(new[] { true });
        viewModel.IsCodeProjectCreated.ShouldBeFalse();
        viewModel.GumProjectPath.ShouldBe(GumOnlyPath());
        _service.Verify(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Ok_IsIgnored_WhileTheNameIsInvalid()
    {
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.ProjectName = ""));

        viewModel.AffirmativeCommand.Execute(null);

        closed.ShouldBeEmpty();
    }

    [Fact]
    public void Cancel_ClosesNegative_WhenIdle()
    {
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create());

        viewModel.NegativeCommand.Execute(null);

        closed.ShouldBe(new[] { false });
    }

    [Fact]
    public async Task Ok_WithACodeProject_CreatesFilesThenRestores_ThenCloses()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(RestoreOk());
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));

        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        closed.ShouldBe(new[] { true });
        viewModel.IsCodeProjectCreated.ShouldBeTrue();
        viewModel.CreatedGumProjectPath.ShouldBe(FilesOk().GumProjectPath);
        viewModel.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public async Task Ok_WithRestoreOff_SkipsTheRestore()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.RunRestore = false;
        }));

        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        closed.ShouldBe(new[] { true });
        _service.Verify(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Ok_StaysOpenWithTheError_WhenCreatingFilesFails()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NewProjectFilesResult { Success = false, ErrorMessage = "Access to the path is denied." });
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));

        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        closed.ShouldBeEmpty();
        viewModel.ErrorMessage.ShouldBe("Access to the path is denied.");
        viewModel.HasError.ShouldBeTrue();
        viewModel.IsBusy.ShouldBeFalse();
        viewModel.IsEditable.ShouldBeTrue();
    }

    [Fact]
    public async Task Ok_ShowsTheValidationError_AndWritesNothing_WhenTheServiceRefusesTheRequest()
    {
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation
        {
            Error = new NewProjectError(NewProjectErrorKind.GumSourceNotFound, "Project already exists: x"),
        });
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        viewModel.ErrorMessage.ShouldBe("Project already exists: x");
        _service.Verify(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Ok_ShowsTheBusyState_AndDisablesTheInputs_WhileFilesAreWritten()
    {
        TaskCompletionSource<NewProjectFilesResult> files = new TaskCompletionSource<NewProjectFilesResult>();
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).Returns(files.Task);
        SetUpRestore(RestoreOk());
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        viewModel.AffirmativeCommand.Execute(null);

        viewModel.IsBusy.ShouldBeTrue();
        viewModel.IsEditable.ShouldBeFalse();
        viewModel.ProgressText.ShouldBe("Creating files...");
        viewModel.CanExecuteAffirmative().ShouldBeFalse();
        files.SetResult(FilesOk());
        await viewModel.CreationTask;
        viewModel.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public async Task Cancel_WhileBusy_CancelsTheWork_AndKeepsTheDialogOpen()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .Returns<ResolvedNewProject, CancellationToken>(async (_, token) =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                }
                return new NewProjectFilesResult { Canceled = true };
            });
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));
        viewModel.AffirmativeCommand.Execute(null);

        viewModel.NegativeCommand.Execute(null);
        await viewModel.CreationTask;

        closed.ShouldBeEmpty();
        viewModel.IsBusy.ShouldBeFalse();
        viewModel.IsEditable.ShouldBeTrue();
        viewModel.ErrorMessage.ShouldBeEmpty();
    }

    [Fact]
    public async Task RestoreFailure_OffersRetryContinueAndBack_AndKeepsTheFiles()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(RestoreFailed());
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));

        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        closed.ShouldBeEmpty();
        viewModel.IsRestoreFailed.ShouldBeTrue();
        viewModel.RestoreFailureText.ShouldBe("NU1101: unable to find package");
        viewModel.IsBusy.ShouldBeFalse();
        viewModel.IsEditable.ShouldBeFalse();
        viewModel.CanExecuteAffirmative().ShouldBeFalse();
    }

    [Fact]
    public async Task Retry_RunsOnlyTheRestore_AndClosesWhenItSucceeds()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(RestoreFailed(), RestoreOk());
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));
        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        viewModel.RetryRestoreCommand.Execute(null);
        await WaitUntil(() => closed.Count > 0);

        closed.ShouldBe(new[] { true });
        viewModel.IsRestoreFailed.ShouldBeFalse();
        viewModel.IsCodeProjectCreated.ShouldBeTrue();
        _service.Verify(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()), Times.Once);
        _service.Verify(x => x.RestoreAsync(FilesOk().CsprojPath!, It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ContinueWithoutRestore_ClosesAccepted_WithTheFilesKept()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(RestoreFailed());
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));
        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        viewModel.ContinueWithoutRestoreCommand.Execute(null);

        closed.ShouldBe(new[] { true });
        viewModel.IsCodeProjectCreated.ShouldBeTrue();
        viewModel.CreatedGumProjectPath.ShouldBe(FilesOk().GumProjectPath);
    }

    [Fact]
    public async Task Back_ReturnsToTheEditableDialog_WithoutClosing()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(RestoreFailed());
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));
        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        viewModel.BackCommand.Execute(null);

        closed.ShouldBeEmpty();
        viewModel.IsRestoreFailed.ShouldBeFalse();
        viewModel.IsEditable.ShouldBeTrue();
    }

    [Fact]
    public async Task Cancel_DuringRestore_ShowsTheRestoreFailurePanel()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        _service.Setup(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns<string, Action<string>?, CancellationToken>(async (_, _, token) =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                }
                return new RestoreResult { Status = RestoreStatus.Canceled };
            });
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);
        viewModel.AffirmativeCommand.Execute(null);
        await WaitUntil(() => viewModel.ProgressText == "Running dotnet restore...");

        viewModel.NegativeCommand.Execute(null);
        await viewModel.CreationTask;

        viewModel.IsRestoreFailed.ShouldBeTrue();
        viewModel.RestoreFailureText.ShouldBe("Restore was canceled.");
    }

    [Fact]
    public async Task Ok_PressedAgainWhileBusy_DoesNotStartASecondCreation()
    {
        TaskCompletionSource<NewProjectFilesResult> files = new TaskCompletionSource<NewProjectFilesResult>();
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).Returns(files.Task);
        SetUpRestore(RestoreOk());
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);
        viewModel.OnAffirmative();
        Task first = viewModel.CreationTask;

        viewModel.OnAffirmative();

        viewModel.CreationTask.ShouldBeSameAs(first);
        files.SetResult(FilesOk());
        await first;
        _service.Verify(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ok_StaysOpenWithTheError_WhenTheServiceThrows()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk is full."));
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));

        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        closed.ShouldBeEmpty();
        viewModel.ErrorMessage.ShouldBe("Disk is full.");
        viewModel.IsBusy.ShouldBeFalse();
        viewModel.IsEditable.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true, "Bubblegum", "forms")]
    [InlineData(false, null, "empty")]
    public async Task Ok_PassesTheChoicesToTheService(bool forms, string? expectedTheme, string expectedTemplate)
    {
        // Each Validate call hands back a uniquely named result, so the request that OK validated is the one whose name
        // reaches CreateFilesAsync; the background checks also call Validate and must not be mistaken for it.
        System.Collections.Concurrent.ConcurrentDictionary<string, NewProjectRequest> requestsByName = new();
        int validateCalls = 0;
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns<NewProjectRequest>(request =>
        {
            string name = "Call" + Interlocked.Increment(ref validateCalls);
            requestsByName[name] = request;
            return new NewProjectValidation { Resolved = new ResolvedNewProject { Name = name, CsprojPath = Valid().Resolved!.CsprojPath } };
        });
        ResolvedNewProject? created = null;
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .Callback<ResolvedNewProject, CancellationToken>((project, _) => created = project)
            .ReturnsAsync(FilesOk());
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "kni";
            vm.SourceLinked = true;
            vm.RunRestore = false;
            vm.SelectedFileFormat = NewProjectDialogViewModel.XmlFormat;
            vm.IsIncludeFormsControls = forms;
            vm.ThemeSelection.SelectedTheme = "Bubblegum";
        });

        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        created.ShouldNotBeNull();
        NewProjectRequest request = requestsByName[created.Name];
        request.Path.ShouldBe(Path.Combine(Folder, "MyGame"));
        request.Platform.ShouldBe("kni");
        request.Template.ShouldBe(expectedTemplate);
        request.Theme.ShouldBe(expectedTheme);
        request.SourceLinked.ShouldBeTrue();
        request.Restore.ShouldBeFalse();
        request.UseXmlFormat.ShouldBeTrue();
    }

    [Theory]
    [InlineData(RestoreStatus.ProcessNotFound, "", "dotnet was not found.", "dotnet was not found.")]
    [InlineData(RestoreStatus.ProcessNotFound, "some output", "", "some output")]
    [InlineData(RestoreStatus.Failed, "", "ignored", "dotnet restore failed.")]
    [InlineData(RestoreStatus.Failed, "", "", "dotnet restore failed.")]
    public async Task RestoreFailure_ShowsTheMostUsefulText(RestoreStatus status, string outputTail, string errorMessage, string expected)
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(new RestoreResult { Status = status, OutputTail = outputTail, ErrorMessage = errorMessage });
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        viewModel.IsRestoreFailed.ShouldBeTrue();
        viewModel.RestoreFailureText.ShouldBe(expected);
    }

    [Fact]
    public async Task RestoreThatThrows_ShowsTheRestoreFailurePanel_WithTheMessage()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        _service.Setup(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));

        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        closed.ShouldBeEmpty();
        viewModel.IsRestoreFailed.ShouldBeTrue();
        viewModel.RestoreFailureText.ShouldBe("boom");
        viewModel.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public async Task Cancel_OnTheRestoreFailurePanel_ClosesWithoutReportingACreatedProject()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(RestoreFailed());
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));
        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        viewModel.NegativeCommand.Execute(null);

        closed.ShouldBe(new[] { false });
        viewModel.IsCodeProjectCreated.ShouldBeFalse();
    }

    [Fact]
    public async Task Restore_ShowsTheLatestOutputLine_WhileItRuns()
    {
        TaskCompletionSource<RestoreResult> restore = new TaskCompletionSource<RestoreResult>();
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        _service.Setup(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns<string, Action<string>?, CancellationToken>((_, output, _) =>
            {
                output!("Determining projects to restore...");
                return restore.Task;
            });
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        viewModel.AffirmativeCommand.Execute(null);
        await WaitUntil(() => viewModel.ProgressDetail == "Determining projects to restore...");

        viewModel.ProgressText.ShouldBe("Running dotnet restore...");
        restore.SetResult(RestoreOk());
        await viewModel.CreationTask;
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (int i = 0; i < 400 && !condition(); i++)
        {
            await Task.Delay(10);
        }
        condition().ShouldBeTrue();
    }

    #endregion
}
