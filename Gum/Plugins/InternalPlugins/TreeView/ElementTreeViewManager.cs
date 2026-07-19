using CommonFormsAndControls;
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
using MaterialDesignThemes.Wpf;
using RenderingLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using ToolsUtilities;
using Application = System.Windows.Application;
using Binding = System.Windows.Data.Binding;
using Color = System.Drawing.Color;
using Cursors = System.Windows.Forms.Cursors;
using DragAction = System.Windows.Forms.DragAction;
using DragDropEffects = System.Windows.Forms.DragDropEffects;
using DragEventArgs = System.Windows.Forms.DragEventArgs;
using Grid = System.Windows.Controls.Grid;
using MessageBox = System.Windows.Forms.MessageBox;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using SystemColors = System.Drawing.SystemColors;
using WpfInput = System.Windows.Input;

namespace Gum.Managers;

#region ExpandedState class
class ExpandedState
{
    public bool ScreensExpanded { get; set; }
    public bool ComponentsExpanded { get; set; }
    public bool StandardsExpanded { get; set; }
    public bool BehaviorsExpanded { get; set; }

    public Dictionary<TreeNode, bool> ExpandedStates { get; set; } = new Dictionary<TreeNode, bool>();

    public void Record(TreeNode treeNode)
    {
        ExpandedStates[treeNode] = treeNode.IsExpanded;

        foreach(TreeNode subNode in treeNode.Nodes)
        {
            // we only care about directory nodes, as only those are going to be recorded.
            if (subNode.Nodes.Count > 0 && subNode.Tag == null)
            {
                // record this bad boy:
                Record(subNode);
            }
        }
    }
    public void Apply()
    {
        foreach(var kvp in ExpandedStates)
        {
            if(kvp.Value)
            {
                kvp.Key.Expand();
            }
            else// if(nodesToKeepExpanded.Contains(kvp.Key) == false)
            {
                kvp.Key.Collapse();
            }
        }
    }

    public void ExpandAll()
    {
        foreach(var kvp in ExpandedStates)
        {
            kvp.Key.Expand();
        }
    }
}
#endregion

#region TreeNodeWrapper Class
class TreeNodeWrapper : ITreeNode
{
    public TreeNode Node { get;  }
    public object Tag => Node.Tag;
    public string Text { get => Node.Text; set => Node.Text = value; }

    public ITreeNode? Parent => Node.Parent != null
        ? new TreeNodeWrapper(Node.Parent)
        : null;

    public FilePath GetFullFilePath() => Node.GetFullFilePath();

    public string FullPath => Node.FullPath;

    public TreeNodeWrapper(TreeNode node)
    {
        if(node == null)
        {
            throw new ArgumentNullException();
        }
        Node = node;
    }

    public void Expand() => Node.Expand();

    public override string ToString() => Node.Text;
}

#endregion

public partial class ElementTreeViewManager : IRecipient<ThemeChangedMessage>, IRecipient<ApplicationStartupMessage>
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

    public const int TransparentImageIndex = 0;
    public const int FolderImageIndex = 1;
    public const int ComponentImageIndex = 2;
    public const int InstanceImageIndex = 3;
    public const int ScreenImageIndex = 4;
    public const int StandardElementImageIndex = 5;
    public const int ExclamationIndex = 6;
    public const int StateImageIndex = 7;
    public const int BehaviorImageIndex = 8;
    public const int DerivedInstanceImageIndex = 9;
    public const int LockedInstanceImageIndex = 10;

    // Per-type standard element icons. Indices must match the TryInjectIcon
    // call order in ElementTreeViewCreator.InjectDynamicIcons.
    public const int ContainerImageIndex = 11;
    public const int SpriteImageIndex = 12;
    public const int NineSliceImageIndex = 13;
    public const int TextImageIndex = 14;
    public const int RectangleImageIndex = 15;
    public const int ColoredRectangleImageIndex = 16;
    public const int CircleImageIndex = 17;
    public const int ColoredCircleImageIndex = 18;
    public const int RoundedRectangleImageIndex = 19;
    public const int PolygonImageIndex = 20;
    public const int ArcImageIndex = 21;
    public const int LineImageIndex = 22;
    public const int CanvasImageIndex = 23;
    public const int LottieAnimationImageIndex = 24;
    public const int SvgImageIndex = 25;

    // Per-type instance icons (same shapes, blue tint instead of purple).
    // Indices must match the TryInjectIcon call order in
    // ElementTreeViewCreator.InjectDynamicIcons.
    public const int ContainerInstanceImageIndex = 26;
    public const int SpriteInstanceImageIndex = 27;
    public const int NineSliceInstanceImageIndex = 28;
    public const int TextInstanceImageIndex = 29;
    public const int RectangleInstanceImageIndex = 30;
    public const int ColoredRectangleInstanceImageIndex = 31;
    public const int CircleInstanceImageIndex = 32;
    public const int ColoredCircleInstanceImageIndex = 33;
    public const int RoundedRectangleInstanceImageIndex = 34;
    public const int PolygonInstanceImageIndex = 35;
    public const int ArcInstanceImageIndex = 36;
    public const int LineInstanceImageIndex = 37;
    public const int CanvasInstanceImageIndex = 38;
    public const int LottieAnimationInstanceImageIndex = 39;
    public const int SvgInstanceImageIndex = 40;

    // The per-type icon maps and the node icon-decision logic now live on TreeNodeImageLogic
    // (accessed via _treeNodeImageLogic) so they can be unit-tested without the WinForms tree.

    // Part of the phantom right-click workaround. Subscribed in Initialize(),
    // checked in ObjectTreeView_MouseClick(). See comments at both sites.
    private MouseButtons _lastMouseDownButton;

    // Forwarding properties for UI controls owned by _viewCreator
    internal MultiSelectTreeView ObjectTreeView => _viewCreator.ObjectTreeView;
    private System.Windows.Controls.ContextMenu _contextMenu => _viewCreator.ContextMenu;
    private FlatSearchListBox FlatList => _viewCreator.FlatList;
    private System.Windows.Forms.Integration.WindowsFormsHost TreeViewHost => _viewCreator.TreeViewHost;
    private System.Windows.Controls.TextBox searchTextBox => _viewCreator.SearchTextBox;
    private System.Windows.Controls.CheckBox deepSearchCheckBox => _viewCreator.DeepSearchCheckBox;

    internal void UpdateCollapseButtonSizes(double baseFontSize) =>
        _viewCreator.UpdateCollapseButtonSizes(baseFontSize);

    TreeNode mScreensTreeNode;
    TreeNode mComponentsTreeNode;
    TreeNode mStandardElementsTreeNode;
    TreeNode mBehaviorsTreeNode;
    TreeNode? mLastHoveredNode;
    private DateTime? hoverStartTime;

    private Cursor AddCursor { get; }


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
                return new TreeNodeWrapper( ObjectTreeView.SelectedNode);
            }
        }
        set
        {
            ObjectTreeView.SelectedNode = (value as TreeNodeWrapper)?.Node;
        }
    }

    public List<ITreeNode> SelectedNodes
    {
        get
        {
            return ObjectTreeView.SelectedNodes.Select(item => new TreeNodeWrapper(item)).ToList<ITreeNode>();
        }
    }

    ExpandedState expandedStateBeforeFilter;
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
        TreeNode treeNode = 
            ObjectTreeView.Nodes.FirstOrDefault() as TreeNode;

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

    public TreeNode RootScreensTreeNode => mScreensTreeNode;

    public TreeNode RootComponentsTreeNode => mComponentsTreeNode;

    public TreeNode RootStandardElementsTreeNode => mStandardElementsTreeNode;

    public TreeNode RootBehaviorsTreeNode => mBehaviorsTreeNode;

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
            var mousePosition = Control.MousePosition;
            var clientPoint = ObjectTreeView.PointToClient(mousePosition);
            return ObjectTreeView.ClientRectangle.Contains(clientPoint);
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
        TreeNodeExtensionMethods.ElementTreeViewManager = this;
        AddCursor = GetAddCursor();
        _dragDropManager = dragDropManager;
        _viewCreator = new ElementTreeViewCreator();

        Cursor GetAddCursor()
        {
            try
            {
                return new Cursor(typeof(Gum.Program), "Content.Cursors.AddCursor.cur");
            }
            catch
            {
                // Vic got this to crash on Sean's machine. Not sure why, but let's tolerate it since it's not breaking
                return Cursor.Current;
            }
        }
    }

    #region Methods


    #region Find/Get
    public TreeNode? GetTreeNodeFor(ElementSave? elementSave)
    {
        if (elementSave == null)
        {
            return null;
        }
        else if (elementSave is ScreenSave screenSave)
        {
            return GetTreeNodeFor(screenSave);
        }
        else if (elementSave is ComponentSave componentSave)
        {
            return GetTreeNodeFor(componentSave);
        }
        else if (elementSave is StandardElementSave standardElementSave)
        {
            return GetTreeNodeFor(standardElementSave);
        }

        return null;
    }

    public TreeNode GetTreeNodeFor(ScreenSave screenSave)
    {
        return GetTreeNodeForTag(screenSave, RootScreensTreeNode);
    }

    public TreeNode GetTreeNodeFor(ComponentSave componentSave)
    {
        return GetTreeNodeForTag(componentSave, RootComponentsTreeNode);
    }

    public TreeNode GetTreeNodeFor(StandardElementSave standardElementSave)
    {
        return GetTreeNodeForTag(standardElementSave, RootStandardElementsTreeNode);
    }

    public TreeNode? GetTreeNodeFor(InstanceSave instanceSave, TreeNode container)
    {
        foreach (TreeNode node in container.Nodes)
        {
            if (node.Tag == instanceSave)
            {
                return node;
            }

            TreeNode? childNode = GetTreeNodeFor(instanceSave, node);
            if (childNode != null)
            {
                return childNode;
            }
        }

        return null;
    }

    public TreeNode? GetInstanceTreeNodeByName(string name, TreeNode container)
    {
        foreach (TreeNode node in container.Nodes)
        {
            if (node.Tag is InstanceSave instanceSave && instanceSave.Name == name)
            {
                return node;
            }

            TreeNode? childNode = GetInstanceTreeNodeByName(name, node);
            if (childNode != null)
            {
                return childNode;
            }
        }

        return null;
    }

    public TreeNode GetTreeNodeFor(BehaviorSave behavior)
    {
        return GetTreeNodeForTag(behavior, RootBehaviorsTreeNode);
    }

    public void UpdateErrorIndicatorsForElement(ElementSave element, bool hasErrors)
    {
        var treeNode = GetTreeNodeFor(element);
        if (treeNode == null) return;

        int desiredIndex = _treeNodeImageLogic.GetElementRefreshImageIndex(element, hasErrors);

        if (treeNode.ImageIndex != desiredIndex)
            treeNode.ImageIndex = desiredIndex;
    }

    public TreeNode GetTreeNodeFor(string absoluteDirectory)
    {
        string relative = FileManager.MakeRelative(absoluteDirectory,
            FileManager.GetDirectory(_projectState.GumProjectSave.FullFileName));


        relative = FileManager.Standardize(relative);
        // in the tool we use forward slashes:
        relative = relative.Replace("\\", "/");

        if (relative.StartsWith("screens/"))
        {
            string modifiedRelative = relative.Substring("screens/".Length);

            return GetTreeNodeFor(modifiedRelative, mScreensTreeNode);
        }
        else if (relative.StartsWith("components/"))
        {
            string modifiedRelative = relative.Substring("components/".Length);

            return GetTreeNodeFor(modifiedRelative, mComponentsTreeNode);
        }
        else if (relative.StartsWith("standards/"))
        {
            string modifiedRelative = relative.Substring("standards/".Length);

            return GetTreeNodeFor(modifiedRelative, mStandardElementsTreeNode);
        }
        else if(relative.StartsWith("behaviors/"))
        {
            string modifiedRelative = relative.Substring("behaviors/".Length);

            return GetTreeNodeFor(modifiedRelative, mBehaviorsTreeNode);
        }

        return null;

    }

    TreeNode GetTreeNodeFor(string relativeDirectory, TreeNode container)
    {
        if (string.IsNullOrEmpty(relativeDirectory))
        {
            return container;
        }

        int indexOfSlash = relativeDirectory.IndexOf('/');
        string whatToLookFor = relativeDirectory;
        string sub = "";

        if (indexOfSlash != -1)
        {
            whatToLookFor = relativeDirectory.Substring(0, indexOfSlash);
            sub = relativeDirectory.Substring(indexOfSlash + 1, relativeDirectory.Length - (indexOfSlash + 1));
        }

        foreach (TreeNode node in container.Nodes)
        {
            if (node.Text.Equals(whatToLookFor, StringComparison.OrdinalIgnoreCase))
            {
                return GetTreeNodeFor(sub, node);
            }
        }

        return null;
    }

    TreeNode GetTreeNodeForTag(object tag, TreeNode container = null)
    {
        if (container == null)
        {
            if (tag is ScreenSave)
            {
                container = RootScreensTreeNode;
            }
            else if (tag is ComponentSave)
            {
                container = RootComponentsTreeNode;
            }
            else if (tag is StandardElementSave)
            {
                container = RootStandardElementsTreeNode;
            }
            else if(tag is BehaviorSave)
            {
                container = RootBehaviorsTreeNode;
            }
        }

        foreach (TreeNode treeNode in container.Nodes)
        {
            if (treeNode.Tag == tag)
            {
                return treeNode;
            }

            var found = GetTreeNodeForTag(tag, treeNode);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    public ITreeNode? GetTreeNodeOver()
    {
        System.Drawing.Point point = ObjectTreeView.PointToClient(Cursor.Position);

        var nodeAtPoint = ObjectTreeView.GetNodeAt(point);

        if(nodeAtPoint == null)
        {
            return null;
        }
        else
        {
            return new TreeNodeWrapper(nodeAtPoint);
        }
    }

    #endregion
    

    public void Initialize()
    {

        var grid = _viewCreator.CreateView(
            onAfterClickSelect: this.ObjectTreeView_AfterClickSelect,
            onAfterSelect: this.ObjectTreeView_AfterSelect_1,
            onKeyDown: this.ObjectTreeView_KeyDown,
            onMouseClick: this.ObjectTreeView_MouseClick,
            onMouseMove: (x, y) => HandleMouseOver(x, y),
            onFontChanged: (sender, _) =>
            {
                if (sender is MultiSelectTreeView { Font: { Size: var fontSize } })
                {
                    const float defaultFontSize = 9f;
                    _viewCreator.UpdateTreeviewIcons(fontSize / defaultFontSize);
                }
            },
            onDragOver: (sender, e) =>
            {
                // allow file drops
                if (e.Data?.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop) == true)
                {
                    e.Effect = DragDropEffects.Copy;
                }

                // allow Standards-palette chip drops onto a Screen/Component (or instance) node
                if (e.Data?.GetDataPresent(DragDropManager.StandardElementNameDataFormat) == true)
                {
                    e.Effect = GetChipDropTargetNode(e) != null
                        ? DragDropEffects.Copy
                        : DragDropEffects.None;
                }

                // auto expand hovered nodes when they're collapsed
                var treeview = (MultiSelectTreeView?)sender;
                Point? pointWithinTreeview = treeview?.PointToClient(new Point(e.X, e.Y));
                if (pointWithinTreeview != null && treeview?.GetNodeAt(pointWithinTreeview.Value) is { } hovered)
                {
                    DelayExpandHoveredNode(hovered);
                }
            },
            onDragDrop: (_, e) =>
            {
                if (e.Data?.GetData(System.Windows.Forms.DataFormats.FileDrop) is string[] files)
                {
                    _dragDropManager.OnFilesDroppedInTreeView(files);
                }
                else if (e.Data?.GetData(DragDropManager.StandardElementNameDataFormat) is string standardTypeName
                    && GetChipDropTargetNode(e) is { } targetNode
                    && ObjectFinder.Self.GetStandardElement(standardTypeName) is { } standardElement)
                {
                    _dragDropManager.HandleDroppedStandardElementOnTreeNode(
                        standardElement, new TreeNodeWrapper(targetNode));
                }
            },
            onQueryContinueDrag: (_, e) =>
            {
                if (e.Action != DragAction.Continue)
                {
                    _dispatcher.Post(() =>
                    {
                        OnSelect(ObjectTreeView.SelectedNode);
                    });
                }
            },
            onValidateSortingDrop: (_, e) =>
            {
                e.Allow = false;

                if (ProcessDrop(e.TargetNode, e.Kind) is { } drop)
                {
                    IEnumerable<ITreeNode> wrappedNodes = e.DraggedNodes.Select(n => new TreeNodeWrapper(n));
                    ITreeNode wrappedTarget = new TreeNodeWrapper(drop.TreeTarget);

                    e.Allow = _dragDropManager.ValidateNodeSorting(wrappedNodes, wrappedTarget, drop.Drop);
                }
            },
            onNodeSortingDropped: (_, e) =>
            {
                if (ProcessDrop(e.TargetNode, e.Kind) is { } drop)
                {
                    IEnumerable<ITreeNode> wrappedNodes = e.DraggedNodes.Select(n => new TreeNodeWrapper(n));
                    ITreeNode wrappedTarget = new TreeNodeWrapper(drop.TreeTarget);

                    e.Kind = e.Kind == MultiSelectTreeView.DropKind.None
                        ? MultiSelectTreeView.DropKind.None
                        : MultiSelectTreeView.DropKind.Into;

                    _dragDropManager.OnNodeSortingDropped(wrappedNodes, wrappedTarget, drop.Drop);
                }
                e.PerformNativeReorder = false;
            },
            onGiveFeedback: (sender, e) =>
            {
                if (InputLibrary.Cursor.Self.IsInWindow)
                {
                    e.UseDefaultCursors = false;
                    System.Windows.Forms.Cursor.Current = AddCursor;
                }
            },
            onFilterTextChanged: text => FilterText = text,
            onSearchNodeSelected: HandleSelectedSearchNode,
            onCollapseAll: () => _collapseToggleService.HandleCollapseAll(ObjectTreeView, () => _viewCreator.CollapseAll()),
            onCollapseToElementLevel: () => _collapseToggleService.HandleCollapseToElementLevel(ObjectTreeView, () => _viewCreator.CollapseToElementLevel()),
            onDeepSearchChecked: () => ReactToFilterTextChanged());

        _tabManager.AddControl(grid, "Project", TabLocation.Left);

        // Workaround for WinForms/WPF interop phantom right-click bug:
        // When a WPF ContextMenu popup dismisses, WinForms never receives the
        // right-button MouseUp. This corrupts WinForms' internal state, causing
        // subsequent left-clicks to generate MouseClick events with e.Button=Right
        // (and no corresponding MouseDown). We track the real button from MouseDown
        // and reset on MouseUp so ObjectTreeView_MouseClick can distinguish real
        // right-clicks from phantom ones. See _lastMouseDownButton field and
        // ObjectTreeView_MouseClick for the other half of this fix.
        ObjectTreeView.MouseDown += (_, e) => _lastMouseDownButton = e.Button;
        ObjectTreeView.MouseUp += (_, _) => _lastMouseDownButton = MouseButtons.None;
        ObjectTreeView.AfterExpand += (_, _) => _collapseToggleService.OnNodeManuallyChanged();
        ObjectTreeView.AfterCollapse += (_, _) => _collapseToggleService.OnNodeManuallyChanged();
        ObjectTreeView.UnhandledException += ex => _dialogService.ShowMessage(ex.Message);
        ObjectTreeView.NavigateBackRequested += (_, _) => _selectionHistory.NavigateBack();
        ObjectTreeView.NavigateForwardRequested += (_, _) => _selectionHistory.NavigateForward();

        ConfigureStandardsPalette();

        RefreshUi();
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
            bool isInTree = ObjectTreeView.Nodes.Contains(mStandardElementsTreeNode);
            if (usePalette && isInTree)
            {
                ObjectTreeView.Nodes.Remove(mStandardElementsTreeNode);
            }
            else if (!usePalette && !isInTree)
            {
                // Restore in canonical order: after Components, before Behaviors.
                int insertIndex = mBehaviorsTreeNode != null
                    ? ObjectTreeView.Nodes.IndexOf(mBehaviorsTreeNode)
                    : ObjectTreeView.Nodes.Count;
                if (insertIndex < 0)
                {
                    insertIndex = ObjectTreeView.Nodes.Count;
                }
                ObjectTreeView.Nodes.Insert(insertIndex, mStandardElementsTreeNode);
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
        var gumProject = _projectState.GumProjectSave;
        List<string> typeNames = gumProject == null
            ? new List<string>()
            : gumProject.StandardElements
                .Where(standard => standard.Name != "Component")
                .Select(standard => standard.Name)
                .ToList();

        _viewCreator.StandardsPalette.RefreshChips(typeNames);
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
    private TreeNode? GetChipDropTargetNode(DragEventArgs e)
    {
        Point clientPoint = ObjectTreeView.PointToClient(new Point(e.X, e.Y));
        TreeNode? node = ObjectTreeView.GetNodeAt(clientPoint);
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
    internal static (TreeNode TreeTarget, DropTarget? Drop)? ProcessDrop(TreeNode? originalTarget, MultiSelectTreeView.DropKind kind)
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
            case MultiSelectTreeView.DropKind.Into:
            case MultiSelectTreeView.DropKind.IntoFirst:
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
            case MultiSelectTreeView.DropKind.After:
            case MultiSelectTreeView.DropKind.Before:
            {
                TreeNode? parent = originalTarget.Parent;
                if (parent == null)
                {
                    return null;
                }
                if (originalTarget.Tag is InstanceSave sibling && sibling.ParentContainer != null)
                {
                    InstanceSave? parentInstance = parent.Tag as InstanceSave;
                    DropPosition position = kind == MultiSelectTreeView.DropKind.Before
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

    private void DelayExpandHoveredNode(TreeNode hoveredNode)
    {
        // Can't do this, it seems to interfere with the Undo History
        //treeview.SelectedNode = hoveredNode;

        // So...lets fake it with backcolor/forecolor instead?
        if (mLastHoveredNode != hoveredNode)
        {
            hoverStartTime = DateTime.Now;
            mLastHoveredNode = hoveredNode;

            // If partially off the screen, make it visible
            if (!hoveredNode.IsVisible)
                hoveredNode.EnsureVisible();
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
            AddAndRemoveFolderNodesFromFileSystem(mStandardElementsTreeNode.GetFullFilePath()!.FullPath, mStandardElementsTreeNode.Nodes);
            AddAndRemoveFolderNodesFromFileSystem(mScreensTreeNode.GetFullFilePath()!.FullPath, mScreensTreeNode.Nodes);
            AddAndRemoveFolderNodesFromFileSystem(mComponentsTreeNode.GetFullFilePath()!.FullPath, mComponentsTreeNode.Nodes);
            AddAndRemoveFolderNodesFromFileSystem(mBehaviorsTreeNode.GetFullFilePath()!.FullPath, mBehaviorsTreeNode.Nodes);


            AddNeededButMissingFromFileSystemFolderNodes();
            //AddAndRemoveFolderNodes(currentDirectory, this.mTreeView.Nodes);
        }
        else
        {
            RootScreensTreeNode.Nodes.Clear();
            RootComponentsTreeNode.Nodes.Clear();
            // maybe we support behavior folders in the future? If so:
            RootBehaviorsTreeNode.Nodes.Clear();
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

    private TreeNode CreateNodeIfNecessary(string directory)
    {
        var treeNode = GetTreeNodeFor(directory);

        if(treeNode == null)
        {
            TreeNode? parentNode = null;
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
                treeNode = parentNode.Nodes.Add(treeNodeText);
                treeNode.ImageIndex = ExclamationIndex;
            }
        }

        return treeNode!;
    }

    private void AddAndRemoveFolderNodesFromFileSystem(string currentDirectory, TreeNodeCollection nodesToAddTo)
    {
        // todo: removes
        var directories = Directory.EnumerateDirectories(currentDirectory).ToArray();

        foreach (string directory in directories)
        {
            TreeNode existingTreeNode = GetTreeNodeFor(directory);

            if (existingTreeNode == null)
            {
                existingTreeNode = nodesToAddTo.Add(FileManager.RemovePath(directory));
                existingTreeNode.ImageIndex = FolderImageIndex;
            }
            AddAndRemoveFolderNodesFromFileSystem(directory, existingTreeNode.Nodes);
        }

        for(int i = nodesToAddTo.Count - 1; i > -1; i--)
        {
            TreeNode node = nodesToAddTo[i];

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
                nodesToAddTo.RemoveAt(i);
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
                TreeNode parentNode = GetTreeNodeFor(fullPath);

                treeNode = AddTreeNodeForElement(screenSave, parentNode, ScreenImageIndex);
            }
        }

        foreach (ComponentSave componentSave in gumProject.Components)
        {
            if (GetTreeNodeFor(componentSave) == null && ShouldShow(componentSave))
            {
                string fullPath = _fileLocations.ComponentsFolder + FileManager.GetDirectory(componentSave.Name);
                TreeNode parentNode = GetTreeNodeFor(fullPath);

                if(parentNode == null)
                {
                    throw new Exception($"Error trying to get parent node for component {fullPath}");
                }

                AddTreeNodeForElement(componentSave, parentNode, ComponentImageIndex);
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
                TreeNode parentNode = GetTreeNodeFor(fullPath);

                AddTreeNodeForBehavior(behaviorSave, parentNode, BehaviorImageIndex);
            }
        }

        #endregion

        #region Remove nodes that are no longer needed

        void RemoveScreenRecursively(TreeNode treeNode, int i, TreeNode container)
        {
            ScreenSave? screen = treeNode.Tag as ScreenSave;

            // If the screen is null, that means that it's a folder TreeNode, so we don't want to remove it
            if (screen != null)
            {
                if (!gumProject.Screens.Contains(screen) || !ShouldShow(screen))
                {
                    container.Nodes.RemoveAt(i);
                }
            }
            else if(treeNode.Nodes != null)
            {
                for(int subI = treeNode.Nodes.Count - 1; subI > -1; subI--)
                {
                    var subnode = treeNode.Nodes[subI];
                    RemoveScreenRecursively(subnode, subI, treeNode);
                }
            }
        }

        for (int i = mScreensTreeNode.Nodes.Count - 1; i > -1; i--)
        {
            RemoveScreenRecursively(mScreensTreeNode.Nodes[i] as TreeNode, i, mScreensTreeNode);
        }

        void RemoveComponentRecursively(TreeNode treeNode, int i, TreeNode container)
        {
            ComponentSave? component = treeNode.Tag as ComponentSave;

            // If the component is null, that means that it's a folder TreeNode, so we don't want to remove it
            if (component != null)
            {
                if (!gumProject.Components.Contains(component) || !ShouldShow(component))
                {
                    container.Nodes.RemoveAt(i);
                }
            }
            else if (treeNode.Nodes != null)
            {
                for (int subI = treeNode.Nodes.Count - 1; subI > -1; subI--)
                {
                    var subnode = treeNode.Nodes[subI];
                    RemoveComponentRecursively(subnode, subI, treeNode);
                }
            }
        }

        for (int i = mComponentsTreeNode.Nodes.Count - 1; i > -1; i--)
        {
            RemoveComponentRecursively(mComponentsTreeNode.Nodes[i], i, mComponentsTreeNode);
        }

        for (int i = mStandardElementsTreeNode.Nodes.Count - 1; i > -1; i-- )
        {
            // Do we want to support folders here?
            StandardElementSave? standardElement = mStandardElementsTreeNode.Nodes[i].Tag as StandardElementSave;

            if (standardElement == null || !gumProject.StandardElements.Contains(standardElement) || !ShouldShow(standardElement))
            {
                mStandardElementsTreeNode.Nodes.RemoveAt(i);
            }
        }

        for(int i = mBehaviorsTreeNode.Nodes.Count - 1; i > -1; i--)
        {
            BehaviorSave? behavior = mBehaviorsTreeNode.Nodes[i].Tag as BehaviorSave;

            if(behavior != null)
            {
                if(behavior == null || !gumProject.Behaviors.Contains(behavior) || !ShouldShow(behavior))
                {
                    mBehaviorsTreeNode.Nodes.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Update the nodes

        // February 2, 2025
        // Not sure why exactly
        // but if we foreach here,
        // it can result in the enumerator
        // returning a null instance. This is
        // fixed by moving to a for-loop.
        var screenList = mScreensTreeNode.Nodes;
        for (int i = 0; i < screenList.Count; i++)
        {
            var treeNode = (TreeNode)screenList[i];
            RefreshUi(treeNode);
        }

        // see above on why we use a for instead foreach
        var componentList = mComponentsTreeNode.Nodes;
        for (int i = 0; i < componentList.Count; i++)
        {
            var treeNode = (TreeNode)componentList[i];
            RefreshUi(treeNode);
        }

        foreach (TreeNode treeNode in mStandardElementsTreeNode.Nodes)
        {
            RefreshUi(treeNode);
        }

        foreach(TreeNode treeNode in mBehaviorsTreeNode.Nodes)
        {
            RefreshUi(treeNode);
        }

        #endregion

        #region Sort everything

        mScreensTreeNode.Nodes.SortByName(recursive:true);

        mComponentsTreeNode.Nodes.SortByName(recursive: true);

        mStandardElementsTreeNode.Nodes.SortByName(recursive: true);

        mBehaviorsTreeNode.Nodes.SortByName(recursive: true);

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

    private TreeNode AddTreeNodeForElement(ElementSave element, TreeNode parentNode, int defaultImageIndex)
    {
        if (parentNode == null)
        {
            throw new NullReferenceException($"{nameof(parentNode)} cannot be null");
        }
        TreeNode treeNode = new TreeNode();

        treeNode.ImageIndex = _treeNodeImageLogic.GetCreateImageIndex(element.IsSourceFileMissing, defaultImageIndex);

        treeNode.Tag = element;
        
        parentNode.Nodes.Add(treeNode);

        return treeNode;
    }

    private void AddTreeNodeForBehavior(BehaviorSave behavior, TreeNode parentNode, int defaultImageIndex)
    {
        TreeNode treeNode = new TreeNode();

        treeNode.ImageIndex = _treeNodeImageLogic.GetCreateImageIndex(behavior.IsSourceFileMissing, defaultImageIndex);

        treeNode.Tag = behavior;

        parentNode.Nodes.Add(treeNode);
    }

    private void CreateRootTreeNodesIfNecessary()
    {
        if (mScreensTreeNode == null)
        {
            mScreensTreeNode = new TreeNode("Screens");
            mScreensTreeNode.ImageIndex = FolderImageIndex;
            ObjectTreeView.Nodes.Add(mScreensTreeNode);

            mComponentsTreeNode = new TreeNode("Components");
            mComponentsTreeNode.ImageIndex = FolderImageIndex;
            ObjectTreeView.Nodes.Add(mComponentsTreeNode);

            mStandardElementsTreeNode = new TreeNode("Standard");
            mStandardElementsTreeNode.ImageIndex = FolderImageIndex;
            // When the experimental Standards palette is on, the Standard folder is replaced by the
            // chip palette, so it is not shown in the tree. The node object is still kept (and still
            // populated) so toggling the setting at runtime can restore it without a full rebuild.
            if (!_projectState.EffectiveUseStandardsPalette)
            {
                ObjectTreeView.Nodes.Add(mStandardElementsTreeNode);
            }

            mBehaviorsTreeNode = new TreeNode("Behaviors");
            mBehaviorsTreeNode.ImageIndex = FolderImageIndex;
            ObjectTreeView.Nodes.Add(mBehaviorsTreeNode);
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
                List<TreeNode> nodes = GetReselectableNodes(
                    _recordedSelectedInstances,
                    instance => FindTreeNodeFor(instance, mRecordedSelectedContainer));

                if (nodes.Count > 1)
                {
                    Select(nodes);
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
                    Select(desiredNode);
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
    internal static List<TreeNode> GetReselectableNodes(
        IReadOnlyList<InstanceSave> recordedInstances,
        Func<InstanceSave, TreeNode?> nodeFinder)
    {
        List<TreeNode> nodes = new List<TreeNode>();
        foreach (InstanceSave instance in recordedInstances)
        {
            TreeNode? node = nodeFinder(instance);
            if (node != null)
            {
                nodes.Add(node);
            }
        }
        return nodes;
    }

    private TreeNode? FindTreeNodeForRecordedObject() =>
        FindTreeNodeFor(mRecordedSelectedObject, mRecordedSelectedContainer);

    private TreeNode? FindTreeNodeFor(object? recordedObject, object? recordedContainer)
    {
        if (recordedObject is InstanceSave instanceSave)
        {
            var behavior = ObjectFinder.Self.GetBehaviorContainerOf(instanceSave);
            if (behavior != null)
            {
                var behaviorNode = GetTreeNodeFor(behavior);
                if (behaviorNode != null)
                    return GetTreeNodeFor(instanceSave, behaviorNode)
                        ?? GetInstanceTreeNodeByName(instanceSave.Name, behaviorNode);
            }

            if (instanceSave.ParentContainer != null)
            {
                var elementNode = GetTreeNodeFor(instanceSave.ParentContainer);
                if (elementNode != null)
                    return GetTreeNodeFor(instanceSave, elementNode)
                        ?? GetInstanceTreeNodeByName(instanceSave.Name, elementNode);
            }

            // Behavior instances have no ParentContainer, and GetBehaviorContainerOf fails when
            // the reference is stale (undo/redo replaces instances with deep-cloned snapshots).
            // Fall back to name-based search using the container recorded before the refresh.
            if (!string.IsNullOrEmpty(instanceSave.Name) &&
                recordedContainer is BehaviorSave recordedBehavior)
            {
                var behaviorNode = GetTreeNodeFor(recordedBehavior);
                if (behaviorNode != null)
                    return GetInstanceTreeNodeByName(instanceSave.Name, behaviorNode);
            }

            return null;
        }
        else if (recordedObject is ElementSave elementSave)
        {
            return GetTreeNodeFor(elementSave);
        }
        else if (recordedObject is BehaviorSave behaviorSave)
        {
            return GetTreeNodeFor(behaviorSave);
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
    // MultiSelectTreeView class has an event called AfterClickSelect
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
            TreeNode? parentTreeNode = GetTreeNodeFor(parent);

            // This could be null if the user started a new project or loaded a different project.
            if (parentTreeNode != null)
            {
                Select(GetTreeNodeFor(instanceSave, parentTreeNode));
            }
        }
        else
        {
            Select((TreeNode?)null);
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

            TreeNode? parentContainer = null;
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

            List<TreeNode> treeNodeList = new List<TreeNode>();

            foreach (var item in list)
            {
                if(parentContainer != null)
                {
                    TreeNode itemTreeNode = GetTreeNodeFor(item, parentContainer);
                    treeNodeList.Add(itemTreeNode);
                }
            }

            Select(treeNodeList);
        }
        else
        {
            Select((TreeNode?)null);
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
                Select((TreeNode?)null);

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

    private void Select(TreeNode? treeNode)
    {
        if (IsInUiInitiatedSelection) return;

        // When the Standards palette is on, standard elements have no visible tree node (the "Standard"
        // folder is detached from the tree). Selecting one as data is valid — the variable grid loads
        // its defaults via the selection cascade — but a detached node must not be pushed into the
        // WinForms TreeView, which would throw. Skip the visual sync in that case.
        if (treeNode != null && treeNode.TreeView != ObjectTreeView)
        {
            return;
        }

        if (ObjectTreeView.SelectedNode != treeNode)
        {
            // See comment above about why we have to manually raise the AfterClick

            ObjectTreeView.SelectedNode = treeNode;

            if (treeNode != null)
            {
                treeNode.EnsureVisible();
            }

            if (!SuppressCallAfterClickSelect)
            {
                ObjectTreeView.CallAfterClickSelect(null, new TreeViewEventArgs(treeNode));
            }
        }
    }

    private void Select(List<TreeNode> treeNodes)
    {
        if (IsInUiInitiatedSelection) return;

        ObjectTreeView.SelectedNodes = treeNodes;

        if (treeNodes.Count != 0)
        {
            treeNodes[0]?.EnsureVisible();

            if (!SuppressCallAfterClickSelect)
            {
                ObjectTreeView.CallAfterClickSelect(null, new TreeViewEventArgs(treeNodes[0]));
            }
        }
    }

    /// <summary>
    /// Refreshes the entirety of the tree view, preserving selection.
    /// </summary>
    public void RefreshUi()
    {
        _collapseToggleService.Clear();
        var expandedPaths = _collapseToggleService.SaveExpandedPaths(ObjectTreeView);
        RecordSelection();
        // brackets are used simply to indicate the recording and selection should
        // go around the rest of the function:
        {
            ObjectTreeView.SuspendLayout();
            CreateRootTreeNodesIfNecessary();

            AddAndRemoveFolderNodes();

            AddAndRemoveScreensComponentsStandardsAndBehaviors();
            ObjectTreeView.ResumeLayout(performLayout:true);

        }
        SelectRecordedSelection();
        _collapseToggleService.RestoreExpandedPaths(ObjectTreeView, expandedPaths);

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
        var foundNode = GetTreeNodeForTag(instanceContainer);

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
        var foundNode = GetTreeNodeForTag(stateContainer);

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
    public void RefreshUi(TreeNode node)
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

        foreach (TreeNode treeNode in node.Nodes)
        {
            if(treeNode != null)
            {
                RefreshUi(treeNode);
            }

        }
    }

    private void RefreshElementTreeNode(TreeNode node, ElementSave elementSave)
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
            TreeNode desiredNode = GetTreeNodeFor(fullPath);
            var parentNode = node.Parent;
            if(parentNode != desiredNode)
            {
                if (parentNode != null)
                {
                    parentNode.Nodes.Remove(node);
                }
                if(desiredNode != null)
                {
                    desiredNode.Nodes.Add(node);
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
                node.Parent.Nodes.SortByName();
            }
        }

        var allTreeNodesRecursively = node.GetAllChildrenNodesRecursively();
        
        // why do we clear? wouldn't this require re-creation of all nodes? that seems like it might be slow...
        //node.Nodes.Clear();
        // Let's be smart about removal...
        foreach(TreeNode instanceNode in allTreeNodesRecursively)
        {
            var instance = instanceNode.Tag as InstanceSave;

            if(instance == null || !allInstances.Contains(instance))
            {
                instanceNode.Remove();
            }
        }

        List<List<InstanceSave>> siblingLists = new ();

        foreach (InstanceSave instance in allInstances)
        {
            TreeNode nodeForInstance = GetTreeNodeFor(instance, node);

            if (nodeForInstance == null)
            {
                nodeForInstance = AddTreeNodeForInstance(instance, node, tolerateMissingTypes:false);
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
                nodeForInstance.Remove();
                desiredParentNode.Nodes.Add(nodeForInstance);
            }

            var nodeParent = nodeForInstance.Parent;
            if (desiredIndex != nodeParent.Nodes.IndexOf(nodeForInstance))
            {
                nodeParent.Nodes.Remove(nodeForInstance);
                nodeParent.Nodes.Insert(desiredIndex, nodeForInstance);
            }

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

    private void RefreshBehaviorTreeNode(TreeNode node, BehaviorSave behavior)
    {
        var allInstances = behavior.RequiredInstances;

        // Remove nodes that no longer have a corresponding instance
        foreach (TreeNode instanceNode in node.Nodes.Cast<TreeNode>().ToList())
        {
            var instance = instanceNode.Tag as InstanceSave;
            if (instance == null || !allInstances.Contains(instance))
            {
                instanceNode.Remove();
            }
        }

        // Add missing nodes and fix ordering by index
        // Behaviors do not support hierarchy so all instances are at the top level
        for (int i = 0; i < allInstances.Count; i++)
        {
            var instance = allInstances[i];
            TreeNode nodeForInstance = GetTreeNodeFor(instance, node);

            if (nodeForInstance == null)
            {
                nodeForInstance = AddTreeNodeForInstance(instance, node, tolerateMissingTypes: true);
            }

            if (instance.DefinedByBase)
            {
                nodeForInstance.ImageIndex = DerivedInstanceImageIndex;
            }

            if (node.Nodes.IndexOf(nodeForInstance) != i)
            {
                node.Nodes.Remove(nodeForInstance);
                node.Nodes.Insert(i, nodeForInstance);
            }
        }
    }

    private TreeNode AddTreeNodeForInstance(InstanceSave instance, TreeNode parentContainerNode, 
        bool tolerateMissingTypes, HashSet<InstanceSave>? pendingAdditions = null)
    {
        TreeNode treeNode = new TreeNode();

        bool validBaseType = ObjectFinder.Self.GetElementSave(instance.BaseType) != null;

        treeNode.ImageIndex = _treeNodeImageLogic.GetInstanceCreateImageIndex(instance, validBaseType, tolerateMissingTypes);

        treeNode.Tag = instance;

        TreeNode parentNode = parentContainerNode;
        InstanceSave parentInstance = FindParentInstance(instance);

        if (parentInstance != null)
        {
            TreeNode parentInstanceNode = GetTreeNodeFor(parentInstance, parentContainerNode);

            // Make sure we are not already trying to add the parent (protects against stack overflow with invalid data)
            if (parentInstanceNode == null && (pendingAdditions == null || !pendingAdditions.Contains(parentInstance)))
            {
                if (pendingAdditions == null)
                {
                    pendingAdditions = new HashSet<InstanceSave>();
                }

                pendingAdditions.Add(parentInstance);
                parentInstanceNode = AddTreeNodeForInstance(parentInstance, parentContainerNode, tolerateMissingTypes, pendingAdditions);
            }

            if (parentInstanceNode != null)
            {
                parentNode = parentInstanceNode;
            }
        }

        parentNode.Nodes.Add(treeNode);

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
    internal void OnSelect(TreeNode? selectedTreeNode)
    {
        TreeNode? treeNode = ObjectTreeView.SelectedNode;

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

    internal void HandleKeyDown(KeyEventArgs e)
    {
        var didTreeViewHaveFocus = ObjectTreeView.ContainsFocus;

        if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
        {
            // Defer EnsureVisible so it runs after the native TreeView has processed
            // the arrow key and moved the selection. BeginInvoke posts to the message
            // queue, which executes after all synchronous WM_KEYDOWN handling (including
            // the AfterSelect notification) has completed.
            ObjectTreeView.BeginInvoke(() => ObjectTreeView.SelectedNode?.EnsureVisible());
            OnSelect((SelectedNode as TreeNodeWrapper)?.Node);
        }

        GumKeyEventArgs keyArgs = e.ToGumKeyEventArgs();
        _hotkeyManager.HandleKeyDownElementTreeView(keyArgs);
        e.Handled = keyArgs.Handled;
        e.SuppressKeyPress = keyArgs.SuppressKeyPress;

        if (didTreeViewHaveFocus)
        {
            // On a delete, the popup appears, which steals focus from the treeview.
            // If we had focus before, let's get it now.
            ObjectTreeView.Focus();
        }

    }

    private void ObjectTreeView_AfterSelect_1(object? sender, TreeViewEventArgs e)
    {
        // If we use AfterClickSelect instead of AfterSelect then
        // we don't get notified when the user selects nothing.
        // Update - we only want to do this if it's null:
        // Otherwise we can't drag drop
        if (ObjectTreeView.SelectedNode == null)
        {
            OnSelect(ObjectTreeView.SelectedNode);
        }
    }

    private void ObjectTreeView_AfterClickSelect(object? sender, TreeViewEventArgs e)
    {
        OnSelect(ObjectTreeView.SelectedNode);
    }

    // Known issue: WinForms and WPF don't play nice with right-clicks. After
    // right-clicking to open a context menu, if you immediately right-click a
    // different node, the context menu just closes. The old (and wanted) behavior
    // is that it would open a new context menu at the new right-click location.
    // This happens because the WPF ContextMenu popup captures the mouse, so the
    // second right-click is consumed by the popup dismiss and never reaches the
    // WinForms TreeView. Properly fixing this requires migrating the TreeView to WPF.
    private void ObjectTreeView_MouseClick(object? sender, MouseEventArgs e)
    {
        // Use _lastMouseDownButton instead of e.Button — see field comment for details.
        if (_lastMouseDownButton == MouseButtons.Right)
        {
            OnSelect(ObjectTreeView.SelectedNode);

            PopulateContextMenu();

            if (_contextMenu.Items.Count > 0)
            {
                _contextMenu.PlacementTarget = TreeViewHost;
                _contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                _contextMenu.IsOpen = true;
            }
        }
    }

    private void ObjectTreeView_KeyDown(object? sender, KeyEventArgs e)
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
        TreeViewHost.Visibility = (!shouldExpand).ToVisibility();

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

    private void HandleSelectedSearchNode(SearchItemViewModel vm)
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
        var objectOver = this.ObjectTreeView.GetNodeAt(x, y);

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

        TreeNode? treeNode = null;

        if (ipso.Tag is InstanceSave instance)
        {
            TreeNode? containerNode = GetTreeNodeFor(_selectedState.SelectedElement);
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


#region TreeNodeExtensionMethods

public static class TreeNodeExtensionMethods
{
    public static ElementTreeViewManager ElementTreeViewManager { get; set; } = default!;

    /// <summary>
    /// Determines whether the tree node represents a Screen element.
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if the node's Tag is a ScreenSave instance; otherwise, false.</returns>
    public static bool IsScreenTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag is ScreenSave;
    }

    /// <summary>
    /// Determines whether the tree node represents a Component element.
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if the node's Tag is a ComponentSave instance; otherwise, false.</returns>
    public static bool IsComponentTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag is ComponentSave;
    }

    /// <summary>
    /// Determines whether the tree node represents a Behavior.
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if the node's Tag is a BehaviorSave instance; otherwise, false.</returns>
    public static bool IsBehaviorTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag is BehaviorSave;
    }

    /// <summary>
    /// Determines whether the tree node represents a Standard element (e.g., Sprite, Text, Container).
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if the node's Tag is a StandardElementSave instance; otherwise, false.</returns>
    public static bool IsStandardElementTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag is StandardElementSave;
    }

    /// <summary>
    /// Determines whether the tree node is one of the top-level element container folders
    /// (Screens, Components, Standard, or Behaviors).
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if the node has no Tag (indicating a top-level folder or subfolder); otherwise, false.</returns>
    /// <remarks>
    /// This returns true for all top-level folders ONLY.
    /// Use IsTopScreenContainerTreeNode, IsTopComponentContainerTreeNode, IsTopStandardElementTreeNode,
    /// or IsTopBehaviorTreeNode to check for specific top-level folders only.
    /// Use IsScreensFolderTreeNode or IsComponentsFolderTreeNode to check for any folder under the
    /// Screens or Components hierarchy (excluding the top-level folders themselves).
    /// </remarks>
    public static bool IsTopElementContainerTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag == null && treeNode.Parent == null;
    }

    /// <summary>
    /// Determines whether the tree node is the top-level "Screens" container folder.
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if this is the top-level Screens folder (root "Screens" node); otherwise, false.</returns>
    /// <summary>
    /// Determines whether the tree node is the top-level "Screens" container folder.
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if this is the top-level Screens folder (root "Screens" node with no parent); otherwise, false.</returns>
    /// <remarks>
    /// This returns true ONLY for the top-level "Screens" folder itself (has no parent and Text is "Screens"),
    /// NOT for subfolders within the Screens hierarchy. Use IsScreensFolderTreeNode to check for subfolders.
    /// </remarks>
    public static bool IsTopScreenContainerTreeNode(this TreeNode treeNode)
    {
        return treeNode.Parent == null && treeNode.Text == "Screens";
    }

    /// <summary>
    /// Determines whether the tree node is the top-level "Behaviors" container folder.
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if this is the top-level Behaviors folder (root "Behaviors" node with no parent); otherwise, false.</returns>
    /// <remarks>
    /// This returns true ONLY for the top-level "Behaviors" folder itself (has no parent and Text is "Behaviors"),
    /// NOT for any subfolders.
    /// </remarks>
    public static bool IsTopBehaviorTreeNode(this TreeNode treeNode)
    {
        return treeNode.Parent == null && treeNode.Text == "Behaviors";
    }

    /// <summary>
    /// Determines whether the tree node is the top-level "Components" container folder.
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if this is the top-level Components folder (root "Components" node); otherwise, false.</returns>
    /// <summary>
    /// Determines whether the tree node is the top-level "Components" container folder.
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if this is the top-level Components folder (root "Components" node with no parent); otherwise, false.</returns>
    /// <remarks>
    /// This returns true ONLY for the top-level "Components" folder itself (has no parent and Text is "Components"),
    /// NOT for subfolders within the Components hierarchy. Use IsComponentsFolderTreeNode to check for subfolders.
    /// </remarks>
    public static bool IsTopComponentContainerTreeNode(this TreeNode treeNode)
    {
        return treeNode.Parent == null && treeNode.Text == "Components";
    }

    /// <summary>
    /// Determines whether the tree node is the top-level "Standard" container folder.
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if this is the top-level Standard folder (root "Standard" node with no parent); otherwise, false.</returns>
    /// <remarks>
    /// This returns true ONLY for the top-level "Standard" folder itself (has no parent and Text is "Standard"),
    /// NOT for any subfolders. The Standard folder contains built-in element types like Sprite, Text, and Container.
    /// </remarks>
    public static bool IsTopStandardElementTreeNode(this TreeNode treeNode)
    {
        return treeNode.Parent == null && treeNode.Text == "Standard";
    }

    /// <summary>
    /// Gets the full file path for the element, folder, or behavior represented by the tree node.
    /// </summary>
    /// <param name="treeNode">The tree node to get the file path for.</param>
    /// <returns>
    /// The full file path as a FilePath object, or null if the project is not saved yet.
    /// For folders, returns the directory path ending with a backslash.
    /// For elements and behaviors, returns the full file path including extension.
    /// </returns>
    public static FilePath? GetFullFilePath(this TreeNode treeNode)
    {
        if (treeNode.IsTopComponentContainerTreeNode() ||
            treeNode.IsTopStandardElementTreeNode() ||
            treeNode.IsTopScreenContainerTreeNode() ||
            treeNode.IsTopBehaviorTreeNode()
            )
        {
            // Locator is retained here (not ctor-injected): this is a static extension method on TreeNode,
            // so there is no instance to hold injected IProjectManager/IDialogService fields. IProjectManager
            // and IDialogService are both DI-registered; the only blocker to draining is the static context.
            if (Locator.GetRequiredService<IProjectManager>().GumProjectSave == null ||
                string.IsNullOrEmpty(Locator.GetRequiredService<IProjectManager>().GumProjectSave.FullFileName))
            {
                Locator.GetRequiredService<IDialogService>().ShowMessage("Project isn't saved yet so the root of the project isn't known");
                return null;
            }
            else
            {
                string projectDirectory = FileManager.GetDirectory(Locator.GetRequiredService<IProjectManager>().GumProjectSave.FullFileName);

                if (treeNode.IsTopComponentContainerTreeNode())
                {
                    return projectDirectory + ElementReference.ComponentSubfolder + "\\";
                }
                else if (treeNode.IsTopStandardElementTreeNode())
                {
                    return projectDirectory + ElementReference.StandardSubfolder + "\\";
                }
                else if (treeNode.IsTopScreenContainerTreeNode())
                {
                    return projectDirectory + ElementReference.ScreenSubfolder + "\\";
                }
                else if(treeNode.IsTopBehaviorTreeNode())
                {
                    return projectDirectory + BehaviorReference.Subfolder + "\\";
                }
                throw new InvalidOperationException();
            }
        }
        else if (treeNode.IsStandardElementTreeNode() ||
            treeNode.IsComponentTreeNode() ||
            treeNode.IsScreenTreeNode())
        {
            ElementSave element = (ElementSave)treeNode.Tag;
            return treeNode.Parent.GetFullFilePath() + treeNode.Text + "." + element.FileExtension;
        }
        else if(treeNode.IsBehaviorTreeNode())
        {
            var behavior = treeNode.Tag as BehaviorSave;
            return treeNode.Parent.GetFullFilePath() + treeNode.Text + "." + BehaviorReference.Extension;
        }
        else
        {
            var toReturn = treeNode.Parent.GetFullFilePath() + treeNode.Text + "\\";
            return toReturn;
        }
    }

    /// <summary>
    /// Determines whether the tree node is a top level or contained folder within the Screens hierarchy 
    /// (includes the top-level "Screens" folder itself).
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if this is a folder anywhere under the top-level "Screens" folder; otherwise, false.</returns>
    /// <summary>
    /// Determines whether the tree node is a top level or contained folder within the Screens hierarchy
    /// (includes the top-level "Screens" folder itself).
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if this is a folder anywhere under the top-level "Screens" folder; otherwise, false.</returns>
    /// <remarks>
    /// This returns true for ANY folder under the Screens hierarchy, including:
    /// - Direct child folders of the top-level "Screens" folder (e.g., "Screens/Menus")
    /// - Nested subfolders at any depth (e.g., "Screens/Menus/MainMenu")
    /// - The top-level "Screens" folder itself (use IsTopScreenContainerTreeNode for that)
    /// Returns false for:
    /// - Screen element nodes (which have a Tag)
    /// </remarks>
    public static bool IsScreensFolderTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag == null &&
            treeNode.Parent != null &&
            (treeNode.Parent.IsScreensFolderTreeNode() ||
            // If the parent is the top screen container and this has no tag, then this is a folder:
            treeNode.Parent.IsTopScreenContainerTreeNode());
    }

    /// <summary>
    /// Determines whether the tree node is part of the Screens folder structure
    /// (either the root Screens folder, a subfolder, or a Screen element within the hierarchy).
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if this node is anywhere within the Screens folder structure; otherwise, false.</returns>
    /// <remarks>
    /// This recursively checks if the node or any of its parents is the root Screens node.
    /// Unlike IsScreensFolderTreeNode and IsTopScreenContainerTreeNode, this returns true for
    /// Screen elements themselves, not just folders.
    /// </remarks>
    /// <remarks>
    /// The <see cref="ITreeNode"/>-typed overload now lives in Gum.Presentation's
    /// TreeNodeFolderExtensions (headless, ADR-0005 Phase 3) instead of this WinForms-cast-based
    /// one, so AddScreenDialogViewModel/DragDropManager and any other <see cref="ITreeNode"/> caller
    /// resolve there without an ambiguous-call conflict.
    /// </remarks>
    public static bool IsPartOfScreensFolderStructure(this TreeNode treeNode)
    {
        if (treeNode == ElementTreeViewManager.RootScreensTreeNode)
            return true;

        if (treeNode.Parent == null)
            return false;

        return treeNode.Parent.IsPartOfScreensFolderStructure();
    }

    /// <summary>
    /// Determines whether the tree node is part of the Components folder structure
    /// (either the root Components folder, a subfolder, or a Component element within the hierarchy).
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if this node is anywhere within the Components folder structure; otherwise, false.</returns>
    /// <remarks>
    /// This recursively checks if the node or any of its parents is the root Components node.
    /// Unlike IsComponentsFolderTreeNode and IsTopComponentContainerTreeNode, this returns true for
    /// Component elements themselves, not just folders.
    /// </remarks>
    /// <remarks>
    /// The <see cref="ITreeNode"/>-typed overload now lives in Gum.Presentation's
    /// TreeNodeFolderExtensions (headless, ADR-0005 Phase 3) instead of this WinForms-cast-based
    /// one, so AddComponentDialogViewModel/DragDropManager and any other <see cref="ITreeNode"/>
    /// caller resolve there without an ambiguous-call conflict.
    /// </remarks>
    public static bool IsPartOfComponentsFolderStructure(this TreeNode treeNode)
    {
        if (treeNode == ElementTreeViewManager.RootComponentsTreeNode)
            return true;

        if (treeNode.Parent == null)
            return false;

        return treeNode.Parent.IsPartOfComponentsFolderStructure();
    }

    /// <summary>
    /// Determines whether the tree node is part of the Standard elements folder structure
    /// (either the root Standard folder, a subfolder, or a Standard element within the hierarchy).
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if this node is anywhere within the Standard elements folder structure; otherwise, false.</returns>
    /// <remarks>
    /// This recursively checks if the node or any of its parents is the root Standard elements node.
    /// Unlike IsTopStandardElementTreeNode, this returns true for Standard element instances themselves, not just folders.
    /// </remarks>
    public static bool IsPartOfStandardElementsFolderStructure(this TreeNode treeNode)
    {
        if (treeNode == ElementTreeViewManager.RootStandardElementsTreeNode)
            return true;

        if (treeNode.Parent == null)
            return false;

        return treeNode.Parent.IsPartOfStandardElementsFolderStructure();
    }

    /// <summary>
    /// Determines whether the tree node is a folder within the Components hierarchy (excluding the top-level "Components" folder itself).
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if this is a folder anywhere under the top-level "Components" folder; otherwise, false.</returns>
    /// <summary>
    /// Determines whether the tree node is a folder within the Components hierarchy
    /// (including the top-level "Components" folder itself).
    /// </summary>
    /// <param name="treeNode">The tree node to check.</param>
    /// <returns>True if this is a folder anywhere under and including the top-level "Components" folder; otherwise, false.</returns>
    /// <remarks>
    /// This returns true for ANY folder under the Components hierarchy, including:
    /// - Direct child folders of the top-level "Components" folder (e.g., "Components/UI")
    /// - Nested subfolders at any depth (e.g., "Components/UI/Buttons")
    /// - The top-level "Components" folder itself (use IsTopComponentContainerTreeNode for that)
    /// Returns false for:
    /// - Component element nodes (which have a Tag)
    /// </remarks>
    public static bool IsComponentsFolderTreeNode(this TreeNode treeNode)
    {
        return treeNode.Tag == null &&
            treeNode.Parent != null &&
            (treeNode.Parent.IsComponentsFolderTreeNode() ||
            // If the parent is the top component container and this has no tag, then this is a folder:
            treeNode.Parent.IsTopComponentContainerTreeNode());
    }

    /// <summary>
    /// Sorts the tree node collection alphabetically by name, with folders appearing before files.
    /// </summary>
    /// <param name="treeNodeCollection">The collection of tree nodes to sort.</param>
    /// <param name="recursive">
    /// If true, recursively sorts all child node collections (except within Screen, Component, Standard, or Behavior element nodes).
    /// Default is false.
    /// </param>
    /// <remarks>
    /// The sort order places folders (Components and Screens subfolders) before individual elements,
    /// and within each category, nodes are sorted alphabetically by their Text property.
    /// When recursive is true, the method will not sort children of element nodes (Screen, Component, Standard, Behavior)
    /// as these typically contain instances and states that should maintain their specific order.
    /// </remarks>
    public static void SortByName(this TreeNodeCollection treeNodeCollection, bool recursive = false)
    {
        int lastObjectExclusive = treeNodeCollection.Count;
        int whereObjectBelongs;
        for (int i = 0 + 1; i < lastObjectExclusive; i++)
        {
            TreeNode first = treeNodeCollection[i];
            TreeNode second = treeNodeCollection[i - 1];
            if (FirstComesBeforeSecond(first, second))
            {
                if (i == 1)
                {
                    TreeNode treeNode = treeNodeCollection[i];
                    treeNodeCollection.RemoveAt(i);

                    treeNodeCollection.Insert(0, treeNode);
                    continue;
                }

                for (whereObjectBelongs = i - 2; whereObjectBelongs > -1; whereObjectBelongs--)
                {
                    second = treeNodeCollection[whereObjectBelongs];
                    if (!FirstComesBeforeSecond(treeNodeCollection[i], second))
                    {
                        TreeNode treeNode = treeNodeCollection[i];

                        treeNodeCollection.RemoveAt(i);
                        treeNodeCollection.Insert(whereObjectBelongs + 1, treeNode);
                        break;
                    }
                    else if (whereObjectBelongs == 0 && FirstComesBeforeSecond(treeNodeCollection[i], treeNodeCollection[0]))
                    {
                        TreeNode treeNode = treeNodeCollection[i];
                        treeNodeCollection.RemoveAt(i);
                        treeNodeCollection.Insert(0, treeNode);
                        break;
                    }
                }
            }
        }

        if(recursive)
        {
            foreach(var node in treeNodeCollection)
            {
                var asTreeNode = node as TreeNode;
                if(asTreeNode != null)
                {
                    var sortInner = asTreeNode.IsScreenTreeNode() == false &&
                        asTreeNode.IsComponentTreeNode() == false &&
                        asTreeNode.IsStandardElementTreeNode() == false &&
                        asTreeNode.IsBehaviorTreeNode() == false;

                    if(sortInner)
                    {
                        asTreeNode.Nodes.SortByName(recursive);
                    }
                }
            }
        }
    }

    private static bool FirstComesBeforeSecond(TreeNode first, TreeNode second)
    {
        bool isFirstDirectory = first.IsComponentsFolderTreeNode() || first.IsScreensFolderTreeNode();
        bool isSecondDirectory = second.IsComponentsFolderTreeNode() || second.IsScreensFolderTreeNode();

        if (isFirstDirectory && !isSecondDirectory)
        {
            return true;
        }
        else if (!isFirstDirectory && isSecondDirectory)
        {
            return false;
        }
        else
        {
            return first.Text.CompareTo(second.Text) < 0;
        }
    }

    /// <summary>
    /// Gets all descendant nodes of the tree node in a flattened list, recursively traversing the entire tree structure.
    /// </summary>
    /// <param name="treeNode">The tree node whose descendants should be collected.</param>
    /// <returns>A list containing all child, grandchild, and deeper descendant nodes in depth-first order.</returns>
    /// <remarks>
    /// The returned list does not include the tree node itself, only its descendants.
    /// Nodes are added in depth-first order (parent node's children are added before their siblings' children).
    /// </remarks>
    public static List<TreeNode> GetAllChildrenNodesRecursively(this TreeNode treeNode)
    {
        List<TreeNode> toReturn = new List<TreeNode>();

        void Fill(TreeNode parent)
        {
            foreach(TreeNode child in parent.Nodes)
            {
                toReturn.Add(child);
                Fill(child);
            }
        }

        Fill(treeNode);

        return toReturn;
    }
}

#endregion
