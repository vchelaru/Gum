using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Splash pass on the New Project dialog (part of #7208): sequences and combinations the first tests did not
/// cover. Each test asserts the behavior the dialog should have.
/// </summary>
public class NewProjectDialogSequenceTests
{
    private static readonly string Folder = Path.Combine(Path.GetTempPath(), "GumNewProjectSequenceTests");

    private readonly Mock<INewProjectService> _service = new();
    private readonly Mock<IExistingCodeProjectDetector> _detector = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly Mock<IProjectState> _projectState = new();

    public NewProjectDialogSequenceTests()
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

    private static NewProjectValidation Exists() => new NewProjectValidation
    {
        Error = new NewProjectError(NewProjectErrorKind.ProjectAlreadyExists, "Gum project already exists: " + Path.Combine(Folder, "MyGame")),
    };

    private static NewProjectFilesResult FilesOk(string name = "MyGame") => new NewProjectFilesResult
    {
        Success = true,
        GumProjectPath = Path.Combine(Folder, name, "Content", "GumProject", "GumProject.gumj"),
        CsprojPath = Path.Combine(Folder, name, name + ".csproj"),
    };

    private static RestoreResult RestoreOk() => new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 };

    private static RestoreResult RestoreFailed() => new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = "NU1101" };

    private NewProjectDialogViewModel Create(Action<NewProjectDialogViewModel>? configure = null)
    {
        NewProjectDialogViewModel viewModel = new NewProjectDialogViewModel(
            new ThemeSelectionViewModel(_formsFileService.Object, _projectState.Object),
            _service.Object, _detector.Object, _dialogService.Object, new Mock<INewProjectLocationSettings>().Object)
        {
            ProjectName = "MyGame",
            Location = Folder,
            IncludeRecommendedPackages = false,
        };
        configure?.Invoke(viewModel);
        return viewModel;
    }

    private void SetUpRestore(params RestoreResult[] results)
    {
        Queue<RestoreResult> queue = new Queue<RestoreResult>(results);
        _service.Setup(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(queue.Dequeue()));
    }

    private static (NewProjectDialogViewModel ViewModel, List<bool> Closed) WithCloseLog(NewProjectDialogViewModel viewModel)
    {
        List<bool> closed = new List<bool>();
        viewModel.RequestClose += (_, affirmed) => closed.Add(affirmed);
        return (viewModel, closed);
    }

    #region After a failed restore

    [Fact]
    public async Task Back_ThenOkWithTheSameChoices_ShowsTheServiceRefusal_AndStaysOpen()
    {
        bool created = false;
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                created = true;
                return Task.FromResult(FilesOk());
            });
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(() => created ? Exists() : Valid());
        SetUpRestore(RestoreFailed());
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));
        await viewModel.ChecksRefresh;
        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;
        viewModel.BackCommand.Execute(null);

        await viewModel.RunCreationAsync();

        closed.ShouldBeEmpty();
        viewModel.ErrorMessage.ShouldStartWith("Gum project already exists");
        viewModel.IsBusy.ShouldBeFalse();
        viewModel.IsEditable.ShouldBeTrue();
    }

    // The files exist after the code project is created, so the service refuses the same request and the target
    // folder is no longer empty.
    private async Task<NewProjectDialogViewModel> CreateWithFailedRestoreAndFilesOnDisk()
    {
        bool created = false;
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                created = true;
                return Task.FromResult(FilesOk());
            });
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(() => created ? Exists() : Valid());
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(It.IsAny<string>(), It.IsAny<string>())).Returns(() => created);
        SetUpRestore(RestoreFailed());
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);
        await viewModel.ChecksRefresh;
        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;
        viewModel.IsRestoreFailed.ShouldBeTrue();
        return viewModel;
    }

    [Fact]
    public async Task Back_AfterAFailedRestore_ShowsTheRefusalAndTheNotEmptyWarning_AndDisablesOk()
    {
        NewProjectDialogViewModel viewModel = await CreateWithFailedRestoreAndFilesOnDisk();

        viewModel.BackCommand.Execute(null);
        await viewModel.ChecksRefresh;

        viewModel.RefusalText.ShouldNotBeEmpty();
        viewModel.IsTargetNotEmptyWarningVisible.ShouldBeTrue();
        viewModel.CanExecuteAffirmative().ShouldBeFalse();
    }

    [Fact]
    public async Task Back_AfterAFailedRestore_AllowsOkAgain_WhenTheNameChangesToAFreeOne()
    {
        NewProjectDialogViewModel viewModel = await CreateWithFailedRestoreAndFilesOnDisk();
        viewModel.BackCommand.Execute(null);
        await viewModel.ChecksRefresh;
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(Valid());
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        viewModel.ProjectName = "Second";
        await viewModel.ChecksRefresh;

        viewModel.RefusalText.ShouldBeEmpty();
        viewModel.IsTargetNotEmptyWarningVisible.ShouldBeFalse();
        viewModel.CanExecuteAffirmative().ShouldBeTrue();
    }

    [Fact]
    public async Task FailedFileCreation_RefreshesTheChecks_ForTheFilesAlreadyWritten()
    {
        bool written = false;
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                written = true;
                return Task.FromResult(new NewProjectFilesResult { Success = false, ErrorMessage = "Disk is full." });
            });
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(() => written ? Exists() : Valid());
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);
        await viewModel.ChecksRefresh;

        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;
        await viewModel.ChecksRefresh;

        viewModel.ErrorMessage.ShouldBe("Disk is full.");
        viewModel.RefusalText.ShouldNotBeEmpty();
        viewModel.CanExecuteAffirmative().ShouldBeFalse();
    }

    [Theory]
    [InlineData(nameof(NewProjectDialogViewModel.ProjectName))]
    [InlineData(nameof(NewProjectDialogViewModel.Location))]
    [InlineData(nameof(NewProjectDialogViewModel.SelectedFileFormat))]
    [InlineData(nameof(NewProjectDialogViewModel.IsIncludeFormsControls))]
    [InlineData(nameof(NewProjectDialogViewModel.IsIncludeDemoScreenGum))]
    [InlineData(nameof(NewProjectDialogViewModel.IncludeCodeProject))]
    [InlineData(nameof(NewProjectDialogViewModel.PlatformName))]
    [InlineData(nameof(NewProjectDialogViewModel.SourceLinkedChoice))]
    [InlineData(nameof(NewProjectDialogViewModel.RunRestore))]
    public async Task ErrorMessage_FromAFailedCreation_ClearsWhenAnInputChanges(string input)
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk is full."));
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);
        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;
        viewModel.HasError.ShouldBeTrue();

        switch (input)
        {
            case nameof(NewProjectDialogViewModel.ProjectName): viewModel.ProjectName = "OtherGame"; break;
            case nameof(NewProjectDialogViewModel.Location): viewModel.Location = Path.Combine(Folder, "Other"); break;
            case nameof(NewProjectDialogViewModel.SelectedFileFormat): viewModel.SelectedFileFormat = NewProjectDialogViewModel.XmlFormat; break;
            case nameof(NewProjectDialogViewModel.IsIncludeFormsControls): viewModel.IsIncludeFormsControls = !viewModel.IsIncludeFormsControls; break;
            case nameof(NewProjectDialogViewModel.IsIncludeDemoScreenGum): viewModel.IsIncludeDemoScreenGum = !viewModel.IsIncludeDemoScreenGum; break;
            case nameof(NewProjectDialogViewModel.IncludeCodeProject): viewModel.IncludeCodeProject = false; break;
            case nameof(NewProjectDialogViewModel.PlatformName): viewModel.PlatformName = "raylib"; break;
            case nameof(NewProjectDialogViewModel.SourceLinkedChoice): viewModel.SourceLinkedChoice = !viewModel.SourceLinkedChoice; break;
            case nameof(NewProjectDialogViewModel.RunRestore): viewModel.RunRestore = !viewModel.RunRestore; break;
        }

        viewModel.ErrorMessage.ShouldBeEmpty();
        viewModel.HasError.ShouldBeFalse();
    }

    [Fact]
    public async Task Back_ThenANewName_CreatesTheSecondProject_AndReportsIt()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .Returns<ResolvedNewProject, CancellationToken>((_, _) => Task.FromResult(FilesOk("Second")));
        SetUpRestore(RestoreFailed(), RestoreOk());
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));
        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;
        viewModel.BackCommand.Execute(null);
        viewModel.ProjectName = "Second";

        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        closed.ShouldBe(new[] { true });
        viewModel.CreatedGumProjectPath.ShouldBe(FilesOk("Second").GumProjectPath);
    }

    [Fact]
    public async Task Retry_ThatFailsAgain_ShowsThePanelAgain_AndIsNoLongerBusy()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(RestoreFailed(), new RestoreResult { Status = RestoreStatus.Failed, OutputTail = "second failure" });
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));
        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        viewModel.RetryRestoreCommand.Execute(null);
        await WaitUntil(() => viewModel.RestoreFailureText == "second failure");

        closed.ShouldBeEmpty();
        viewModel.IsRestoreFailed.ShouldBeTrue();
        viewModel.IsBusy.ShouldBeFalse();
    }

    [Fact]
    public async Task Retry_HidesTheFailurePanel_WhileTheRestoreRuns()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        TaskCompletionSource<RestoreResult> second = new TaskCompletionSource<RestoreResult>();
        Queue<Task<RestoreResult>> restores = new Queue<Task<RestoreResult>>(new[] { Task.FromResult(RestoreFailed()), second.Task });
        _service.Setup(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() => restores.Dequeue());
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);
        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        viewModel.RetryRestoreCommand.Execute(null);

        viewModel.IsRestoreFailed.ShouldBeFalse();
        viewModel.IsBusy.ShouldBeTrue();
        viewModel.ProgressText.ShouldBe("Running dotnet restore...");
        second.SetResult(RestoreOk());
        await WaitUntil(() => !viewModel.IsBusy);
    }

    [Fact]
    public async Task Cancel_WhileFilesAreWritten_SkipsTheRestore_ThenCancelAgainCloses()
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

        _service.Verify(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()), Times.Never);
        viewModel.IsRestoreFailed.ShouldBeFalse();
        closed.ShouldBeEmpty();

        viewModel.NegativeCommand.Execute(null);

        closed.ShouldBe(new[] { false });
        viewModel.IsCodeProjectCreated.ShouldBeFalse();
    }

    [Fact]
    public async Task Cancel_ThenOkAgain_CreatesTheProject()
    {
        int calls = 0;
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .Returns<ResolvedNewProject, CancellationToken>(async (_, token) =>
            {
                if (++calls == 1)
                {
                    try
                    {
                        await Task.Delay(Timeout.Infinite, token);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    return new NewProjectFilesResult { Canceled = true };
                }
                return FilesOk();
            });
        SetUpRestore(RestoreOk());
        (NewProjectDialogViewModel viewModel, List<bool> closed) = WithCloseLog(Create(vm => vm.IncludeCodeProject = true));
        viewModel.AffirmativeCommand.Execute(null);
        viewModel.NegativeCommand.Execute(null);
        await viewModel.CreationTask;

        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;

        closed.ShouldBe(new[] { true });
        viewModel.IsCodeProjectCreated.ShouldBeTrue();
    }

    #endregion

    #region Odd orders of toggles

    [Fact]
    public void SourceLinkedTick_SurvivesAPlatformRoundTripThroughKniSkia()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "raylib";
            vm.SourceLinked = true;
        });

        viewModel.PlatformName = "kniskia";
        viewModel.SourceLinked.ShouldBeTrue();
        viewModel.PlatformName = "raylib";

        viewModel.SourceLinked.ShouldBeTrue();
        viewModel.CommandLine.ShouldContain("--source-linked");
    }

    [Fact]
    public void KniSkiaForcesSourceLinked_ButItDoesNotTickTheBoxForOtherPlatforms()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "kniskia";
        });
        viewModel.CommandLine.ShouldContain("--source-linked");

        viewModel.PlatformName = "monogame";

        viewModel.SourceLinked.ShouldBeFalse();
        viewModel.SourceLinkedChoice.ShouldBeFalse();
        viewModel.CommandLine.ShouldNotContain("--source-linked");
    }

    [Fact]
    public void KniSkia_WithoutACheckout_ClearsTheMessage_WhenTheCodeProjectIsUnticked()
    {
        _service.Setup(x => x.IsSourceLinkedAvailable).Returns(false);
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "kniskia";
        });
        viewModel.ValidationMessage.ShouldNotBeEmpty();

        viewModel.IncludeCodeProject = false;

        viewModel.ValidationMessage.ShouldBeEmpty();
        viewModel.CommandLine.ShouldNotContain("--source-linked");
        viewModel.CommandLine.ShouldNotContain("--platform");
    }

    [Fact]
    public void FormsOffThenOn_BringsBackTheChosenTheme_InTheCommandAndTheRequest()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.ThemeSelection.SelectedTheme = "Bubblegum");

        viewModel.IsIncludeFormsControls = false;
        viewModel.CommandLine.ShouldNotContain("--theme");
        viewModel.IsIncludeFormsControls = true;

        viewModel.CommandLine.ShouldEndWith("--theme Bubblegum");
    }

    [Fact]
    public async Task ThemeChangedWhileFormsAreOff_IsUsedOnceFormsAreTurnedOn()
    {
        List<NewProjectRequest> requests = new List<NewProjectRequest>();
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(RestoreOk());
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.IsIncludeFormsControls = false;
        });
        viewModel.ThemeSelection.SelectedTheme = "Bubblegum";
        viewModel.IsIncludeFormsControls = true;
        await viewModel.ChecksRefresh;
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Callback<NewProjectRequest>(requests.Add).Returns(Valid());

        await viewModel.RunCreationAsync();

        NewProjectRequest request = requests.Last();
        request.Template.ShouldBe("forms");
        request.Theme.ShouldBe("Bubblegum");
    }

    [Fact]
    public void NoThemesShipped_StillImportsTheDefaultThemeName()
    {
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string>());
        NewProjectDialogViewModel viewModel = Create();

        viewModel.ThemeSelection.SelectedTheme.ShouldBeNull();
        viewModel.CommandLine.ShouldNotContain("--theme");
        viewModel.ThemeSelection.GetSelectedThemeOrDefault().ShouldBe("Standard");
    }

    #endregion

    #region Stale state after an input change

    [Fact]
    public async Task Refusal_StillShows_WhenTheDetectorCannotReadTheFolder()
    {
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(Exists());
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(It.IsAny<string>(), It.IsAny<string>())).Throws(new IOException("boom"));
        NewProjectDialogViewModel viewModel = Create();

        await viewModel.ChecksRefresh;

        viewModel.RefusalText.ShouldNotBeEmpty();
        viewModel.CanExecuteAffirmative().ShouldBeFalse();
    }

    [Fact]
    public async Task Refusal_ClearsWhenTheNameChangesToOneThatIsFree()
    {
        _service.Setup(x => x.Validate(It.Is<NewProjectRequest>(request => request.Path!.Contains("Taken")))).Returns(Exists());
        NewProjectDialogViewModel viewModel = Create(vm => vm.ProjectName = "Taken");
        await viewModel.ChecksRefresh;
        viewModel.IsRefused.ShouldBeTrue();

        viewModel.ProjectName = "Free";
        await viewModel.ChecksRefresh;

        viewModel.RefusalText.ShouldBeEmpty();
        viewModel.IsRefused.ShouldBeFalse();
        viewModel.CanExecuteAffirmative().ShouldBeTrue();
    }

    [Fact]
    public async Task TargetNotEmptyWarning_FollowsTheLocation()
    {
        string other = Path.Combine(Path.GetTempPath(), "GumNewProjectSequenceOther");
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(Folder, It.IsAny<string>())).Returns(true);
        NewProjectDialogViewModel viewModel = Create();
        await viewModel.ChecksRefresh;
        viewModel.IsTargetNotEmptyWarningVisible.ShouldBeTrue();

        viewModel.Location = other;
        await viewModel.ChecksRefresh;

        viewModel.IsTargetNotEmptyWarningVisible.ShouldBeFalse();
        viewModel.TargetNotEmptyText.ShouldBeEmpty();
    }

    [Fact]
    public async Task StaleWarnings_FromASlowerEarlierRefresh_AreDiscarded()
    {
        ManualResetEventSlim release = new ManualResetEventSlim();
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(It.IsAny<string>(), "Slow"))
            .Returns(() =>
            {
                release.Wait(TimeSpan.FromSeconds(10));
                return true;
            });
        _detector.Setup(x => x.TargetExistsAndIsNotEmpty(It.IsAny<string>(), "Fast")).Returns(false);
        NewProjectDialogViewModel viewModel = Create(vm => vm.ProjectName = "Slow");

        viewModel.ProjectName = "Fast";
        await viewModel.ChecksRefresh;
        release.Set();
        await Task.Delay(100);

        viewModel.IsTargetNotEmptyWarningVisible.ShouldBeFalse();
        viewModel.TargetNotEmptyText.ShouldBeEmpty();
    }

    #endregion

    #region The command against the request

    private record ParsedCommand(string Path, string? Platform, string? Template, string? Theme, bool SourceLinked, bool NoRestore);

    private static ParsedCommand Parse(string command)
    {
        int open = command.IndexOf('"');
        int close = command.IndexOf('"', open + 1);
        string path = command.Substring(open + 1, close - open - 1);
        string[] words = command.Substring(close + 1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string? platform = null;
        string? template = null;
        string? theme = null;
        bool sourceLinked = false;
        bool noRestore = false;
        for (int i = 0; i < words.Length; i++)
        {
            switch (words[i])
            {
                case "--platform": platform = words[++i]; break;
                case "--template": template = words[++i]; break;
                case "--theme": theme = words[++i]; break;
                case "--source-linked": sourceLinked = true; break;
                case "--no-restore": noRestore = true; break;
                default: throw new InvalidOperationException("Unknown option " + words[i]);
            }
        }
        return new ParsedCommand(path, platform, template, theme, sourceLinked, noRestore);
    }

    [Fact]
    public async Task CommandLine_MeansWhatOkSends_ForEveryCombinationOfTheOptions()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>())).ReturnsAsync(FilesOk());
        SetUpRestore(Enumerable.Repeat(RestoreOk(), 400).ToArray());
        int testThread = Environment.CurrentManagedThreadId;
        NewProjectRequest? sent = null;
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>()))
            .Callback<NewProjectRequest>(request =>
            {
                if (Environment.CurrentManagedThreadId == testThread)
                {
                    sent = request;
                }
            })
            .Returns(Valid());

        foreach (bool code in new[] { false, true })
        foreach (bool forms in new[] { true, false })
        foreach (string theme in new[] { "Standard", "Bubblegum" })
        foreach (string platform in new[] { "monogame", "kniskia", "raylib" })
        foreach (bool linked in new[] { false, true })
        foreach (bool restore in new[] { true, false })
        foreach (bool xml in new[] { false, true })
        {
            if (xml && code)
            {
                continue; // A known gap, tracked in #7215.
            }
            NewProjectDialogViewModel viewModel = Create(vm =>
            {
                vm.IncludeCodeProject = code;
                vm.IsIncludeFormsControls = forms;
                vm.ThemeSelection.SelectedTheme = theme;
                vm.PlatformName = platform;
                vm.SourceLinkedChoice = linked;
                vm.RunRestore = restore;
                vm.SelectedFileFormat = xml ? NewProjectDialogViewModel.XmlFormat : NewProjectDialogViewModel.JsonFormat;
            });
            string label = $"code={code} forms={forms} theme={theme} platform={platform} linked={linked} restore={restore} xml={xml}";
            ParsedCommand command = Parse(viewModel.CommandLine);

            sent = null;
            Task run = viewModel.RunCreationAsync();
            await run;

            sent.ShouldNotBeNull(label);
            sent.Path.ShouldBe(command.Path, label);
            sent.Platform.ShouldBe(command.Platform, label);
            sent.Template.ShouldBe(command.Template ?? "forms", label);
            sent.Theme.ShouldBe(forms ? command.Theme ?? "Standard" : null, label);
            sent.SourceLinked.ShouldBe(command.SourceLinked, label);
            if (code)
            {
                sent.Restore.ShouldBe(!command.NoRestore, label);
            }
            sent.UseXmlFormat.ShouldBe(xml, label);
        }
    }

    #endregion

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (int i = 0; i < 400 && !condition(); i++)
        {
            await Task.Delay(10);
        }
        condition().ShouldBeTrue();
    }
}

/// <summary>The gumcli command at the edges of its inputs.</summary>
public class NewProjectCommandLineSequenceTests
{
    [Fact]
    public void GetPath_AtAFilesystemRoot_KeepsTheRoot_ForBothKindsOfProject()
    {
        string root = Path.GetPathRoot(Path.GetTempPath())!;

        NewProjectCommandLine.GetPath(root, "MyGame", ".gumj", withCodeProject: true).ShouldBe(Path.Combine(root, "MyGame"));
        NewProjectCommandLine.GetPath(root, "MyGame", ".gumx", withCodeProject: false).ShouldBe(Path.Combine(root, "MyGame", "MyGame.gumx"));
    }

    [Fact]
    public void Build_WithNoPlatform_NeverShowsThePlatformOnlyOptions()
    {
        string command = NewProjectCommandLine.Build("/a/MyGame/MyGame.gumj", null, includeForms: false, theme: "Bubblegum", sourceLinked: true, restore: false);

        command.ShouldBe("gumcli new \"/a/MyGame/MyGame.gumj\" --template empty");
    }

    [Fact]
    public void Build_WithFormsAndNoThemeName_AddsNoThemeOption()
    {
        NewProjectCommandLine.Build("/a/Game", "raylib", includeForms: true, theme: null, sourceLinked: false, restore: true)
            .ShouldBe("gumcli new \"/a/Game\" --platform raylib");
        NewProjectCommandLine.Build("/a/Game", "raylib", includeForms: true, theme: "", sourceLinked: false, restore: true)
            .ShouldBe("gumcli new \"/a/Game\" --platform raylib");
    }
}

/// <summary>The theme picker as the New Project dialog drives it.</summary>
public class ThemeSelectionSequenceTests
{
    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly Mock<IProjectState> _projectState = new();

    public ThemeSelectionSequenceTests()
    {
        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard", "Bubblegum" });
        _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");
        _projectState.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());
    }

    [Fact]
    public void SelectedTheme_ChangedTwiceToTheSameValue_RaisesOnce()
    {
        ThemeSelectionViewModel themeSelection = new(_formsFileService.Object, _projectState.Object);
        int raised = 0;
        themeSelection.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ThemeSelectionViewModel.SelectedTheme))
            {
                raised++;
            }
        };

        themeSelection.SelectedTheme = "Bubblegum";
        themeSelection.SelectedTheme = "Bubblegum";

        raised.ShouldBe(1);
    }

    [Fact]
    public void TargetProject_SetBackToNull_DescribesAgainstTheLoadedProjectAgain()
    {
        string themeDirectory = Path.Combine(Path.GetTempPath(), "GumThemeSelectionSequence_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(themeDirectory);
        try
        {
            File.WriteAllText(Path.Combine(themeDirectory, ThemeRequirements.ThemeRequirementsFileName), "RequiresSkiaShapes: true");
            _formsFileService.Setup(x => x.GetThemeDirectory("Standard")).Returns(themeDirectory);
            GumProjectSave loaded = new();
            loaded.StandardElementReferences.Add(new ElementReference { Name = "Svg", ElementType = ElementType.Standard });
            _projectState.Setup(x => x.GumProjectSave).Returns(loaded);
            ThemeSelectionViewModel themeSelection = new(_formsFileService.Object, _projectState.Object);
            themeSelection.HasRequirements.ShouldBeFalse();
            themeSelection.TargetProject = new GumProjectSave();
            themeSelection.HasRequirements.ShouldBeTrue();

            themeSelection.TargetProject = null;

            themeSelection.HasRequirements.ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(themeDirectory, recursive: true);
        }
    }
}
