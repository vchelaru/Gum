using CommunityToolkit.Mvvm.Messaging;
using EditorTabPlugin_XNA.Services;
using EditorTabPlugin_XNA.ViewModels;
using EditorTabPlugin_XNA.Views;
using Gum.Commands;
using Gum.Controls;
using Gum.Dialogs;
using Gum.Extensions;
using Gum.Localization;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.EditorTab.Views;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.ScrollBarPlugin;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.ViewModels;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WpfScrollBar = System.Windows.Controls.Primitives.ScrollBar;

namespace Gum.Plugins.InternalPlugins.EditorTab;

/// <summary>
/// The WPF head's editor tab: <see cref="EditorTabPluginBase"/> completed with the WPF canvas,
/// the XAML toolbar, WPF scroll bars, a WPF context menu, OLE drag and drop, and the ShadowDusk
/// shader resolver.
/// </summary>
[Export(typeof(PluginBase))]
internal class MainEditorTabPlugin : EditorTabPluginBase
{
    private WireframeControl? _wireframeControl;
    private EditorControls? _editorControls;
    private readonly ContextMenu _wireframeContextMenu = new ContextMenu();

    [ImportingConstructor]
    public MainEditorTabPlugin(
        ISelectedState selectedState,
        IProjectManager projectManager,
        IGuiCommands guiCommands,
        IOutputManager outputManager,
        LocalizationService localizationService,
        IReorderLogic reorderLogic,
        INameVerifier nameVerifier,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        IWireframeObjectManager wireframeObjectManager,
        FileLocations fileLocations,
        IUndoManager undoManager,
        IDialogService dialogService,
        IHotkeyManager hotkeyManager,
        IElementCommands elementCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IUiSettingsService uiSettingsService,
        WireframeCommands wireframeCommands,
        IMessenger messenger,
        IThemingService themingService,
        IDragDropManager dragDropManager,
        ICircularReferenceManager circularReferenceManager,
        IFavoriteComponentManager favoriteComponentManager,
        IPluginManager pluginManager)
        : base(selectedState, projectManager, guiCommands, outputManager, localizationService, reorderLogic,
            nameVerifier, variableInCategoryPropagationLogic, wireframeObjectManager, fileLocations, undoManager,
            dialogService, hotkeyManager, elementCommands, fileCommands, setVariableLogic, uiSettingsService,
            wireframeCommands, messenger, themingService, dragDropManager, circularReferenceManager,
            favoriteComponentManager, pluginManager)
    {
    }

    /// <inheritdoc/>
    protected override WireframeCanvasCore CreateCanvas(IDialogService dialogService, IOutputManager outputManager, IPluginManager pluginManager)
    {
        _wireframeControl = new WireframeControl(dialogService, outputManager, pluginManager)
        {
            AllowDrop = true,
            Name = "wireframeControl1",
        };
        _wireframeControl.SizeChanged += (_, _) => OnCanvasResized();
        _wireframeControl.MouseDown += (_, e) =>
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                ShowCanvasContextMenu();
            }
        };
        _wireframeControl.Drop += OnWireframeDrop;
        _wireframeControl.DragEnter += OnWireframeDragEnter;
        // Unlike WinForms, WPF does not carry the effect chosen on DragEnter forward into each
        // DragOver, so the same decision has to be re-applied per move or the drop goes invalid.
        _wireframeControl.DragOver += OnWireframeDragOver;
        _wireframeControl.KeyDown += (_, args) =>
        {
            if (args.Key == Key.Tab)
            {
                _guiCommands.ToggleToolVisibility();
                // Otherwise WPF also moves focus off the canvas.
                args.Handled = true;
            }
        };
        return _wireframeControl.Core;
    }

    /// <inheritdoc/>
    protected override void BuildEditorTab(EditorViewModel editorViewModel, ScrollbarService scrollbarService)
    {
        WpfScrollBar verticalScrollBar = new WpfScrollBar { Orientation = Orientation.Vertical };
        WpfScrollBar horizontalScrollBar = new WpfScrollBar { Orientation = Orientation.Horizontal };
        scrollbarService.HandleWireframeInitialized(
            new WpfScrollBarAdapter(horizontalScrollBar),
            new WpfScrollBarAdapter(verticalScrollBar),
            new FrameworkElementScrollSurfaceAdapter(_wireframeControl!));

        Grid wpfGrid = new();
        wpfGrid.RowDefinitions.Add(new() { Height = GridLength.Auto });
        wpfGrid.RowDefinitions.Add(new() { Height = GridLength.Auto });
        wpfGrid.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star) });

        _editorControls = new EditorControls();
        wpfGrid.Children.Add(_editorControls);
        Grid.SetRow(_editorControls, 0);

        GridSnapWarningBar gridSnapWarningBar = new();
        wpfGrid.Children.Add(gridSnapWarningBar);
        Grid.SetRow(gridSnapWarningBar, 1);

        wpfGrid.Children.Add(CreateCanvasGrid(verticalScrollBar, horizontalScrollBar));

        _tabManager.AddControl(wpfGrid, "Editor", TabLocation.RightTop);
        wpfGrid.DataContext = editorViewModel;
    }

    /// <summary>
    /// Builds the canvas plus its scroll bars - vertical along the right edge, horizontal along the
    /// bottom - and places it in the editor tab's third row.
    /// </summary>
    private Grid CreateCanvasGrid(WpfScrollBar verticalScrollBar, WpfScrollBar horizontalScrollBar)
    {
        Grid canvasGrid = new();
        canvasGrid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) });
        canvasGrid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        canvasGrid.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star) });
        canvasGrid.RowDefinitions.Add(new() { Height = GridLength.Auto });

        canvasGrid.Children.Add(_wireframeControl!);

        canvasGrid.Children.Add(verticalScrollBar);
        Grid.SetColumn(verticalScrollBar, 1);

        canvasGrid.Children.Add(horizontalScrollBar);
        Grid.SetRow(horizontalScrollBar, 1);

        Grid.SetRow(canvasGrid, 2);
        return canvasGrid;
    }

    /// <inheritdoc/>
    protected override Func<string, object?>? CreateRenderTargetShaderResolver() => RenderTargetShaderResolver.Resolve;

    /// <inheritdoc/>
    protected override bool IsContextMenuOpen => _wireframeContextMenu.IsOpen;

    /// <inheritdoc/>
    protected override void ShowContextMenu(IReadOnlyList<ContextMenuItemViewModel> items)
    {
        _wireframeContextMenu.Items.Clear();
        foreach (ContextMenuItemViewModel item in items)
        {
            _wireframeContextMenu.Items.Add(item.ToMenuItem());
        }
        _wireframeContextMenu.Placement = PlacementMode.MousePoint;
        _wireframeContextMenu.IsOpen = true;
    }

    /// <inheritdoc/>
    protected override void OnUiBaseFontSizeChanged(double size) => _editorControls?.UpdateButtonSizes(size);

    // WPF glue only - the decision and the drop handling live in the base.
    private void OnWireframeDragEnter(object? sender, DragEventArgs e)
    {
        e.Effects = DecideWireframeDropEffect(
            WpfWireframeDropPayloadReader.Read(e.Data), reportBlockedReason: true);
        e.Handled = true;
    }

    private void OnWireframeDragOver(object? sender, DragEventArgs e)
    {
        // Same decision as DragEnter, but silent: a rejected drag would otherwise report its reason
        // on every mouse move rather than once per drag.
        e.Effects = DecideWireframeDropEffect(
            WpfWireframeDropPayloadReader.Read(e.Data), reportBlockedReason: false);
        e.Handled = true;
    }

    private void OnWireframeDrop(object? sender, DragEventArgs e)
    {
        HandleWireframeDrop(WpfWireframeDropPayloadReader.Read(e.Data));
        e.Handled = true;
    }

    internal DragDropEffects DecideWireframeDropEffect(WireframeDropPayload payload, bool reportBlockedReason) =>
        DecideWireframeDropAccepted(payload, reportBlockedReason) ? DragDropEffects.Copy : DragDropEffects.None;
}
