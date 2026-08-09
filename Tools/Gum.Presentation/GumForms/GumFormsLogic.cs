using Gum.DataTypes;
using Gum.ToolStates;
using GumFormsPlugin.Services;
using GumFormsPlugin.ViewModels;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace GumFormsPlugin;

/// <summary>
/// Business logic behind the "Add Forms Components" menu item, relocated out of the WPF-hosted
/// <c>MainGumFormsPlugin</c> (ADR-0005 Phase 3) so it can be unit tested headlessly.
/// </summary>
public class GumFormsLogic
{
    private readonly IFormsFileService _formsFileService;
    private readonly IFormsThemeImporter _themeImporter;
    private readonly IProjectState _projectState;

    public GumFormsLogic(
        IFormsFileService formsFileService,
        IFormsThemeImporter themeImporter,
        IProjectState projectState)
    {
        _formsFileService = formsFileService;
        _themeImporter = themeImporter;
        _projectState = projectState;
    }

    /// <summary>
    /// Whether the project already has Forms components imported. Independent of the theme picker -
    /// the default theme's destination set is used since the same destination paths get written
    /// regardless of which theme produced them.
    /// </summary>
    public bool GetIfProjectHasForms()
    {
        Dictionary<string, FilePath> files =
            _formsFileService.GetSourceDestinations(_formsFileService.DefaultThemeName, isIncludeDemoScreenGum: false);

        return files.Values
            .Any(item =>
                item.Extension != "png" &&
                item.Extension != "gutx" &&
                item.Extension != "fnt" &&
                item.Extension != "bmfc" &&
                item.Extension != "setj" &&
                item.Extension != "json" &&
                item.Exists());
    }

    /// <summary>
    /// Whether the "Add Forms Components" menu item should be present for the given project. A
    /// newly created project has no <c>FullFileName</c> yet, so it cannot have forms - checking the
    /// save parameter directly avoids any stale <see cref="IProjectState"/> state.
    /// </summary>
    public bool ShouldShowAddFormsMenuItem(GumProjectSave? save)
    {
        bool hasForms = !string.IsNullOrEmpty(save?.FullFileName) && GetIfProjectHasForms();
        return !hasForms;
    }

    /// <summary>
    /// Builds the "Add Forms" dialog view model, or returns false with an explanatory message if
    /// the project must be saved first.
    /// </summary>
    public bool TryCreateAddFormsViewModel(out AddFormsViewModel? viewModel, out string? blockedMessage)
    {
        if (_projectState.NeedsToSaveProject)
        {
            viewModel = null;
            blockedMessage = "You must first save the project before importing forms";
            return false;
        }

        viewModel = new AddFormsViewModel(_formsFileService, _themeImporter, _projectState);
        blockedMessage = null;
        return true;
    }
}
