using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Extensions;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.Plugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Xml.Linq;
using ToolsUtilities;
using WpfDataUi.DataTypes;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace Gum.Commands;


public class GuiCommands : IGuiCommands
{
    #region Fields/Properties
    
    private readonly Lazy<ISelectedState> _lazySelectedState;
    private readonly IDispatcher _dispatcher;
    private readonly IOutputManager _outputManager;
    // Lazy because PropertyGridManager depends on IGuiCommands; this breaks the DI construction cycle.
    private readonly Lazy<PropertyGridManager> _lazyPropertyGridManager;
    private readonly IPluginManager _pluginManager;
    private readonly ISpinnerFactory _spinnerFactory;

    private ISelectedState _selectedState => _lazySelectedState.Value;

    #endregion

    public GuiCommands(
        Lazy<ISelectedState> lazySelectedState,
        IDispatcher dispatcher,
        IOutputManager outputManager,
        Lazy<PropertyGridManager> lazyPropertyGridManager,
        IPluginManager pluginManager,
        ISpinnerFactory spinnerFactory)
    {
        _lazySelectedState = lazySelectedState;
        _dispatcher = dispatcher;
        _outputManager = outputManager;
        _lazyPropertyGridManager = lazyPropertyGridManager;
        _pluginManager = pluginManager;
        _spinnerFactory = spinnerFactory;
    }
    
    public void BroadcastRefreshBehaviorView()
    {
        _pluginManager.RefreshBehaviorView(
            _selectedState.SelectedElement);
    }

    #region Refresh Commands

    public void RefreshStateTreeView()
    {
        _pluginManager.RefreshStateTreeView();
    }

    public void RefreshVariables(bool force = false)
    {
        _pluginManager.RefreshVariableView(force);
    }

    /// <summary>
    /// Refreshes the displayed values without clearing and recreating the grid
    /// </summary>
    public void RefreshVariableValues()
    {
        _lazyPropertyGridManager.Value.RefreshVariablesDataGridValues();
    }

    public void RefreshElementTreeView()
    {
        _pluginManager.RefreshElementTreeView();
    }

    public void RefreshElementTreeView(IInstanceContainer instanceContainer)
    {
        _pluginManager.RefreshElementTreeView(instanceContainer);
    }

    #endregion

    public void PrintOutput(string output)
    {
        _dispatcher.Invoke(() => _outputManager.AddOutput(output));
    }

    #region Show/Hide Tools
    
    public void ToggleToolVisibility()
    {
        //var areToolsVisible = mMainWindow.LeftAndEverythingContainer.Panel1Collapsed == false;

        //if(areToolsVisible)
        //{
        //    HideTools();
        //}
        //else
        //{
        //    ShowTools();
        //}
    }


    #endregion

    public void FocusSearch()
    {
        _pluginManager.FocusSearch();
    }

    /// <inheritdoc/>
    public ISpinner ShowSpinner() => _spinnerFactory.Create();

    /// <inheritdoc/>
    public void ActivateMainWindow()
    {
        if (Application.Current?.MainWindow is not { } mainWindow)
        {
            return;
        }

        mainWindow.Activate();

        // Windows throttles Activate() calls from a background process - grant this process
        // permission to steal foreground focus first, then take it directly via Win32.
        // EnsureHandle forces the Win32 HWND to be created if it doesn't exist yet.
        IntPtr windowHandle = new WindowInteropHelper(mainWindow).EnsureHandle();
        NativeMethods.AllowSetForegroundWindow(NativeMethods.ASFW_ANY);
        NativeMethods.SetForegroundWindow(windowHandle);
    }

    private static class NativeMethods
    {
        public const int ASFW_ANY = -1;

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool AllowSetForegroundWindow(int dwProcessId);
    }
}
