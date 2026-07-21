using Gum.DataTypes;
using Gum.Gui.Controls;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Gum.Commands;
using Gum.Logic.FileWatch;
using ToolsUtilities;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.Localization;

namespace Gum.Plugins.PropertiesWindowPlugin;

/// <summary>
/// Plugin for displaying project properties
/// </summary>
[Export(typeof(PluginBase))]
class MainPropertiesWindowPlugin : PriorityPlugin
{
    #region Fields/Properties

    ProjectPropertiesControl control;

    ProjectPropertiesViewModel viewModel;
    [Import("LocalizationService")]
    public LocalizationService LocalizationService
    {
        get;
        set;
    }
    #endregion

    private readonly IFontManager _fontManager;
    private readonly IWireframeCommands _wireframeCommands;
    private readonly IDialogService _dialogService;
    private readonly IDispatcher _dispatcher;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly FileWatchLogic _fileWatchLogic;
    private readonly IProjectState _projectState;
    private readonly IPluginManager _pluginManager;
    private readonly IProjectManager _projectManager;
    private FilePath? _fontCharacterFileAbsolute;

    private IPluginTab? _pluginTab;

    private ProjectPropertiesChangeLogic? _changeLogic;

    [ImportingConstructor]
    public MainPropertiesWindowPlugin(
        IFontManager fontManager,
        IWireframeCommands wireframeCommands,
        IDialogService dialogService,
        IDispatcher dispatcher,
        IWireframeObjectManager wireframeObjectManager,
        FileWatchLogic fileWatchLogic,
        IProjectState projectState,
        IPluginManager pluginManager,
        IProjectManager projectManager)
    {
        _fontManager = fontManager;
        _wireframeCommands = wireframeCommands;
        _dialogService = dialogService;
        _dispatcher = dispatcher;
        _wireframeObjectManager = wireframeObjectManager;
        _fileWatchLogic = fileWatchLogic;
        _projectState = projectState;
        _pluginManager = pluginManager;
        _projectManager = projectManager;
    }

    public override void StartUp()
    {
        this.AddMenuItem(new List<string> { "Edit", "Properties" }).Click += HandlePropertiesClicked;

        _changeLogic = new ProjectPropertiesChangeLogic(
            _projectManager,
            _fontManager,
            _dialogService,
            _projectState,
            _wireframeObjectManager,
            _fileCommands,
            _wireframeCommands,
            _guiCommands,
            _pluginManager,
            LocalizationService);

        viewModel = new PropertiesWindowPlugin.ProjectPropertiesViewModel();
        viewModel.PropertyChanged += HandlePropertyChanged;

        // todo - handle loading new Gum project when this window is shown - re-call BindTo
        this.ProjectLoad += HandleProjectLoad;
        this.ReactToFileChanged += HandleFileChanged;
        _fileCommands.LocalizationLoaded += HandleLocalizationLoaded;

        control = new();
        control.CloseClicked += HandleCloseClicked;
        
        _pluginTab = _tabManager.AddControl(control, "Project Properties");
        _pluginTab.Hide();
    }

    private void HandleLocalizationLoaded()
    {
        if (control == null || viewModel == null) return;
        viewModel.UpdateLanguageNameFromIndex(LocalizationService.Languages);
        control.ViewModel = null;
        control.ViewModel = viewModel;
    }

    private void HandleProjectLoad(GumProjectSave obj)
    {
        if (control != null && viewModel != null)
        {
            viewModel.SetFrom(_projectManager.AutoSave, _projectState.GumProjectSave);
            control.ViewModel = null;
            control.ViewModel = viewModel;
            RefreshFontRangeEditability();
            
            if(viewModel.UseFontCharacterFile)
            {
                var absolute = new FilePath(_projectState.ProjectDirectory + ".gumfcs");
                _fontCharacterFileAbsolute = absolute;

                if(System.IO.File.Exists(absolute.FullPath))
                {
                    var ranges = BmfcSave.GenerateRangesFromFile(absolute.FullPath);
                    viewModel.FontRanges = ranges;
                }
            }
            else
            {
                _fontCharacterFileAbsolute = null;
            }

            _fileWatchLogic.RefreshRootDirectory();
        }
    }

    private void HandlePropertiesClicked(object? sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            viewModel.SetFrom(_projectManager.AutoSave, _projectState.GumProjectSave);
            control.ViewModel = viewModel;
            if(_pluginTab != null)
            {
                _pluginTab.Show();
                _pluginTab.CanClose = true;
                _pluginTab.IsSelected = true;
            }
            RefreshFontRangeEditability();
        }
        catch (Exception ex)
        {
            _guiCommands.PrintOutput($"Error showing project properties:\n{ex.ToString()}");
        }
    }

    private async void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var result = await _changeLogic!.HandlePropertyChanged(viewModel, e.PropertyName);

        if (result.FontCharacterFileChanged)
        {
            _fontCharacterFileAbsolute = result.FontCharacterFileAbsolute;
            RefreshFontRangeEditability();
        }
    }
    private async void HandleFileChanged(FilePath file)
    {
        if(_fontCharacterFileAbsolute != null && file == _fontCharacterFileAbsolute)
        {
            if(System.IO.File.Exists(_fontCharacterFileAbsolute.FullPath))
            {
                var ranges = BmfcSave.GenerateRangesFromFile(_fontCharacterFileAbsolute.FullPath);

                _dispatcher.Invoke(() =>
                {
                    viewModel.FontRanges = ranges;
                    control?.DataGrid.Refresh();
                });

                try
                {
                    _fontManager.DeleteFontCacheFolder();
                }
                catch(System.IO.IOException)
                {
                    // ignore, if the folder is locked fonts will be recreated on next change
                }

                await _fontManager.CreateAllMissingFontFiles(_projectState.GumProjectSave);
            }
        }
    }
    private void RefreshFontRangeEditability()
    {
        if(control != null)
        {
            var member = control.DataGrid.GetInstanceMember(nameof(viewModel.FontRanges));
            if(member != null)
            {
                member.IsReadOnly = viewModel.UseFontCharacterFile;
            }
            control.DataGrid.Refresh();
        }
    }
    private void HandleCloseClicked(object? sender, EventArgs e)
    {
        _pluginTab?.Hide();
    }
}
