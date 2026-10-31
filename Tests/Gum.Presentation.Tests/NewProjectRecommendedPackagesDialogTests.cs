using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// The "Include recommended packages" checkbox of the New Project dialog (#7265): when it shows, what the command line
/// chains, what the request asks for, and how a failed add behaves (the restore failure panel).
/// </summary>
public class NewProjectRecommendedPackagesDialogTests
{
    private static readonly string Folder = Path.Combine(Path.GetTempPath(), "GumNewProjectPackageDialogTests");
    private static readonly string GameFolder = Path.Combine(Folder, "MyGame");
    private static readonly string Csproj = Path.Combine(GameFolder, "MyGame.csproj");

    private readonly Mock<INewProjectService> _service = new();
    private readonly Mock<IExistingCodeProjectDetector> _detector = new();
    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly List<string> _calls = new List<string>();
    // What creation was handed. The dialog also validates on a pool thread to refresh its checks, so the last recorded request is not
    // always the creation's; the resolved project given to CreateFilesAsync is.
    private readonly List<ResolvedNewProject> _created = new List<ResolvedNewProject>();
    private bool LastCreationAddsPackages => _created.Last().RecommendedPackages.Count > 0;
    private readonly Queue<AddPackagesResult> _addResults = new Queue<AddPackagesResult>();
    private readonly Queue<RestoreResult> _restoreResults = new Queue<RestoreResult>();
    private IReadOnlyList<string> _resolvedPackages = new[] { "Gum.Shapes.MonoGame", "Gum.Expressions" };

    public NewProjectRecommendedPackagesDialogTests()
    {
        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard" });
        _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");
        _service.Setup(x => x.IsSourceLinkedAvailable).Returns(true);
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns((NewProjectRequest request) =>
        {
            return new NewProjectValidation
            {
                Resolved = new ResolvedNewProject
                {
                    Name = "MyGame", CsprojPath = Csproj,
                    RecommendedPackages = request.IncludeRecommendedPackages ? _resolvedPackages : new string[0]
                }
            };
        });
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .Returns((ResolvedNewProject resolved, CancellationToken _) =>
            {
                _calls.Add("files");
                lock (_created) { _created.Add(resolved); }
                return Task.FromResult(new NewProjectFilesResult
                {
                    Success = true,
                    GumProjectPath = Path.Combine(GameFolder, "Content", "GumProject", "GumProject.gumj"),
                    CsprojPath = Csproj
                });
            });
        _service.Setup(x => x.AddPackagesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns((string _, IReadOnlyList<string> packages, Action<string>? _, CancellationToken _) =>
            {
                _calls.Add("add " + string.Join(",", packages));
                return Task.FromResult(_addResults.Count > 0 ? _addResults.Dequeue() : AddOk());
            });
        _service.Setup(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _calls.Add("restore");
                return Task.FromResult(_restoreResults.Count > 0 ? _restoreResults.Dequeue() : new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
            });
    }

    private static AddPackagesResult AddOk() => new AddPackagesResult { Status = RestoreStatus.Succeeded };

    private static AddPackagesResult AddFailed(string package) =>
        new AddPackagesResult { Status = RestoreStatus.Failed, FailedPackage = package, OutputTail = "error: could not write the project" };

    private NewProjectDialogViewModel Create(Action<NewProjectDialogViewModel>? configure = null)
    {
        NewProjectDialogViewModel viewModel = new NewProjectDialogViewModel(
            new ThemeSelectionViewModel(_formsFileService.Object, _projectState.Object),
            _service.Object, _detector.Object, new Mock<IDialogService>().Object, new Mock<INewProjectLocationSettings>().Object)
        {
            ProjectName = "MyGame",
            Location = Folder,
            IsIncludeDemoScreenGum = false,
            GenerateCodeChoice = false,
        };
        configure?.Invoke(viewModel);
        return viewModel;
    }

    private static string Add(string package) => $"dotnet add \"{Csproj}\" package {package} --no-restore";

    #region Visibility and default

    [Fact]
    public void IncludeRecommendedPackages_DefaultsToChecked()
    {
        Create().IncludeRecommendedPackages.ShouldBeTrue();
    }

    [Fact]
    public void IsRecommendedPackagesVisible_OnlyWithACodeProject()
    {
        NewProjectDialogViewModel viewModel = Create();
        viewModel.IsRecommendedPackagesVisible.ShouldBeFalse();

        viewModel.IncludeCodeProject = true;
        viewModel.IsRecommendedPackagesVisible.ShouldBeTrue();

        viewModel.IncludeCodeProject = false;
        viewModel.IsRecommendedPackagesVisible.ShouldBeFalse();
    }

    [Fact]
    public void IsRecommendedPackagesVisible_RaisesWhenTheCodeProjectIsToggled()
    {
        NewProjectDialogViewModel viewModel = Create();
        List<string?> raised = new List<string?>();
        viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        viewModel.IncludeCodeProject = true;

        raised.ShouldContain(nameof(NewProjectDialogViewModel.IsRecommendedPackagesVisible));
        raised.ShouldContain(nameof(NewProjectDialogViewModel.RecommendedPackagesChecked));
    }

    [Theory]
    [InlineData("monogame", true)]
    [InlineData("kni", true)]
    [InlineData("raylib", true)]
    [InlineData("stride", true)]
    [InlineData("silknet", true)]
    [InlineData("kniskia", false)]
    public void AddsRecommendedPackages_FollowsThePlatformsPackageTable(string platform, bool adds)
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = platform;
        });

        viewModel.AddsRecommendedPackages.ShouldBe(adds);
        viewModel.IsRecommendedPackagesEnabled.ShouldBe(adds);
    }

    [Fact]
    public void AddsRecommendedPackages_IsOff_WhenLinkingGumSource()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.SourceLinked = true;
        });

        viewModel.IsRecommendedPackagesEnabled.ShouldBeFalse();
        viewModel.RecommendedPackagesChecked.ShouldBeFalse();
        viewModel.CommandLine.ShouldNotContain("dotnet add");
    }

    [Fact]
    public void RecommendedPackagesChecked_Setter_TogglesTheOption()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        viewModel.RecommendedPackagesChecked = false;

        viewModel.IncludeRecommendedPackages.ShouldBeFalse();
    }

    [Theory]
    [InlineData("monogame", "KernSmith.MonoGameGum, Gum.Expressions")]
    [InlineData("raylib", "KernSmith.RaylibGum, Gum.Expressions")]
    [InlineData("stride", "Gum.Expressions")]
    public void RecommendedPackagesNote_ListsThePackagesTheCheckboxAdds(string platform, string expected)
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = platform;
        });

        viewModel.RecommendedPackagesNote.ShouldContain(expected);
    }

    #endregion

    #region Command line

    [Fact]
    public void CommandLine_WithTheDefaults_ChainsTheAddsAndTheRestore()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "monogame";
        });

        viewModel.CommandLine.ShouldBe(
            $"gumcli new \"{GameFolder}\" --platform monogame --no-restore"
            + $" && {Add("KernSmith.MonoGameGum")} && {Add("Gum.Expressions")}"
            + $" && dotnet restore \"{Csproj}\"");
    }

    [Fact]
    public void CommandLine_WithoutRestore_EndsAfterTheAdds()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "stride";
            vm.RunRestore = false;
        });

        viewModel.CommandLine.ShouldBe($"gumcli new \"{GameFolder}\" --platform stride --no-restore && {Add("Gum.Expressions")}");
    }

    [Fact]
    public void CommandLine_Unchecked_IsThePlainCommand()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "raylib";
            vm.IncludeRecommendedPackages = false;
        });

        viewModel.CommandLine.ShouldBe($"gumcli new \"{GameFolder}\" --platform raylib");
    }

    [Fact]
    public void CommandLine_WithoutACodeProject_IgnoresThePackages()
    {
        NewProjectDialogViewModel viewModel = Create();

        viewModel.CommandLine.ShouldNotContain("dotnet");
    }

    [Fact]
    public void CommandLine_ChainsOnOneLine_WithoutANewline()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        viewModel.CommandLine.ShouldNotContain("\n");
    }

    [Fact]
    public void CommandLine_TogglingTheCheckbox_UpdatesTheLine()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);
        viewModel.CommandLine.ShouldContain("dotnet add");

        viewModel.IncludeRecommendedPackages = false;

        viewModel.CommandLine.ShouldNotContain("dotnet add");
    }

    #endregion

    #region Request and run
    [Fact]
    public async Task Run_WithSourceLinked_RequestsNoPackages()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.SourceLinked = true;
        });

        await viewModel.RunCreationAsync();

        LastCreationAddsPackages.ShouldBeFalse();
    }

    [Fact]
    public async Task Run_ForKniSkia_RequestsNoPackages_AndCommandLineHasNoAdd()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "kniskia";
        });

        viewModel.CommandLine.ShouldNotContain("dotnet add");
        await viewModel.RunCreationAsync();

        LastCreationAddsPackages.ShouldBeFalse();
    }

    [Fact]
    public void SourceLinkedThenBackToNuGet_ReChecksTheBox_WithTheChainBack()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        viewModel.SourceLinked = true;
        viewModel.SourceLinked = false;

        viewModel.RecommendedPackagesChecked.ShouldBeTrue();
        viewModel.CommandLine.ShouldContain("dotnet add");
    }


    [Fact]
    public async Task SwitchingToKniSkiaAndBack_UnchecksThenReChecksTheBox_AndThePackagesReturn()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "monogame";
        });
        viewModel.RecommendedPackagesChecked.ShouldBeTrue();

        viewModel.PlatformName = "kniskia";

        viewModel.RecommendedPackagesChecked.ShouldBeFalse();
        viewModel.IsRecommendedPackagesEnabled.ShouldBeFalse();
        viewModel.CommandLine.ShouldNotContain("dotnet add");
        await viewModel.RunCreationAsync();
        LastCreationAddsPackages.ShouldBeFalse();

        viewModel.PlatformName = "monogame";

        viewModel.RecommendedPackagesChecked.ShouldBeTrue();
        viewModel.IsRecommendedPackagesEnabled.ShouldBeTrue();
        viewModel.CommandLine.ShouldContain(Add("KernSmith.MonoGameGum"));
        _created.Clear();
        await viewModel.RunCreationAsync();
        LastCreationAddsPackages.ShouldBeTrue();
    }

    [Fact]
    public async Task Run_AsksTheServiceForThePackages_AddsThemAfterTheFiles_AndRestoresLast()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        await viewModel.RunCreationAsync();

        LastCreationAddsPackages.ShouldBeTrue();
        _calls.ShouldBe(new[] { "files", "add Gum.Shapes.MonoGame,Gum.Expressions", "restore" });
        viewModel.IsCodeProjectCreated.ShouldBeTrue();
    }

    [Fact]
    public async Task Run_Unchecked_AsksForNoPackages_AndAddsNone()
    {
        _resolvedPackages = Array.Empty<string>();
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.IncludeRecommendedPackages = false;
        });

        await viewModel.RunCreationAsync();

        LastCreationAddsPackages.ShouldBeFalse();
        _calls.ShouldBe(new[] { "files", "restore" });
    }

    [Fact]
    public async Task Run_WhenAnAddFails_ShowsTheFailurePanel_NamesThePackage_AndSkipsTheRestore()
    {
        _addResults.Enqueue(AddFailed("Gum.Shapes.MonoGame"));
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        await viewModel.RunCreationAsync();

        viewModel.IsRestoreFailed.ShouldBeTrue();
        viewModel.RestoreFailureHeading.ShouldBe(NewProjectDialogText.PackagesFailedHeading);
        viewModel.RestoreFailureText.ShouldContain("Gum.Shapes.MonoGame");
        viewModel.RestoreFailureText.ShouldContain("could not write the project");
        viewModel.IsCodeProjectCreated.ShouldBeFalse();
        _calls.ShouldBe(new[] { "files", "add Gum.Shapes.MonoGame,Gum.Expressions" });
    }

    [Fact]
    public async Task Retry_AfterAFailedAdd_AddsAgainThenRestoresAndFinishes()
    {
        _addResults.Enqueue(AddFailed("Gum.Shapes.MonoGame"));
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);
        await viewModel.RunCreationAsync();

        viewModel.RetryRestoreCommand.Execute(null);
        for (int i = 0; i < 200 && !viewModel.IsCodeProjectCreated; i++)
        {
            await Task.Delay(10);
        }

        viewModel.IsRestoreFailed.ShouldBeFalse();
        viewModel.IsCodeProjectCreated.ShouldBeTrue();
        _calls.ShouldBe(new[]
        {
            "files", "add Gum.Shapes.MonoGame,Gum.Expressions", "add Gum.Shapes.MonoGame,Gum.Expressions", "restore"
        });
    }

    [Fact]
    public async Task Continue_AfterAFailedAdd_KeepsTheFilesAndFinishes()
    {
        _addResults.Enqueue(AddFailed("Gum.Expressions"));
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);
        await viewModel.RunCreationAsync();

        viewModel.ContinueWithoutRestoreCommand.Execute(null);

        viewModel.IsCodeProjectCreated.ShouldBeTrue();
        viewModel.CreatedGumProjectPath.ShouldNotBeNull();
    }

    [Fact]
    public async Task Back_AfterAFailedAdd_ReturnsToTheEditableDialog()
    {
        _addResults.Enqueue(AddFailed("Gum.Expressions"));
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);
        await viewModel.RunCreationAsync();

        viewModel.BackCommand.Execute(null);

        viewModel.IsRestoreFailed.ShouldBeFalse();
        viewModel.IsEditable.ShouldBeTrue();
    }

    [Fact]
    public async Task Run_WhenAnAddIsCanceled_ShowsTheFailurePanel()
    {
        _addResults.Enqueue(new AddPackagesResult { Status = RestoreStatus.Canceled, FailedPackage = "Gum.Expressions" });
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);

        await viewModel.RunCreationAsync();

        viewModel.IsRestoreFailed.ShouldBeTrue();
        viewModel.RestoreFailureText.ShouldContain("canceled");
    }

    [Fact]
    public async Task Run_WithoutRestore_StillAddsThePackages_ThenFinishes()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.RunRestore = false;
        });

        await viewModel.RunCreationAsync();

        _calls.ShouldBe(new[] { "files", "add Gum.Shapes.MonoGame,Gum.Expressions" });
        viewModel.IsCodeProjectCreated.ShouldBeTrue();
    }

    #endregion
}
