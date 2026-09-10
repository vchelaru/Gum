using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Services;
using Gum.ToolStates;

namespace Gum.Avalonia.Services;

/// <summary>
/// Avalonia implementation of <see cref="IGuiCommands"/>. Everything routes through the plugin
/// host and the output manager exactly as the WPF version does; the one framework-specific
/// member, <see cref="ActivateMainWindow"/>, uses Avalonia's window activation instead of Win32.
/// </summary>
public class AvaloniaGuiCommands : IGuiCommands
{
    private readonly Lazy<ISelectedState> _lazySelectedState;
    private readonly IDispatcher _dispatcher;
    private readonly IOutputManager _outputManager;
    private readonly IPluginManager _pluginManager;
    private readonly ISpinnerFactory _spinnerFactory;

    /// <summary>Creates the commands; selected state is lazy to break a construction cycle.</summary>
    public AvaloniaGuiCommands(
        Lazy<ISelectedState> lazySelectedState,
        IDispatcher dispatcher,
        IOutputManager outputManager,
        IPluginManager pluginManager,
        ISpinnerFactory spinnerFactory)
    {
        _lazySelectedState = lazySelectedState;
        _dispatcher = dispatcher;
        _outputManager = outputManager;
        _pluginManager = pluginManager;
        _spinnerFactory = spinnerFactory;
    }

    /// <inheritdoc/>
    public void BroadcastRefreshBehaviorView() => _pluginManager.RefreshBehaviorView(_lazySelectedState.Value.SelectedElement!);

    /// <inheritdoc/>
    public void RefreshStateTreeView() => _pluginManager.RefreshStateTreeView();

    /// <inheritdoc/>
    public void RefreshVariables(bool force = false) => _pluginManager.RefreshVariableView(force);

    /// <inheritdoc/>
    public void RefreshVariableValues()
    {
        // The WPF head refreshes its property grid's values in place here; this head has no grid
        // until phase 70, and a full refresh is the closest equivalent.
        _pluginManager.RefreshVariableView(false);
    }

    /// <inheritdoc/>
    public void RefreshElementTreeView() => _pluginManager.RefreshElementTreeView();

    /// <inheritdoc/>
    public void RefreshElementTreeView(IInstanceContainer instanceContainer) => _pluginManager.RefreshElementTreeView(instanceContainer);

    /// <inheritdoc/>
    public void PrintOutput(string output) => _dispatcher.Invoke(() => _outputManager.AddOutput(output));

    /// <inheritdoc/>
    public void ToggleToolVisibility() { }

    /// <inheritdoc/>
    public void FocusSearch() => _pluginManager.FocusSearch();

    /// <inheritdoc/>
    public void FocusVariableFilter() => _pluginManager.FocusVariableFilter();

    /// <inheritdoc/>
    public ISpinner ShowSpinner() => _spinnerFactory.Create();

    /// <inheritdoc/>
    public void ActivateMainWindow()
    {
        _dispatcher.Post(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { IsVisible: true } window })
            {
                window.Activate();
            }
        });
    }
}
