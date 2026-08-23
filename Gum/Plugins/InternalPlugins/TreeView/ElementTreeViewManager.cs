using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Input;
using Gum.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Mvvm;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Plugins.InternalPlugins.TreeView.ViewModels;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.SelectionHistory;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using static Gum.Managers.TreeNodeImageIndices;
using MaterialDesignThemes.Wpf;
using RenderingLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ToolsUtilities;
using Application = System.Windows.Application;
using Binding = System.Windows.Data.Binding;
using Grid = System.Windows.Controls.Grid;
using WpfInput = System.Windows.Input;

namespace Gum.Managers;

// Nodes implement ITreeNode directly now, so no per-access wrapper type is needed.

public partial class ElementTreeViewManager : IRecipient<ThemeChangedMessage>, IRecipient<ApplicationStartupMessage>, IElementTreeRoots
{
    #region Fields

    private readonly ISelectedState _selectedState;
    private readonly IEditCommands _editCommands;
    private readonly IGuiCommands _guiCommands;
    private readonly IDialogService _dialogService;
    private readonly IFileCommands _fileCommands;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly ITabManager _tabManager;
    private readonly ICircularReferenceManager _circularReferenceManager;
    private readonly IFavoriteComponentManager _favoriteComponentManager;
    private readonly ISelectionHistory _selectionHistory;
    private readonly ElementTreeViewCreator _viewCreator;

    // The *ImageIndex constants and the node icon-decision logic live on
    // TreeNodeImageIndices/TreeNodeImageLogic (Gum.Presentation, accessed via _treeNodeImageLogic
    // and the using static below) so they can be unit-tested without a tree and referenced from
    // headless code.

    // Forwarding properties for UI controls owned by _viewCreator
    internal GumTreeView ObjectTreeView => _viewCreator.ObjectTreeView;
    private System.Windows.Controls.ContextMenu _contextMenu => _viewCreator.ContextMenu;
    private FlatSearchListBox FlatList => _viewCreator.FlatList;
    private System.Windows.Controls.TextBox searchTextBox => _viewCreator.SearchTextBox;
    private System.Windows.Controls.CheckBox deepSearchCheckBox => _viewCreator.DeepSearchCheckBox;

    internal void UpdateCollapseButtonSizes(double baseFontSize) =>
        _viewCreator.UpdateCollapseButtonSizes(baseFontSize);

    /// <summary>
    /// The tree's top-level nodes. This is the tree's real root collection rather than the four
    /// m*GumTreeNode fields, because Standard Elements is conditionally absent from the tree while its
    /// field stays populated.
    /// </summary>
    internal IReadOnlyList<ITreeNode> RootTreeNodes => ObjectTreeView.Nodes.ToList<ITreeNode>();

    ITreeNodeMutable mScreensTreeNode;
    ITreeNodeMutable mComponentsTreeNode;
    ITreeNodeMutable mStandardElementsTreeNode;
    ITreeNodeMutable mBehaviorsTreeNode;
    GumTreeNode? mLastHoveredNode;
    private DateTime? hoverStartTime;

    private WpfInput.Cursor AddCursor { get; }


    /// <summary>
    /// Used to store off what was previously selected
    /// when the tree view refreshes itself - so the user
    /// doesn't lose the old selection.
    /// </summary>
    object? mRecordedSelectedObject;

    /// <summary>
    /// When the recorded selection is an instance, this stores the behavior or element
    /// that owned it at record time. Used as a fallback container for name-based node
    /// lookup after undo/redo replaces instance objects with deep-cloned snapshots,
    /// making reference-based searches fail.
    /// </summary>
    object? mRecordedSelectedContainer;

    /// <summary>
    /// The full set of selected instances captured at record time when more than one
    /// instance is selected. Used to restore a multi-selection after a tree refresh so
    /// it does not collapse to the single primary instance.
    /// </summary>
    List<InstanceSave> _recordedSelectedInstances;
    #endregion

    #region Properties

    public ITreeNode? SelectedNode
    {
        get
        {
            // This could be called before the tree is created:
            if (ObjectTreeView?.SelectedNode == null)
            {
                return null;
            }
            else
            {
                return  ObjectTreeView.SelectedNode;
            }
        }
        set
        {
            ObjectTreeView.SelectedNode = value as GumTreeNode;
        }
    }

    public List<ITreeNode> SelectedNodes
    {
        get
        {
            return ObjectTreeView.SelectedNodes.Select(item => item).ToList<ITreeNode>();
        }
    }

    string filterText;
    public string FilterText
    {
        get => filterText;
        set 
        {
            if(value != filterText)
            {
                filterText = value;
                ReactToFilterTextChanged();

            }
        }
    }


    private void SelectFirstElement()
    {
        GumTreeNode treeNode = 
            ObjectTreeView.Nodes.FirstOrDefault() as GumTreeNode;

        while(treeNode != null)
        {
            if (treeNode.Tag != null)
            {
                Select(treeNode);
                break;
            }
            else
            {
                treeNode = treeNode.NextVisibleNode;
            }
        }
    }

    public ITreeNodeMutable RootScreensTreeNode => mScreensTreeNode;

    public ITreeNodeMutable RootComponentsTreeNode => mComponentsTreeNode;

    public ITreeNodeMutable RootStandardElementsTreeNode => mStandardElementsTreeNode;

    public ITreeNodeMutable RootBehaviorsTreeNode => mBehaviorsTreeNode;

    // The four root fields are ITreeNodeMutable, which extends ITreeNode, so these satisfy
    // IElementTreeRoots directly.
    ITreeNode? IElementTreeRoots.Screens => mScreensTreeNode;
    ITreeNode? IElementTreeRoots.Components => mComponentsTreeNode;
    ITreeNode? IElementTreeRoots.StandardElements => mStandardElementsTreeNode;
    ITreeNode? IElementTreeRoots.Behaviors => mBehaviorsTreeNode;

    private IDragDropManager _dragDropManager;
    private readonly ICopyPasteLogic _copyPasteLogic;
    private readonly IMessenger _messenger;
    private readonly IDeleteLogic _deleteLogic;
    private readonly IUndoManager _undoManager;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IFileLocations _fileLocations;
    private readonly IElementCommands _elementCommands;
    private readonly INameVerifier _nameVerifier;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly IProjectState _projectState;
    private readonly ICollapseToggleService _collapseToggleService;
    private readonly TreeNodeImageLogic _treeNodeImageLogic;
    private readonly StandardElementsManagerGumTool _standardElementsManagerGumTool;
    private readonly IPluginManager _pluginManager;
    private readonly IDispatcher _dispatcher;

    public bool HasMouseOver
    {
        get
        {
            System.Windows.Point position = WpfInput.Mouse.GetPosition(ObjectTreeView);
            return position.X >= 0 && position.Y >= 0 &&
                position.X <= ObjectTreeView.ActualWidth && position.Y <= ObjectTreeView.ActualHeight;
        }
    }

    #endregion

    public ElementTreeViewManager(
        ISelectedState selectedState,
        IEditCommands editCommands,
        IGuiCommands guiCommands,
        IDialogService dialogService,
        IFileCommands fileCommands,
        IHotkeyManager hotkeyManager,
        ITabManager tabManager,
        ICopyPasteLogic copyPasteLogic,
        IMessenger messenger,
        IDeleteLogic deleteLogic,
        IUndoManager undoManager,
        IWireframeObjectManager wireframeObjectManager,
        IFileLocations fileLocations,
        IElementCommands elementCommands,
        INameVerifier nameVerifier,
        ISetVariableLogic setVariableLogic,
        ICircularReferenceManager circularReferenceManager,
        IFavoriteComponentManager favoriteComponentManager,
        ISelectionHistory selectionHistory,
        IProjectState projectState,
        StandardElementsManagerGumTool standardElementsManagerGumTool,
        IDragDropManager dragDropManager,
        IPluginManager pluginManager,
        IDispatcher dispatcher)
    {
        _selectedState = selectedState;
        _editCommands = editCommands;
        _guiCommands = guiCommands;
        _dialogService = dialogService;
        _fileCommands = fileCommands;
        _hotkeyManager = hotkeyManager;
        _tabManager = tabManager;
        _copyPasteLogic = copyPasteLogic;
        _messenger = messenger;
        _messenger.RegisterAll(this);
        _deleteLogic = deleteLogic;
        _undoManager = undoManager;
        _wireframeObjectManager = wireframeObjectManager;
        _fileLocations = fileLocations;
        _elementCommands = elementCommands;
        _nameVerifier = nameVerifier;
        _setVariableLogic = setVariableLogic;
        _circularReferenceManager = circularReferenceManager;
        _favoriteComponentManager = favoriteComponentManager;
        _selectionHistory = selectionHistory;
        _projectState = projectState;
        _standardElementsManagerGumTool = standardElementsManagerGumTool;
        _pluginManager = pluginManager;
        _dispatcher = dispatcher;
        _collapseToggleService = new CollapseToggleService();
        _treeNodeImageLogic = new TreeNodeImageLogic();
        _recordedSelectedInstances = new List<InstanceSave>();
        AddCursor = GetAddCursor();
        _dragDropManager = dragDropManager;
        _viewCreator = new ElementTreeViewCreator();

        WpfInput.Cursor GetAddCursor()
        {
            try
            {
                using System.IO.Stream? stream = typeof(Gum.Program).Assembly
                    .GetManifestResourceStream("Gum.Content.Cursors.AddCursor.cur");

                return stream != null ? new WpfInput.Cursor(stream) : WpfInput.Cursors.Arrow;
            }
            catch
            {
                // This has crashed on at least one machine. It is only a cursor, so tolerate it.
                return WpfInput.Cursors.Arrow;
            }
        }
    }

    #region Methods


    #region Find/Get

    // Every method in this region delegates to the headless ITreeNode/IElementTreeRoots-typed search
    // extensions in Gum.Presentation (TreeNodeSearchExtensions/TreeNodeRootSearchExtensions/
    // TreeNodeDirectoryExtensions), casting the result back to the concrete GumTreeNode. The cast is
    // always safe: every node this class constructs is a GumTreeNode.
    public GumTreeNode? GetTreeNodeFor(ElementSave? elementSave) =>
        (GumTreeNode?)((IElementTreeRoots)this).GetTreeNodeFor(elementSave);

    public GumTreeNode GetTreeNodeFor(ScreenSave screenSave) =>
        (GumTreeNode)((IElementTreeRoots)this).GetTreeNodeFor(screenSave);

    public GumTreeNode GetTreeNodeFor(ComponentSave componentSave) =>
        (GumTreeNode)((IElementTreeRoots)this).GetTreeNodeFor(componentSave);

    public GumTreeNode GetTreeNodeFor(StandardElementSave standardElementSave) =>
        (GumTreeNode)((IElementTreeRoots)this).GetTreeNodeFor(standardElementSave);

    public GumTreeNode? GetTreeNodeFor(InstanceSave instanceSave, GumTreeNode container) =>
        (GumTreeNode?)((ITreeNode)container).GetTreeNodeFor(instanceSave);

    public GumTreeNode? GetInstanceTreeNodeByName(string name, GumTreeNode container) =>
        (GumTreeNode?)((ITreeNode)container).GetInstanceTreeNodeByName(name);

    public GumTreeNode GetTreeNodeFor(BehaviorSave behavior) =>
        (GumTreeNode)((IElementTreeRoots)this).GetTreeNodeFor(behavior);

    public void UpdateErrorIndicatorsForElement(ElementSave element, bool hasErrors)
    {
        var treeNode = GetTreeNodeFor(element);
        if (treeNode == null) return;

        int desiredIndex = _treeNodeImageLogic.GetElementRefreshImageIndex(element, hasErrors);

        if (treeNode.ImageIndex != desiredIndex)
            treeNode.ImageIndex = desiredIndex;
    }

    public GumTreeNode? GetTreeNodeFor(string absoluteDirectory) =>
        (GumTreeNode?)((IElementTreeRoots)this).GetTreeNodeFor(
            absoluteDirectory,
            FileManager.GetDirectory(_projectState.GumProjectSave.FullFileName));

    public ITreeNode? GetTreeNodeOver()
    {
        var nodeAtPoint = ObjectTreeView.GetNodeAt(WpfInput.Mouse.GetPosition(ObjectTreeView));

        if(nodeAtPoint == null)
        {
            return null;
        }
        else
        {
            return nodeAtPoint;
        }
    }

    #endregion
    

    public void Initialize()
    {
        var grid = _viewCreator.CreateView(
            onFilterTextChanged: text => FilterText = text,
            onSearchNodeSelected: HandleSelectedSearchNode,
            onCollapseAll: () => _collapseToggleService.HandleCollapseAll(RootTreeNodes, () => _viewCreator.CollapseAll()),
            onCollapseToElementLevel: () => _collapseToggleService.HandleCollapseToElementLevel(RootTreeNodes, () => _viewCreator.CollapseToElementLevel()),
            onDeepSearchChecked: () => ReactToFilterTextChanged());

        _tabManager.AddControl(grid, "Project", TabLocation.Left);

        WireTreeViewEvents();

        ConfigureStandardsPalette();

        RefreshUi();
    }

    private void WireTreeViewEvents()
    {
        ObjectTreeView.AfterClickSelect += ObjectTreeView_AfterClickSelect;
        ObjectTreeView.AfterSelect += ObjectTreeView_AfterSelect;
        ObjectTreeView.KeyDown += ObjectTreeView_KeyDown;
        ObjectTreeView.ContextMenuOpening += ObjectTreeView_ContextMenuOpening;
        ObjectTreeView.ContextMenu = _contextMenu;
        ObjectTreeView.NodeExpansionChangedByUser += (_, _) => _collapseToggleService.OnNodeManuallyChanged();
        ObjectTreeView.UnhandledException += ex => _dialogService.ShowMessage(ex.Message);
        ObjectTreeView.MouseMove += (_, e) =>
        {
            System.Windows.Point position = e.GetPosition(ObjectTreeView);
            HandleMouseOver((int)position.X, (int)position.Y);
        };

        ObjectTreeView.DragOver += HandleTreeDragOver;
        ObjectTreeView.Drop += HandleTreeDrop;
        ObjectTreeView.ValidateSortingDrop += HandleValidateSortingDrop;
        ObjectTreeView.NodeSortingDropped += HandleNodeSortingDropped;
        ObjectTreeView.GiveFeedback += HandleTreeGiveFeedback;
        ObjectTreeView.QueryContinueDrag += (_, e) =>
        {
            if (e.Action != System.Windows.DragAction.Continue)
            {
                _dispatcher.Post(() => OnSelect(ObjectTreeView.SelectedNode));
            }
        };
    }

    /// <summary>
    /// Handles payloads that did not come from the tree - files from Explorer and Standards-palette
    /// chips. Node reordering is the control's own concern and arrives via
    /// <see cref="HandleValidateSortingDrop"/>.
    /// </summary>
    private void HandleTreeDragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(System.Windows.DataFormats.FileDrop) == true)
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
            e.Handled = true;
        }
        else if (e.Data?.GetDataPresent(DragDropManager.StandardElementNameDataFormat) == true)
        {
            e.Effects = GetChipDropTargetNode(e) != null
                ? System.Windows.DragDropEffects.Copy
                : System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        if (ObjectTreeView.GetNodeAt(e.GetPosition(ObjectTreeView)) is { } hovered)
        {
            DelayExpandHoveredNode(hovered);
        }
    }

    private void HandleTreeDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data?.GetData(System.Windows.DataFormats.FileDrop) is string[] files)
        {
            _dragDropManager.OnFilesDroppedInTreeView(files);
        }
        else if (e.Data?.GetData(DragDropManager.StandardElementNameDataFormat) is string standardTypeName
            && GetChipDropTargetNode(e) is { } targetNode
            && ObjectFinder.Self.GetStandardElement(standardTypeName) is { } standardElement)
        {
            _dragDropManager.HandleDroppedStandardElementOnTreeNode(standardElement, targetNode);
        }
    }

    private void HandleValidateSortingDrop(object? sender, TreeDropValidationEventArgs e)
    {
        e.Allow = false;

        if (ProcessDrop(e.TargetNode, e.Kind) is { } drop)
        {
            e.Allow = _dragDropManager.ValidateNodeSorting(e.DraggedNodes, drop.TreeTarget, drop.Drop);
        }
    }

    private void HandleNodeSortingDropped(object? sender, TreeDropEventArgs e)
    {
        if (ProcessDrop(e.TargetNode, e.Kind) is { } drop)
        {
            _dragDropManager.OnNodeSortingDropped(e.DraggedNodes, drop.TreeTarget, drop.Drop);
        }
    }

    private void HandleTreeGiveFeedback(object sender, System.Windows.GiveFeedbackEventArgs e)
    {
        if (InputLibrary.Cursor.Self.IsInWindow)
        {
            e.UseDefaultCursors = false;
            System.Windows.Input.Mouse.SetCursor(AddCursor);
            e.Handled = true;
        }
    }

    private void ConfigureStandardsPalette()
    {
        var palette = _viewCreator.StandardsPalette;

        palette.CurrentElementNameProvider = () =>
            _selectedState.SelectedElement is { } element && element is not StandardElementSave
                ? element.Name
                : null;

        palette.AddToCurrentRequested = AddStandardInstanceToCurrentElement;

        palette.EditDefaultsRequested = typeName =>
        {
            if (ObjectFinder.Self.GetStandardElement(typeName) is { } standardElement)
            {
                _selectedState.SelectedElement = standardElement;
            }
        };

        ApplyStandardsPaletteMode();
    }

    /// <summary>
    /// Applies the current UseStandardsPalette setting: removes or restores the Standard folder in
    /// the tree, shows or hides the chip palette, and (when on) repopulates the chips. Call after the
    /// setting is toggled and whenever a project loads.
    /// </summary>
    public void ApplyStandardsPaletteMode()
    {
        bool usePalette = _projectState.EffectiveUseStandardsPalette;

        if (mStandardElementsTreeNode != null)
        {
            // ObjectTreeView.Nodes holds concrete nodes, so the ITreeNodeMutable root fields need a
            // cast here. Always safe - every node this class constructs is a GumTreeNode.
            GumTreeNode standardElementsTreeNode = (GumTreeNode)mStandardElementsTreeNode;
            bool isInTree = ObjectTreeView.Nodes.Contains(standardElementsTreeNode);
            if (usePalette && isInTree)
            {
                ObjectTreeView.Nodes.Remove(standardElementsTreeNode);
            }
            else if (!usePalette && !isInTree)
            {
                // Restore in canonical order: after Components, before Behaviors.
                int insertIndex = mBehaviorsTreeNode != null
                    ? ObjectTreeView.Nodes.IndexOf((GumTreeNode)mBehaviorsTreeNode)
                    : ObjectTreeView.Nodes.Count;
                if (insertIndex < 0)
                {
                    insertIndex = ObjectTreeView.Nodes.Count;
                }
                ObjectTreeView.Nodes.Insert(insertIndex, standardElementsTreeNode);
            }
        }

        var palette = _viewCreator.StandardsPalette;
        palette.Visibility = usePalette ? Visibility.Visible : Visibility.Collapsed;
        if (usePalette)
        {
            RefreshStandardsPaletteChips();
        }
    }

    /// <summary>
    /// Highlights the palette chip matching the selected element when it is a standard (so the chip
    /// being edited via "Edit defaults..." is visibly indicated), or clears the highlight otherwise.
    /// </summary>
    public void HighlightStandardInPalette(ElementSave? selectedElement)
    {
        string? typeName = selectedElement is StandardElementSave ? selectedElement.Name : null;
        _viewCreator.StandardsPalette.SetSelectedStandardType(typeName);
    }

    private void RefreshStandardsPaletteChips()
    {
        var typeNames = GetAvailableStandardInstanceTypes(_projectState.GumProjectSave);

        _viewCreator.StandardsPalette.RefreshChips(typeNames);
    }

    /// <summary>
    /// Alphabetizes standard type names for the palette, matching the ordering the classic
    /// "Standard" tree folder applies via <c>SortByName</c> -- the palette otherwise inherits
    /// <see cref="GumProjectSave.StandardElements"/>'s dictionary-insertion order, which has no
    /// relation to display order.
    /// </summary>
    internal static List<string> SortStandardTypeNamesForPalette(IReadOnlyList<string> typeNames)
    {
        return typeNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void AddStandardInstanceToCurrentElement(string typeName)
    {
        var target = _selectedState.SelectedElement;
        if (target == null || target is StandardElementSave)
        {
            return;
        }

        if (ObjectFinder.Self.GetStandardElement(typeName) is not { } standardElement)
        {
            return;
        }

        using var undoLock = _undoManager.RequestLock();
        string name = _elementCommands.GetUniqueNameForNewInstance(standardElement, target);
        _elementCommands.AddInstance(target, name, typeName);
    }

    /// <summary>
    /// Returns the tree node under the drag cursor if it is a valid Standards-chip drop target
    /// (a Screen/Component element, or an instance within one); otherwise null.
    /// </summary>
    private GumTreeNode? GetChipDropTargetNode(System.Windows.DragEventArgs e)
    {
        GumTreeNode? node = ObjectTreeView.GetNodeAt(e.GetPosition(ObjectTreeView));
        if (node == null)
        {
            return null;
        }
        if (node.Tag is ElementSave element && element is not StandardElementSave)
        {
            return node;
        }
        if (node.Tag is InstanceSave)
        {
            return node;
        }
        return null;
    }

    /// <summary>
    /// Maps a tree drop (target node + kind) to the tree node that becomes the
    /// drop's container and a typed <see cref="DropTarget"/> describing where
    /// the dragged item lands inside that container's flat instances list.
    /// Returns the tree node alone (with a null <see cref="DropTarget"/>) for
    /// folder/behavior drops where flat-list semantics do not apply.
    /// Internal so it can be unit-tested without instantiating the manager.
    /// </summary>
    internal static (GumTreeNode TreeTarget, DropTarget? Drop)? ProcessDrop(GumTreeNode? originalTarget, TreeDropKind kind)
    {
        if (originalTarget == null)
        {
            return null;
        }

        // Issue #2864: a drop whose visual adornment is "rectangle around the
        // row" (Into and IntoFirst both draw the same box) must append to the
        // flat Instances list the new visual will be added to — never insert
        // at a stale tree-child index. The user-facing distinction is
        // "box vs. line": box = append, line = insert at sibling position.
        // Inserting at index 0 is still reachable as DropKind.Before on the
        // parent's first child, which draws a line.
        switch (kind)
        {
            case TreeDropKind.Into:
            case TreeDropKind.IntoFirst:
                switch (originalTarget.Tag)
                {
                    case ElementSave element:
                        return (originalTarget, new DropTarget(element, null, new DropPosition.Append()));
                    case InstanceSave instance when instance.ParentContainer != null:
                        return (originalTarget, new DropTarget(instance.ParentContainer, instance, new DropPosition.Append()));
                    default:
                        // Folder/behavior drops: no flat-list semantics. The caller
                        // routes by treeNode kind (IsTopComponentContainerTreeNode etc.).
                        return (originalTarget, null);
                }
            case TreeDropKind.After:
            case TreeDropKind.Before:
            {
                GumTreeNode? parent = originalTarget.Parent;
                if (parent == null)
                {
                    return null;
                }
                if (originalTarget.Tag is InstanceSave sibling && sibling.ParentContainer != null)
                {
                    InstanceSave? parentInstance = parent.Tag as InstanceSave;
                    DropPosition position = kind == TreeDropKind.Before
                        ? new DropPosition.BeforeSibling(sibling)
                        : new DropPosition.AfterSibling(sibling);
                    return (parent, new DropTarget(sibling.ParentContainer, parentInstance, position));
                }
                // Reordering element/folder/behavior nodes does not feed an
                // instances list — the downstream consumer reads the tree node.
                return (parent, null);
            }
            default:
                return null;
        }
    }


    internal void FocusSearch()
    {
        searchTextBox.Focus();
    }

    void IRecipient<ThemeChangedMessage>.Receive(ThemeChangedMessage message)
    {
        _viewCreator.ApplyThemeColors();
    }

    private void DelayExpandHoveredNode(GumTreeNode hoveredNode)
    {
        // Can't do this, it seems to interfere with the Undo History
        //treeview.SelectedNode = hoveredNode;

        // So...lets fake it with backcolor/forecolor instead?
        if (mLastHoveredNode != hoveredNode)
        {
            hoverStartTime = DateTime.Now;
            mLastHoveredNode = hoveredNode;

            // If partially off the screen, make it visible
            ObjectTreeView.EnsureVisible(hoveredNode);
        }
        else
        {
            // Make it so that we can EXPAND folders or nodes/items if we hover for half a second
            if (hoveredNode.Nodes.Count > 0 && !hoveredNode.IsExpanded)
            {
                if (hoverStartTime == null)
                {
                    hoverStartTime = DateTime.Now;
                }

                TimeSpan duration = (TimeSpan)(DateTime.Now - hoverStartTime);
                int hoverDelayMiliseconds = 500;
                if (duration.TotalMilliseconds > hoverDelayMiliseconds)
                {
                    hoveredNode.Expand();
                }
            }
        }
    }

    private void AddAndRemoveFolderNodes()
    {
        if (ObjectFinder.Self.GumProjectSave != null && 
            
            !string.IsNullOrEmpty(ObjectFinder.Self.GumProjectSave.FullFileName))
        {
            string currentDirectory = FileManager.GetDirectory(ObjectFinder.Self.GumProjectSave.FullFileName);

            // Let's make sure these folders exist, they better!
            Directory.CreateDirectory(mStandardElementsTreeNode.GetFullFilePath()!.FullPath);
            Directory.CreateDirectory(mScreensTreeNode.GetFullFilePath()!.FullPath);
            Directory.CreateDirectory(mComponentsTreeNode.GetFullFilePath()!.FullPath);
            Directory.CreateDirectory(mBehaviorsTreeNode.GetFullFilePath()!.FullPath);


            // add folders to the screens, entities, and standard elements
            AddAndRemoveFolderNodesFromFileSystem(mStandardElementsTreeNode.GetFullFilePath()!.FullPath, mStandardElementsTreeNode);
            AddAndRemoveFolderNodesFromFileSystem(mScreensTreeNode.GetFullFilePath()!.FullPath, mScreensTreeNode);
            AddAndRemoveFolderNodesFromFileSystem(mComponentsTreeNode.GetFullFilePath()!.FullPath, mComponentsTreeNode);
            AddAndRemoveFolderNodesFromFileSystem(mBehaviorsTreeNode.GetFullFilePath()!.FullPath, mBehaviorsTreeNode);


            AddNeededButMissingFromFileSystemFolderNodes();
            //AddAndRemoveFolderNodes(currentDirectory, this.mTreeView.Nodes);
        }
        else
        {
            RootScreensTreeNode.ClearChildren();
            RootComponentsTreeNode.ClearChildren();
            // maybe we support behavior folders in the future? If so:
            RootBehaviorsTreeNode.ClearChildren();
        }
    }

    private void AddNeededButMissingFromFileSystemFolderNodes()
    {
        var project = ObjectFinder.Self.GumProjectSave;
        System.Diagnostics.Debug.Assert(project != null, "GumProjectSave was null when trying to add missing folder nodes.");
        HashSet<string> neededFolders = new HashSet<string>();

        foreach(var element in project.AllElements)
        {
            var rootDirectoryForElementType =
                element is ScreenSave ? _fileLocations.ScreensFolder
                : element is ComponentSave ? _fileLocations.ComponentsFolder
                : element is StandardElementSave ? _fileLocations.StandardsFolder
                : string.Empty;

            string fullPath = rootDirectoryForElementType + FileManager.GetDirectory(element.Name);

            if(!neededFolders.Contains(fullPath))
            {
                neededFolders.Add(fullPath);
            }
        }

        foreach(var item in neededFolders)
        {
            CreateNodeIfNecessary(item);
        }
    }

    private GumTreeNode CreateNodeIfNecessary(string directory)
    {
        var treeNode = GetTreeNodeFor(directory);

        if(treeNode == null)
        {
            GumTreeNode? parentNode = null;
            string parentDirectory = string.Empty;
            try
            {
                parentDirectory = FileManager.GetDirectory(directory);
            }
            catch { }

            if(parentDirectory != string.Empty)
            {
                parentNode = CreateNodeIfNecessary(parentDirectory);
            }

            if(parentNode != null)
            {
                var treeNodeText = FileManager.RemovePath(directory);
                if(treeNodeText?.EndsWith("/") == true)
                {
                    treeNodeText = treeNodeText.Substring(0, treeNodeText.Length - 1);
                }
                treeNode = new GumTreeNode(treeNodeText);
                // parentNode is always a GumTreeNode, which implements ITreeNodeMutable directly.
                ((ITreeNodeMutable)parentNode).AddChild((ITreeNodeMutable)treeNode);
                treeNode.ImageIndex = ExclamationIndex;
            }
        }

        return treeNode!;
    }

    // nodesToAddTo is ITreeNodeMutable so this method's add/remove decisions go through the mutation
    // interface rather than the concrete node type.
    private void AddAndRemoveFolderNodesFromFileSystem(string currentDirectory, ITreeNodeMutable nodesToAddTo)
    {
        // todo: removes
        var directories = Directory.EnumerateDirectories(currentDirectory).ToArray();

        foreach (string directory in directories)
        {
            ITreeNodeMutable existingTreeNode = (ITreeNodeMutable)GetTreeNodeFor(directory);

            if (existingTreeNode == null)
            {
                existingTreeNode = new GumTreeNode(FileManager.RemovePath(directory));
                nodesToAddTo.AddChild(existingTreeNode);
                existingTreeNode.ImageIndex = FolderImageIndex;
            }
            AddAndRemoveFolderNodesFromFileSystem(directory, existingTreeNode);
        }

        for(int i = nodesToAddTo.ChildCount - 1; i > -1; i--)
        {
            ITreeNodeMutable node = nodesToAddTo.GetChildAt(i);

            bool found = false;

            foreach (string directory in directories)
            {
                string directoryStripped = FileManager.RemovePath(directory);

                if (directoryStripped.Equals(node.Text, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            // only remove nodes if they are directory nodes (aka they have a null tag)
            if (!found && node.Tag == null)
            {
                nodesToAddTo.RemoveChildAt(i);
            }
        }
    }

    bool ShouldShow(ScreenSave screen) => string.IsNullOrEmpty(filterText) || screen.Name.ToLower().Contains(filterText.ToLower());
    bool ShouldShow(ComponentSave component) => string.IsNullOrEmpty(filterText) || component.Name.ToLower().Contains(filterText.ToLower());
    bool ShouldShow(StandardElementSave standardElementSave) => string.IsNullOrEmpty(filterText) || standardElementSave.Name.ToLower().Contains(filterText.ToLower());
    bool ShouldShow(BehaviorSave behavior) => string.IsNullOrEmpty(filterText) || behavior.Name?.ToLower().Contains(filterText.ToLower()) == true;

    private void AddAndRemoveScreensComponentsStandardsAndBehaviors()
    {
        var gumProject = _projectState.GumProjectSave;
        /////////////Early Out////////////////
        if (gumProject == null)
            return;
        ////////////End Early Out////////////

        // Save off old selected stuff
        InstanceSave? selectedInstance = _selectedState.SelectedInstance;
        ElementSave? selectedElement = _selectedState.SelectedElement;
        BehaviorSave? selectedBehavior = _selectedState.SelectedBehavior;


        #region Add nodes that haven't been added yet

        foreach (ScreenSave screenSave in gumProject.Screens)
        {
            var treeNode = GetTreeNodeFor(screenSave);
            if (treeNode == null && ShouldShow(screenSave))
            {
                string fullPath = _fileLocations.ScreensFolder + FileManager.GetDirectory(screenSave.Name);
                GumTreeNode parentNode = GetTreeNodeFor(fullPath);

                // The return value isn't read afterward - treeNode above only guards whether the
                // node already exists.
                AddTreeNodeForElement(screenSave, (ITreeNodeMutable)parentNode, ScreenImageIndex);
            }
        }

        foreach (ComponentSave componentSave in gumProject.Components)
        {
            if (GetTreeNodeFor(componentSave) == null && ShouldShow(componentSave))
            {
                string fullPath = _fileLocations.ComponentsFolder + FileManager.GetDirectory(componentSave.Name);
                GumTreeNode parentNode = GetTreeNodeFor(fullPath);

                if(parentNode == null)
                {
                    throw new Exception($"Error trying to get parent node for component {fullPath}");
                }

                AddTreeNodeForElement(componentSave, (ITreeNodeMutable)parentNode, ComponentImageIndex);
            }
        }

        foreach (StandardElementSave standardSave in gumProject.StandardElements)
        {
            if (standardSave.Name != "Component")
            {
                if (GetTreeNodeFor(standardSave) == null &&  ShouldShow(standardSave))
                {
                    AddTreeNodeForElement(standardSave, mStandardElementsTreeNode, _treeNodeImageLogic.GetImageIndexForStandardElement(standardSave.Name));
                }
            }
        }

        foreach(BehaviorSave behaviorSave in gumProject.Behaviors)
        {
            if(GetTreeNodeFor(behaviorSave) == null && ShouldShow(behaviorSave))
            {
                string fullPath = _fileLocations.BehaviorsFolder;
                
                if(behaviorSave.Name != null)
                {
                    fullPath = _fileLocations.BehaviorsFolder + FileManager.GetDirectory(behaviorSave.Name);
                }
                GumTreeNode parentNode = GetTreeNodeFor(fullPath);

                AddTreeNodeForBehavior(behaviorSave, (ITreeNodeMutable)parentNode, BehaviorImageIndex);
            }
        }

        #endregion

        #region Remove nodes that are no longer needed

        mScreensTreeNode.RemoveRecursivelyIfStale<ScreenSave>(
            screen => gumProject.Screens.Contains(screen) && ShouldShow(screen));

        mComponentsTreeNode.RemoveRecursivelyIfStale<ComponentSave>(
            component => gumProject.Components.Contains(component) && ShouldShow(component));

        // Standard elements don't support subfolders, so this pass is flat (non-recursive) and,
        // unlike the screen/component pass above, removes any non-StandardElementSave-tagged node
        // outright rather than recursing into it.
        for (int i = mStandardElementsTreeNode.ChildCount - 1; i > -1; i--)
        {
            // Do we want to support folders here?
            StandardElementSave? standardElement = mStandardElementsTreeNode.GetChildAt(i).Tag as StandardElementSave;

            if (standardElement == null || !gumProject.StandardElements.Contains(standardElement) || !ShouldShow(standardElement))
            {
                mStandardElementsTreeNode.RemoveChildAt(i);
            }
        }

        // Also flat (non-recursive): unlike standard elements above, a non-BehaviorSave-tagged node
        // (a behavior subfolder) is left alone rather than removed.
        for (int i = mBehaviorsTreeNode.ChildCount - 1; i > -1; i--)
        {
            BehaviorSave? behavior = mBehaviorsTreeNode.GetChildAt(i).Tag as BehaviorSave;

            if (behavior != null && (!gumProject.Behaviors.Contains(behavior) || !ShouldShow(behavior)))
            {
                mBehaviorsTreeNode.RemoveChildAt(i);
            }
        }

        #endregion

        #region Update the nodes

        RefreshChildNodes(mScreensTreeNode, RefreshUi);

        RefreshChildNodes(mComponentsTreeNode, RefreshUi);

        RefreshChildNodes(mStandardElementsTreeNode, RefreshUi);

        RefreshChildNodes(mBehaviorsTreeNode, RefreshUi);

        #endregion

        #region Sort everything

        mScreensTreeNode.SortByName(recursive:true);

        mComponentsTreeNode.SortByName(recursive: true);

        mStandardElementsTreeNode.SortByName(recursive: true);

        mBehaviorsTreeNode.SortByName(recursive: true);

        #endregion

        #region Re-select whatever was selected before

        try
        {
            if (selectedInstance != null)
            {
                _selectedState.SelectedInstance = selectedInstance;
            }
            if(selectedBehavior != null)
            {
                _selectedState.SelectedBehavior = selectedBehavior;
            }
        }
        catch
        {
            // This exception can happen if a user has an item selected, then loads a new 
            // project. In that case the previous selection will no longer be valid, so this
            // fails. That's okay.
        }
        #endregion
    }

    // parentNode is ITreeNodeMutable rather than the concrete node type so the construction work
    // (Tag/ImageIndex/child-add) is expressed through the mutation interface.
    private ITreeNodeMutable AddTreeNodeForElement(ElementSave element, ITreeNodeMutable parentNode, int defaultImageIndex)
    {
        if (parentNode == null)
        {
            throw new NullReferenceException($"{nameof(parentNode)} cannot be null");
        }
        ITreeNodeMutable treeNode = new GumTreeNode();

        treeNode.ImageIndex = _treeNodeImageLogic.GetCreateImageIndex(element.IsSourceFileMissing, defaultImageIndex);

        treeNode.SetTag(element);

        parentNode.AddChild(treeNode);

        return treeNode;
    }

    private void AddTreeNodeForBehavior(BehaviorSave behavior, ITreeNodeMutable parentNode, int defaultImageIndex)
    {
        ITreeNodeMutable treeNode = new GumTreeNode();

        treeNode.ImageIndex = _treeNodeImageLogic.GetCreateImageIndex(behavior.IsSourceFileMissing, defaultImageIndex);

        treeNode.SetTag(behavior);

        parentNode.AddChild(treeNode);
    }

    private void CreateRootTreeNodesIfNecessary()
    {
        if (mScreensTreeNode == null)
        {
            // ObjectTreeView.Nodes holds concrete nodes, so each root is built as a GumTreeNode local
            // and then assigned to its ITreeNodeMutable field.
            GumTreeNode screensTreeNode = new GumTreeNode("Screens");
            screensTreeNode.ImageIndex = FolderImageIndex;
            ObjectTreeView.Nodes.Add(screensTreeNode);
            mScreensTreeNode = screensTreeNode;

            GumTreeNode componentsTreeNode = new GumTreeNode("Components");
            componentsTreeNode.ImageIndex = FolderImageIndex;
            ObjectTreeView.Nodes.Add(componentsTreeNode);
            mComponentsTreeNode = componentsTreeNode;

            GumTreeNode standardElementsTreeNode = new GumTreeNode("Standard");
            standardElementsTreeNode.ImageIndex = FolderImageIndex;
            // When the experimental Standards palette is on, the Standard folder is replaced by the
            // chip palette, so it is not shown in the tree. The node object is still kept (and still
            // populated) so toggling the setting at runtime can restore it without a full rebuild.
            if (!_projectState.EffectiveUseStandardsPalette)
            {
                ObjectTreeView.Nodes.Add(standardElementsTreeNode);
            }
            mStandardElementsTreeNode = standardElementsTreeNode;

            GumTreeNode behaviorsTreeNode = new GumTreeNode("Behaviors");
            behaviorsTreeNode.ImageIndex = FolderImageIndex;
            ObjectTreeView.Nodes.Add(behaviorsTreeNode);
            mBehaviorsTreeNode = behaviorsTreeNode;
        }
    }


    public void RecordSelection()
    {
        mRecordedSelectedObject =
            (object?)_selectedState.SelectedInstance ??
            (object?)_selectedState.SelectedElement ??
            (object?)_selectedState.SelectedBehavior;

        // When an instance is selected, record its container so FindTreeNodeForRecordedObject
        // can fall back to a name-based search if the instance reference becomes stale after
        // undo/redo replaces it with a deep-cloned snapshot object.
        mRecordedSelectedContainer = _selectedState.SelectedInstance != null
            ? (object?)_selectedState.SelectedBehavior ?? _selectedState.SelectedElement
            : null;

        // Record the full multi-selection so a tree refresh can restore every selected
        // instance rather than collapsing to the single primary one (issue #2954).
        _recordedSelectedInstances = _selectedState.SelectedInstances.ToList();
    }

    public void SelectRecordedSelection()
    {
        try
        {
            // Restore a multi-selection first so it isn't collapsed to the single
            // primary instance after a tree refresh (issue #2954).
            if (_recordedSelectedInstances.Count > 1)
            {
                List<ITreeNode> nodes = GetReselectableNodes(
                    _recordedSelectedInstances,
                    instance => FindTreeNodeFor(instance, mRecordedSelectedContainer));

                if (nodes.Count > 1)
                {
                    Select(nodes.Cast<GumTreeNode>().ToList());
                    return;
                }
            }

            if (mRecordedSelectedObject != null)
            {
                var desiredNode = FindTreeNodeForRecordedObject();

                if (desiredNode != null)
                {
                    // Use the tree-node-based Select so the correct node is set even when
                    // the equality check in the _selectedState setter short-circuits (i.e.
                    // the instance was never un-assigned, so assigning it again fires no events
                    // and the tree node is never updated to reflect the new node).
                    Select((GumTreeNode)desiredNode);
                }
                else
                {
                    // Node not found (object may have been deleted). Fall back to the
                    // state-based path, which preserves the existing restoration behavior.
                    if (mRecordedSelectedObject is InstanceSave instanceSave)
                        _selectedState.SelectedInstance = instanceSave;
                    else if (mRecordedSelectedObject is ElementSave elementSave)
                        _selectedState.SelectedElement = elementSave;
                    else if (mRecordedSelectedObject is BehaviorSave behaviorSave)
                        _selectedState.SelectedBehavior = behaviorSave;
                }
            }
        }
        catch
        {
            // no big deal, this could have been re-loaded
        }
    }

    /// <summary>
    /// Maps each recorded instance to its current tree node via <paramref name="nodeFinder"/>,
    /// dropping instances that no longer have a node (e.g. deleted before the refresh) while
    /// preserving order. Used to restore a multi-selection after a tree refresh.
    /// </summary>
    internal static List<ITreeNode> GetReselectableNodes(
        IReadOnlyList<InstanceSave> recordedInstances,
        Func<InstanceSave, ITreeNode?> nodeFinder)
    {
        List<ITreeNode> nodes = new List<ITreeNode>();
        foreach (InstanceSave instance in recordedInstances)
        {
            ITreeNode? node = nodeFinder(instance);
            if (node != null)
            {
                nodes.Add(node);
            }
        }
        return nodes;
    }

    private ITreeNode? FindTreeNodeForRecordedObject() =>
        FindTreeNodeFor(mRecordedSelectedObject, mRecordedSelectedContainer);

    private ITreeNode? FindTreeNodeFor(object? recordedObject, object? recordedContainer)
    {
        if (recordedObject is InstanceSave instanceSave)
        {
            var behavior = ObjectFinder.Self.GetBehaviorContainerOf(instanceSave);
            if (behavior != null)
            {
                var behaviorNode = GetTreeNodeFor(behavior);
                if (behaviorNode != null)
                    return (ITreeNode?)GetTreeNodeFor(instanceSave, behaviorNode)
                        ?? (ITreeNode?)GetInstanceTreeNodeByName(instanceSave.Name, behaviorNode);
            }

            if (instanceSave.ParentContainer != null)
            {
                var elementNode = GetTreeNodeFor(instanceSave.ParentContainer);
                if (elementNode != null)
                    return (ITreeNode?)GetTreeNodeFor(instanceSave, elementNode)
                        ?? (ITreeNode?)GetInstanceTreeNodeByName(instanceSave.Name, elementNode);
            }

            // Behavior instances have no ParentContainer, and GetBehaviorContainerOf fails when
            // the reference is stale (undo/redo replaces instances with deep-cloned snapshots).
            // Fall back to name-based search using the container recorded before the refresh.
            if (!string.IsNullOrEmpty(instanceSave.Name) &&
                recordedContainer is BehaviorSave recordedBehavior)
            {
                var behaviorNode = GetTreeNodeFor(recordedBehavior);
                if (behaviorNode != null)
                    return (ITreeNode?)GetInstanceTreeNodeByName(instanceSave.Name, behaviorNode);
            }

            return null;
        }
        else if (recordedObject is ElementSave elementSave)
        {
            return (ITreeNode?)GetTreeNodeFor(elementSave);
        }
        else if (recordedObject is BehaviorSave behaviorSave)
        {
            return (ITreeNode?)GetTreeNodeFor(behaviorSave);
        }

        return null;
    }

    // Discussion about Selection
    // Selection is a rather complicated
    // system in Gum because tree nodes can
    // be selected in a number of ways:
    // 1.  The user can push/release (click)
    // 2.  The user can select an item in the
    //     wireframe window which in turn selects
    //     the appropriate tree node.
    // 3.  The user pushes on a tree node, but then
    //     drags off of it to do a drag+drop somewhere
    //     else.
    // We want the app to refresh what it is displaying
    // in scenario 1 and 2, but not in 3.  Therefore the
    // GumTreeView class has an event called AfterClickSelect
    // which only fires when the user actually clicks on an item (1) so
    // that #3 doesn't fire off an event.  However, this means that #2 will
    // no longer fire off the event either.  We need to then make sure that #2
    // does still fire off an event, so we'll do this by manually raising the event
    // in the Select methods where a Save object is selected.
    public void Select(InstanceSave instanceSave, ElementSave parent)
    {
        if (IsInUiInitiatedSelection) return;
        if (instanceSave != null)
        {
            GumTreeNode? parentTreeNode = GetTreeNodeFor(parent);

            // This could be null if the user started a new project or loaded a different project.
            if (parentTreeNode != null)
            {
                Select(GetTreeNodeFor(instanceSave, parentTreeNode));
            }
        }
        else
        {
            Select((GumTreeNode?)null);
        }
    }

    public void Select(BehaviorSave behavior)
    {
        if (IsInUiInitiatedSelection) return;

        if (behavior != null)
        {
            var treeNode = GetTreeNodeFor(behavior);

            Select(treeNode);
        }
    }

    public void Select(IEnumerable<InstanceSave> list)
    {
        if (IsInUiInitiatedSelection) return;

        if (list.Count() != 0)
        {
            var firstItem = list.First();

            GumTreeNode? parentContainer = null;
            if(firstItem.ParentContainer != null)
            {
                parentContainer = GetTreeNodeFor(firstItem.ParentContainer);
            }
            else
            {
                var behavior = ObjectFinder.Self.GetBehaviorContainerOf(firstItem);
                if(behavior != null)
                {
                    parentContainer = GetTreeNodeFor(behavior);
                }
            }

            List<GumTreeNode> treeNodeList = parentContainer != null
                ? GetReselectableNodes(list.ToList(), item => GetTreeNodeFor(item, parentContainer))
                    .Cast<GumTreeNode>()
                    .ToList()
                : new List<GumTreeNode>();

            Select(treeNodeList);
        }
        else
        {
            Select((GumTreeNode?)null);
        }
    }


    public void Select(ElementSave elementSave)
    {
        if (IsInUiInitiatedSelection) return;

        if (elementSave == null)
        {
            if (ObjectTreeView.SelectedNode != null && ObjectTreeView.SelectedNode.Tag != null && ObjectTreeView.SelectedNode.Tag is ElementSave)
            {
                // why do we explicitly set this here rather than calling Select? If we set it to null without calling that, we don't get the benefit of the 
                // plugins being notified of a null selection:
                //ObjectTreeView.SelectedNode = null;
                Select((GumTreeNode?)null);

            }
        }
        else
        {
            var treeNode = GetTreeNodeFor(elementSave);

            if(treeNode == null && !string.IsNullOrEmpty(searchTextBox.Text))
            {
                searchTextBox.Text = null;
                treeNode = GetTreeNodeFor(elementSave);
            }

            Select(treeNode);
        }
    }

    private void Select(GumTreeNode? treeNode)
    {
        if (IsInUiInitiatedSelection) return;

        treeNode = ResolveNodeToSelect(treeNode, ObjectTreeView.Nodes);

        if (ObjectTreeView.SelectedNode != treeNode)
        {
            // See comment above about why we have to manually raise the AfterClick

            ObjectTreeView.SelectedNode = treeNode;

            if (treeNode != null)
            {
                ObjectTreeView.EnsureVisible(treeNode);
            }

            if (!SuppressCallAfterClickSelect)
            {
                ObjectTreeView.CallAfterClickSelect(treeNode);
            }
        }
    }

    /// <summary>
    /// Resolves the node that should actually be selected. A node detached from the tree - never
    /// attached, as standard elements are while the Standards palette is on, or detached during a
    /// rebuild - has no visible row to select, so it resolves to null (clearing any prior selection)
    /// rather than being selected outright.
    /// </summary>
    internal static GumTreeNode? ResolveNodeToSelect(GumTreeNode? treeNode, GumTreeNodeCollection rootNodes)
    {
        if (treeNode != null && !IsInTree(treeNode, rootNodes))
        {
            return null;
        }

        return treeNode;
    }

    private static bool IsInTree(GumTreeNode node, GumTreeNodeCollection rootNodes)
    {
        GumTreeNode root = node;
        while (root.Parent is { } parent)
        {
            root = parent;
        }

        return rootNodes.Contains(root);
    }

    private void Select(List<GumTreeNode> treeNodes)
    {
        if (IsInUiInitiatedSelection) return;

        ObjectTreeView.SelectedNodes = treeNodes;

        if (treeNodes.Count != 0)
        {
            ObjectTreeView.EnsureVisible(treeNodes[0]);

            if (!SuppressCallAfterClickSelect)
            {
                ObjectTreeView.CallAfterClickSelect(treeNodes[0]);
            }
        }
    }

    /// <summary>
    /// Refreshes the entirety of the tree view, preserving selection.
    /// </summary>
    public void RefreshUi()
    {
        _collapseToggleService.Clear();
        var expandedPaths = _collapseToggleService.SaveExpandedPaths(RootTreeNodes);
        RecordSelection();
        // brackets are used simply to indicate the recording and selection should
        // go around the rest of the function:
        {
            CreateRootTreeNodesIfNecessary();

            AddAndRemoveFolderNodes();

            AddAndRemoveScreensComponentsStandardsAndBehaviors();

        }
        SelectRecordedSelection();
        _collapseToggleService.RestoreExpandedPaths(RootTreeNodes, expandedPaths);

        // Keep the chip palette in sync with the project's standards. A full tree rebuild is the
        // signal that standard elements may have changed (e.g. "Add Skia Standard Elements", which
        // only calls RefreshElementTreeView and raises no ElementAdd event). RefreshChips is
        // idempotent, so this is a no-op when the standard set is unchanged.
        if (_projectState.EffectiveUseStandardsPalette)
        {
            RefreshStandardsPaletteChips();
        }
    }


    public void RefreshUi(IInstanceContainer instanceContainer)
    {
        var foundNode = (GumTreeNode?)((IElementTreeRoots)this).GetTreeNodeForTag(instanceContainer);

        if(foundNode != null)
        {
            RecordSelection();
            RefreshUi(foundNode);
            SelectRecordedSelection();
        }
    }

    /// <summary>
    /// Refreshes the tree nodes for the argument stateContainer. This includes the displayed text and contained nodes, and the parent
    /// folder node.
    /// </summary>
    /// <param name="stateContainer">The StateContainer to refresh.</param>
    public void RefreshUi(IStateContainer stateContainer)
    {
        var foundNode = (GumTreeNode?)((IElementTreeRoots)this).GetTreeNodeForTag(stateContainer);

        if(foundNode != null)
        {
            RecordSelection();
            RefreshUi(foundNode);
            SelectRecordedSelection();
        }
    }

    public void RefreshUi(InstanceSave instance)
    {
        var parentElement = instance.ParentContainer;
        if (parentElement == null)
        {
            return;
        }

        var parentTreeNode = GetTreeNodeFor(parentElement);
        if(parentTreeNode == null)
        {
            return;
        }

        var treeNode = GetTreeNodeFor(instance, parentTreeNode);
        if(treeNode != null)
        {
            RefreshUi(treeNode);
        }
    }
    public void RefreshUi(GumTreeNode node)
    {
        if(node  == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        if (node.Tag is ElementSave elementSave)
        {
            RefreshElementTreeNode(node, elementSave);
        }
        else if (node.Tag is InstanceSave instanceSave)
        {
            // this if check improves speed quite a bit!
            if(instanceSave.Name != node.Text)
            {
                node.Text = instanceSave.Name;
            }

            var currentIndex = node.ImageIndex;
            if (currentIndex == InstanceImageIndex || currentIndex == LockedInstanceImageIndex)
            {
                int desiredImageIndex = instanceSave.Locked ? LockedInstanceImageIndex : InstanceImageIndex;
                if (currentIndex != desiredImageIndex)
                {
                    node.ImageIndex = desiredImageIndex;
                }
            }
        }
        else if(node.Tag is BehaviorSave behavior)
        {
            if(behavior.Name != node.Text)
            {
                node.Text = behavior.Name;
            }
            RefreshBehaviorTreeNode(node, behavior);
        }

        RefreshChildNodes(node, RefreshUi);
    }

    /// <summary>
    /// Refreshes every child of <paramref name="node"/>, against a snapshot of the child list taken
    /// up front. Refreshing a child can reparent it - an element whose folder changed moves under the
    /// folder node that now owns it - which mutates the collection being walked.
    /// </summary>
    internal static void RefreshChildNodes(ITreeNodeMutable node, Action<GumTreeNode> refreshChild)
    {
        List<GumTreeNode> children = new List<GumTreeNode>(node.ChildCount);
        for (int i = 0; i < node.ChildCount; i++)
        {
            if (node.GetChildAt(i) is GumTreeNode child)
            {
                children.Add(child);
            }
        }

        foreach (GumTreeNode child in children)
        {
            refreshChild(child);
        }
    }

    private void RefreshElementTreeNode(GumTreeNode node, ElementSave elementSave)
    {
        // This could be because of a corruption:
        if (string.IsNullOrEmpty(elementSave.Name))
        {
            throw new ArgumentException("ElementSave cannot have a null name");
        }
        List<InstanceSave> expandedInstances = new List<InstanceSave>();
        List<InstanceSave> allInstances = elementSave.Instances;

        if(elementSave is ScreenSave || elementSave is ComponentSave)
        {

            string fullPath;
            if(elementSave is ScreenSave)
            {
                fullPath = _fileLocations.ScreensFolder + FileManager.GetDirectory(elementSave.Name);
            }
            else
            {
                fullPath = _fileLocations.ComponentsFolder + FileManager.GetDirectory(elementSave.Name);
            }
            GumTreeNode desiredNode = GetTreeNodeFor(fullPath);
            var parentNode = node.Parent;
            if(parentNode != desiredNode)
            {
                if (parentNode != null)
                {
                    ((ITreeNodeMutable)node).RemoveSelf();
                }
                if(desiredNode != null)
                {
                    ((ITreeNodeMutable)desiredNode).AddChild((ITreeNodeMutable)node);
                }
            }
        }

        foreach (InstanceSave instance in allInstances)
        {
            // use name because an undo can change references. Same with reloads if were called there
            var treeNode = GetInstanceTreeNodeByName(instance.Name, node);

            if (treeNode?.Nodes.Count > 0 && treeNode?.IsExpanded == true)
            {
                expandedInstances.Add(instance);
            }
        }

        var nodeText = FileManager.RemovePath(elementSave.Name);
        if(nodeText != node.Text)
        {
            var hadTextBefore = !string.IsNullOrEmpty(node.Text);
            node.Text = nodeText;

            if(hadTextBefore && node.Parent != null)
            {
                ((ITreeNodeMutable)node.Parent).SortByName();
            }
        }

        var allTreeNodesRecursively = ((ITreeNode)node).GetAllChildrenNodesRecursively();

        // why do we clear? wouldn't this require re-creation of all nodes? that seems like it might be slow...
        //node.Nodes.Clear();
        // Let's be smart about removal...
        foreach(ITreeNode instanceNode in allTreeNodesRecursively)
        {
            var instance = instanceNode.Tag as InstanceSave;

            if(instance == null || !allInstances.Contains(instance))
            {
                ((ITreeNodeMutable)instanceNode).RemoveSelf();
            }
        }

        List<List<InstanceSave>> siblingLists = new ();

        foreach (InstanceSave instance in allInstances)
        {
            GumTreeNode nodeForInstance = GetTreeNodeFor(instance, node);

            if (nodeForInstance == null)
            {
                nodeForInstance = (GumTreeNode)AddTreeNodeForInstance(instance, (ITreeNodeMutable)node, tolerateMissingTypes:false);
            }

            if(instance.DefinedByBase)
            {
                nodeForInstance.ImageIndex = DerivedInstanceImageIndex;
            }

            // todo - do this after we have all the children created:
            if (expandedInstances.Any(item => item.Name == instance.Name))
            {
                nodeForInstance.Expand();
            }

            var siblingInstances = siblingLists.FirstOrDefault(item => item.Contains(instance));
            if (siblingInstances == null)
            {
                siblingInstances = instance.GetSiblingsIncludingThis();
                siblingLists.Add(siblingInstances);
            }
            var desiredIndex = siblingInstances.IndexOf(instance);

            var container = instance.ParentContainer ?? ObjectFinder.Self.GetElementContainerOf(instance);
            var defaultState = container.DefaultState;
            //var thisParentValue = defaultState.GetValueOrDefault<string>($"{instance.Name}.Parent");
            var thisParentValue = defaultState.GetValueRecursive($"{instance.Name}.Parent") as string;

            // If thisParentValue has a period, the instance is attached to an item inside the parent.
            if(thisParentValue?.Contains(".") == true)
            {
                thisParentValue = thisParentValue.Substring(0, thisParentValue.IndexOf('.'));
            }

            var desiredParentNode = node;
            if(!string.IsNullOrEmpty(thisParentValue))
            {
                var instanceParent = allInstances.FirstOrDefault(item => item.Name == thisParentValue);

                if(instanceParent != null)
                {
                    desiredParentNode = GetTreeNodeFor(instanceParent, node);
                }
            }
            if(desiredParentNode != nodeForInstance.Parent && desiredParentNode != null &&
                // Just in case Gum gets into a weird circular reference situation.
                // Gum should protect against this at a higher level, but in case it fails to we
                // don't want to bring down the entire treeview so let's run a last minute check:
                nodeForInstance != desiredParentNode)
            {
                ((ITreeNodeMutable)nodeForInstance).RemoveSelf();
                ((ITreeNodeMutable)desiredParentNode).AddChild((ITreeNodeMutable)nodeForInstance);
            }

            ((ITreeNodeMutable)nodeForInstance).MoveToIndex(desiredIndex);

            var element = ObjectFinder.Self.GetElementSave(instance.BaseType);

            int desiredImageIndex = _treeNodeImageLogic.GetInstanceRefreshImageIndex(instance, element);

            if(nodeForInstance.ImageIndex != desiredImageIndex)
            {
                nodeForInstance.ImageIndex = desiredImageIndex;
            }
        }

        foreach(var expandedInstance in expandedInstances)
        {
            var toExpand = GetInstanceTreeNodeByName(expandedInstance.Name, node);
            toExpand?.Expand();
        }
    }

    private void RefreshBehaviorTreeNode(GumTreeNode node, BehaviorSave behavior)
    {
        var allInstances = behavior.RequiredInstances;

        // Remove nodes that no longer have a corresponding instance
        foreach (GumTreeNode instanceNode in node.Nodes.Cast<GumTreeNode>().ToList())
        {
            var instance = instanceNode.Tag as InstanceSave;
            if (instance == null || !allInstances.Contains(instance))
            {
                ((ITreeNodeMutable)instanceNode).RemoveSelf();
            }
        }

        // Add missing nodes and fix ordering by index
        // Behaviors do not support hierarchy so all instances are at the top level
        for (int i = 0; i < allInstances.Count; i++)
        {
            var instance = allInstances[i];
            GumTreeNode nodeForInstance = GetTreeNodeFor(instance, node);

            if (nodeForInstance == null)
            {
                nodeForInstance = (GumTreeNode)AddTreeNodeForInstance(instance, (ITreeNodeMutable)node, tolerateMissingTypes: true);
            }

            if (instance.DefinedByBase)
            {
                nodeForInstance.ImageIndex = DerivedInstanceImageIndex;
            }

            ((ITreeNodeMutable)nodeForInstance).MoveToIndex(i);
        }
    }

    // parentContainerNode is ITreeNodeMutable for the same reason as AddTreeNodeForElement above.
    private ITreeNodeMutable AddTreeNodeForInstance(InstanceSave instance, ITreeNodeMutable parentContainerNode,
        bool tolerateMissingTypes, HashSet<InstanceSave>? pendingAdditions = null)
    {
        ITreeNodeMutable treeNode = new GumTreeNode();

        bool validBaseType = ObjectFinder.Self.GetElementSave(instance.BaseType) != null;

        treeNode.ImageIndex = _treeNodeImageLogic.GetInstanceCreateImageIndex(instance, validBaseType, tolerateMissingTypes);

        treeNode.SetTag(instance);

        ITreeNodeMutable parentNode = parentContainerNode;
        InstanceSave parentInstance = FindParentInstance(instance);

        if (parentInstance != null)
        {
            GumTreeNode parentInstanceNode = GetTreeNodeFor(parentInstance, (GumTreeNode)parentContainerNode);

            // Make sure we are not already trying to add the parent (protects against stack overflow with invalid data)
            if (parentInstanceNode == null && (pendingAdditions == null || !pendingAdditions.Contains(parentInstance)))
            {
                if (pendingAdditions == null)
                {
                    pendingAdditions = new HashSet<InstanceSave>();
                }

                pendingAdditions.Add(parentInstance);
                parentInstanceNode = (GumTreeNode)AddTreeNodeForInstance(parentInstance, parentContainerNode, tolerateMissingTypes, pendingAdditions);
            }

            if (parentInstanceNode != null)
            {
                parentNode = (ITreeNodeMutable)parentInstanceNode;
            }
        }

        parentNode.AddChild(treeNode);

        return treeNode;
    }

    private InstanceSave? FindParentInstance(InstanceSave instance)
    {
        if(instance is BehaviorInstanceSave)
        {
            // instances in behaviors cannot (currently) have parents
            return null;
        }
        else
        {
            ElementSave element = instance.ParentContainer ?? ObjectFinder.Self.GetElementContainerOf(instance);

            string name = instance.Name + ".Parent";
            VariableSave? variable = element.DefaultState.Variables.FirstOrDefault(v => v.Name == name);

            if (variable != null && variable.SetsValue && variable.Value != null)
            {
                string parentName = (string) variable.Value;

                // This could be attached to a child inside the parent. Therefore, if ParentInstance contains a dot, return 
                // the instance with the name before the dot
                if (parentName.Contains('.'))
                {
                    parentName = parentName.Substring(0, parentName.IndexOf('.'));
                }

                return element.GetInstance(parentName);
            }
        }

        return null;
    }

    bool IsInUiInitiatedSelection = false;

    /// <summary>
    /// When true, Select methods update the tree node visually but skip
    /// CallAfterClickSelect to avoid re-firing the plugin event cascade.
    /// Used when the tree view is syncing to match a selection that already
    /// triggered plugin events (e.g. InstanceSelected → tree sync).
    /// </summary>
    internal bool SuppressCallAfterClickSelect;
    internal void OnSelect(ITreeNode? selectedTreeNode)
    {
        GumTreeNode? treeNode = ObjectTreeView.SelectedNode;

        object? selectedObject = null;

        if (treeNode != null)
        {
            selectedObject = treeNode.Tag;
        }


        try
        {
            IsInUiInitiatedSelection = true;
            if (selectedObject == null)
            {
                _selectedState.SelectedElement = null;
                _selectedState.SelectedBehavior = null;
                _selectedState.SelectedInstance = null;

                // do nothing
            }
            else if(selectedObject is ElementSave elementSave)
            {
                _selectedState.SelectedInstance = null;
                var elements = this.SelectedNodes
                    .Where(item => item.Tag is ElementSave)
                    .Select(item => (ElementSave)item.Tag);

                _selectedState.SelectedElements = elements;
            }
            else if (selectedObject is InstanceSave selectedInstance)
            {
                var instances = this.SelectedNodes.Select(item => item.Tag)
                    .Where(item => item is InstanceSave)
                    .Select(item => (InstanceSave)item);

                //_selectedState.SelectedInstance = selectedInstance;
                _selectedState.SelectedInstances = instances;
            }
            else if(selectedObject is BehaviorSave behavior)
            {
                var behaviors = this.SelectedNodes.Select(item => item.Tag)
                    .Where(item => item is BehaviorSave)
                    .Select(item => (BehaviorSave)item);

                _selectedState.SelectedBehaviors = behaviors;
            }

            _pluginManager.TreeNodeSelected(selectedTreeNode);

        }
        finally
        {
            IsInUiInitiatedSelection = false;
        }
    }

    public void VerifyComponentsAreInTreeView(GumProjectSave gumProject)
    {
        foreach (ComponentSave component in gumProject.Components)
        {
            if (GetTreeNodeFor(component) == null)
            {
                throw new Exception();
            }
        }
    }

    internal void HandleKeyDown(WpfInput.KeyEventArgs e)
    {
        var didTreeViewHaveFocus = ObjectTreeView.IsKeyboardFocusWithin;

        if (e.Key == WpfInput.Key.Up || e.Key == WpfInput.Key.Down)
        {
            // The tree moves the selection in its own handler, so scroll to wherever it landed.
            if (ObjectTreeView.SelectedNode is { } selected)
            {
                ObjectTreeView.EnsureVisible(selected);
            }
            OnSelect(ObjectTreeView.SelectedNode);
        }

        GumKeyEventArgs keyArgs = e.ToGumKeyEventArgs();
        _hotkeyManager.HandleKeyDownElementTreeView(keyArgs);
        e.Handled = keyArgs.Handled;

        if (didTreeViewHaveFocus)
        {
            // On a delete, the popup appears, which steals focus from the treeview.
            // If we had focus before, let's get it now.
            ObjectTreeView.Focus();
        }

    }

    private void ObjectTreeView_AfterSelect(GumTreeNode? node)
    {
        // If we use AfterClickSelect instead of AfterSelect then
        // we don't get notified when the user selects nothing.
        // Update - we only want to do this if it's null:
        // Otherwise we can't drag drop
        if (ObjectTreeView.SelectedNode == null)
        {
            OnSelect((ITreeNode?)ObjectTreeView.SelectedNode);
        }
    }

    private void ObjectTreeView_AfterClickSelect(GumTreeNode? node)
    {
        OnSelect(ObjectTreeView.SelectedNode);
    }

    private void ObjectTreeView_ContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
    {
        OnSelect(ObjectTreeView.SelectedNode);

        PopulateContextMenu();

        if (_contextMenu.Items.Count == 0)
        {
            // Nothing applies to this selection; suppress rather than open an empty popup.
            e.Handled = true;
        }
    }

    private void ObjectTreeView_KeyDown(object? sender, WpfInput.KeyEventArgs e)
    {
        HandleKeyDown(e);
    }


    #endregion

    #region Searching



    private void ReactToFilterTextChanged()
    {
        var shouldExpand = false;

        if (!string.IsNullOrEmpty(filterText))
        {
            shouldExpand = true;
        }

        FlatList.Visibility = shouldExpand.ToVisibility();
        ObjectTreeView.Visibility = (!shouldExpand).ToVisibility();

        //RefreshUi();

        if (!string.IsNullOrEmpty(filterText) && SelectedNode?.Tag == null)
        {
            //SelectFirstElement();
        }


        if (shouldExpand)
        {

            FlatList.FlatList.Items.Clear();

            if(filterText != null)
            {
                var filterTextLower = filterText.ToLower();
                var project = _projectState.GumProjectSave;
                foreach (var screen in project.Screens)
                {
                    if (screen.Name.ToLower().Contains(filterTextLower))
                    {
                        AddToFlatList(screen);
                    }

                    if (deepSearchCheckBox.IsChecked is true)
                    {
                        SearchInstanceVariables(screen, filterTextLower);
                    }
                }
                foreach (var component in project.Components)
                {
                    if (component.Name.ToLower().Contains(filterTextLower))
                    {
                        AddToFlatList(component);
                    }

                    foreach (var instance in component.Instances)
                    {
                        if (instance.Name.ToLower().Contains(filterTextLower))
                        {
                            AddToFlatList(instance, $"{component.Name}/{instance.Name} ({instance.BaseType})");
                        }
                    }

                    if (deepSearchCheckBox.IsChecked is true)
                    {
                        SearchInstanceVariables(component, filterTextLower);
                    }
                }
                foreach (var standard in project.StandardElements)
                {
                    if (standard.Name.ToLower().Contains(filterTextLower))
                    {
                        AddToFlatList(standard);
                    }

                    if (deepSearchCheckBox.IsChecked is true)
                    {
                        SearchInstanceVariables(standard, filterTextLower);
                    }
                }

                foreach(var behavior in project.Behaviors)
                {
                    // Feb 5, 2025 - at some point a behavior with an empty name
                    // snuck into a FRB project. We shouldn't crash here because of it...
                    if(behavior.Name?.ToLower().Contains(filterTextLower) == true)
                    {
                        AddToFlatList(behavior);
                    }
                }
            }

            if(FlatList.FlatList.Items.Count > 0)
            {
                FlatList.FlatList.SelectedIndex = 0;
            }
        }
    }

    private void SearchInstanceVariables(ElementSave element, string filterTextLower )
    {
        foreach (var state in element.AllStates)
        {
            foreach (var variable in state.Variables)
            {
                if (variable == null)
                {
                    continue;
                }

                if (variable.Value != null && (variable.Value is string str) && str.ToLower().Contains(filterTextLower))
                {
                    var instance = element.Instances.FirstOrDefault(item => item.Name == variable.SourceObject);
                    if(instance != null)
                    {
                        AddToFlatList(instance, $"{variable.Name}={variable.Value} on {element.Name}/{variable.SourceObject}");
                    }
                    else
                    {
                        AddToFlatList(element, $"{variable.Name}={variable.Value} on {element.Name}");
                    }
                }
            }
        }
    }

    private void AddToFlatList(object element, string customName = "")
    {
        if (element == null)
        {
            throw new ArgumentNullException($"{nameof(element)}");
        }
        var vm = new SearchItemViewModel();
        vm.BackingObject = element;
        vm.CustomText = customName;
        FlatList.FlatList.Items.Add(vm);
    }

    private void HandleSelectedSearchNode(SearchItemViewModel? vm)
    {
        var backingObject = vm?.BackingObject;
        if(backingObject != null)
        {
            if (backingObject is ScreenSave asScreen)
                _selectedState.SelectedElement = asScreen;
            else if (backingObject is ComponentSave asComponent)
                _selectedState.SelectedElement = asComponent;
            else if (backingObject is StandardElementSave asStandard)
                _selectedState.SelectedElement = asStandard;
            else if (backingObject is InstanceSave asInstance)
                _selectedState.SelectedInstance = asInstance;
            else if (backingObject is VariableSave asVariable)
                _selectedState.SelectedBehaviorVariable = asVariable;
            else if(backingObject is BehaviorSave asBehavior)
                _selectedState.SelectedBehavior = asBehavior;

            searchTextBox.Text = null;
            FilterText = string.Empty;
        }
    }


    #endregion


    internal void HandleMouseOver(int x, int y)
    {
        var objectOver = this.ObjectTreeView.GetNodeAt(new System.Windows.Point(x, y));

        ElementSave? element = null;
        InstanceSave? instance = null;

        if(objectOver != null && objectOver.Tag != null)
        {
            if(objectOver.Tag is ElementSave)
            {
                element = objectOver.Tag as ElementSave;
            }
            else if(objectOver.Tag is InstanceSave)
            {
                instance = objectOver.Tag as InstanceSave;
            }
        }

        GraphicalUiElement? whatToHighlight = null;

        if(element != null)
        {
            whatToHighlight = _wireframeObjectManager.GetRepresentation(element);
        }
        else if(instance != null)
        {
            whatToHighlight = _wireframeObjectManager.GetRepresentation(instance, null);
        }

        if(_pluginManager.IsInitialized)
        {
            _pluginManager.SetHighlightedIpso(whatToHighlight);
        }
    }

    internal void HighlightTreeNodeForIpso(IPositionedSizedObject? ipso)
    {
        if (ipso == null)
        {
            ObjectTreeView.SetExternalHotNode(null);
            return;
        }

        GumTreeNode? treeNode = null;

        if (ipso.Tag is InstanceSave instance)
        {
            GumTreeNode? containerNode = GetTreeNodeFor(_selectedState.SelectedElement);
            if (containerNode == null)
            {
                var behavior = ObjectFinder.Self.GetBehaviorContainerOf(instance);
                if (behavior != null)
                {
                    containerNode = GetTreeNodeFor(behavior);
                }
            }
            if (containerNode == null && instance.ParentContainer != null)
            {
                containerNode = GetTreeNodeFor(instance.ParentContainer);
            }
            if (containerNode != null)
            {
                treeNode = GetTreeNodeFor(instance, containerNode);
            }
        }
        else if (ipso.Tag is ElementSave element)
        {
            treeNode = GetTreeNodeFor(element);
        }

        ObjectTreeView.SetExternalHotNode(treeNode);
    }

    void IRecipient<ApplicationStartupMessage>.Receive(ApplicationStartupMessage message)
    {
        _viewCreator.ApplyThemeColors();
    }
}
