using Gum.Commands;
using Gum.DataTypes;
using Gum.Dialogs;
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
    readonly ISelectedState _selectedState;
    readonly IWireframeCommands _wireframeCommands;
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

    [ImportingConstructor]
    public MainTextureCoordinatePlugin(
        ISelectedState selectedState,
        IWireframeCommands wireframeCommands,
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        ITabManager tabManager,
        IHotkeyManager hotkeyManager,
        IProjectManager projectManager,
        IFileWatchManager fileWatchManager,
        IMessenger messenger,
        IThemingService themingService)
    {
        _selectedState = selectedState;
        _wireframeCommands = wireframeCommands;

        _displayController = new TextureCoordinateDisplayController(
            selectedState,
            undoManager,
            guiCommands,
            fileCommands,
            setVariableLogic,
            tabManager,
            hotkeyManager,
            new ScrollBarLogicWpf(),
            messenger,
            themingService);

        _viewModel = new (
            projectManager,
            fileCommands,
            fileWatchManager,
            guiCommands,
            _displayController);

        messenger.RegisterAll(this);

        // ObjectFinder is the sanctioned static singleton; this Locator call resolves
        // ObjectFinder.Self and is intentionally not drained.
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

    private void HandleTreeNodeSelected(ITreeNode? treeNode)
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
