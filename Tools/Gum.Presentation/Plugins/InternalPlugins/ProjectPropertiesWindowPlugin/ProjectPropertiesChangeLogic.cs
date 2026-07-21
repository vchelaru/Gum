using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary.Graphics.Fonts;
using ToolsUtilities;

namespace Gum.Plugins.PropertiesWindowPlugin;

/// <summary>
/// Outcome of <see cref="ProjectPropertiesChangeLogic.HandlePropertyChanged"/> that the plugin
/// (which owns the actual view/control and its own cached font-character-file path) still needs
/// to act on.
/// </summary>
public class ProjectPropertyChangeResult
{
    /// <summary>
    /// True only when the changed property was <see cref="ProjectPropertiesViewModel.UseFontCharacterFile"/>,
    /// in which case the plugin should update its own cached font-character-file path to
    /// <see cref="FontCharacterFileAbsolute"/> and refresh the Variables grid's read-only state
    /// for the FontRanges row.
    /// </summary>
    public bool FontCharacterFileChanged { get; init; }

    /// <summary>
    /// The new cached font-character-file path. Only meaningful when
    /// <see cref="FontCharacterFileChanged"/> is true.
    /// </summary>
    public FilePath? FontCharacterFileAbsolute { get; init; }
}

/// <summary>
/// Decision logic for what happens when a property on <see cref="ProjectPropertiesViewModel"/>
/// changes - path normalization, font-range validation/regeneration, and deciding whether the
/// project should be saved and the wireframe refreshed. Extracted from
/// <c>MainPropertiesWindowPlugin</c> (which stays a thin MEF wrapper owning the properties
/// window/control) so this logic can be constructed and tested without plugin composition.
/// </summary>
public class ProjectPropertiesChangeLogic
{
    private readonly IProjectManager _projectManager;
    private readonly IFontManager _fontManager;
    private readonly IDialogService _dialogService;
    private readonly IProjectState _projectState;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IFileCommands _fileCommands;
    private readonly IWireframeCommands _wireframeCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly IPluginManager _pluginManager;
    private readonly ILocalizationService _localizationService;

    public ProjectPropertiesChangeLogic(
        IProjectManager projectManager,
        IFontManager fontManager,
        IDialogService dialogService,
        IProjectState projectState,
        IWireframeObjectManager wireframeObjectManager,
        IFileCommands fileCommands,
        IWireframeCommands wireframeCommands,
        IGuiCommands guiCommands,
        IPluginManager pluginManager,
        ILocalizationService localizationService)
    {
        _projectManager = projectManager;
        _fontManager = fontManager;
        _dialogService = dialogService;
        _projectState = projectState;
        _wireframeObjectManager = wireframeObjectManager;
        _fileCommands = fileCommands;
        _wireframeCommands = wireframeCommands;
        _guiCommands = guiCommands;
        _pluginManager = pluginManager;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Reacts to a single property changing on <paramref name="viewModel"/>: applies the view
    /// model back onto the loaded project, runs any property-specific side effect (path
    /// normalization, font regeneration, etc.), and saves/refreshes the wireframe when needed.
    /// </summary>
    public async Task<ProjectPropertyChangeResult> HandlePropertyChanged(ProjectPropertiesViewModel viewModel, string? propertyName)
    {
        if (viewModel.IsUpdatingFromModel)
        {
            return new ProjectPropertyChangeResult();
        }

        viewModel.ApplyToModelObjects();
        _projectManager.AutoSave = viewModel.AutoSave;

        bool shouldSaveAndRefresh = true;
        bool shouldReloadContent = false;
        bool fontCharacterFileChanged = false;
        FilePath? fontCharacterFileAbsolute = null;
        switch (propertyName)
        {
            case nameof(viewModel.LocalizationFiles):

                // Normalize any absolute paths to project-relative so .gumx stays portable.
                bool normalizedAny = false;
                List<string> normalized = new List<string>(viewModel.LocalizationFiles.Count);
                foreach (string file in viewModel.LocalizationFiles)
                {
                    if (!string.IsNullOrEmpty(file) && FileManager.IsRelative(file) == false)
                    {
                        normalized.Add(FileManager.MakeRelative(file, _projectState.ProjectDirectory, preserveCase: true));
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
            case nameof(viewModel.FontRanges):
                bool isValid = BmfcSave.GetIfIsValidRange(viewModel.FontRanges);
                bool didFixChangeThings = false;
                if (!isValid)
                {
                    string fixedRange = BmfcSave.TryFixRange(viewModel.FontRanges);
                    if (fixedRange != viewModel.FontRanges)
                    {
                        // this will recursively call this property, so we'll use this bool to leave this method
                        didFixChangeThings = true;
                        viewModel.FontRanges = fixedRange;
                    }
                }

                if (!didFixChangeThings)
                {
                    if (isValid == false)
                    {
                        _dialogService.ShowMessage("The entered Font Range is not valid.");
                    }
                    else
                    {
                        if (_projectState.GumProjectSave != null)
                        {
                            bool wasAbleToDelete = false;
                            try
                            {
                                _fontManager.DeleteFontCacheFolder();
                                wasAbleToDelete = true;
                            }
                            catch (System.IO.IOException exception)
                            {
                                wasAbleToDelete = false;

                                string message =
                                    "Attempted to delete font cache folder to re-create it with the new font range values " +
                                    $"but was unable to do so:\n\n{exception}";
                                _dialogService.ShowMessage(message);
                            }

                            if (wasAbleToDelete)
                            {
                                await _fontManager.CreateAllMissingFontFiles(
                                    _projectState.GumProjectSave);
                            }
                        }
                        shouldSaveAndRefresh = true;
                        shouldReloadContent = true;
                    }
                }
                break;
            case nameof(viewModel.UseFontCharacterFile):
                fontCharacterFileChanged = true;
                if (viewModel.UseFontCharacterFile)
                {
                    FilePath absolute = new FilePath(_projectState.ProjectDirectory + ".gumfcs");
                    fontCharacterFileAbsolute = absolute;

                    if (System.IO.File.Exists(absolute.FullPath))
                    {
                        string ranges = BmfcSave.GenerateRangesFromFile(absolute.FullPath);
                        viewModel.FontRanges = ranges;
                    }
                }
                else
                {
                    fontCharacterFileAbsolute = null;
                    viewModel.FontRanges = BmfcSave.DefaultRanges;
                }
                break;
            case nameof(viewModel.SinglePixelTextureFile):
            case nameof(viewModel.SinglePixelTextureTop):
            case nameof(viewModel.SinglePixelTextureLeft):
            case nameof(viewModel.SinglePixelTextureRight):
            case nameof(viewModel.SinglePixelTextureBottom):

                if (!string.IsNullOrEmpty(viewModel.SinglePixelTextureFile) && FileManager.IsRelative(viewModel.SinglePixelTextureFile) == false)
                {
                    // This will loop:
                    viewModel.SinglePixelTextureFile = FileManager.MakeRelative(viewModel.SinglePixelTextureFile,
                        _projectState.ProjectDirectory, preserveCase: true);
                    shouldSaveAndRefresh = false;
                }

                break;
            case nameof(viewModel.FontGenerator):
                try
                {
                    _fontManager.DeleteFontCacheFolder();
                }
                catch (System.IO.IOException exception)
                {
                    _dialogService.ShowMessage(
                        "Attempted to delete font cache folder to re-create it with the new font generator " +
                        $"but was unable to do so:\n\n{exception}");
                    break;
                }

                await _fontManager.CreateAllMissingFontFiles(_projectState.GumProjectSave);
                shouldReloadContent = true;
                _guiCommands.RefreshVariables(force: true);
                break;
            case nameof(viewModel.ShowCheckerBackground):
                // Checkerboard visibility is handled via WireframePropertyChanged,
                // so skip the wireframe refresh to avoid resetting the texture
                // coordinates tab camera position.
                _fileCommands.TryAutoSaveProject();
                shouldSaveAndRefresh = false;
                break;
        }

        _pluginManager.ProjectPropertySet(propertyName);

        if (shouldSaveAndRefresh)
        {
            _wireframeCommands.Refresh(forceLayout: true, forceReloadContent: shouldReloadContent);

            _fileCommands.TryAutoSaveProject();
        }

        return new ProjectPropertyChangeResult
        {
            FontCharacterFileChanged = fontCharacterFileChanged,
            FontCharacterFileAbsolute = fontCharacterFileAbsolute
        };
    }
}
