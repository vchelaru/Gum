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
/// WPF-hosted plugin entry point for the File Watch debug panel. All of the WPF-free business logic
/// (project/variable-change reactions, the debug panel's display-refresh data) lives in
/// <see cref="FileWatchPluginController"/> (issue #3931) - this class owns only the real platform
/// glue: the WPF control/tab/menu-item wiring and the timer subscription.
/// </summary>
[Export(typeof(PluginBase))]
public class MainFileWatchPlugin : PriorityPlugin
{
    #region Fields/Properties

    private readonly PeriodicUiTimer refreshDisplayTimer;
    private readonly FileWatchPluginController _controller;

    FileWatchViewModel viewModel;

    IPluginTab pluginTab;
    System.Windows.Controls.MenuItem showFileWatchMenuItem;

    #endregion

    [ImportingConstructor]
    public MainFileWatchPlugin(IFileWatchManager fileWatchManager, FileWatchLogic fileWatchLogic, PeriodicUiTimer periodicUiTimer)
    {
        _controller = new FileWatchPluginController(fileWatchManager, fileWatchLogic);
        refreshDisplayTimer = periodicUiTimer;
    }

    public override void StartUp()
    {
        var control = new FileWatchControl();

        viewModel = new FileWatchViewModel();
        control.DataContext = viewModel;

        viewModel.PropertyChanged += HandleViewModelPropertyChanged;

        pluginTab = _tabManager.AddControl(control, "File Watch", TabLocation.RightBottom);
        pluginTab.Hide();

        pluginTab.TabHidden += HandleTabHidden;
        pluginTab.TabShown += HandleTabShown;
        pluginTab.CanClose = true;

        showFileWatchMenuItem = this.AddMenuItem("View", "Show File Watch");
        showFileWatchMenuItem.Click += HandleShowFileWatch;

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

    private void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue) =>
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

    private void HandleShowFileWatch(object? sender, System.Windows.RoutedEventArgs e)
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
