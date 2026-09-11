using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using EditorTabPlugin_XNA.Services;
using EditorTabPlugin_XNA.ViewModels;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Localization;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.EditorTab;
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
using Gum.DataTypes;
using Gum.Avalonia.Themes;

namespace Gum.Avalonia.Plugins.EditorTab;

/// <summary>
/// The Avalonia head's editor tab: <see cref="EditorTabPluginBase"/> completed with the Avalonia
/// canvas, a code-built toolbar over the shared <see cref="EditorViewModel"/>, Avalonia scroll
/// bars, an Avalonia context menu, and file drops. Render-target shaders have no resolver here
/// yet (the WPF head's compiler ships Windows-only binaries), so shaded containers preview unshaded.
/// </summary>
[Export(typeof(PluginBase))]
public class AvaloniaEditorTabPlugin : EditorTabPluginBase
{
    private WireframeCanvasControl? _canvasControl;
    private readonly ContextMenu _contextMenu = new ContextMenu();

    [ImportingConstructor]
    public AvaloniaEditorTabPlugin(
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
        _canvasControl = new WireframeCanvasControl(dialogService, outputManager, pluginManager);
        _canvasControl.SizeChanged += (_, _) => OnCanvasResized();
        _canvasControl.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            if (e.GetCurrentPoint(_canvasControl).Properties.IsRightButtonPressed)
            {
                ShowCanvasContextMenu();
            }
        }, global::Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
        _canvasControl.AddHandler(InputElement.KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Tab)
            {
                _guiCommands.ToggleToolVisibility();
                // Otherwise Avalonia also moves focus off the canvas.
                e.Handled = true;
            }
        }, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);

        DragDrop.SetAllowDrop(_canvasControl, true);
        _canvasControl.AddHandler(DragDrop.DragEnterEvent, (_, e) => e.DragEffects = DecideDropEffects(e, reportBlockedReason: true));
        _canvasControl.AddHandler(DragDrop.DragOverEvent, (_, e) => e.DragEffects = DecideDropEffects(e, reportBlockedReason: false));
        _canvasControl.AddHandler(DragDrop.DropEvent, (_, e) =>
        {
            HandleWireframeDrop(ReadPayload(e));
            e.Handled = true;
        });
        return _canvasControl.Core;
    }

    /// <inheritdoc/>
    protected override void BuildEditorTab(EditorViewModel editorViewModel, ScrollbarService scrollbarService)
    {
        ScrollBar verticalScrollBar = new ScrollBar { Orientation = Orientation.Vertical, AllowAutoHide = false };
        ScrollBar horizontalScrollBar = new ScrollBar { Orientation = Orientation.Horizontal, AllowAutoHide = false };
        scrollbarService.HandleWireframeInitialized(
            new AvaloniaScrollBarAdapter(horizontalScrollBar),
            new AvaloniaScrollBarAdapter(verticalScrollBar),
            new ControlScrollSurfaceAdapter(_canvasControl!));

        Grid canvasGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            RowDefinitions = new RowDefinitions("*,Auto"),
        };
        canvasGrid.Children.Add(_canvasControl!);
        Grid.SetColumn(verticalScrollBar, 1);
        canvasGrid.Children.Add(verticalScrollBar);
        Grid.SetRow(horizontalScrollBar, 1);
        canvasGrid.Children.Add(horizontalScrollBar);

        TextBlock gridSnapWarning = new TextBlock
        {
            Background = Brushes.Orange,
            Foreground = Brushes.Black,
            Padding = new Thickness(6, 3),
            [!TextBlock.TextProperty] = new Binding(nameof(EditorViewModel.GridSnapWarningText)),
            [!Visual.IsVisibleProperty] = new Binding(nameof(EditorViewModel.HasGridSnapWarning)),
        };

        DockPanel tab = new DockPanel { DataContext = editorViewModel };
        Control toolbar = BuildToolbar();
        DockPanel.SetDock(toolbar, global::Avalonia.Controls.Dock.Top);
        DockPanel.SetDock(gridSnapWarning, global::Avalonia.Controls.Dock.Top);
        tab.Children.Add(toolbar);
        tab.Children.Add(gridSnapWarning);
        tab.Children.Add(canvasGrid);

        _tabManager.AddControl(tab, "Editor", TabLocation.RightTop);
    }

    /// <summary>The zoom, canvas size, font scale, and grid-snap controls the WPF toolbar shows.</summary>
    private static Control BuildToolbar()
    {
        StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(4, 2) };

        panel.Children.Add(SmallButton("-", nameof(EditorViewModel.ZoomOutCommand)));
        panel.Children.Add(new ComboBox
        {
            Width = 100,
            DisplayMemberBinding = new Binding(nameof(ZoomLevel.ZoomDisplay)),
            [!ItemsControl.ItemsSourceProperty] = new Binding(nameof(EditorViewModel.ZoomLevels)),
            [!SelectingItemsControl.SelectedItemProperty] = new Binding(nameof(EditorViewModel.PercentZoomLevel)) { Mode = BindingMode.TwoWay },
        });
        panel.Children.Add(SmallButton("+", nameof(EditorViewModel.ZoomInCommand)));

        panel.Children.Add(new ComboBox
        {
            Width = 180,
            Margin = new Thickness(10, 0, 0, 0),
            DisplayMemberBinding = new Binding(nameof(CustomCanvasSize.FriendlyName)),
            [!ItemsControl.ItemsSourceProperty] = new Binding(nameof(EditorViewModel.CustomCanvasSizes)),
            [!SelectingItemsControl.SelectedItemProperty] = new Binding(nameof(EditorViewModel.SelectedCustomCanvasSize)) { Mode = BindingMode.TwoWay },
        });

        panel.Children.Add(Label("Font Scale:", 20));
        panel.Children.Add(SmallButton("-", nameof(EditorViewModel.FontScaleDecreaseCommand)));
        panel.Children.Add(new TextBlock
        {
            Width = 40,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            [!TextBlock.TextProperty] = new Binding(nameof(EditorViewModel.GlobalFontScaleDisplay)),
        });
        panel.Children.Add(SmallButton("+", nameof(EditorViewModel.FontScaleIncreaseCommand)));

        panel.Children.Add(new CheckBox
        {
            Content = "Snap to Grid",
            Margin = new Thickness(20, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            [!ToggleButton.IsCheckedProperty] = new Binding(nameof(EditorViewModel.SnapToGrid)) { Mode = BindingMode.TwoWay },
        });
        panel.Children.Add(Label("Grid Size:", 10));
        panel.Children.Add(new TextBox
        {
            Width = 40,
            VerticalAlignment = VerticalAlignment.Center,
            [!TextBox.TextProperty] = new Binding(nameof(EditorViewModel.GridSize)) { Mode = BindingMode.TwoWay },
        });
        return panel;
    }

    private static Button SmallButton(string content, string commandPath) => new Button
    {
        Classes = { GumChromeStyles.FlatButtonClass },
        Content = content,
        Width = 24,
        Padding = new Thickness(0),
        HorizontalContentAlignment = HorizontalAlignment.Center,
        [!Button.CommandProperty] = new Binding(commandPath),
    };

    private static TextBlock Label(string text, double leftMargin) => new TextBlock
    {
        Text = text,
        Margin = new Thickness(leftMargin, 0, 4, 0),
        VerticalAlignment = VerticalAlignment.Center,
    };

    /// <inheritdoc/>
    // This head renders through KNI's SDL2/GL backend, so shaders compile to the OpenGL target.
    protected override Func<string, object?>? CreateRenderTargetShaderResolver() =>
        RenderTargetShaderResolver.For(ShadowDusk.Core.PlatformTarget.OpenGL);

    /// <inheritdoc/>
    protected override bool IsContextMenuOpen => _contextMenu.IsOpen;

    /// <inheritdoc/>
    protected override void ShowContextMenu(IReadOnlyList<ContextMenuItemViewModel> items)
    {
        AvaloniaContextMenus.Populate(_contextMenu, items);
        _contextMenu.Open(_canvasControl);
    }

    // Drag and drop glue: files from the OS, a Standards-palette chip by its data format, and nodes
    // or search results dragged out of the element tree (their tags travel in TreeDragPayload).
    private DragDropEffects DecideDropEffects(DragEventArgs e, bool reportBlockedReason) =>
        DecideWireframeDropAccepted(ReadPayload(e), reportBlockedReason) ? DragDropEffects.Copy : DragDropEffects.None;

    private static WireframeDropPayload ReadPayload(DragEventArgs e)
    {
        string? standardElementTypeName = e.DataTransfer.TryGetValue(AvaloniaDragFormats.StandardElementName);
        string[]? files = e.DataTransfer.TryGetFiles()?
            .Select(item => item.TryGetLocalPath())
            .Where(path => path != null)
            .Select(path => path!)
            .ToArray();
        // A folder row's tag is null; it is kept so the drag still reads as a node drag that drops nothing.
        List<object>? nodeTags = e.DataTransfer.Contains(AvaloniaDragFormats.TreeNodes) && TreeDragPayload.Tags is { } tags
            ? tags.Select(tag => tag!).ToList()
            : null;
        return new WireframeDropPayload(standardElementTypeName, nodeTags, files is { Length: > 0 } ? files : null);
    }
}
