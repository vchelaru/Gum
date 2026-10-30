using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ToolsUtilities;
using System.Collections.Generic;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.ProjectServices;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// New projects used to be created empty, leaving a first-time user with no screen and no Forms
/// controls. These pin the starter content and the opt-outs at each step.
/// </summary>
public class NewProjectLogicTests
{
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IFormsFileService> _formsFileService = new();
    private readonly Mock<IProjectState> _projectState = new();
    private readonly Mock<IFormsThemeImporter> _themeImporter = new();
    private readonly Mock<ICopyPasteProjectCommands> _projectCommands = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IDefaultFontBundler> _defaultFontBundler = new();
    private readonly Mock<IDeleteLogic> _deleteLogic = new();
    private readonly NewProjectLogic _logic;

    public NewProjectLogicTests()
    {
        _formsFileService.Setup(x => x.DefaultThemeName).Returns("Standard");
        _formsFileService.Setup(x => x.GetAvailableThemes()).Returns(new List<string> { "Standard", "Bubblegum" });
        _formsFileService.Setup(x => x.GetThemeDirectory(It.IsAny<string>())).Returns("C:/nonexistent-theme/");
        _projectState.Setup(x => x.GumProjectSave).Returns(new GumProjectSave());
        _fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(true);
        _projectManager.Setup(x => x.GumProjectSave).Returns(new GumProjectSave { FullFileName = "C:/nonexistent-project/Project.gumx" });
        _projectManager.Setup(x => x.CreateBlankProject()).Returns(new GumProjectSave());

        _logic = new NewProjectLogic(
            _projectManager.Object,
            _dialogService.Object,
            _fileCommands.Object,
            _themeImporter.Object,
            _projectCommands.Object,
            _selectedState.Object,
            _defaultFontBundler.Object,
            _deleteLogic.Object);
    }

    private Action<NewProjectDialogViewModel>? _dialogInitializer;

    private ThemeSelectionViewModel CreateThemeSelection() => new(_formsFileService.Object, _projectState.Object);

    private bool CaptureInitializer(Action<NewProjectDialogViewModel>? initializer)
    {
        _dialogInitializer = initializer;
        return true;
    }

    /// <summary>
    /// Sets up the options dialog to return <paramref name="accepted"/>, with Forms inclusion set
    /// to <paramref name="isIncludeFormsControls"/> and, when given, a specific theme selected.
    /// </summary>
    private void SetUpDialog(
        bool accepted,
        bool isIncludeFormsControls = true,
        string? selectedTheme = null,
        bool isIncludeDemoScreenGum = false)
    {
        ThemeSelectionViewModel themeSelection = CreateThemeSelection();
        if (selectedTheme != null)
        {
            themeSelection.SelectedTheme = selectedTheme;
        }

        NewProjectDialogViewModel viewModel = CreateViewModel(themeSelection);
        viewModel.IsIncludeFormsControls = isIncludeFormsControls;
        viewModel.IsIncludeDemoScreenGum = isIncludeDemoScreenGum;
        _dialogService
            .Setup(x => x.Show(It.Is<Action<NewProjectDialogViewModel>?>(i => CaptureInitializer(i)), out viewModel))
            .Returns(accepted);
    }

    private static readonly string ChosenFolder = Path.Combine(Path.GetTempPath(), "GumNewProjectLogicTests");

    // Where the dialog's name, location and format put the project.
    private static readonly string ChosenLocation = Path.Combine(ChosenFolder, "Chosen", "Chosen.gumj");

    private NewProjectDialogViewModel CreateViewModel(ThemeSelectionViewModel themeSelection)
    {
        Mock<INewProjectService> service = new();
        Mock<IExistingCodeProjectDetector> detector = new();
        return new NewProjectDialogViewModel(themeSelection, service.Object, detector.Object, _dialogService.Object)
        {
            ProjectName = "Chosen",
            Location = ChosenFolder,
        };
    }

    [Fact]
    public async Task CreateNewProjectAsync_KeepsTheOpenProject_WhenTheOptionsDialogIsCancelled()
    {
        SetUpDialog(accepted: false);

        await _logic.CreateNewProjectAsync();

        // Cancelling New Project must not unload the project that was open (#5968).
        _projectManager.Verify(x => x.CreateNewProject(), Times.Never);
        _selectedState.VerifySet(x => x.SelectedElement = null, Times.Never);
    }

    [Fact]
    public async Task CreateNewProjectAsync_CreatesABlankProject_WhenCancelledAndNoProjectIsLoaded()
    {
        // At startup nothing is loaded yet, and the tool assumes a non-null GumProjectSave.
        _projectManager.Setup(x => x.GumProjectSave).Returns((GumProjectSave?)null);
        SetUpDialog(accepted: false);

        await _logic.CreateNewProjectAsync();

        _projectManager.Verify(x => x.CreateNewProject(), Times.Once);
    }

    [Fact]
    public async Task CreateNewProjectAsync_ShowsTheDialogAgainstABlankProject_NotTheOpenOne()
    {
        GumProjectSave blankProject = new GumProjectSave();
        _projectManager.Setup(x => x.CreateBlankProject()).Returns(blankProject);
        SetUpDialog(accepted: false);

        await _logic.CreateNewProjectAsync();

        NewProjectDialogViewModel viewModel = CreateViewModel(CreateThemeSelection());
        _dialogInitializer.ShouldNotBeNull();
        _dialogInitializer!(viewModel);
        viewModel.ThemeSelection.TargetProject.ShouldBeSameAs(blankProject);
    }

    [Fact]
    public async Task CreateNewProjectAsync_ReplacesTheProjectOnlyAfterTheDialogIsAccepted()
    {
        List<string> calls = new();
        ThemeSelectionViewModel themeSelection = CreateThemeSelection();
        NewProjectDialogViewModel viewModel = CreateViewModel(themeSelection);
        _dialogService
            .Setup(x => x.Show(It.IsAny<Action<NewProjectDialogViewModel>?>(), out viewModel))
            .Returns(true)
            .Callback(() => calls.Add("dialog"));
        _projectManager.Setup(x => x.CreateNewProject()).Callback(() => calls.Add("create"));
        _projectManager.Setup(x => x.SetProjectLocation(ChosenLocation)).Callback(() => calls.Add("setLocation"));

        await _logic.CreateNewProjectAsync();

        calls.ShouldBe(new[] { "dialog", "create", "setLocation" });
    }

    [Fact]
    public async Task CloseProjectAsync_ClosesTheOpenProjectBeforeShowingTheDialog()
    {
        List<string> calls = new();
        _projectManager.Setup(x => x.CreateNewProject()).Callback(() => calls.Add("create"));
        NewProjectDialogViewModel viewModel = CreateViewModel(CreateThemeSelection());
        _dialogService
            .Setup(x => x.Show(It.IsAny<Action<NewProjectDialogViewModel>?>(), out viewModel))
            .Returns(false)
            .Callback(() => calls.Add("dialog"));

        await _logic.CloseProjectAsync();

        calls.ShouldBe(new[] { "create", "dialog" });
    }

    [Fact]
    public async Task CloseProjectAsync_ClearsTheSelection_WhenTheDialogIsCancelled()
    {
        SetUpDialog(accepted: false);

        await _logic.CloseProjectAsync();

        _selectedState.VerifySet(x => x.SelectedElement = null, Times.Once);
        _selectedState.VerifySet(x => x.SelectedInstance = null, Times.Once);
        _selectedState.VerifySet(x => x.SelectedBehavior = null, Times.Once);
        _selectedState.VerifySet(x => x.SelectedStateCategorySave = null, Times.Once);
        _selectedState.VerifySet(x => x.SelectedStateSave = null, Times.Once);
    }

    [Fact]
    public async Task CloseProjectAsync_CreatesTheStarterProject_WhenTheDialogIsAccepted()
    {
        SetUpDialog(accepted: true);

        await _logic.CloseProjectAsync();

        _projectManager.Verify(x => x.SetProjectLocation(ChosenLocation), Times.Once);
        _projectCommands.Verify(x => x.AddScreen(It.IsAny<ScreenSave>()), Times.Once);
    }

    [Fact]
    public async Task CreateNewProjectAsync_ClearsTheSelection_OnlyOnceTheNewProjectReplacesTheOpenOne()
    {
        SetUpDialog(accepted: true);

        await _logic.CreateNewProjectAsync();

        _selectedState.VerifySet(x => x.SelectedElement = null, Times.Once);
        _selectedState.VerifySet(x => x.SelectedInstance = null, Times.Once);
        _selectedState.VerifySet(x => x.SelectedBehavior = null, Times.Once);
        _selectedState.VerifySet(x => x.SelectedStateCategorySave = null, Times.Once);
        _selectedState.VerifySet(x => x.SelectedStateSave = null, Times.Once);
    }

    [Fact]
    public async Task CreateNewProjectAsync_ImportsNothing_WhenTheOptionsDialogIsCancelled()
    {
        SetUpDialog(accepted: false);

        await _logic.CreateNewProjectAsync();

        _projectManager.Verify(x => x.AskUserForNewProjectLocation(), Times.Never);
        _themeImporter.Verify(x => x.ImportThemeAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        _projectCommands.Verify(x => x.AddScreen(It.IsAny<ScreenSave>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewProjectAsync_ForcesContainedElementsOnTheFirstSave()
    {
        // SetProjectLocation, called just above this save, already set FullFileName --
        // so SaveProject's own internal isProjectNew detection would report false on this call even
        // though it is genuinely the project's first save, silently skipping every Standard's .gutx
        // file. forceSaveContainedElements: true bypasses that and must not regress to the default.
        SetUpDialog(accepted: true);

        await _logic.CreateNewProjectAsync();

        _fileCommands.Verify(x => x.TryAutoSaveProject(true), Times.Once);
    }

    [Fact]
    public async Task CreateNewProjectAsync_ImportsTheDefaultThemeAndAddsAStartingScreen()
    {
        SetUpDialog(accepted: true, isIncludeFormsControls: true);

        await _logic.CreateNewProjectAsync();

        _themeImporter.Verify(x => x.ImportThemeAsync("Standard", false), Times.Once);
        _projectCommands.Verify(
            x => x.AddScreen(It.Is<ScreenSave>(s => s.Name == NewProjectLogic.StartingScreenName)),
            Times.Once);
    }

    [Fact]
    public async Task CreateNewProjectAsync_ImportsTheThemePickedInTheDialog_WhenItDiffersFromTheDefault()
    {
        SetUpDialog(accepted: true, isIncludeFormsControls: true, selectedTheme: "Bubblegum");

        await _logic.CreateNewProjectAsync();

        _themeImporter.Verify(x => x.ImportThemeAsync("Bubblegum", false), Times.Once);
    }

    [Fact]
    public async Task CreateNewProjectAsync_ImportsTheDemoScreenGum_WhenCheckedInTheDialog()
    {
        SetUpDialog(accepted: true, isIncludeFormsControls: true, isIncludeDemoScreenGum: true);

        await _logic.CreateNewProjectAsync();

        _themeImporter.Verify(x => x.ImportThemeAsync("Standard", true), Times.Once);
    }

    [Fact]
    public async Task CreateNewProjectAsync_StillAddsAStartingScreen_WhenFormsAreDeclined()
    {
        SetUpDialog(accepted: true, isIncludeFormsControls: false);

        await _logic.CreateNewProjectAsync();

        _themeImporter.Verify(x => x.ImportThemeAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        _projectCommands.Verify(x => x.AddScreen(It.IsAny<ScreenSave>()), Times.Once);
    }

    [Fact]
    public async Task CreateNewProjectAsync_SelectsTheStartingScreen()
    {
        SetUpDialog(accepted: true);

        await _logic.CreateNewProjectAsync();

        _selectedState.VerifySet(
            x => x.SelectedScreen = It.Is<ScreenSave>(s => s.Name == NewProjectLogic.StartingScreenName),
            Times.Once);
    }

    [Fact]
    public async Task CreateNewProjectAsync_ImportsNothing_WhenTheInitialSaveFails()
    {
        SetUpDialog(accepted: true);
        _fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(false);

        await _logic.CreateNewProjectAsync();

        _themeImporter.Verify(x => x.ImportThemeAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        _projectCommands.Verify(x => x.AddScreen(It.IsAny<ScreenSave>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewProjectAsync_BundlesTheDefaultFont_AfterTheFirstSave()
    {
        // StandardElementsManager's Text standard points its Font default at this bundled file
        // instead of a system font name (#4674), so every new project needs it on disk -- even
        // when Forms controls (which bring their own theme fonts) are declined.
        SetUpDialog(accepted: true, isIncludeFormsControls: false);

        await _logic.CreateNewProjectAsync();

        _defaultFontBundler.Verify(x => x.CopyTo(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateNewProjectAsync_DoesNotBundleTheDefaultFont_WhenTheInitialSaveFails()
    {
        SetUpDialog(accepted: true);
        _fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(false);

        await _logic.CreateNewProjectAsync();

        _defaultFontBundler.Verify(x => x.CopyTo(It.IsAny<string>()), Times.Never);
    }

    #region Code project

    private static string Slashes(string path) => path.Replace((char)92, (char)47);

    private static readonly string CreatedGumProject = Path.Combine(ChosenFolder, "Game", "Content", "GumProject", "GumProject.gumj");

    private async Task<NewProjectDialogViewModel> SetUpCreatedCodeProject(GumProjectSave loaded, bool includeForms = true, bool includeDemo = true)
    {
        Mock<INewProjectService> service = new();
        service.Setup(x => x.IsSourceLinkedAvailable).Returns(false);
        service.Setup(x => x.Validate(It.IsAny<NewProjectRequest>())).Returns(new NewProjectValidation { Resolved = new ResolvedNewProject() });
        service.Setup(x => x.CreateFilesAsync(It.IsAny<ResolvedNewProject>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NewProjectFilesResult { Success = true, GumProjectPath = CreatedGumProject, CsprojPath = "g.csproj" });
        NewProjectDialogViewModel viewModel = new(CreateThemeSelection(), service.Object, new Mock<IExistingCodeProjectDetector>().Object, _dialogService.Object)
        {
            ProjectName = "Game",
            Location = ChosenFolder,
            IncludeCodeProject = true,
            RunRestore = false,
            IsIncludeFormsControls = includeForms,
            IsIncludeDemoScreenGum = includeDemo,
        };
        await viewModel.RunCreationAsync();
        viewModel.IsCodeProjectCreated.ShouldBeTrue();

        _dialogService.Setup(x => x.Show(It.IsAny<Action<NewProjectDialogViewModel>?>(), out viewModel)).Returns(true);
        _projectManager.Setup(x => x.GumProjectSave).Returns(loaded);
        return viewModel;
    }

    [Fact]
    public async Task CreateNewProjectAsync_WithACodeProject_LoadsTheCreatedGumProject_WithoutCreatingABlankOne()
    {
        await SetUpCreatedCodeProject(new GumProjectSave());

        await _logic.CreateNewProjectAsync();

        _projectManager.Verify(x => x.LoadProjectAsync(It.Is<FilePath>(path => Slashes(path.FullPath) == Slashes(CreatedGumProject))), Times.Once);
        _projectManager.Verify(x => x.CreateNewProject(), Times.Never);
        _projectManager.Verify(x => x.SetProjectLocation(It.IsAny<string>()), Times.Never);
        _themeImporter.Verify(x => x.ImportThemeAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewProjectAsync_WithACodeProject_AddsAndSelectsTheStartingScreen()
    {
        await SetUpCreatedCodeProject(new GumProjectSave());

        await _logic.CreateNewProjectAsync();

        _projectCommands.Verify(x => x.AddScreen(It.Is<ScreenSave>(screen => screen.Name == NewProjectLogic.StartingScreenName)), Times.Once);
        _selectedState.VerifySet(x => x.SelectedScreen = It.Is<ScreenSave>(screen => screen.Name == NewProjectLogic.StartingScreenName), Times.Once);
    }

    [Fact]
    public async Task CreateNewProjectAsync_WithACodeProject_KeepsAnExistingStartingScreen()
    {
        GumProjectSave loaded = new GumProjectSave();
        loaded.Screens.Add(new ScreenSave { Name = NewProjectLogic.StartingScreenName });
        await SetUpCreatedCodeProject(loaded);

        await _logic.CreateNewProjectAsync();

        _projectCommands.Verify(x => x.AddScreen(It.IsAny<ScreenSave>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewProjectAsync_WithACodeProject_RemovesTheDemoScreen_WhenItIsUnchecked()
    {
        GumProjectSave loaded = new GumProjectSave();
        ScreenSave demo = new ScreenSave { Name = "DemoScreenGum" };
        loaded.Screens.Add(demo);
        await SetUpCreatedCodeProject(loaded, includeDemo: false);

        await _logic.CreateNewProjectAsync();

        _deleteLogic.Verify(x => x.RemoveElement(demo), Times.Once);
    }

    [Fact]
    public async Task CreateNewProjectAsync_WithACodeProject_RemovesAThemedDemoScreen_WhenItIsUnchecked()
    {
        GumProjectSave loaded = new GumProjectSave();
        ScreenSave themedDemo = new ScreenSave { Name = "Bubblegum/DemoScreenGum" };
        ScreenSave other = new ScreenSave { Name = "Bubblegum/StylesScreen" };
        loaded.Screens.Add(themedDemo);
        loaded.Screens.Add(other);
        await SetUpCreatedCodeProject(loaded, includeDemo: false);

        await _logic.CreateNewProjectAsync();

        _deleteLogic.Verify(x => x.RemoveElement(themedDemo), Times.Once);
        _deleteLogic.Verify(x => x.RemoveElement(other), Times.Never);
    }

    [Fact]
    public async Task CreateNewProjectAsync_WithACodeProject_KeepsTheDemoScreen_WhenItIsChecked_OrFormsAreOff()
    {
        GumProjectSave loaded = new GumProjectSave();
        loaded.Screens.Add(new ScreenSave { Name = "DemoScreenGum" });
        await SetUpCreatedCodeProject(loaded, includeDemo: true);
        await _logic.CreateNewProjectAsync();

        await SetUpCreatedCodeProject(loaded, includeForms: false, includeDemo: false);
        await _logic.CreateNewProjectAsync();

        _deleteLogic.Verify(x => x.RemoveElement(It.IsAny<ElementSave>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewProjectAsync_WithACodeProject_LeavesTheOpenProjectLoaded_WhenTheDialogIsCancelled()
    {
        SetUpDialog(accepted: false);

        await _logic.CreateNewProjectAsync();

        _projectManager.Verify(x => x.LoadProjectAsync(It.IsAny<FilePath>()), Times.Never);
        _projectManager.Verify(x => x.CreateNewProject(), Times.Never);
    }

    #endregion
}
