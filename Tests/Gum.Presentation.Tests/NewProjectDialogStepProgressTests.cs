using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>#7411: the busy text names the step's position, such as "Step 2 of 4".</summary>
public class NewProjectDialogStepProgressTests
{
    private static readonly string Folder = Path.Combine(Path.GetTempPath(), "GumNewProjectStepProgressTests");

    private readonly Mock<INewProjectService> _service = new();
    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly List<string> _seen = new();
    private NewProjectDialogViewModel _viewModel = null!;

    public NewProjectDialogStepProgressTests()
    {
        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard" });
        _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");
        _service.Setup(x => x.IsSourceLinkedAvailable).Returns(true);
    }

    private void Arrange(bool packages, bool restore, bool codegen, params RestoreResult[] restoreResults)
    {
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation
        {
            Resolved = new ResolvedNewProject
            {
                Name = "MyGame",
                CsprojPath = Path.Combine(Folder, "MyGame", "MyGame.csproj"),
                RecommendedPackages = packages ? new[] { "Gum.Shapes.MonoGame" } : new string[0],
                GenerateCode = codegen,
                IncludeFormsTemplate = codegen,
            },
        });
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _seen.Add(_viewModel.ProgressText);
                return Task.FromResult(new NewProjectFilesResult
                {
                    Success = true,
                    GumProjectPath = Path.Combine(Folder, "MyGame", "Content", "GumProject", "GumProject.gumj"),
                    CsprojPath = Path.Combine(Folder, "MyGame", "MyGame.csproj"),
                });
            });
        _service.Setup(x => x.AddPackagesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _seen.Add(_viewModel.ProgressText);
                return Task.FromResult(new AddPackagesResult { Status = RestoreStatus.Succeeded });
            });
        _service.Setup(x => x.AddPackagesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _seen.Add(_viewModel.ProgressText);
                return Task.FromResult(new AddPackagesResult { Status = RestoreStatus.Succeeded });
            });
        Queue<RestoreResult> queue = new Queue<RestoreResult>(restoreResults);
        _service.Setup(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _seen.Add(_viewModel.ProgressText);
                return Task.FromResult(queue.Count > 0 ? queue.Dequeue() : new RestoreResult { Status = RestoreStatus.Succeeded });
            });
        _service.Setup(x => x.GenerateCodeAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<NewProjectFilesResult>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _seen.Add(_viewModel.ProgressText);
                return Task.FromResult(new GenerateCodeResult { Success = true });
            });

        _viewModel = new NewProjectDialogViewModel(
            new ThemeSelectionViewModel(_formsFileService.Object, new Mock<Gum.ToolStates.IProjectState>().Object),
            _service.Object, new Mock<IExistingCodeProjectDetector>().Object, new Mock<IDialogService>().Object,
            new Mock<INewProjectLocationSettings>().Object)
        {
            ProjectName = "MyGame",
            Location = Folder,
            IncludeCodeProject = true,
            IncludeRecommendedPackages = false,
            IsIncludeDemoScreenGum = false,
            GenerateCodeChoice = false,
            RunRestore = restore,
        };
    }

    [Fact]
    public async Task NoPackagesNoRestore_IsOneStep_WithNoPosition()
    {
        Arrange(packages: false, restore: false, codegen: false);

        _viewModel.AffirmativeCommand.Execute(null);
        await _viewModel.CreationTask;

        _seen.ShouldBe(new[] { "Creating files..." });
    }

    [Fact]
    public async Task PackagesOnly_NumbersTwoSteps()
    {
        Arrange(packages: true, restore: false, codegen: false);

        _viewModel.AffirmativeCommand.Execute(null);
        await _viewModel.CreationTask;

        _seen.ShouldBe(new[] { "Step 1 of 2: Creating files...", "Step 2 of 2: Adding recommended packages..." });
    }

    [Fact]
    public async Task PackagesAndRestore_NumbersThreeSteps()
    {
        Arrange(packages: true, restore: true, codegen: false);

        _viewModel.AffirmativeCommand.Execute(null);
        await _viewModel.CreationTask;

        _seen.ShouldBe(new[]
        {
            "Step 1 of 3: Creating files...",
            "Step 2 of 3: Adding recommended packages...",
            "Step 3 of 3: Running dotnet restore...",
        });
    }

    [Fact]
    public async Task RestoreOnly_SkipsThePackagesStep_WithoutAGapInTheNumbers()
    {
        Arrange(packages: false, restore: true, codegen: false);

        _viewModel.AffirmativeCommand.Execute(null);
        await _viewModel.CreationTask;

        _seen.ShouldBe(new[] { "Step 1 of 2: Creating files...", "Step 2 of 2: Running dotnet restore..." });
    }

    [Fact]
    public async Task AllFourSteps_AreNumberedInOrder()
    {
        Arrange(packages: true, restore: true, codegen: true);

        _viewModel.AffirmativeCommand.Execute(null);
        await _viewModel.CreationTask;

        _seen.ShouldBe(new[]
        {
            "Step 1 of 4: Creating files...",
            "Step 2 of 4: Adding recommended packages...",
            "Step 3 of 4: Running dotnet restore...",
            "Step 4 of 4: Generating code for the demo screen...",
        });
    }

    [Fact]
    public async Task Retry_AfterAFailedRestore_KeepsTheNumbersOfTheOriginalRun()
    {
        Arrange(packages: true, restore: true, codegen: true, new RestoreResult { Status = RestoreStatus.Failed, OutputTail = "NU1101" });
        _viewModel.AffirmativeCommand.Execute(null);
        await _viewModel.CreationTask;
        _seen.Clear();

        _viewModel.RetryRestoreCommand.Execute(null);
        for (int i = 0; i < 400 && _viewModel.IsBusy; i++)
        {
            await Task.Delay(10);
        }

        _seen.ShouldBe(new[]
        {
            "Step 2 of 4: Adding recommended packages...",
            "Step 3 of 4: Running dotnet restore...",
            "Step 4 of 4: Generating code for the demo screen...",
        });
    }
}
