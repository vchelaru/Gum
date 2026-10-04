using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
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
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Localization;
using Gum.Logic;
using Gum.Logic.FileWatch;
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
/// bars, an Avalonia context menu, and file drops. Render-target shaders compile for OpenGL, since
/// this head renders through KNI's SDL2/GL backend.
/// </summary>
[Export(typeof(PluginBase))]
public class AvaloniaEditorTabPlugin : EditorTabPluginBase, IRecipient<EditorCanvasFrameRequestMessage>, IRecipient<EditorCanvasStateRequestMessage>
{
    private readonly ICanvasRedrawScheduler _canvasRedrawScheduler;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private WireframeCanvasControl? _canvasControl;
    private EditorToolbar? _toolbar;
    private readonly ContextMenu _contextMenu = new ContextMenu();
    private static readonly TimeSpan ContextMenuFrameWait = TimeSpan.FromMilliseconds(250);
    private bool _isContextMenuPending;

    [ImportingConstructor]
    public AvaloniaEditorTabPlugin(
        ISelectedState selectedState,
        IProjectManager projectManager,
        IGuiCommands guiCommands,
        IOutputManager outputManager,
        LocalizationService localizationService,
        IReorderLogic reorderLogic,
        IAddInstanceLogic addInstanceLogic,
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
        IPluginManager pluginManager,
        IFileWatchIgnoreList fileWatchIgnoreList,
        IProjectState projectState,
        ICanvasRedrawScheduler canvasRedrawScheduler)
        : base(selectedState, projectManager, guiCommands, outputManager, localizationService, reorderLogic,
            addInstanceLogic, variableInCategoryPropagationLogic, wireframeObjectManager, fileLocations, undoManager,
            dialogService, hotkeyManager, elementCommands, fileCommands, setVariableLogic, uiSettingsService,
            wireframeCommands, messenger, themingService, dragDropManager, circularReferenceManager,
            favoriteComponentManager, pluginManager, fileWatchIgnoreList, projectState)
    {
        _canvasRedrawScheduler = canvasRedrawScheduler;
        _wireframeObjectManager = wireframeObjectManager;
    }

    /// <inheritdoc/>
    protected override WireframeCanvasCore CreateCanvas(IDialogService dialogService, IOutputManager outputManager, IPluginManager pluginManager)
    {
        _canvasControl = new WireframeCanvasControl(dialogService, outputManager, pluginManager, _canvasRedrawScheduler);
        // Changes that arrive without input: a project load, a file changed on disk, an async
        // reload. All of them end in a wireframe refresh.
        WireframeRefreshed += _canvasRedrawScheduler.RequestRedraw;
        _canvasRedrawScheduler.AddContinuousRedrawSource(() => CanvasAnimationActivity.IsAnimating(_wireframeObjectManager.RootGue));
        _canvasControl.SizeChanged += (_, _) => OnCanvasResized();
        _canvasControl.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            if (e.GetCurrentPoint(_canvasControl).Properties.IsRightButtonPressed)
            {
                ShowCanvasContextMenuAfterNextFrame();
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

    // The canvas reads the right press on its next frame, which selects what is under the pointer,
    // so the menu opens after that frame and is for that selection. A menu opened at the press takes
    // the pointer before the canvas sees the button down, and acts on the previous selection. A
    // canvas that is not drawing (a failed frame retries slowly) still gets its menu, after a
    // short wait; presses while one menu is pending open no second one.
    private async void ShowCanvasContextMenuAfterNextFrame()
    {
        if (_isContextMenuPending)
        {
            return;
        }
        _isContextMenuPending = true;
        try
        {
            await Task.WhenAny(_canvasControl!.NextFramePresentedAsync(), Task.Delay(ContextMenuFrameWait));
            ShowCanvasContextMenu();
        }
        finally
        {
            _isContextMenuPending = false;
        }
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
        EditorToolbar toolbar = new EditorToolbar();
        _toolbar = toolbar;
        DockPanel.SetDock(toolbar, global::Avalonia.Controls.Dock.Top);
        DockPanel.SetDock(gridSnapWarning, global::Avalonia.Controls.Dock.Top);
        tab.Children.Add(toolbar);
        tab.Children.Add(gridSnapWarning);
        tab.Children.Add(canvasGrid);

        _tabManager.AddControl(tab, "Editor", TabLocation.RightTop);
    }

    void IRecipient<EditorCanvasFrameRequestMessage>.Receive(EditorCanvasFrameRequestMessage message)
    {
        message.Reply(WaitForCanvasFrameAsync());
    }

    void IRecipient<EditorCanvasStateRequestMessage>.Receive(EditorCanvasStateRequestMessage message)
    {
        message.Reply(_canvasControl?.DescribeFrameState() ?? "the Editor canvas was never created");
    }

    private async Task<bool> WaitForCanvasFrameAsync()
    {
        if (_canvasControl == null)
        {
            throw new InvalidOperationException("The Editor canvas has not been created, so it cannot draw a frame.");
        }
        await _canvasControl.NextFramePresentedAsync();
        return true;
    }

    /// <summary>The toolbar above the canvas, for tests; null until the editor tab is built.</summary>
    internal EditorToolbar? Toolbar => _toolbar;

    /// <summary>The wireframe canvas, for tests; null until the editor tab is built.</summary>
    internal WireframeCanvasControl? CanvasControl => _canvasControl;

    /// <summary>The canvas's right-click menu, for tests.</summary>
    internal ContextMenu CanvasContextMenu => _contextMenu;

    /// <summary>
    /// True from a right press until its menu has been opened (or found nothing to show), for
    /// tests. The menu opens on a dispatcher job posted from the thread pool, so a test cannot
    /// know it has run from the frames it drew alone.
    /// </summary>
    internal bool IsCanvasContextMenuPending => _isContextMenuPending;

    /// <inheritdoc/>
    protected override void OnUiBaseFontSizeChanged(double size) => _toolbar?.UpdateButtonSizes(size);

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
