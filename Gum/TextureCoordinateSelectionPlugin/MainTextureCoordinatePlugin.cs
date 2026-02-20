using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.Messaging;
using TextureCoordinateSelectionPlugin.Logic;
using TextureCoordinateSelectionPlugin.Models;
using TextureCoordinateSelectionPlugin.ViewModels;

namespace TextureCoordinateSelectionPlugin;

[Export(typeof(PluginBase))]
public class MainTextureCoordinatePlugin : PluginBase, IRecipient<UiBaseFontSizeChangedMessage>
{
    #region Fields/Properties

    PluginTab textureCoordinatePluginTab = default!;
    ISelectedState _selectedState;
    IWireframeCommands _wireframeCommands;
    TextureCoordinateDisplayController _displayController;
    MainControlViewModel _viewModel;
    ExposedTextureCoordinateLogic _exposedCoordinateLogic;

    public override string FriendlyName
    {
        get
        {
            return "Texture Coordinate Selection Plugin";
        }
    }

    public override Version Version
    {
        get => new Version(1, 0, 0);
    }

    #endregion

    public MainTextureCoordinatePlugin()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _wireframeCommands = Locator.GetRequiredService<IWireframeCommands>();

        _displayController = new TextureCoordinateDisplayController(
            Locator.GetRequiredService<ISelectedState>(),
            Locator.GetRequiredService<IUndoManager>(),
            Locator.GetRequiredService<IGuiCommands>(),
            Locator.GetRequiredService<IFileCommands>(),
            Locator.GetRequiredService<ISetVariableLogic>(),
            Locator.GetRequiredService<ITabManager>(),
            Locator.GetRequiredService<IHotkeyManager>(),
            new ScrollBarLogicWpf());

        _viewModel = new (
            Locator.GetRequiredService<IProjectManager>(),
            Locator.GetRequiredService<IFileCommands>(),
            Locator.GetRequiredService<IFileWatchManager>(),
            Locator.GetRequiredService<IGuiCommands>(),
            _displayController);
        


        Locator.GetRequiredService<IMessenger>().RegisterAll(this);


        _exposedCoordinateLogic = new ExposedTextureCoordinateLogic(
            Locator.GetRequiredService<IObjectFinder>());
    }

    public override bool ShutDown(PluginShutDownReason shutDownReason)
    {
        if (textureCoordinatePluginTab is not null)
        {
            RemoveTab(textureCoordinatePluginTab);
        }

        _displayController?.Dispose();

        return true;
    }

    public override void StartUp()
    {
        textureCoordinatePluginTab = _displayController.CreateControl(_viewModel, out var availableZoomLevels);
        _viewModel.AvailableZoomLevels = availableZoomLevels;
        textureCoordinatePluginTab.Hide();
        textureCoordinatePluginTab.GotFocus += HandleTabShown;

        AssignEvents();
    }

    private void HandleTabShown()
    {
        _displayController.CenterCameraOnSelection();
    }

    void IRecipient<UiBaseFontSizeChangedMessage>.Receive(UiBaseFontSizeChangedMessage message)
    {
        _displayController.UpdateButtonSizes(message.Size);
    }

    private void AssignEvents()
    {
        this.TreeNodeSelected += HandleTreeNodeSelected;

        this.VariableSetLate += HandleVariableSet;
        // This is needed for when undos happen
        this.WireframeRefreshed += HandleWireframeRefreshed;
        this.WireframePropertyChanged += HandleWireframePropertyChanged;

        this.ProjectLoad += HandleProjectLoaded;
    }

    private void HandleProjectLoaded(GumProjectSave save)
    {
        _viewModel.LoadSettings();
        _displayController.SetCheckerboardVisible(save.ShowCheckerBackground);
    }

    private void HandleWireframePropertyChanged(string name)
    {
        if (name == nameof(IWireframeCommands.IsBackgroundGridVisible))
        {
            _displayController.SetCheckerboardVisible(_wireframeCommands.IsBackgroundGridVisible);
        }
    }

    private void HandleWireframeRefreshed()
    {
        var element = _selectedState.SelectedElement;
        if (_selectedState.SelectedInstance != null)
        {
            element = ObjectFinder.Self.GetElementSave(_selectedState.SelectedInstance);
        }

        var hasTextureCoordinates = element != null && _exposedCoordinateLogic.IsDirectSpriteOrNineSlice(element);

        if (!hasTextureCoordinates && _selectedState.SelectedInstance != null && element != null)
        {
            var sets = _exposedCoordinateLogic.GetExposedSets(element);
            _viewModel.UpdateExposedSources(sets, preserveSelection: true);
            hasTextureCoordinates = sets.Count > 0;
        }
        else
        {
            _viewModel.UpdateExposedSources(new List<ExposedTextureCoordinateSet>(), preserveSelection: false);
        }

        if (hasTextureCoordinates)
        {
            textureCoordinatePluginTab.Show();
            _displayController.Refresh();
        }
        else
        {
            textureCoordinatePluginTab.Hide();
        }
    }

    private void HandleTreeNodeSelected(TreeNode? treeNode)
    {
        _displayController.Refresh();
        _displayController.CenterCameraOnSelection();
    }

    private void HandleVariableSet(ElementSave element, InstanceSave? instance, string variableName, object? oldValue)
    {
        _displayController.Refresh();
        _displayController.RefreshSelector(Logic.RefreshType.Force);
    }
}
