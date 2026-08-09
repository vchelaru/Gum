using Gum.Commands;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using GumFormsPlugin.Services;

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
    private readonly IFormsFileService _formsFileService;
    private readonly IFormsThemeImporter _themeImporter;
    // Named for its first consumer; AddScreen is the call needed here.
    private readonly ICopyPasteProjectCommands _projectCommands;
    private readonly ISelectedState _selectedState;

    public NewProjectLogic(
        IProjectManager projectManager,
        IDialogService dialogService,
        IFileCommands fileCommands,
        IFormsFileService formsFileService,
        IFormsThemeImporter themeImporter,
        ICopyPasteProjectCommands projectCommands,
        ISelectedState selectedState)
    {
        _projectManager = projectManager;
        _dialogService = dialogService;
        _fileCommands = fileCommands;
        _formsFileService = formsFileService;
        _themeImporter = themeImporter;
        _projectCommands = projectCommands;
        _selectedState = selectedState;
    }

    /// <inheritdoc/>
    public void CreateNewProject()
    {
        // Create first so the tool always has a valid GumProjectSave, even if the user backs out
        // of everything below.
        _projectManager.CreateNewProject();

        if (!_dialogService.Show<NewProjectDialogViewModel>(null, out NewProjectDialogViewModel viewModel))
        {
            return;
        }

        // The theme import copies files next to the gumx, so the project needs a location on disk
        // before it can run.
        if (!_projectManager.AskUserForProjectNameIfNecessary(out _))
        {
            return;
        }

        if (!_fileCommands.TryAutoSaveProject())
        {
            return;
        }

        if (viewModel.IsIncludeFormsControls)
        {
            _themeImporter.ImportTheme(_formsFileService.DefaultThemeName, isIncludeDemoScreenGum: false);
        }

        ScreenSave startingScreen = new() { Name = StartingScreenName };
        _projectCommands.AddScreen(startingScreen);
        _selectedState.SelectedScreen = startingScreen;
    }
}
