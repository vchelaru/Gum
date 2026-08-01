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
        // IsLoaded: font generation can run before the main window has completed its Show()
        // sequence (e.g. during initial project load at startup) - Activate() throws
        // InvalidOperationException ("Cannot call DragMove or Activate before a Window is shown")
        // if called that early.
        if (Application.Current?.MainWindow is not { IsLoaded: true } mainWindow)
        {
            return;
        }

        // EnsureHandle forces the Win32 HWND to be created if it doesn't exist yet.
        IntPtr windowHandle = new WindowInteropHelper(mainWindow).EnsureHandle();
        NativeMethods.ForceForegroundWindow(windowHandle);
        mainWindow.Activate();
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Forces <paramref name="hWnd"/> to the foreground even when this process isn't already
        /// foreground. A plain <c>SetForegroundWindow</c> call from a background process is
        /// throttled by Windows and silently no-ops; temporarily attaching this thread's input
        /// state to the currently-foreground window's thread is the documented workaround.
        /// </summary>
        public static void ForceForegroundWindow(IntPtr hWnd)
        {
            uint foregroundThreadId = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            uint currentThreadId = GetCurrentThreadId();

            if (foregroundThreadId != currentThreadId)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, true);
                SetForegroundWindow(hWnd);
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
            else
            {
                SetForegroundWindow(hWnd);
            }
        }
    }
}
