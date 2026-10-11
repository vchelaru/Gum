using Gum.Commands;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Logic;

/// <inheritdoc/>
public class NewProjectLogic : INewProjectLogic
{
    /// <summary>
    /// Name given to the screen every new project starts with. New projects are empty, so this
    /// cannot collide with an existing element.
    /// </summary>
    public const string StartingScreenName = "MainScreen";

    private readonly IProjectManager _projectManager;
    private readonly IDialogService _dialogService;
    private readonly IFileCommands _fileCommands;
    private readonly IFormsThemeImporter _themeImporter;
    // Named for its first consumer; AddScreen is the call needed here.
    private readonly ICopyPasteProjectCommands _projectCommands;
    private readonly ISelectedState _selectedState;
    private readonly IDefaultFontBundler _defaultFontBundler;

    public NewProjectLogic(
        IProjectManager projectManager,
        IDialogService dialogService,
        IFileCommands fileCommands,
        IFormsThemeImporter themeImporter,
        ICopyPasteProjectCommands projectCommands,
        ISelectedState selectedState,
        IDefaultFontBundler defaultFontBundler)
    {
        _projectManager = projectManager;
        _dialogService = dialogService;
        _fileCommands = fileCommands;
        _themeImporter = themeImporter;
        _projectCommands = projectCommands;
        _selectedState = selectedState;
        _defaultFontBundler = defaultFontBundler;
    }

    /// <inheritdoc/>
    public async Task CreateNewProjectAsync()
    {
        // The open project is replaced only once the user has accepted both prompts, so backing out
        // leaves it loaded (#5968). The theme picker describes what the new project would need, so
        // it is pointed at a blank one rather than the open project.
        GumProjectSave blankProject = _projectManager.CreateBlankProject();
        if (!_dialogService.Show<NewProjectDialogViewModel>(
                vm => vm.ThemeSelection.TargetProject = blankProject, out NewProjectDialogViewModel viewModel))
        {
            EnsureProjectLoaded();
            return;
        }

        // The theme import copies files next to the gumx, so the project needs a location on disk
        // before it can run.
        if (_projectManager.AskUserForNewProjectLocation() is not { } projectLocation)
        {
            EnsureProjectLoaded();
            return;
        }

        _selectedState.SelectedElement = null;
        _selectedState.SelectedInstance = null;
        _selectedState.SelectedBehavior = null;
        _selectedState.SelectedStateCategorySave = null;
        _selectedState.SelectedStateSave = null;

        _projectManager.CreateNewProject();
        _projectManager.SetProjectLocation(projectLocation);

        // SetProjectLocation just set FullFileName above, so SaveProject's own
        // internal call to it (to compute saveContainedElements) would see a non-empty name and
        // report isProjectNew: false -- even though this is genuinely the project's first save.
        // Force it, since this method already knows unambiguously that it is.
        if (!_fileCommands.TryAutoSaveProject(forceSaveContainedElements: true))
        {
            return;
        }

        // The Text standard's Font default (StandardElementsManager) points at this bundled
        // file rather than a system font name, so every new project needs it on disk regardless
        // of whether a Forms theme (which brings its own fonts) is also imported below (#4674).
        _defaultFontBundler.CopyTo(FileManager.GetDirectory(_projectManager.GetLoadedProject().GetSavedFileName()));

        if (viewModel.IsIncludeFormsControls)
        {
            await _themeImporter.ImportThemeAsync(
                viewModel.ThemeSelection.GetSelectedThemeOrDefault(), viewModel.IsIncludeDemoScreenGum);
        }

        ScreenSave startingScreen = new() { Name = StartingScreenName };
        _projectCommands.AddScreen(startingScreen);
        _selectedState.SelectedScreen = startingScreen;
    }

    // The tool assumes a non-null GumProjectSave, so a cancel at startup (nothing loaded yet) still
    // needs a blank one.
    private void EnsureProjectLoaded()
    {
        if (_projectManager.GumProjectSave == null)
        {
            _projectManager.CreateNewProject();
        }
    }
}
