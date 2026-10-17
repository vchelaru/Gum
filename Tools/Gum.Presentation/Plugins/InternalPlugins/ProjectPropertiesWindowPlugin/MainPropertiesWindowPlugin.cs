using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.SettingsWindow;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using ToolsUtilities;

namespace Gum.Plugins.PropertiesWindowPlugin;

/// <summary>
/// The project's properties, shared by both heads, as the General, Canvas and Localization pages of
/// the Settings window. Keeps <see cref="ProjectPropertiesViewModel"/> in step with the loaded project
/// and hands each change to <see cref="ProjectPropertiesChangeLogic"/>. Each head supplies the page
/// view (its settings page view registry).
/// </summary>
[Export(typeof(PluginBase))]
class MainPropertiesWindowPlugin : CorePriorityPlugin
{
    #region Fields/Properties

    private readonly IWireframeCommands _wireframeCommands;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IProjectState _projectState;
    private readonly IPluginManager _pluginManager;
    private readonly IProjectManager _projectManager;
    private readonly LocalizationService _localizationService;

    private ProjectPropertiesViewModel _viewModel = null!;
    private ProjectPropertiesChangeLogic _changeLogic = null!;

    #endregion

    [ImportingConstructor]
    public MainPropertiesWindowPlugin(
        IWireframeCommands wireframeCommands,
        IWireframeObjectManager wireframeObjectManager,
        IProjectState projectState,
        IPluginManager pluginManager,
        IProjectManager projectManager,
        LocalizationService localizationService)
    {
        _wireframeCommands = wireframeCommands;
        _wireframeObjectManager = wireframeObjectManager;
        _projectState = projectState;
        _pluginManager = pluginManager;
        _projectManager = projectManager;
        _localizationService = localizationService;
    }

    public override void StartUp()
    {
        _changeLogic = new ProjectPropertiesChangeLogic(
            _projectManager,
            _projectState,
            _wireframeObjectManager,
            _fileCommands,
            _wireframeCommands,
            _pluginManager,
            _localizationService);

        _viewModel = new ProjectPropertiesViewModel();
        _viewModel.PropertyChanged += HandlePropertyChanged;

        this.ProjectLoad += HandleProjectLoad;
        _fileCommands.LocalizationLoaded += HandleLocalizationLoaded;

        // Each head resolves a page's content to its own view (its settings page view registry).
        AddSettingsPage(new SettingsPage(
            SettingsPageIds.ProjectGeneral, "General", SettingsSection.Project,
            new ProjectPropertiesPageViewModel(_viewModel, ProjectPropertiesSection.General), order: 0)
        {
            Activated = Reload,
            Keywords = new[] { "texture filter", "unit values", "android", "file names", "single pixel texture" },
        });
        AddSettingsPage(new SettingsPage(
            SettingsPageIds.Canvas, "Canvas", SettingsSection.Project,
            new ProjectPropertiesPageViewModel(_viewModel, ProjectPropertiesSection.Canvas), order: 1)
        {
            Activated = Reload,
            Keywords = new[] { "width", "height", "size", "guides", "outlines", "checker" },
        });
        AddSettingsPage(new SettingsPage(
            SettingsPageIds.Localization, "Localization", SettingsSection.Project,
            new ProjectPropertiesPageViewModel(_viewModel, ProjectPropertiesSection.Localization), order: 3)
        {
            Activated = Reload,
            Keywords = new[] { "language", "csv", "resx", "translation" },
        });
    }

    private void HandleLocalizationLoaded()
    {
        _viewModel.UpdateLanguageNameFromIndex(_localizationService.Languages);
        RefreshLanguagesAndReload();
    }

    private void HandleProjectLoad(GumProjectSave project)
    {
        using var _ = Gum.Diagnostics.StartupTiming.Time("MainPropertiesWindowPlugin.HandleProjectLoad (total)");
        using (Gum.Diagnostics.StartupTiming.Time("  SetFrom + reload"))
        {
            _viewModel.SetFrom(project);
            RefreshLanguagesAndReload();
        }
    }

    // Shown each time a page is opened, so the page reflects the project as it is now.
    private void Reload()
    {
        try
        {
            if (_projectState.GumProjectSave is not { } project)
            {
                return;
            }
            _viewModel.SetFrom(project);
            RefreshLanguagesAndReload();
        }
        catch (Exception ex)
        {
            _guiCommands.PrintOutput($"Error showing project properties:\n{ex}");
        }
    }

    // A fresh list each time, so the change reaches views even when the languages are unchanged.
    private void RefreshLanguagesAndReload()
    {
        _viewModel.AvailableLanguages = _localizationService.Languages.ToArray();
        _viewModel.NotifyReloaded();
    }

    private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _changeLogic.HandlePropertyChanged(_viewModel, e.PropertyName);
    }
}
