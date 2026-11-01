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
/// The New Project dialog against the real <see cref="NewProjectService"/> (#7328 splash): the dialog's state must be a
/// request the service accepts, and what the dialog shows as ticked must be what the service gets.
/// </summary>
public class NewProjectDialogServiceConsistencyRedTests : IDisposable
{
    private readonly string _folder;
    private readonly string _checkout;
    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly RecordingService _service;

    public NewProjectDialogServiceConsistencyRedTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "Gum Dialog Consistency " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _checkout = GumSourceLocator.Find(AppContext.BaseDirectory) ?? throw new InvalidOperationException("Run from a Gum checkout.");
        string themes = Path.Combine(_checkout, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes");
        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(
            new[] { "Standard" }.Concat(Directory.GetDirectories(themes).Select(Path.GetFileName).Select(name => name!)).ToList());
        _service = new RecordingService(new NewProjectService(
            new FormsThemeLocator(new[] { themes }), new OkRestore(), new OkAdd(), new[] { _checkout }, new OkCodegen()));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private NewProjectDialogViewModel Create(Action<NewProjectDialogViewModel>? configure = null)
    {
        NewProjectDialogViewModel viewModel = new NewProjectDialogViewModel(
            new ThemeSelectionViewModel(_formsFileService.Object, _projectState.Object),
            _service, new Mock<IExistingCodeProjectDetector>().Object, new Mock<IDialogService>().Object,
            new Mock<INewProjectLocationSettings>().Object)
        {
            ProjectName = "Game",
            Location = _folder,
        };
        configure?.Invoke(viewModel);
        return viewModel;
    }

    [Theory]
    [InlineData("Program")]
    [InlineData("MonoGameGum")]
    [InlineData("GumCommon")]
    public void OkIsOffUpFront_ForACodeProjectNameTheServiceWillRefuse(string name)
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "monogame";
            vm.ProjectName = name;
        });
        _service.Validate(new NewProjectRequest { Path = Path.Combine(_folder, name), Platform = "monogame", Restore = false })
            .Resolved.ShouldBeNull();

        viewModel.CanExecuteAffirmative().ShouldBeFalse("The dialog lets OK through and the refusal only shows after pressing it.");
    }

    private class RecordingService : INewProjectService
    {
        private readonly INewProjectService _inner;

        public RecordingService(INewProjectService inner) => _inner = inner;

        public ResolvedNewProject? LastResolved { get; set; }

        public bool IsSourceLinkedAvailable => _inner.IsSourceLinkedAvailable;

        public string? FindGumSourceDirectory() => _inner.FindGumSourceDirectory();

        public NewProjectValidation Validate(NewProjectRequest request) => _inner.Validate(request);

        public Task<NewProjectFilesResult> CreateFilesAsync(ResolvedNewProject project, CancellationToken cancellationToken)
        {
            LastResolved = project;
            return Task.FromResult(new NewProjectFilesResult
            {
                Success = true,
                GumProjectPath = project.GumProjectPath,
                CsprojPath = project.CsprojPath,
            });
        }

        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });

        public Task<AddPackagesResult> AddPackagesAsync(
            string csprojPath, IReadOnlyList<string> packages, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            Task.FromResult(new AddPackagesResult { Status = RestoreStatus.Succeeded });

        public Task<GenerateCodeResult> GenerateCodeAsync(
            ResolvedNewProject project, NewProjectFilesResult files, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            Task.FromResult(new GenerateCodeResult { Success = true });

        public void UpdateHost(ResolvedNewProject project, NewProjectFilesResult files, bool packagesAdded, bool screenCodeGenerated)
        {
        }

        public Task<NewProjectResult> ExecuteAsync(NewProjectRequest request, IProgress<NewProjectProgress>? progress, CancellationToken cancellationToken) =>
            _inner.ExecuteAsync(request, progress, cancellationToken);
    }

    private class OkRestore : IRestoreRunner
    {
        public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
    }

    private class OkAdd : IPackageAddRunner
    {
        public Task<RestoreResult> AddAsync(string csprojPath, string packageId, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
            Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
    }

    private class OkCodegen : Gum.ProjectServices.CodeGeneration.IHeadlessCodegenRunner
    {
        public int Run(string projectPath, IReadOnlyList<string>? elementNames, bool prune, TextWriter output, TextWriter error) => 0;
    }
}
