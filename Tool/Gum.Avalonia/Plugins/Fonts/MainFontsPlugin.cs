using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.FontsPlugin;
using Gum.ProjectServices;
using Gum.Services;
using Gum.Services.Fonts;
using Gum.ToolStates;
using RenderingLibrary.Graphics.Fonts;
using ToolsUtilities;

namespace Gum.Avalonia.Plugins.Fonts;

/// <summary>
/// The Font Settings tab (Edit > Font Settings): the character ranges per font and the font generation settings.
/// Keeps <see cref="FontsViewModel"/> in step with the loaded project and hands each change to
/// <see cref="FontsChangeLogic"/>, and keeps the project-wide ranges in step with the project's
/// .gumfcs font character file while one is in use.
/// </summary>
[Export(typeof(PluginBase))]
public class MainFontsPlugin : PluginBase, IPriorityPlugin
{
    private readonly IFontManager _fontManager;
    private readonly IWireframeCommands _wireframeCommands;
    private readonly IDispatcher _dispatcher;
    private readonly IProjectState _projectState;
    private readonly IPluginManager _pluginManager;

    private FontsViewModel _viewModel = null!;
    private FontsChangeLogic _changeLogic = null!;
    private IPluginTab? _pluginTab;
    private FilePath? _fontCharacterFileAbsolute;

    /// <summary>Creates the plugin over the tool services it acts through.</summary>
    [ImportingConstructor]
    public MainFontsPlugin(
        IFontManager fontManager,
        IWireframeCommands wireframeCommands,
        IDispatcher dispatcher,
        IProjectState projectState,
        IPluginManager pluginManager)
    {
        _fontManager = fontManager;
        _wireframeCommands = wireframeCommands;
        _dispatcher = dispatcher;
        _projectState = projectState;
        _pluginManager = pluginManager;
    }

    /// <inheritdoc/>
    public override string FriendlyName => "Font Settings";

    /// <inheritdoc/>
    public override Version Version => new Version();

    /// <inheritdoc/>
    public override void StartUp()
    {
        AddMenuEntry(HandleFontsClicked, "Edit", "Font Settings");

        _changeLogic = new FontsChangeLogic(
            _fontManager,
            _dialogService,
            _projectState,
            _fileCommands,
            _wireframeCommands,
            _guiCommands,
            _pluginManager);

        _viewModel = new FontsViewModel();
        _viewModel.PropertyChanged += HandlePropertyChanged;

        this.ProjectLoad += HandleProjectLoad;
        this.ReactToFileChanged += HandleFileChanged;

        // The head resolves the view model to its view (TabViewRegistry).
        _pluginTab = _tabManager.AddControl(_viewModel, "Font Settings");
        _pluginTab.Hide();
    }

    /// <inheritdoc/>
    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    private void HandleProjectLoad(GumProjectSave project)
    {
        _viewModel.SetFrom(project);
        UseCharacterFileRangesWhenInUse();
    }

    private void HandleFontsClicked()
    {
        try
        {
            if (_projectState.GumProjectSave is not { } project)
            {
                return;
            }

            _viewModel.SetFrom(project);
            UseCharacterFileRangesWhenInUse();
            RefreshFontUsage(project);
            _viewModel.NotifyReloaded();

            if (_pluginTab != null)
            {
                _pluginTab.Show();
                _pluginTab.CanClose = true;
                _pluginTab.IsSelected = true;
            }
        }
        catch (Exception ex)
        {
            _guiCommands.PrintOutput($"Error showing the Font Settings tab:\n{ex}");
        }
    }

    /// <summary>While the project uses a .gumfcs file, its characters are the project-wide ranges.</summary>
    private void UseCharacterFileRangesWhenInUse()
    {
        if (!_viewModel.UseFontCharacterFile)
        {
            _fontCharacterFileAbsolute = null;
            return;
        }

        FilePath absolute = new FilePath(_projectState.ProjectDirectory + ".gumfcs");
        _fontCharacterFileAbsolute = absolute;

        if (System.IO.File.Exists(absolute.FullPath))
        {
            _viewModel.DefaultEntry.Ranges = BmfcSave.GenerateRangesFromFile(absolute.FullPath);
        }
    }

    /// <summary>Offers the fonts the project uses, and warns on rows for a font no Text uses.</summary>
    private void RefreshFontUsage(GumProjectSave project)
    {
        System.Collections.Generic.IReadOnlyList<BmfcSave> fonts = FontRangeEntryChecker.CollectFonts(project);
        _viewModel.UpdateFontUsage(
            FontRangeEntryChecker.GetUsedFontNames(fonts),
            FontRangeEntryChecker.FindUnmatchedEntries(project, fonts).Select(entry => entry.FontFamily!.Trim()));
    }

    private async void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        FontsChangeResult result = await _changeLogic.HandlePropertyChanged(_viewModel, e.PropertyName);

        if (result.FontCharacterFileChanged)
        {
            _fontCharacterFileAbsolute = result.FontCharacterFileAbsolute;
        }

        if (e.PropertyName == nameof(FontsViewModel.Entries) && !_viewModel.IsUpdatingFromModel
            && _projectState.GumProjectSave is { } project)
        {
            RefreshFontUsage(project);
        }
    }

    private async void HandleFileChanged(FilePath file)
    {
        if (_fontCharacterFileAbsolute == null || file != _fontCharacterFileAbsolute)
        {
            return;
        }

        if (System.IO.File.Exists(_fontCharacterFileAbsolute.FullPath))
        {
            string ranges = BmfcSave.GenerateRangesFromFile(_fontCharacterFileAbsolute.FullPath);

            _dispatcher.Invoke(() => _viewModel.DefaultEntry.Ranges = ranges);

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
