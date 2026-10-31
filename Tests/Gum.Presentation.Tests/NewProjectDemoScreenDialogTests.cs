using System;
using System.Collections.Generic;
using System.IO;
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
/// The "Generate code" checkbox of the New Project dialog (#7268): when it shows, what the command line
/// chains, what the request asks for, the order of the steps, and how a failed code generation behaves.
/// </summary>
public class NewProjectDemoScreenDialogTests
{
    private static readonly string Folder = Path.Combine(Path.GetTempPath(), "GumNewProjectDemoDialogTests");
    private static readonly string GameFolder = Path.Combine(Folder, "MyGame");
    private static readonly string Csproj = Path.Combine(GameFolder, "MyGame.csproj");
    private static readonly string GumProject = Path.Combine(GameFolder, "Content", "GumProject", "GumProject.gumj");

    private readonly Mock<INewProjectService> _service = new();
    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly List<string> _calls = new List<string>();
    private readonly List<NewProjectRequest> _requests = new List<NewProjectRequest>();
    // What creation was asked to write: validation also runs for the dialog's own checks, so the request list is not only the creation's.
    private readonly List<ResolvedNewProject> _created = new List<ResolvedNewProject>();
    private readonly Queue<GenerateCodeResult> _codegenResults = new Queue<GenerateCodeResult>();
    private bool _hasDemo = true;

    public NewProjectDemoScreenDialogTests()
    {
        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard" });
        _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");
        _service.Setup(x => x.IsSourceLinkedAvailable).Returns(true);
        _service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns((NewProjectRequest request) =>
        {
            _requests.Add(request);
            return new NewProjectValidation
            {
                Resolved = new ResolvedNewProject
                {
                    Name = "MyGame", CsprojPath = Csproj, GenerateCode = request.GenerateCode, IncludeFormsTemplate = request.Template == "forms",
                    RecommendedPackages = request.IncludeRecommendedPackages ? new[] { "Gum.Expressions" } : new string[0]
                }
            };
        });
        _service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .Returns((ResolvedNewProject resolved, CancellationToken _) =>
            {
                _calls.Add("files");
                _created.Add(resolved);
                return Task.FromResult(new NewProjectFilesResult
                {
                    Success = true, GumProjectPath = GumProject, CsprojPath = Csproj,
                    DemoScreen = _hasDemo ? new DemoScreenInfo { Name = "DemoScreenGum", ClassName = "DemoScreenGum", Namespace = "MyGame.Screens" } : null
                });
            });
        _service.Setup(x => x.AddPackagesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _calls.Add("add");
                return Task.FromResult(new AddPackagesResult { Status = RestoreStatus.Succeeded });
            });
        _service.Setup(x => x.RestoreAsync(It.IsAny<string>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _calls.Add("restore");
                return Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
            });
        _service.Setup(x => x.GenerateCodeAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<NewProjectFilesResult>(), It.IsAny<Action<string>?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _calls.Add("codegen");
                return Task.FromResult(_codegenResults.Count > 0 ? _codegenResults.Dequeue() : new GenerateCodeResult { Success = true });
            });
    }

    private NewProjectDialogViewModel Create(Action<NewProjectDialogViewModel>? configure = null)
    {
        NewProjectDialogViewModel viewModel = new NewProjectDialogViewModel(
            new ThemeSelectionViewModel(_formsFileService.Object, new Mock<IProjectState>().Object),
            _service.Object, new Mock<IExistingCodeProjectDetector>().Object, new Mock<IDialogService>().Object,
            new Mock<INewProjectLocationSettings>().Object)
        {
            ProjectName = "MyGame",
            Location = Folder,
            IncludeCodeProject = true,
            IsIncludeDemoScreenGum = true,
            IncludeRecommendedPackages = false,
        };
        configure?.Invoke(viewModel);
        return viewModel;
    }

    private static string Codegen => $"gumcli codegen \"{GumProject}\"";

    #region Visibility and default

    [Fact]
    public void GenerateCode_DefaultsToChecked()
    {
        Create().GenerateCodeChecked.ShouldBeTrue();
    }

    // Visible with any C# project; enabled when there is something to generate (Forms template) and the restore runs.
    [Theory]
    [InlineData(true, true, true, true)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public void TheCheckBox_IsVisibleWithACodeProject_AndEnabledWhenThereIsSomethingToGenerateAndTheRestoreRuns(
        bool codeProject, bool forms, bool restore, bool expectedEnabled)
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeCodeProject = codeProject;
            vm.IsIncludeFormsControls = forms;
            vm.RunRestore = restore;
        });

        viewModel.IsGenerateCodeVisible.ShouldBe(codeProject);
        viewModel.IsGenerateCodeEnabled.ShouldBe(expectedEnabled && codeProject);
        viewModel.GeneratesCode.ShouldBe(expectedEnabled && codeProject);
        viewModel.GenerateCodeChecked.ShouldBe(expectedEnabled && codeProject);
    }

    [Fact]
    public void WithNothingToGenerate_TheBoxIsDisabledUnticked_WithAShortNote_AndNoCodegenStep()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IsIncludeFormsControls = false);

        viewModel.IsGenerateCodeVisible.ShouldBeTrue();
        viewModel.IsGenerateCodeEnabled.ShouldBeFalse();
        viewModel.GenerateCodeChecked.ShouldBeFalse();
        viewModel.GenerateCodeNote.ShouldBe("Nothing to generate yet: the project has no screens or components. Include Forms controls to have some.");
        viewModel.CommandLine.ShouldNotContain("codegen");
    }

    [Fact]
    public void TheCheckBox_DoesNotDependOnTheDemoScreen()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IsIncludeDemoScreenGum = false);

        viewModel.IsGenerateCodeVisible.ShouldBeTrue();
        viewModel.GeneratesCode.ShouldBeTrue();
        viewModel.CommandLine.ShouldEndWith($" && {Codegen}");
    }

    // The tick is the user's choice: Forms, Restore, the platform and the code project switch the box off and on, never the tick.
    [Fact]
    public void ATickSurvivesTheBoxBeingDisabledAndReEnabled()
    {
        NewProjectDialogViewModel viewModel = Create();
        viewModel.GenerateCodeChecked.ShouldBeTrue();

        viewModel.IsIncludeFormsControls = false;
        viewModel.GenerateCodeChecked.ShouldBeFalse();
        viewModel.IsIncludeFormsControls = true;
        viewModel.GenerateCodeChecked.ShouldBeTrue();

        viewModel.RunRestore = false;
        viewModel.GenerateCodeChecked.ShouldBeFalse();
        viewModel.RunRestore = true;
        viewModel.GenerateCodeChecked.ShouldBeTrue();

        viewModel.PlatformName = "kniskia";
        viewModel.PlatformName = "monogame";
        viewModel.GenerateCodeChecked.ShouldBeTrue();

        viewModel.IncludeCodeProject = false;
        viewModel.IncludeCodeProject = true;
        viewModel.GenerateCodeChecked.ShouldBeTrue();
    }

    [Fact]
    public void AnUntickSurvivesTheBoxBeingDisabledAndReEnabled()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.GenerateCodeChecked = false);

        viewModel.IsIncludeFormsControls = false;
        viewModel.IsIncludeFormsControls = true;
        viewModel.RunRestore = false;
        viewModel.RunRestore = true;

        viewModel.GenerateCodeChecked.ShouldBeFalse();
        viewModel.CommandLine.ShouldNotContain("codegen");
    }

    [Fact]
    public void IsGenerateCodeEnabled_RaisesWhenFormsAreToggled()
    {
        NewProjectDialogViewModel viewModel = Create();
        List<string?> raised = new List<string?>();
        viewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        viewModel.IsIncludeFormsControls = false;

        raised.ShouldContain(nameof(NewProjectDialogViewModel.IsGenerateCodeEnabled));
    }

    [Fact]
    public void GeneratesCode_IsOff_WhenUnchecked()
    {
        Create(vm => vm.GenerateCodeChecked = false).GeneratesCode.ShouldBeFalse();
    }

    #endregion

    #region Command line

    [Fact]
    public void CommandLine_WithPackagesAndRestore_ChainsCodegenLast()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeRecommendedPackages = true);

        viewModel.CommandLine.ShouldEndWith($" && dotnet restore \"{Csproj}\" && {Codegen}");
        viewModel.CommandLine.ShouldContain("--no-restore && dotnet add");
    }

    // Code generation reads the restored runtime package to choose its syntax, so without the restore it is skipped and the
    // game loads the screen at runtime. The checkbox is disabled and shows unticked.
    [Fact]
    public void WithoutRestore_NoCodegenStepIsShown_TheBoxIsDisabled_AndTheNoteSaysWhy()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeRecommendedPackages = true;
            vm.RunRestore = false;
        });

        viewModel.CommandLine.ShouldNotContain("codegen");
        viewModel.IsGenerateCodeEnabled.ShouldBeFalse();
        viewModel.GenerateCodeChecked.ShouldBeFalse();
        viewModel.CommandLineNote.ShouldContain("needs the restore");
    }

    [Fact]
    public async Task RemoveDemoScreenFromHost_RewritesTheHostWithoutTheScreen_KeepingThePackages()
    {
        NewProjectDialogViewModel viewModel = Create(vm =>
        {
            vm.IncludeRecommendedPackages = true;
            vm.IsIncludeDemoScreenGum = false;
        });
        await viewModel.RunCreationAsync();

        viewModel.RemoveDemoScreenFromHost();

        _service.Verify(x => x.UpdateHost(
            It.IsAny<ResolvedNewProject>(), It.Is<NewProjectFilesResult>(files => files.DemoScreen == null), true, false), Times.Once);
    }

    [Fact]
    public void CommandLine_WithoutPackages_IsTheNewCommandThenCodegen()
    {
        NewProjectDialogViewModel viewModel = Create();

        viewModel.CommandLine.ShouldEndWith($" && {Codegen}");
        viewModel.CommandLine.ShouldNotContain("dotnet add");
        viewModel.CommandLine.ShouldNotContain("--no-restore");
    }

    [Fact]
    public void CommandLine_Unchecked_HasNoCodegenStep()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.GenerateCodeChecked = false);

        viewModel.CommandLine.ShouldNotContain("codegen");
    }

    #endregion

    #region Creation

    [Fact]
    public async Task Run_AsksForCodeGenerationAndTheDemo()
    {
        NewProjectDialogViewModel viewModel = Create();

        await viewModel.RunCreationAsync();

        _created[^1].GenerateCode.ShouldBeTrue();
    }

    [Fact]
    public async Task Run_WithTheDemoUnchecked_StillGeneratesTheProjectsCode()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IsIncludeDemoScreenGum = false);
        _hasDemo = false;

        await viewModel.RunCreationAsync();

        _created[^1].GenerateCode.ShouldBeTrue();
        _calls.ShouldContain("codegen");
    }

    [Fact]
    public async Task Run_RunsFilesAddRestoreThenCodegen_AndFinishes()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeRecommendedPackages = true);

        await viewModel.RunCreationAsync();

        _calls.ShouldBe(new[] { "files", "add", "restore", "codegen" });
        viewModel.IsCodeProjectCreated.ShouldBeTrue();
    }

    [Fact]
    public async Task Run_WithoutRestore_RunsNoCodegen()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.RunRestore = false);

        await viewModel.RunCreationAsync();

        _calls.ShouldBe(new[] { "files" });
        viewModel.IsCodeProjectCreated.ShouldBeTrue();
    }

    [Fact]
    public async Task Run_WithNothingToGenerate_RunsNoCodegen()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IsIncludeFormsControls = false);

        await viewModel.RunCreationAsync();

        _calls.ShouldBe(new[] { "files", "restore" });
    }

    [Fact]
    public async Task Run_WhenCodegenFails_ShowsTheFailurePanelWithTheOutput()
    {
        _codegenResults.Enqueue(new GenerateCodeResult { OutputTail = "error: DemoScreenGum: bad" });
        NewProjectDialogViewModel viewModel = Create();

        await viewModel.RunCreationAsync();

        viewModel.IsRestoreFailed.ShouldBeTrue();
        viewModel.RestoreFailureHeading.ShouldBe("The code project was created, but generating code failed.");
        viewModel.RestoreFailureText.ShouldContain("DemoScreenGum: bad");
        viewModel.IsCodeProjectCreated.ShouldBeFalse();
    }

    [Fact]
    public async Task Retry_AfterAFailedCodegen_RunsRestoreAndCodegenAgain()
    {
        _codegenResults.Enqueue(new GenerateCodeResult { OutputTail = "boom" });
        NewProjectDialogViewModel viewModel = Create();
        await viewModel.RunCreationAsync();
        _calls.Clear();

        viewModel.RetryRestoreCommand.Execute(null);
        await Task.Delay(200);

        _calls.ShouldBe(new[] { "restore", "codegen" });
        viewModel.IsCodeProjectCreated.ShouldBeTrue();
    }

    [Fact]
    public async Task Continue_AfterAFailedCodegen_KeepsTheFilesAndFinishes()
    {
        _codegenResults.Enqueue(new GenerateCodeResult { OutputTail = "boom" });
        NewProjectDialogViewModel viewModel = Create();
        await viewModel.RunCreationAsync();

        viewModel.ContinueWithoutRestoreCommand.Execute(null);

        viewModel.IsCodeProjectCreated.ShouldBeTrue();
        viewModel.CreatedGumProjectPath.ShouldBe(GumProject);
    }

    [Fact]
    public async Task Run_AfterTheAddsSucceed_RewritesTheHostForThePackages()
    {
        NewProjectDialogViewModel viewModel = Create(vm => vm.IncludeRecommendedPackages = true);

        await viewModel.RunCreationAsync();

        _service.Verify(x => x.UpdateHost(It.IsAny<ResolvedNewProject>(), It.IsAny<NewProjectFilesResult>(), true, false), Times.Once);
    }

    #endregion
}
