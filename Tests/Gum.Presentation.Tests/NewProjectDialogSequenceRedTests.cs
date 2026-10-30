using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.Commands;
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
/// Splash pass on the New Project dialog (part of #7208): sequences and combinations nobody tested. Each test
/// asserts the behavior the dialog should have.
/// </summary>
public class NewProjectDialogSequenceRedTests
{
    private static readonly string Folder = Path.Combine(Path.GetTempPath(), "GumNewProjectSequenceRedTests");

    private readonly Mock<INewProjectService> _service = new();
    private readonly Mock<IExistingCodeProjectDetector> _detector = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly Mock<IProjectState> _projectState = new();

    public NewProjectDialogSequenceRedTests()
    {
        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard", "Bubblegum" });
        _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");
        _service.Setup(x => x.IsSourceLinkedAvailable).Returns(true);
        _service.Setup(x => x.GetPlan(It.IsAny<NewProjectRequest>())).Returns(
            new NewProjectPlan("root", new[] { "A.gumj", "Fonts/F.ttf" }, new[] { "Empty" }));
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

    private static NewProjectFilesResult FilesOk() => new NewProjectFilesResult
    {
        Success = true,
        GumProjectPath = Path.Combine(Folder, "MyGame", "Content", "GumProject", "GumProject.gumj"),
        CsprojPath = Path.Combine(Folder, "MyGame", "MyGame.csproj"),
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

    private void SetUpRestore(params RestoreResult[] results)
    {
        Queue<RestoreResult> queue = new Queue<RestoreResult>(results);
        _service.Setup(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(queue.Dequeue()));
    }

    private static RestoreResult RestoreFailed() => new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = "NU1101" };

    // A code project whose restore failed, with the files now on disk: the service refuses the same request, and the
    // target folder is no longer empty.
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
        await viewModel.PlanRefresh;

        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;
        viewModel.IsRestoreFailed.ShouldBeTrue();
        return viewModel;
    }

    [Fact]
    public async Task Back_AfterAFailedRestore_ShowsTheRefusalForTheFilesNowOnDisk()
    {
        NewProjectDialogViewModel viewModel = await CreateWithFailedRestoreAndFilesOnDisk();

        viewModel.BackCommand.Execute(null);
        await viewModel.PlanRefresh;

        viewModel.RefusalText.ShouldNotBeEmpty();
        viewModel.CanExecuteAffirmative().ShouldBeFalse();
    }

    [Fact]
    public async Task Back_AfterAFailedRestore_ShowsTheTargetNotEmptyWarning()
    {
        NewProjectDialogViewModel viewModel = await CreateWithFailedRestoreAndFilesOnDisk();

        viewModel.BackCommand.Execute(null);
        await viewModel.PlanRefresh;

        viewModel.IsTargetNotEmptyWarningVisible.ShouldBeTrue();
    }

    [Fact]
    public async Task ErrorMessage_FromAFailedCreation_ClearsWhenAnInputChanges()
    {
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk is full."));
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeCodeProject = true);
        viewModel.AffirmativeCommand.Execute(null);
        await viewModel.CreationTask;
        viewModel.HasError.ShouldBeTrue();

        viewModel.ProjectName = "OtherGame";

        viewModel.ErrorMessage.ShouldBeEmpty();
        viewModel.HasError.ShouldBeFalse();
    }

    #region NewProjectLogic

    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IFormsThemeImporter> _themeImporter = new();
    private readonly Mock<ICopyPasteProjectCommands> _projectCommands = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IDefaultFontBundler> _defaultFontBundler = new();
    private readonly Mock<IDeleteLogic> _deleteLogic = new();

    [Fact]
    public async Task CreateNewProjectAsync_WithACodeProject_DoesNotThrow_WhenTheCreatedProjectCannotBeLoaded()
    {
        GumProjectSave? current = new GumProjectSave();
        _projectManager.Setup(x => x.GumProjectSave).Returns(() => current);
        _projectManager.Setup(x => x.CreateBlankProject()).Returns(new GumProjectSave());
        // A load that fails leaves no project loaded.
        _projectManager.Setup(x => x.LoadProjectAsync(It.IsAny<FilePath>()))
            .Callback(() => current = null)
            .Returns(Task.CompletedTask);
        Mock<INewProjectService> service = new();
        service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation { Resolved = new ResolvedNewProject() });
        service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NewProjectFilesResult { Success = true, GumProjectPath = Path.Combine(Folder, "G", "Content", "GumProject", "GumProject.gumj"), CsprojPath = "g.csproj" });
        NewProjectDialogViewModel viewModel = new(
            new ThemeSelectionViewModel(_formsFileService.Object, _projectState.Object),
            service.Object, _detector.Object, _dialogService.Object)
        {
            ProjectName = "G",
            Location = Folder,
            IncludeCodeProject = true,
            RunRestore = false,
        };
        await viewModel.RunCreationAsync();
        _dialogService.Setup(x => x.Show(It.IsAny<Action<NewProjectDialogViewModel>?>(), out viewModel)).Returns(true);
        NewProjectLogic logic = new(
            _projectManager.Object, _dialogService.Object, _fileCommands.Object, _themeImporter.Object,
            _projectCommands.Object, _selectedState.Object, _defaultFontBundler.Object, _deleteLogic.Object);

        Func<Task> create = () => logic.CreateNewProjectAsync();

        await Should.NotThrowAsync(create);
    }

    #endregion
}
