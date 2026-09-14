using Gum.Menus;
using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Plugins.BaseClasses;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Gum.Services;
using ToolsUtilities;

namespace Gum.Plugins.FileWatchPlugin;

/// <summary>
/// Plugin entry point for the File Watch debug panel, shared by both heads. All of the WPF-free business logic
/// (project/variable-change reactions, the debug panel's display-refresh data) lives in
/// <see cref="FileWatchPluginController"/> (issue #3931) - this class owns only the
/// tab/menu-item wiring and the timer subscription; each head supplies the view for
/// <see cref="FileWatchViewModel"/>.
/// </summary>
[Export(typeof(PluginBase))]
public class MainFileWatchPlugin : CorePriorityPlugin
{
    #region Fields/Properties

    private readonly PeriodicUiTimer refreshDisplayTimer;
    private readonly FileWatchPluginController _controller;

    FileWatchViewModel viewModel = null!;

    IPluginTab pluginTab = null!;
    MenuItemModel showFileWatchMenuItem = null!;

    #endregion

    [ImportingConstructor]
    public MainFileWatchPlugin(IFileWatchManager fileWatchManager, FileWatchLogic fileWatchLogic, PeriodicUiTimer periodicUiTimer)
    {
        _controller = new FileWatchPluginController(fileWatchManager, fileWatchLogic);
        refreshDisplayTimer = periodicUiTimer;
    }

    public override void StartUp()
    {
        viewModel = new FileWatchViewModel();

        viewModel.PropertyChanged += HandleViewModelPropertyChanged;

        // Each head resolves the view model to its own view (TabViewRegistry).
        pluginTab = _tabManager.AddControl(viewModel, "File Watch", TabLocation.RightBottom);
        pluginTab.Hide();

        pluginTab.TabHidden += HandleTabHidden;
        pluginTab.TabShown += HandleTabShown;
        pluginTab.CanClose = true;

        showFileWatchMenuItem = AddMenuEntry(HandleShowFileWatch, "View", "Show File Watch");

        const int millisecondsTimerFrequency = 200;
        refreshDisplayTimer.Tick += HandleRefreshDisplayTimerElapsed;
        refreshDisplayTimer.Start(TimeSpan.FromMilliseconds(millisecondsTimerFrequency));

        AssignEvents();
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        _controller.HandleViewModelPropertyChanged(viewModel, e);

    private void AssignEvents()
    {
        this.ProjectLoad += HandleProjectLoad;
        this.ProjectLocationSet += HandleProjectLocationSet;
        this.VariableSet += HandleVariableSet;
    }

    private void HandleVariableSet(ElementSave element, InstanceSave? instance, string variableName, object? oldValue) =>
        _controller.HandleVariableSet(element, instance, variableName, oldValue);

    private void HandleProjectLocationSet(FilePath path) =>
        _controller.HandleProjectLocationSet(path);

    private void HandleProjectLoad(GumProjectSave save) =>
        _controller.HandleProjectLoad(save);

    private void HandleTabShown()
    {
        showFileWatchMenuItem.Header = "Hide File Watch";
    }

    private void HandleTabHidden()
    {
        showFileWatchMenuItem.Header = "Show File Watch";
    }

    private void HandleShowFileWatch()
    {
        pluginTab.IsVisible = !pluginTab.IsVisible;
        if(pluginTab.IsVisible)
        {
            pluginTab.IsSelected = true;
        }
    }

    private void HandleRefreshDisplayTimerElapsed() =>
        _controller.RefreshDisplay(viewModel);
}
