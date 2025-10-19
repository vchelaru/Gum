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
    
    private readonly Lazy<ISelectedState> _lazySelectedState;
    private readonly IDispatcher _dispatcher;
    private readonly IOutputManager _outputManager;

    private ISelectedState _selectedState => _lazySelectedState.Value;

    #endregion

    public GuiCommands(
        Lazy<ISelectedState> lazySelectedState, 
        IDispatcher dispatcher, 
        IOutputManager outputManager)
    {
        _lazySelectedState = lazySelectedState;
        _dispatcher = dispatcher;
        _outputManager = outputManager;
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

    public void RefreshElementTreeView()
    {
        PluginManager.Self.RefreshElementTreeView();
    }

    public void RefreshElementTreeView(IInstanceContainer instanceContainer)
    {
        PluginManager.Self.RefreshElementTreeView(instanceContainer);
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
        PluginManager.Self.FocusSearch();
    }

    public Spinner ShowSpinner()
    {
        var spinner = new Gum.Controls.Spinner();
        spinner.Show();

        return spinner;
    }
}
