using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using ToolsUtilities;

namespace Gum.Plugins.PropertiesWindowPlugin;

/// <summary>
/// The Project Properties tab (Edit > Properties), shared by both heads. Keeps
/// <see cref="ProjectPropertiesViewModel"/> in step with the loaded project and hands each change to
/// <see cref="ProjectPropertiesChangeLogic"/>. Each head supplies the view (TabViewRegistry).
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
    private IPluginTab? _pluginTab;

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
        AddMenuEntry(HandlePropertiesClicked, "Edit", "Properties");

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
        _viewModel.CloseRequested += () => _pluginTab?.Hide();

        this.ProjectLoad += HandleProjectLoad;
        _fileCommands.LocalizationLoaded += HandleLocalizationLoaded;

        // Each head resolves the view model to its own view (TabViewRegistry).
        _pluginTab = _tabManager.AddControl(_viewModel, "Project Properties");
        _pluginTab.Hide();
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
            _viewModel.SetFrom(_projectManager.AutoSave, project);
            RefreshLanguagesAndReload();
        }
    }

    private void HandlePropertiesClicked()
    {
        try
        {
            if (_projectState.GumProjectSave is not { } project)
            {
                return;
            }
            _viewModel.SetFrom(_projectManager.AutoSave, project);
            RefreshLanguagesAndReload();
            if (_pluginTab != null)
            {
                _pluginTab.Show();
                _pluginTab.CanClose = true;
                _pluginTab.IsSelected = true;
            }
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
