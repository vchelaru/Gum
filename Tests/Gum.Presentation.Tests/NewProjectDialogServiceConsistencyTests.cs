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
public class NewProjectDialogServiceConsistencyTests : IDisposable
{
    private readonly string _folder;
    private readonly string _checkout;
    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly RecordingService _service;

    public NewProjectDialogServiceConsistencyTests()
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

    [Fact]
    public async Task EveryCombinationOfTheOptions_IsARequestTheRealServiceAccepts_AndTheRequestMatchesWhatTheDialogShows()
    {
        int count = 0;
        foreach (bool code in new[] { false, true })
        foreach (bool forms in new[] { true, false })
        foreach (string platform in NewProjectPlatforms.MainNames.Select(entry => entry.Name))
        foreach (NewProjectPackageChannel channel in Enum.GetValues<NewProjectPackageChannel>())
        foreach (bool restore in new[] { true, false })
        foreach (bool packages in new[] { true, false })
        foreach (bool generate in new[] { true, false })
        foreach (bool demo in new[] { true, false })
        foreach (bool xml in new[] { false, true })
        {
            if (!code && (platform != "monogame" || channel != NewProjectPackageChannel.Stable || packages || generate || !demo))
            {
                continue; // Nothing of these shows without a code project.
            }

            NewProjectDialogViewModel viewModel = Create(vm =>
            {
                vm.IncludeCodeProject = code;
                vm.IsIncludeFormsControls = forms;
                vm.PlatformName = platform;
                vm.PackageChannelChoice = channel;
                vm.RunRestore = restore;
                vm.IncludeRecommendedPackages = packages;
                vm.GenerateCodeChoice = generate;
                vm.IsIncludeDemoScreenGum = demo;
                vm.SelectedFileFormat = xml ? NewProjectDialogViewModel.XmlFormat : NewProjectDialogViewModel.JsonFormat;
            });
            string label = $"code={code} forms={forms} platform={platform} channel={channel} restore={restore} packages={packages} generate={generate} demo={demo} xml={xml}";

            _service.LastResolved = null;
            await viewModel.RunCreationAsync();

            viewModel.ErrorMessage.ShouldBe(string.Empty, label);
            ResolvedNewProject resolved = _service.LastResolved.ShouldNotBeNull(label);
            (resolved.Platform != null).ShouldBe(code, label);
            (resolved.GumSourceDirectory != null).ShouldBe(code && viewModel.SourceLinked, label);
            resolved.PackageChannel.ShouldBe(code ? viewModel.PackageChannel : NewProjectPackageChannel.Stable, label);
            resolved.UsesPrerelease.ShouldBe(code && viewModel.IsPreview, label);
            if (code && channel == NewProjectPackageChannel.Preview && platform != "kniskia")
            {
                resolved.PackageChannel.ShouldBe(NewProjectPackageChannel.Preview, label);
                viewModel.CommandLine.ShouldContain("--preview", Case.Sensitive, label);
            }
            // Source-linked links the add-ons as projects instead of adding packages.
            (resolved.RecommendedPackages.Count > 0).ShouldBe(viewModel.AddsRecommendedPackages && !viewModel.SourceLinked, label);
            (resolved.LinkedAddOns.Count > 0).ShouldBe(viewModel.AddsRecommendedPackages && viewModel.SourceLinked, label);
            resolved.GenerateCode.ShouldBe(code && viewModel.GeneratesCode, label);
            resolved.IncludeFormsTemplate.ShouldBe(forms, label);
            if (viewModel.AddsRecommendedPackages)
            {
                viewModel.RecommendedPackagesChecked.ShouldBeTrue(label);
            }
            if (code && packages && !viewModel.AddsRecommendedPackages)
            {
                viewModel.RecommendedPackagesChecked.ShouldBeFalse(label);
                viewModel.IsRecommendedPackagesEnabled.ShouldBeFalse(label);
            }
            count++;
        }

        count.ShouldBeGreaterThan(200);
    }

    [Fact]
    public async Task SourceLinked_WithPackagesTicked_KeepsTheBoxTickedAndEnabled_AndLinksTheAddOns()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "monogame";
            vm.IncludeRecommendedPackages = true;
            vm.SourceLinkedChoice = true;
        });

        viewModel.SourceLinked.ShouldBeTrue();
        viewModel.IsRecommendedPackagesEnabled.ShouldBeTrue();
        viewModel.RecommendedPackagesChecked.ShouldBeTrue();
        viewModel.CommandLine.ShouldContain("--source-linked");
        viewModel.CommandLine.ShouldNotContain(" package ");

        await viewModel.RunCreationAsync();

        ResolvedNewProject resolved = _service.LastResolved.ShouldNotBeNull();
        resolved.LinkedAddOns.Count.ShouldBeGreaterThan(0);
        resolved.RecommendedPackages.Count.ShouldBe(0);
    }

    [Fact]
    public void Defaults_AreWhatTheServiceTreatsAsTheDefaultRequest()
    {
        NewProjectDialogViewModel viewModel = Create();

        viewModel.IncludeCodeProject.ShouldBeFalse();
        viewModel.IsIncludeFormsControls.ShouldBeTrue();
        viewModel.UseXmlFormat.ShouldBeFalse();
        viewModel.SourceLinkedChoice.ShouldBeFalse();
        viewModel.RunRestore.ShouldBeTrue();
        viewModel.CommandLine.ShouldBe($"gumcli new \"{Path.Combine(_folder, "Game", "Game.gumj")}\"");
    }

    [Theory]
    [InlineData("class")]
    [InlineData("_under")]
    [InlineData("Game1")]
    [InlineData("Gum")]
    [InlineData("KernSmith")]
    public async Task ACodeProjectNameTheDialogAccepts_IsCreatedByTheRealService(string name)
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.PlatformName = "monogame";
            vm.ProjectName = name;
        });
        viewModel.ValidationMessage.ShouldBe(string.Empty);

        await viewModel.RunCreationAsync();

        viewModel.ErrorMessage.ShouldBe(string.Empty);
    }

    [Theory]
    [InlineData("My Game")]
    [InlineData("a.b")]
    [InlineData("1abc")]
    public void ACodeProjectNameWithSpacesDotsOrALeadingDigit_IsRefusedByTheDialogUpFront(string name)
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = true;
            vm.ProjectName = name;
        });

        viewModel.ValidationMessage.ShouldBe(NewProjectDialogText.InvalidCSharpName);
        viewModel.CanExecuteAffirmative().ShouldBeFalse();
    }

    [Theory]
    [InlineData("My Game")]
    [InlineData("a.b")]
    [InlineData("1abc")]
    [InlineData("été")]
    public async Task AGumOnlyNameWithSpacesDotsDigitsOrAccents_IsCreatedByTheRealService(string name)
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.ProjectName = name);
        viewModel.ValidationMessage.ShouldBe(string.Empty);

        await viewModel.RunCreationAsync();

        viewModel.ErrorMessage.ShouldBe(string.Empty);
        _service.LastResolved.ShouldNotBeNull();
        _service.LastResolved!.GumProjectPath.ShouldEndWith(name + ".gumj");
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("com1")]
    [InlineData("Aux.txt")]
    [InlineData("a.")]
    [InlineData("a|b")]
    public void AFolderNameTheDialogRefuses_IsRefusedByTheServiceWithTheSameMessage(string name)
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.ProjectName = name);

        NewProjectValidation validation = _service.Validate(new NewProjectRequest { Path = Path.Combine(_folder, name) });

        viewModel.ValidationMessage.ShouldNotBeEmpty();
        validation.Resolved.ShouldBeNull();
        validation.Error!.Message.ShouldBe(viewModel.ValidationMessage);
    }

    [Fact]
    public async Task LocationWithSpaces_IsCarriedIntoTheServiceAndTheCommandLine()
    {
        string spaced = Path.Combine(_folder, "Vic Personal", "Documents", "Gum Projects");
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.Location = spaced;
            vm.IncludeCodeProject = true;
        });

        await viewModel.RunCreationAsync();

        viewModel.ErrorMessage.ShouldBe(string.Empty);
        _service.LastResolved!.RootDirectory.ShouldBe(Path.Combine(spaced, "Game"));
        viewModel.CommandLine.ShouldContain($"\"{Path.Combine(spaced, "Game")}\"");
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

        public Task<AddPackagesResult> AddPackagesAsync(
            string csprojPath, IReadOnlyList<string> packages, bool prerelease, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
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
