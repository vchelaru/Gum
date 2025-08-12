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

public class GuiCommands : IGuiCommands
{
    #region Fields/Properties

    public System.Windows.Forms.Cursor AddCursor { get; set; }


    MainPanelControl mainPanelControl;

    private readonly Lazy<ISelectedState> _lazySelectedState;
    private ISelectedState _selectedState => _lazySelectedState.Value;

    #endregion

    public GuiCommands(Lazy<ISelectedState> lazySelectedState)
    {
        _lazySelectedState = lazySelectedState;
    }

    public void Initialize(MainPanelControl mainPanelControl)
    {
        this.mainPanelControl = mainPanelControl;
    }

    public void BroadcastRefreshBehaviorView()
    {
        PluginManager.Self.RefreshBehaviorView(
            _selectedState.SelectedElement);
    }

    #region Refresh Commands

    public void RefreshStateTreeView()
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

    #region Move to Cursor

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
