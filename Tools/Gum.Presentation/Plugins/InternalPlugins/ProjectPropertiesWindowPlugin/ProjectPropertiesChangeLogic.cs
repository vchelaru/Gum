using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using Gum.Wireframe;
using ToolsUtilities;

namespace Gum.Plugins.PropertiesWindowPlugin;

/// <summary>
/// Decision logic for what happens when a property on <see cref="ProjectPropertiesViewModel"/>
/// changes - path normalization, and deciding whether the project should be saved and the
/// wireframe refreshed. Extracted from
/// <c>MainPropertiesWindowPlugin</c> (which stays a thin MEF wrapper owning the properties
/// window/control) so this logic can be constructed and tested without plugin composition.
/// </summary>
public class ProjectPropertiesChangeLogic
{
    private readonly IProjectManager _projectManager;
    private readonly IProjectState _projectState;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IFileCommands _fileCommands;
    private readonly IWireframeCommands _wireframeCommands;
    private readonly IPluginManager _pluginManager;
    private readonly ILocalizationService _localizationService;

    public ProjectPropertiesChangeLogic(
        IProjectManager projectManager,
        IProjectState projectState,
        IWireframeObjectManager wireframeObjectManager,
        IFileCommands fileCommands,
        IWireframeCommands wireframeCommands,
        IPluginManager pluginManager,
        ILocalizationService localizationService)
    {
        _projectManager = projectManager;
        _projectState = projectState;
        _wireframeObjectManager = wireframeObjectManager;
        _fileCommands = fileCommands;
        _wireframeCommands = wireframeCommands;
        _pluginManager = pluginManager;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Reacts to a single property changing on <paramref name="viewModel"/>: applies the view
    /// model back onto the loaded project, runs any property-specific side effect (path
    /// normalization, etc.), and saves/refreshes the wireframe when needed.
    /// </summary>
    public void HandlePropertyChanged(ProjectPropertiesViewModel viewModel, string? propertyName)
    {
        // View-only state (the language list) is not project data.
        if (viewModel.IsUpdatingFromModel
            || propertyName is nameof(ProjectPropertiesViewModel.AvailableLanguages))
        {
            return;
        }

        viewModel.ApplyToModelObjects();
        _projectManager.AutoSave = viewModel.AutoSave;

        bool shouldSaveAndRefresh = true;
        switch (propertyName)
        {
            case nameof(viewModel.LocalizationFiles):

                // Normalize any absolute paths to project-relative so .gumx stays portable. An
                // unsaved project has no directory to be relative to, so paths stay absolute.
                string? projectDirectory = _projectState.ProjectDirectory;
                bool normalizedAny = false;
                List<string> normalized = new List<string>(viewModel.LocalizationFiles.Count);
                foreach (string file in viewModel.LocalizationFiles)
                {
                    if (projectDirectory != null && !string.IsNullOrEmpty(file) && FileManager.IsRelative(file) == false)
                    {
                        normalized.Add(FileManager.MakeRelative(file, projectDirectory, preserveCase: true));
                        normalizedAny = true;
                    }
                    else
                    {
                        normalized.Add(file);
                    }
                }

                if (normalizedAny)
                {
                    // This re-enters HandlePropertyChanged which will hit the else branch.
                    viewModel.LocalizationFiles = normalized;
                    shouldSaveAndRefresh = false;
                }
                else
                {
                    _fileCommands.LoadLocalizationFile();
                    _wireframeObjectManager.RefreshAll(forceLayout: true, forceReloadTextures: false);
                }
                break;
            case nameof(viewModel.LanguageName):
                int languageIdx = _localizationService.Languages.ToList().IndexOf(viewModel.LanguageName) + 1;
                if (languageIdx > 0 && languageIdx != viewModel.LanguageIndex)
                {
                    viewModel.LanguageIndex = languageIdx;
                    _localizationService.CurrentLanguage = languageIdx;
                }
                else
                {
                    shouldSaveAndRefresh = false;
                }
                break;
            case nameof(viewModel.LanguageIndex):
                _localizationService.CurrentLanguage = viewModel.LanguageIndex;
                break;
            case nameof(viewModel.ShowLocalization):
                shouldSaveAndRefresh = true;
                break;
            case nameof(viewModel.SinglePixelTextureFile):
            case nameof(viewModel.SinglePixelTextureTop):
            case nameof(viewModel.SinglePixelTextureLeft):
            case nameof(viewModel.SinglePixelTextureRight):
            case nameof(viewModel.SinglePixelTextureBottom):

                if (_projectState.ProjectDirectory is { } projectDirectoryForTexture
                    && !string.IsNullOrEmpty(viewModel.SinglePixelTextureFile)
                    && FileManager.IsRelative(viewModel.SinglePixelTextureFile) == false)
                {
                    // This will loop:
                    viewModel.SinglePixelTextureFile = FileManager.MakeRelative(viewModel.SinglePixelTextureFile,
                        projectDirectoryForTexture, preserveCase: true);
                    shouldSaveAndRefresh = false;
                }

                break;
            case nameof(viewModel.ShowCheckerBackground):
                // Checkerboard visibility is handled via WireframePropertyChanged,
                // so skip the wireframe refresh to avoid resetting the texture
                // coordinates tab camera position.
                _fileCommands.TryAutoSaveProject();
                shouldSaveAndRefresh = false;
                break;
        }

        if (propertyName != null)
        {
            _pluginManager.ProjectPropertySet(propertyName);
        }

        if (shouldSaveAndRefresh)
        {
            _wireframeCommands.Refresh(forceLayout: true, forceReloadContent: false);

            _fileCommands.TryAutoSaveProject();
        }
    }
}
