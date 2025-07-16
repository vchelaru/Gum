using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Gum.DataTypes;
using Gum.Plugins;
using Gum.Controls;
using Gum.Extensions;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins.VariableGrid;
using Gum.ToolCommands;
using CommonFormsAndControls;
using Gum.Undo;
using Gum.Logic;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using System.IO;
using ToolsUtilities;
using WpfDataUi.DataTypes;
using Gum.PropertyGridHelpers;
using System.Xml.Linq;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Microsoft.Extensions.Hosting;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media;
using Gum.Services.Dialogs;
using DialogResult = System.Windows.Forms.DialogResult;

namespace Gum.Commands;

public class GuiCommands
{
    #region Fields/Properties

    public MainWindow MainWindow { get; private set; }

    public System.Windows.Forms.Cursor AddCursor { get; set; }


    MainPanelControl mainPanelControl;

    private readonly ISelectedState _selectedState;

    #endregion

    public GuiCommands()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
    }

    internal void Initialize(MainWindow mainWindow, MainPanelControl mainPanelControl)
    {
        this.MainWindow = mainWindow;
        this.mainPanelControl = mainPanelControl;
    }

    internal void BroadcastRefreshBehaviorView()
    {
        PluginManager.Self.RefreshBehaviorView(
            _selectedState.SelectedElement);
    }

    internal void BroadcastBehaviorReferencesChanged()
    {
        PluginManager.Self.BehaviorReferencesChanged(
            _selectedState.SelectedElement);
    }

    #region Refresh Commands

    internal void RefreshStateTreeView()
    {
        PluginManager.Self.RefreshStateTreeView();
    }

    public void RefreshVariables(bool force = false)
    {
        PluginManager.Self.RefreshVariableView(force);
    }

    /// <summary>
    /// Refreshes the displayed values without clearing and recreating the grid
    /// </summary>
    public void RefreshVariableValues()
    {
        PropertyGridManager.Self.RefreshVariablesDataGridValues();
    }

    const int DefaultFontSize = 11;

    int _uiZoomValue = 100;
    const int MinUiZoomValue = 70;
    const int MaxUiZoomValue = 500;
    public int UiZoomValue
    {
        get => _uiZoomValue;
        set
        {
            if (value > MaxUiZoomValue)
            {
                _uiZoomValue = MaxUiZoomValue;
            }
            else if (value < MinUiZoomValue)
            {
                _uiZoomValue = MinUiZoomValue;
            }
            else
            {
                _uiZoomValue = value;
            }
            UpdateUiToZoomValue();
        }
    }

    private void UpdateUiToZoomValue()
    {
        var fontSize = DefaultFontSize * UiZoomValue / 100.0f;

        mainPanelControl.FontSize = fontSize;

        PluginManager.Self.HandleUiZoomValueChanged();
    }


    public void RefreshElementTreeView()
    {
        PluginManager.Self.RefreshElementTreeView();
    }

    public void RefreshElementTreeView(IInstanceContainer instanceContainer)
    {
        PluginManager.Self.RefreshElementTreeView(instanceContainer);
    }

    #endregion

    #region Tab Controls

    public PluginTab AddControl(System.Windows.FrameworkElement control, string tabTitle, TabLocation tabLocation = TabLocation.CenterBottom)
    {
        CheckForInitialization();
        return mainPanelControl.AddWpfControl(control, tabTitle, tabLocation);
    }

    public void ShowTab(PluginTab tab, bool focus = true) =>
        mainPanelControl.ShowTab(tab, focus);

    public void HideTab(PluginTab tab)
    {
        mainPanelControl.HideTab(tab);
    }

    public PluginTab AddControl(System.Windows.Forms.Control control, string tabTitle, TabLocation tabLocation)
    {
        CheckForInitialization();
        return mainPanelControl.AddWinformsControl(control, tabTitle, tabLocation);
    }

    private void CheckForInitialization()
    {
        if (mainPanelControl == null)
        {
            throw new InvalidOperationException("Need to call Initialize first");
        }
    }

    public PluginTab AddWinformsControl(Control control, string tabTitle, TabLocation tabLocation)
    {
        return mainPanelControl.AddWinformsControl(control, tabTitle, tabLocation);
    }

    public bool IsTabVisible(PluginTab pluginTab)
    {
        return mainPanelControl.IsTabVisible(pluginTab);
    }


    public void RemoveControl(System.Windows.Controls.UserControl control)
    {
        mainPanelControl.RemoveWpfControl(control);
    }

    /// <summary>
    /// Selects the tab which contains the argument control
    /// </summary>
    /// <param name="control">The control to show.</param>
    /// <returns>Whether the control was shown. If the control is not found, false is returned.</returns>
    public bool ShowTabForControl(System.Windows.Controls.UserControl control)
    {
        return mainPanelControl.ShowTabForControl(control);
    }


    internal bool IsTabFocused(PluginTab pluginTab) =>
                    mainPanelControl.IsTabFocused(pluginTab);

    #endregion

    #region Move to Cursor

    public void PositionWindowByCursor(System.Windows.Forms.Form window)
    {
        var mousePosition = GumCommands.Self.GuiCommands.GetMousePosition();

        window.Location = new System.Drawing.Point(mousePosition.X - window.Width / 2, mousePosition.Y - window.Height / 2);
    }

    public void MoveToCursor(System.Windows.Window window)
    {
        window.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;

        double width = window.Width;
        if (double.IsNaN(width))
        {
            width = 0;
        }
        double height = window.Height;
        if (double.IsNaN(height))
        {
            // Let's just assume some small height so it doesn't appear down below the cursor:
            //height = 0;
            height = 64;
        }

        var scaledX = MainWindow.LogicalToDeviceUnits(System.Windows.Forms.Control.MousePosition.X);

        var source = System.Windows.PresentationSource.FromVisual(mainPanelControl);


        double mousePositionX = Control.MousePosition.X;
        double mousePositionY = Control.MousePosition.Y;

        if (source != null)
        {
            mousePositionX /= source.CompositionTarget.TransformToDevice.M11;
            mousePositionY /= source.CompositionTarget.TransformToDevice.M22;
        }

        window.Left = mousePositionX - width / 2;
        window.Top = mousePositionY - height / 2;

        window.ShiftWindowOntoScreen();
    }
    #endregion

    public void PrintOutput(string output)
    {
        DoOnUiThread(() => OutputManager.Self.AddOutput(output));
    }

    #region Show/Hide Tools

    public System.Drawing.Point GetMousePosition()
    {
        return MainWindow.MousePosition;
    }

    public void HideTools()
    {
        mainPanelControl.HideTools();
    }

    public void ShowTools()
    {
        mainPanelControl.ShowTools();
    }


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

    internal void FocusSearch()
    {
        PluginManager.Self.FocusSearch();
    }

    public Spinner ShowSpinner()
    {
        var spinner = new Gum.Controls.Spinner();
        spinner.Show();

        return spinner;
    }

    public void DoOnUiThread(Action action)
    {
        mainPanelControl.Dispatcher.Invoke(action);
    }

}
