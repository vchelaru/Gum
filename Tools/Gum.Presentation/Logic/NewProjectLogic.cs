using Gum.Commands;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using System.IO;
using System.Linq;
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
    private readonly IDeleteLogic _deleteLogic;
    private readonly INewProjectLocationSettings _locationSettings;

    public NewProjectLogic(
        IProjectManager projectManager,
        IDialogService dialogService,
        IFileCommands fileCommands,
        IFormsThemeImporter themeImporter,
        ICopyPasteProjectCommands projectCommands,
        ISelectedState selectedState,
        IDefaultFontBundler defaultFontBundler,
        IDeleteLogic deleteLogic,
        INewProjectLocationSettings locationSettings)
    {
        _projectManager = projectManager;
        _dialogService = dialogService;
        _fileCommands = fileCommands;
        _themeImporter = themeImporter;
        _projectCommands = projectCommands;
        _selectedState = selectedState;
        _defaultFontBundler = defaultFontBundler;
        _deleteLogic = deleteLogic;
        _locationSettings = locationSettings;
    }

    /// <inheritdoc/>
    public async Task CreateNewProjectAsync()
    {
        // The open project is replaced only once the user has accepted the dialog (and, with a code project,
        // the files were created), so backing out leaves it loaded (#5968). The theme picker describes what
        // the new project would need, so it is pointed at a blank one rather than the open project.
        GumProjectSave blankProject = _projectManager.CreateBlankProject();
        if (!_dialogService.Show<NewProjectDialogViewModel>(
                vm => vm.ThemeSelection.TargetProject = blankProject, out NewProjectDialogViewModel viewModel))
        {
            EnsureProjectLoaded();
            return;
        }

        if (viewModel.IsCodeProjectCreated && viewModel.CreatedGumProjectPath is { } createdProjectPath)
        {
            // The dialog already wrote the files, so the creation succeeded whatever the load does.
            _locationSettings.RememberNewProjectParent(viewModel.ProjectParentFolder);
            await LoadCreatedCodeProjectAsync(viewModel, createdProjectPath);
            return;
        }

        // The theme import copies files next to the project file, so the project needs a location on disk
        // before it can run.
        string projectLocation = viewModel.GumProjectPath;

        // The dialog already refuses this, but the flow must not overwrite a project even when it is reached some other way.
        if (FindExistingProjectFile(projectLocation) is { } existingProject)
        {
            _dialogService.ShowMessage($"Project already exists: {existingProject}");
            EnsureProjectLoaded();
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(projectLocation)!);

        ClearSelection();

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

        _locationSettings.RememberNewProjectParent(viewModel.ProjectParentFolder);

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

    // The project file at this path, in either format, if there is one.
    private static string? FindExistingProjectFile(string projectPath)
    {
        foreach (string extension in new[] { ".gumj", ".gumx" })
        {
            string candidate = Path.ChangeExtension(projectPath, extension);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    // The dialog already wrote the Gum project and the code project around it; the tool only opens the Gum
    // project and applies the choices the creators cannot express (the demo screen) and the starting screen.
    private async Task LoadCreatedCodeProjectAsync(NewProjectDialogViewModel viewModel, string gumProjectPath)
    {
        ClearSelection();
        await _projectManager.LoadProjectAsync(new FilePath(gumProjectPath));

        // A failed load leaves no project (the project manager already explained why when it could), and an
        // ignored one (another load in flight) leaves the previous project open. In both the created project is
        // not the loaded one, so say where the files are and edit nothing; with no project, leave the tool with a
        // blank one like a cancelled dialog does.
        if (!IsLoaded(gumProjectPath))
        {
            _dialogService.ShowMessage(
                $"The C# project was created, but its Gum project could not be opened:\n\n{gumProjectPath}");
            EnsureProjectLoaded();
            return;
        }

        GumProjectSave project = _projectManager.GetLoadedProject();
        if (viewModel.IsIncludeFormsControls && !viewModel.IsIncludeDemoScreenGum)
        {
            foreach (ScreenSave demoScreen in project.Screens.Where(screen => DemoScreenRule.IsDemoScreenName(screen.Name)).ToList())
            {
                // Removing the element leaves its file behind, and the project would not list it, so delete it too.
                FilePath? demoFile = _fileCommands.GetFullFileName(demoScreen);
                _deleteLogic.RemoveElement(demoScreen);
                if (demoFile != null && File.Exists(demoFile.FullPath))
                {
                    File.Delete(demoFile.FullPath);
                }
            }

            // The game was written while the demo screen existed (so the shown command and the dialog create the same files).
            viewModel.RemoveDemoScreenFromHost();
        }

        ScreenSave startingScreen = project.Screens.FirstOrDefault(screen => screen.Name == StartingScreenName) ?? new ScreenSave { Name = StartingScreenName };
        if (!project.Screens.Contains(startingScreen))
        {
            _projectCommands.AddScreen(startingScreen);
        }
        _selectedState.SelectedScreen = startingScreen;
    }

    private bool IsLoaded(string gumProjectPath) =>
        _projectManager.GumProjectSave is { FullFileName: { Length: > 0 } loadedPath }
        && new FilePath(loadedPath) == new FilePath(gumProjectPath);

    /// <inheritdoc/>
    public async Task CloseProjectAsync()
    {
        ClearSelection();
        _projectManager.CreateNewProject();

        await CreateNewProjectAsync();
    }

    private void ClearSelection()
    {
        _selectedState.SelectedElement = null;
        _selectedState.SelectedInstance = null;
        _selectedState.SelectedBehavior = null;
        _selectedState.SelectedStateCategorySave = null;
        _selectedState.SelectedStateSave = null;
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
