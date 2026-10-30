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
/// The New Project dialog's rules: the live gumcli command, the warnings, validation, the Files plan and the
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
        _service.Setup(x => x.GetPlan(It.IsAny<NewProjectRequest>())).Returns(
            new NewProjectPlan("root", new[] { "A.gumj", "Fonts/F.ttf", "Fonts/G.ttf" }, new[] { "Empty" }));
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
            _service.Object, _detector.Object, _dialogService.Object)
        {
            ProjectName = "MyGame",
            Location = Folder,
        };
        configure?.Invoke(viewModel);
        return viewModel;
    }

    private static readonly string Sep = Path.DirectorySeparatorChar.ToString();

    private static readonly char OtherSeparator = Path.DirectorySeparatorChar == (char)92 ? (char)47 : (char)92;

    private static IEnumerable<string> AllDisplayNames(NewProjectPlanNode node) =>
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

        await viewModel.PlanRefresh;

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

        await viewModel.PlanRefresh;

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

        await viewModel.PlanRefresh;

        viewModel.ExistingCodeProjectTip.ShouldContain("(Program.cs)");
    }

    [Fact]
    public async Task TargetNotEmptyWarning_Shows_WhenTheNewFolderHasFiles()
    {
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(Folder, "MyGame")).Returns(true);
        NewProjectDialogViewModel viewModel = Create();

        await viewModel.PlanRefresh;

        viewModel.IsTargetNotEmptyWarningVisible.ShouldBeTrue();
        viewModel.TargetNotEmptyText.ShouldBe($"{Path.Combine(Folder, "MyGame")} already has files. Files with the same names may be overwritten.");
    }

    [Fact]
    public async Task Warnings_DoNotBlockOk()
    {
        _detector.Setup(x => x.FindExistingCodeProject(It.IsAny<string>())).Returns(Path.Combine(Folder, "Other.csproj"));
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        await viewModel.PlanRefresh;

        viewModel.CanExecuteAffirmative().ShouldBeTrue();
    }

    #endregion

    #region Plan

    [Fact]
    public async Task Plan_FillsTheTree_WithCountsPerFolderAndATotal()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        await viewModel.PlanRefresh;

        viewModel.PlanFileCount.ShouldBe(3);
        viewModel.FilesCountText.ShouldBe("(3)");
        viewModel.PlanTotalText.ShouldBe("3 files");
        NewProjectPlanNode root = viewModel.PlanTree.Single();
        root.DisplayName.ShouldBe(NewProjectDisplayPath.AsFolder(Path.Combine(Folder, "MyGame")));
        root.CountText.ShouldBe("3");
        root.Children.Select(child => child.DisplayName).ShouldBe(new[] { "Empty" + Sep, "Fonts" + Sep, "A.gumj" });
        root.Children.Single(child => child.Name == "Fonts").CountText.ShouldBe("2");
        root.Children.Single(child => child.Name == "Empty").CountText.ShouldBe("0");
        root.Children.Single(child => child.Name == "A.gumj").IsMainFile.ShouldBeFalse();
    }

    [Fact]
    public async Task Plan_ForAGumOnlyProject_ShowsNoCount_AndSaysTheToolAddsMore()
    {
        NewProjectDialogViewModel viewModel = Create();

        await viewModel.PlanRefresh;

        viewModel.FilesCountText.ShouldBeEmpty();
        viewModel.PlanTotalText.ShouldBe(NewProjectDialogText.GumOnlyFilesNote);
        viewModel.PlanTree.Single().Children.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 3)]
    public async Task Plan_LeavesOutTheDemoScreen_WhenItIsUnchecked(bool includeDemo, int expectedFiles)
    {
        _service.Setup(x => x.GetPlan(It.IsAny<NewProjectRequest>())).Returns(new NewProjectPlan(
            "root", new[] { "Screens/DemoScreenGum.gusj", "Screens/Other.gusj", "A.gumj" }, Array.Empty<string>()));
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.IsIncludeDemoScreenGum = includeDemo;
        });

        await viewModel.PlanRefresh;

        viewModel.PlanFileCount.ShouldBe(expectedFiles);
    }

    [Fact]
    public async Task Plan_HighlightsTheGumProjectFile()
    {
        _service.Setup(x => x.GetPlan(It.IsAny<NewProjectRequest>())).Returns(
            new NewProjectPlan("root", new[] { "MyGame.gumj", "Other.png" }, Array.Empty<string>()));
        NewProjectDialogViewModel viewModel = Create();

        await viewModel.PlanRefresh;

        viewModel.PlanTree.Single().Children.Single(child => child.Name == "MyGame.gumj").IsMainFile.ShouldBeTrue();
    }

    [Fact]
    public async Task Plan_StartsFoldersCollapsed_ExceptTheRoot_AndExpandAllOpensEverything()
    {
        NewProjectDialogViewModel viewModel = Create();
        await viewModel.PlanRefresh;
        NewProjectPlanNode root = viewModel.PlanTree.Single();

        root.IsExpanded.ShouldBeTrue();
        root.Children.Single(child => child.Name == "Fonts").IsExpanded.ShouldBeFalse();

        viewModel.SetPlanExpanded(true);
        root.Children.Single(child => child.Name == "Fonts").IsExpanded.ShouldBeTrue();
        viewModel.SetPlanExpanded(false);
        root.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public async Task Plan_IsAskedForTheCurrentChoices()
    {
        System.Collections.Concurrent.ConcurrentQueue<NewProjectRequest> requests = new();
        _service.Setup(x => x.GetPlan(It.IsAny<NewProjectRequest>())).Callback<NewProjectRequest>(request => requests.Enqueue(request))
            .Returns(new NewProjectPlan("root", new[] { "A" }, Array.Empty<string>()));
        NewProjectDialogViewModel viewModel = Create();

        viewModel.IncludeCodeProject = true;
        viewModel.PlatformName = "raylib";
        viewModel.IsIncludeFormsControls = false;
        viewModel.SelectedFileFormat = NewProjectDialogViewModel.XmlFormat;
        await viewModel.PlanRefresh;

        // Earlier refreshes may finish in any order; one of them carries all of the final choices.
        requests.ShouldContain(request => request.Platform == "raylib" && request.Template == "empty" && request.UseXmlFormat
            && request.Path == Path.Combine(Folder, "MyGame"));
    }

    [Fact]
    public async Task Plan_DoesNotBlockTyping()
    {
        using ManualResetEventSlim release = new ManualResetEventSlim();
        _service.Setup(x => x.GetPlan(It.IsAny<NewProjectRequest>())).Returns(() =>
        {
            release.Wait(TimeSpan.FromSeconds(30));
            return new NewProjectPlan("root", new[] { "A" }, Array.Empty<string>());
        });
        NewProjectDialogViewModel viewModel = Create();

        viewModel.ProjectName = "Typed";

        viewModel.PlanRefresh.IsCompleted.ShouldBeFalse();
        viewModel.CommandLine.ShouldContain("Typed");
        release.Set();
        await viewModel.PlanRefresh;
        viewModel.PlanFileCount.ShouldBe(1);
    }

    [Fact]
    public async Task Plan_KeepsOnlyTheLatestResult()
    {
        using ManualResetEventSlim release = new ManualResetEventSlim();
        int calls = 0;
        _service.Setup(x => x.GetPlan(It.IsAny<NewProjectRequest>())).Returns(() =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                release.Wait(TimeSpan.FromSeconds(30));
                return new NewProjectPlan("root", new[] { "Stale" }, Array.Empty<string>());
            }
            return new NewProjectPlan("root", new[] { "Fresh1", "Fresh2" }, Array.Empty<string>());
        });
        NewProjectDialogViewModel viewModel = Create();
        Task first = viewModel.PlanRefresh;

        viewModel.ProjectName = "Second";
        await viewModel.PlanRefresh;
        release.Set();
        await first;

        viewModel.PlanFileCount.ShouldBe(2);
    }

    [Fact]
    public async Task Plan_IsEmpty_WhenTheServiceHasNone()
    {
        _service.Setup(x => x.GetPlan(It.IsAny<NewProjectRequest>())).Returns((NewProjectPlan?)null);
        NewProjectDialogViewModel viewModel = Create();

        await viewModel.PlanRefresh;

        viewModel.PlanTree.ShouldBeEmpty();
        viewModel.FilesCountText.ShouldBeEmpty();
    }

    #endregion

    #region Separators

    [Fact]
    public async Task Plan_ShowsOnlyTheOsSeparator_WhateverTheServiceReturns()
    {
        _service.Setup(x => x.GetPlan(It.IsAny<NewProjectRequest>())).Returns(new NewProjectPlan(
            "root",
            new[] { "Content/GumProject/GumProject.gumj", "Content" + (char)92 + "GumProject" + (char)92 + "Fonts" + (char)92 + "F.ttf", "Game1.cs" },
            new[] { "Content/GumProject/Behaviors" }));
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.Location = WithOtherSeparator(Folder);
            vm.IncludeCodeProject = true;
        });

        await viewModel.PlanRefresh;

        NewProjectPlanNode root = viewModel.PlanTree.Single();
        List<string> names = AllDisplayNames(root).ToList();
        names.ShouldNotContain(name => name.Contains(OtherSeparator));
        root.DisplayName.ShouldBe(NewProjectDisplayPath.AsFolder(Path.Combine(Folder, "MyGame")));
        root.Children.Select(child => child.DisplayName).ShouldContain("Content" + Sep);
        names.ShouldContain("Behaviors" + Sep);
        names.ShouldContain("GumProject.gumj");
        viewModel.PlanTree.Single().Children.Single(child => child.Name == "Content").Children.Single(child => child.Name == "GumProject")
            .Children.Single(child => child.Name == "GumProject.gumj").IsMainFile.ShouldBeTrue();
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

        await viewModel.PlanRefresh;

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

        await viewModel.PlanRefresh;

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

        await viewModel.PlanRefresh;

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

        await viewModel.PlanRefresh;

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

        await viewModel.PlanRefresh;

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
