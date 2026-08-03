using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Gum.Managers;

namespace Gum.Controls;

/// <summary>
/// The element tree's control. A WPF <see cref="TreeView"/> that supports multi-selection, which
/// the base control does not.
/// </summary>
/// <remarks>
/// <para>
/// Selection is tracked on the <see cref="GumTreeNode"/> models rather than through
/// <see cref="TreeViewItem.IsSelected"/>, because <see cref="TreeView"/> enforces a single selected
/// item and would clear the previous one on every change. The row template binds its selected
/// visual to <see cref="GumTreeNode.IsSelected"/>, so any number of rows can show as selected. This
/// mirrors what the WinForms control it replaces did - there, by cancelling <c>OnBeforeSelect</c>
/// and recoloring rows by hand.
/// </para>
/// <para>
/// Because the base control's selection is never engaged, keyboard navigation is handled here too
/// rather than inherited - the same reason the WinForms control overrode <c>OnKeyDown</c>.
/// </para>
/// </remarks>
public partial class GumTreeView : TreeView
{
    private readonly TreeNodeRangeSelectionLogic _rangeSelectionLogic = new();
    private readonly TreeNodeMouseUpSelectionLogic _mouseUpSelectionLogic = new();
    private readonly TreeNodeMouseDownSelectionLogic _mouseDownSelectionLogic = new();
    private readonly TreeNodeKeyNavigationLogic _keyNavigationLogic = new();
    private readonly TreeNodeClickDispatchLogic _clickDispatchLogic = new();

    private readonly List<GumTreeNode> _selectedNodes = new();
    private GumTreeNode? _selectedNode;
    private bool _selectedNodeChangedOnPush;
    private GumTreeNode? _hotNode;

    /// <summary>
    /// Roughly how many rows fit in the viewport. Drives PageUp/PageDown, replacing the WinForms
    /// control's <c>VisibleCount</c>.
    /// </summary>
    private int VisibleRowCount
    {
        get
        {
            double rowHeight = FontSize * 2;
            return rowHeight > 0 ? Math.Max(1, (int)(ActualHeight / rowHeight)) : 1;
        }
    }

    public GumTreeView()
    {
        Nodes = new GumTreeNodeCollection();
        ItemsSource = Nodes;
        Focusable = true;
        AllowDrop = true;

        // Rows raise these when their bound IsExpanded changes, whoever changed it - matching what
        // the WinForms AfterExpand/AfterCollapse pair covered.
        AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler((_, _) => NotifyExpansionChangedByUser()));
        AddHandler(TreeViewItem.CollapsedEvent, new RoutedEventHandler((_, _) => NotifyExpansionChangedByUser()));
    }

    /// <summary>
    /// Edge length of each row's icon. Scales with the UI font size, which is what kept the WinForms
    /// tree's icons in proportion when the app font changed - there, by regenerating the whole image
    /// list at a new pixel size.
    /// </summary>
    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
        nameof(IconSize), typeof(double), typeof(GumTreeView), new FrameworkPropertyMetadata(16.0));

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    #region Nodes and selection

    /// <summary>
    /// The tree's root nodes. Named to match the WinForms collection it replaces so the
    /// tree-building code reads unchanged.
    /// </summary>
    public GumTreeNodeCollection Nodes { get; }

    /// <summary>
    /// Whether the tree refuses to end up with an empty selection.
    /// </summary>
    public bool AlwaysHaveOneNodeSelected { get; set; }

    /// <summary>
    /// Whether pressing the mouse selects, or whether selection waits for the release. Gum selects
    /// on release so that starting a drag from an unselected row does not change the selection.
    /// </summary>
    public bool IsSelectingOnPush { get; set; } = true;

    public MultiSelectBehavior MultiSelectBehavior { get; set; }

    /// <summary>
    /// The primary selected node - the anchor for range selection and the one reported to the rest
    /// of the tool. Setting it replaces the whole selection.
    /// </summary>
    public GumTreeNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (value == _selectedNode)
            {
                return;
            }

            ClearSelectedNodes();

            if (value != null)
            {
                ReactToClickedNode(value);
            }
        }
    }

    /// <summary>
    /// Every selected node. Setting it replaces the selection wholesale and raises
    /// <see cref="AfterSelect"/> once.
    /// </summary>
    public IReadOnlyList<GumTreeNode> SelectedNodes
    {
        get => _selectedNodes;
        set
        {
            // Copied first: callers can legitimately pass the live selection back in, and clearing
            // would then empty the very collection being iterated.
            GumTreeNode[] requested = value?.ToArray() ?? Array.Empty<GumTreeNode>();

            ClearSelectedNodes();

            foreach (GumTreeNode node in requested)
            {
                SetNodeSelected(node, true);
            }

            OnAfterSelect(_selectedNode);
        }
    }

    /// <summary>
    /// The node drawn as hovered. Settable so another view (the wireframe) can highlight the row for
    /// whatever the cursor is over there.
    /// </summary>
    public void SetExternalHotNode(GumTreeNode? node)
    {
        if (_hotNode == node)
        {
            return;
        }

        if (_hotNode != null)
        {
            _hotNode.IsHot = false;
        }

        _hotNode = node;

        if (_hotNode != null)
        {
            _hotNode.IsHot = true;
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when a click settles the selection. Distinct from <see cref="AfterSelect"/>, which
    /// also fires for programmatic and keyboard-driven selection.
    /// </summary>
    public event Action<GumTreeNode?>? AfterClickSelect;

    /// <summary>
    /// Raised whenever the selection changes, however it changed.
    /// </summary>
    public event Action<GumTreeNode?>? AfterSelect;

    /// <summary>
    /// Raised when an exception is caught while reacting to tree input. This control has no
    /// dependency on the host's dialog service, so it hands the exception off here instead of
    /// showing UI itself.
    /// </summary>
    public event Action<Exception>? UnhandledException;

    /// <summary>
    /// Raised when a node is expanded or collapsed by the user, as opposed to by code.
    /// </summary>
    public event EventHandler? NodeExpansionChangedByUser;

    /// <summary>
    /// Re-raises <see cref="AfterClickSelect"/> for a selection that came from code rather than a
    /// click, so downstream listeners see the same cascade either way.
    /// </summary>
    public void CallAfterClickSelect(GumTreeNode? node) => AfterClickSelect?.Invoke(node);

    #endregion

    #region Expansion

    public void CollapseAll()
    {
        foreach (GumTreeNode node in Nodes.SelectMany(root => root.SelfAndDescendants()))
        {
            node.IsExpanded = false;
        }
    }

    /// <summary>
    /// Scrolls <paramref name="node"/> into view, expanding its ancestors first so it is reachable.
    /// </summary>
    public void EnsureVisible(GumTreeNode node)
    {
        node.EnsureAncestorsExpanded();

        // The container for a freshly-expanded ancestor's child does not exist until layout runs.
        Dispatcher.BeginInvoke(
            new Action(() => ContainerFor(node)?.BringIntoView()),
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// The realized row for a node, or null if it has not been created (its parent is collapsed, or
    /// layout has not run yet).
    /// </summary>
    internal TreeViewItem? ContainerFor(GumTreeNode node)
    {
        ItemsControl? owner = node.Parent is { } parent ? ContainerFor(parent) : this;
        return owner?.ItemContainerGenerator.ContainerFromItem(node) as TreeViewItem;
    }

    #endregion

    #region Hit testing

    /// <summary>
    /// The node whose row contains <paramref name="point"/>, in this control's coordinates, or null
    /// if the point is not over a row.
    /// </summary>
    public GumTreeNode? GetNodeAt(Point point)
    {
        if (InputHitTest(point) is not DependencyObject hit)
        {
            return null;
        }

        return FindAncestorItem(hit)?.DataContext as GumTreeNode;
    }

    /// <summary>
    /// Whether a click landed on a row's expand/collapse toggle rather than on the row itself.
    /// </summary>
    private static bool IsExpanderClick(object? originalSource)
    {
        if (originalSource is not DependencyObject source)
        {
            return false;
        }

        // Stop at the row: a ToggleButton found above it belongs to an ancestor row, not this click.
        for (DependencyObject? current = source;
             current is not null and not TreeViewItem;
             current = GetVisualOrLogicalParent(current))
        {
            if (current is System.Windows.Controls.Primitives.ToggleButton)
            {
                return true;
            }
        }

        return false;
    }

    private static TreeViewItem? FindAncestorItem(DependencyObject start)
    {
        for (DependencyObject? current = start; current != null; current = GetVisualOrLogicalParent(current))
        {
            if (current is TreeViewItem item)
            {
                return item;
            }
        }

        return null;
    }

    private static DependencyObject? GetVisualOrLogicalParent(DependencyObject current) =>
        current is Visual or System.Windows.Media.Media3D.Visual3D
            ? VisualTreeHelper.GetParent(current)
            : LogicalTreeHelper.GetParent(current);

    #endregion

    #region Mouse input

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        // An expander click changes expansion only. Letting it fall through would also select the
        // row, which makes expanding a folder disruptive - it would move the selection away from
        // whatever the user is working on.
        if (IsExpanderClick(e.OriginalSource))
        {
            base.OnPreviewMouseDown(e);
            return;
        }

        try
        {
            GumTreeNode? previousSelectedNode = _selectedNode;
            GumTreeNode? node = GetNodeAt(e.GetPosition(this));

            if (node != null)
            {
                bool shouldReact = _mouseDownSelectionLogic.ShouldReactToClick(
                    _selectedNodes.Contains(node), e.ChangedButton, Keyboard.Modifiers,
                    MultiSelectBehavior, IsSelectingOnPush);

                if (shouldReact)
                {
                    ReactToClickedNode(node);
                }

                // Otherwise this is either a context-menu click on an already-selected row or the
                // start of a drag - the release decides.
            }
            else
            {
                ReactToClickedNode(null);
            }

            _selectedNodeChangedOnPush = previousSelectedNode != _selectedNode;

            if (!_selectedNodeChangedOnPush &&
                _selectedNodes.Count > 1 &&
                Keyboard.Modifiers == ModifierKeys.None)
            {
                // A plain click on a multi-selection collapses it back to one on release.
                _selectedNodeChangedOnPush = true;
            }

            BeginPotentialDrag(e);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }

        base.OnPreviewMouseDown(e);
    }

    protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
    {
        if (IsExpanderClick(e.OriginalSource))
        {
            EndPotentialDrag();
            base.OnPreviewMouseUp(e);
            return;
        }

        try
        {
            EndPotentialDrag();

            GumTreeNode? node = GetNodeAt(e.GetPosition(this));

            if (node != null)
            {
                bool isNodeInMultiSelection = _selectedNodes.Count > 1 && _selectedNodes.Contains(node);

                bool shouldSelect = _mouseUpSelectionLogic.ShouldSelect(
                    Keyboard.Modifiers, MultiSelectBehavior, isNodeInMultiSelection, IsSelectingOnPush,
                    e.ChangedButton);

                if (shouldSelect)
                {
                    ReactToClickedNode(node);
                }

                if (_selectedNodeChangedOnPush || !IsSelectingOnPush)
                {
                    AfterClickSelect?.Invoke(node);
                }
            }
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }

        base.OnPreviewMouseUp(e);
    }

    #endregion

    #region Keyboard input

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl)
        {
            return;
        }

        try
        {
            if (_selectedNode == null && Nodes.Count > 0)
            {
                SetNodeSelected(Nodes[0], true);
            }

            if (_selectedNode is not { } selected)
            {
                return;
            }

            bool shiftDown = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            bool altDown = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
            bool controlDown = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            switch (e.Key)
            {
                case Key.Left when selected.IsExpanded && selected.ChildCount > 0:
                    selected.Collapse();
                    e.Handled = true;
                    break;
                case Key.Left when selected.Parent != null:
                    SelectSingleNodeAndNotify(selected.Parent);
                    e.Handled = true;
                    break;
                case Key.Right when !selected.IsExpanded:
                    selected.Expand();
                    e.Handled = true;
                    break;
                case Key.Right when selected.FirstNode is { } firstChild:
                    SelectSingleNodeAndNotify(firstChild);
                    e.Handled = true;
                    break;
                case Key.Up when !altDown && !controlDown:
                    if (selected.PrevVisibleNode is { } previous)
                    {
                        ReactToClickedNode(previous);
                        AfterClickSelect?.Invoke(previous);
                        e.Handled = true;
                    }
                    break;
                case Key.Down when !altDown && !controlDown:
                    if (selected.NextVisibleNode is { } next)
                    {
                        ReactToClickedNode(next);
                        AfterClickSelect?.Invoke(next);
                        e.Handled = true;
                    }
                    break;
                case Key.Home:
                    ApplyNavigationTarget(
                        _keyNavigationLogic.GetHomeTarget(selected, Nodes, shiftDown, out bool selectHomeRange),
                        selectHomeRange);
                    e.Handled = true;
                    break;
                case Key.End:
                    ApplyNavigationTarget(
                        _keyNavigationLogic.GetEndTarget(selected, Nodes, shiftDown, out bool selectEndRange),
                        selectEndRange);
                    e.Handled = true;
                    break;
                case Key.PageUp:
                    SelectSingleNodeAndNotify(_keyNavigationLogic.GetPageUpTarget(selected, VisibleRowCount));
                    e.Handled = true;
                    break;
                case Key.PageDown:
                    SelectSingleNodeAndNotify(_keyNavigationLogic.GetPageDownTarget(selected, VisibleRowCount));
                    e.Handled = true;
                    break;
            }
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }

        base.OnKeyDown(e);
    }

    private void ApplyNavigationTarget(GumTreeNode? target, bool selectRange)
    {
        if (target == null)
        {
            return;
        }

        if (selectRange)
        {
            ReactToClickedNode(target);
        }
        else
        {
            SelectSingleNode(target);
        }

        AfterClickSelect?.Invoke(target);
    }

    /// <summary>
    /// Selects a single node and notifies <see cref="AfterClickSelect"/> - unlike
    /// <see cref="SelectSingleNode"/> alone (also used by the mouse-click path, which raises
    /// <see cref="AfterClickSelect"/> itself), keyboard navigation has no other caller to raise it.
    /// </summary>
    private void SelectSingleNodeAndNotify(GumTreeNode? node)
    {
        if (node == null)
        {
            return;
        }

        SelectSingleNode(node);
        AfterClickSelect?.Invoke(node);
    }

    #endregion

    #region Selection mechanics

    private void ReactToClickedNode(GumTreeNode? node)
    {
        bool shouldRaiseEvents = true;

        TreeNodeClickReaction reaction = _clickDispatchLogic.GetReaction(
            hasClickedNode: node != null,
            hasExistingSelection: _selectedNode != null,
            AlwaysHaveOneNodeSelected,
            Keyboard.Modifiers,
            MultiSelectBehavior);

        switch (reaction)
        {
            case TreeNodeClickReaction.DeselectAll:
                ClearSelectedNodes();
                break;
            case TreeNodeClickReaction.None:
                shouldRaiseEvents = false;
                break;
            case TreeNodeClickReaction.ToggleSelection:
                SetNodeSelected(node!, !_selectedNodes.Contains(node!));
                break;
            case TreeNodeClickReaction.RangeSelect:
                _rangeSelectionLogic.SelectRange(_selectedNode!, node!, n => SetNodeSelected(n, true));
                break;
            case TreeNodeClickReaction.SingleSelect:
                SelectSingleNode(node!);
                break;
        }

        if (shouldRaiseEvents)
        {
            OnAfterSelect(node);
        }
    }

    private void SelectSingleNode(GumTreeNode? node)
    {
        if (node == null)
        {
            return;
        }

        ClearSelectedNodes();
        SetNodeSelected(node, true);
        EnsureVisible(node);
    }

    private void SetNodeSelected(GumTreeNode? node, bool selected)
    {
        if (node == null)
        {
            return;
        }

        if (selected)
        {
            _selectedNode = node;

            if (!_selectedNodes.Contains(node))
            {
                _selectedNodes.Add(node);
            }

            node.IsSelected = true;
        }
        else if (_selectedNodes.Remove(node))
        {
            node.IsSelected = false;

            if (_selectedNode == node)
            {
                _selectedNode = _selectedNodes.Count > 0 ? _selectedNodes[^1] : null;
            }
        }
    }

    private void ClearSelectedNodes()
    {
        foreach (GumTreeNode node in _selectedNodes)
        {
            node.IsSelected = false;
        }

        _selectedNodes.Clear();
        _selectedNode = null;
    }

    private void OnAfterSelect(GumTreeNode? node)
    {
        foreach (GumTreeNode selected in _selectedNodes)
        {
            selected.EnsureAncestorsExpanded();
        }

        AfterSelect?.Invoke(node);
    }

    #endregion

    #region Expansion notifications

    protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        // A removed node must not linger in the selection - the WinForms control papered over this
        // by re-checking every selected node's owning tree on read.
        PruneDetachedSelection();
    }

    private void PruneDetachedSelection()
    {
        for (int i = _selectedNodes.Count - 1; i >= 0; i--)
        {
            if (!IsInTree(_selectedNodes[i]))
            {
                SetNodeSelected(_selectedNodes[i], false);
            }
        }
    }

    private bool IsInTree(GumTreeNode node)
    {
        GumTreeNode root = node;
        while (root.Parent is { } parent)
        {
            root = parent;
        }

        return Nodes.Contains(root);
    }

    /// <summary>
    /// Called by the row template when the user toggles an expander, so the collapse-toggle service
    /// can tell a user-driven change from a programmatic one.
    /// </summary>
    internal void NotifyExpansionChangedByUser() =>
        NodeExpansionChangedByUser?.Invoke(this, EventArgs.Empty);

    #endregion

    private void HandleException(Exception ex) => UnhandledException?.Invoke(ex);
}
