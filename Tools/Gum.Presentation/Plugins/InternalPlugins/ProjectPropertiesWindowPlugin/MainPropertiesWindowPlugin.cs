using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary.Graphics.Fonts;
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

    private readonly IFontManager _fontManager;
    private readonly IWireframeCommands _wireframeCommands;
    private readonly IDialogService _dialogServiceForChanges;
    private readonly IDispatcher _dispatcher;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IProjectState _projectState;
    private readonly IPluginManager _pluginManager;
    private readonly IProjectManager _projectManager;
    private readonly LocalizationService _localizationService;

    private ProjectPropertiesViewModel _viewModel = null!;
    private ProjectPropertiesChangeLogic _changeLogic = null!;
    private IPluginTab? _pluginTab;
    private FilePath? _fontCharacterFileAbsolute;

    #endregion

    [ImportingConstructor]
    public MainPropertiesWindowPlugin(
        IFontManager fontManager,
        IWireframeCommands wireframeCommands,
        IDialogService dialogService,
        IDispatcher dispatcher,
        IWireframeObjectManager wireframeObjectManager,
        IProjectState projectState,
        IPluginManager pluginManager,
        IProjectManager projectManager,
        LocalizationService localizationService)
    {
        _fontManager = fontManager;
        _wireframeCommands = wireframeCommands;
        _dialogServiceForChanges = dialogService;
        _dispatcher = dispatcher;
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
            _fontManager,
            _dialogServiceForChanges,
            _projectState,
            _wireframeObjectManager,
            _fileCommands,
            _wireframeCommands,
            _guiCommands,
            _pluginManager,
            _localizationService);

        _viewModel = new ProjectPropertiesViewModel();
        _viewModel.PropertyChanged += HandlePropertyChanged;
        _viewModel.CloseRequested += () => _pluginTab?.Hide();

        this.ProjectLoad += HandleProjectLoad;
        this.ReactToFileChanged += HandleFileChanged;
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

        using (Gum.Diagnostics.StartupTiming.Time("  font character file ranges"))
        {
            if (_viewModel.UseFontCharacterFile)
            {
                var absolute = new FilePath(_projectState.ProjectDirectory + ".gumfcs");
                _fontCharacterFileAbsolute = absolute;

                if (System.IO.File.Exists(absolute.FullPath))
                {
                    var ranges = BmfcSave.GenerateRangesFromFile(absolute.FullPath);
                    _viewModel.FontRanges = ranges;
                }
            }
            else
            {
                _fontCharacterFileAbsolute = null;
            }
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

    private async void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var result = await _changeLogic.HandlePropertyChanged(_viewModel, e.PropertyName);

        if (result.FontCharacterFileChanged)
        {
            _fontCharacterFileAbsolute = result.FontCharacterFileAbsolute;
        }
    }

    private async void HandleFileChanged(FilePath file)
    {
        if (_fontCharacterFileAbsolute != null && file == _fontCharacterFileAbsolute)
        {
            if (System.IO.File.Exists(_fontCharacterFileAbsolute.FullPath))
            {
                var ranges = BmfcSave.GenerateRangesFromFile(_fontCharacterFileAbsolute.FullPath);

                _dispatcher.Invoke(() => _viewModel.FontRanges = ranges);

                try
                {
                    _fontManager.DeleteFontCacheFolder();
                }
                catch (System.IO.IOException)
                {
                    // ignore, if the folder is locked fonts will be recreated on next change
                }

                if (_projectState.GumProjectSave is { } project)
                {
                    await _fontManager.CreateAllMissingFontFiles(project);
                }
            }
        }
    }
}
