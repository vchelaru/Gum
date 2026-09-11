using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Managers;

namespace Gum.Controls;

/// <summary>
/// The element tree's selection and the rules that change it: the primary node, the full
/// multi-selection, the hovered node, and the reactions to clicks, key presses and drag starts.
/// Both heads' tree controls feed it hit-tested nodes, buttons and keys and scroll when it asks, so
/// the selection behavior is written once.
/// </summary>
/// <remarks>
/// Selection is kept on the nodes (<see cref="GumTreeNode.IsSelected"/>) rather than on the
/// control's row containers, because a native tree view tracks a single selected item and clears
/// the previous one on every change.
/// </remarks>
public class TreeSelectionModel
{
    private readonly Func<TreeModifierKeys> _currentModifiers;
    private readonly Action<GumTreeNode> _ensureVisible;
    private readonly List<GumTreeNode> _selectedNodes;
    private readonly TreeNodeClickDispatchLogic _clickDispatchLogic;
    private readonly TreeNodeMouseDownSelectionLogic _mouseDownSelectionLogic;
    private readonly TreeNodeMouseUpSelectionLogic _mouseUpSelectionLogic;
    private readonly TreeNodeKeyNavigationLogic _keyNavigationLogic;
    private readonly TreeNodeRangeSelectionLogic _rangeSelectionLogic;

    private GumTreeNode? _selectedNode;
    private GumTreeNode? _hotNode;
    private bool _selectedNodeChangedOnPush;

    /// <summary>Creates the model over a tree's root nodes.</summary>
    /// <param name="nodes">The tree's root collection.</param>
    /// <param name="currentModifiers">Reads the modifier keys held right now.</param>
    /// <param name="ensureVisible">Expands a node's ancestors and scrolls it into view.</param>
    public TreeSelectionModel(GumTreeNodeCollection nodes, Func<TreeModifierKeys> currentModifiers, Action<GumTreeNode> ensureVisible)
    {
        Nodes = nodes;
        _currentModifiers = currentModifiers;
        _ensureVisible = ensureVisible;
        _selectedNodes = new List<GumTreeNode>();
        _clickDispatchLogic = new TreeNodeClickDispatchLogic();
        _mouseDownSelectionLogic = new TreeNodeMouseDownSelectionLogic();
        _mouseUpSelectionLogic = new TreeNodeMouseUpSelectionLogic();
        _keyNavigationLogic = new TreeNodeKeyNavigationLogic();
        _rangeSelectionLogic = new TreeNodeRangeSelectionLogic();
        IsSelectingOnPush = true;

        // A removed node must not linger in the selection.
        Nodes.CollectionChanged += (_, _) => PruneDetachedSelection();
    }

    #region Properties

    /// <summary>The tree's root nodes.</summary>
    public GumTreeNodeCollection Nodes { get; }

    /// <summary>Whether the tree refuses to end up with an empty selection.</summary>
    public bool AlwaysHaveOneNodeSelected { get; set; }

    /// <summary>
    /// Whether pressing the pointer selects, or whether selection waits for the release. Gum selects
    /// on release so that starting a drag from an unselected row does not change the selection.
    /// </summary>
    public bool IsSelectingOnPush { get; set; }

    /// <summary>How clicks extend the selection across several nodes.</summary>
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

    #endregion

    #region Events

    /// <summary>
    /// Raised when a click or key press settles the selection. Distinct from <see cref="AfterSelect"/>,
    /// which also fires for programmatic selection.
    /// </summary>
    public event Action<GumTreeNode?>? AfterClickSelect;

    /// <summary>Raised whenever the selection changes, however it changed.</summary>
    public event Action<GumTreeNode?>? AfterSelect;

    /// <summary>
    /// Raised when an exception is caught while reacting to tree input, so the host can report it
    /// instead of the input handler crashing the tool.
    /// </summary>
    public event Action<Exception>? UnhandledException;

    /// <summary>
    /// Re-raises <see cref="AfterClickSelect"/> for a selection that came from code rather than a
    /// click, so downstream listeners see the same cascade either way.
    /// </summary>
    public void CallAfterClickSelect(GumTreeNode? node) => AfterClickSelect?.Invoke(node);

    #endregion

    #region Hover

    /// <summary>
    /// Sets the node drawn as hovered. Called by the view for the row under the pointer and by
    /// another view (the wireframe) to highlight the row for whatever the cursor is over there.
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

    #region Pointer and keyboard input

    /// <summary>
    /// Reacts to a pointer press over <paramref name="node"/>, or over empty space when it is null.
    /// Presses on an expander toggle should not be passed here: they change expansion only.
    /// </summary>
    public void HandlePointerPressed(GumTreeNode? node, TreePointerButton button)
    {
        try
        {
            GumTreeNode? previousSelectedNode = _selectedNode;

            if (node != null)
            {
                bool shouldReact = _mouseDownSelectionLogic.ShouldReactToClick(
                    _selectedNodes.Contains(node), button, _currentModifiers(),
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
                _currentModifiers() == TreeModifierKeys.None)
            {
                // A plain click on a multi-selection collapses it back to one on release.
                _selectedNodeChangedOnPush = true;
            }
        }
        catch (Exception ex)
        {
            UnhandledException?.Invoke(ex);
        }
    }

    /// <summary>Reacts to a pointer release over <paramref name="node"/>, or over empty space when it is null.</summary>
    public void HandlePointerReleased(GumTreeNode? node, TreePointerButton button)
    {
        try
        {
            if (node == null)
            {
                return;
            }

            bool isNodeInMultiSelection = _selectedNodes.Count > 1 && _selectedNodes.Contains(node);

            bool shouldSelect = _mouseUpSelectionLogic.ShouldSelect(
                _currentModifiers(), MultiSelectBehavior, isNodeInMultiSelection, IsSelectingOnPush, button);

            if (shouldSelect)
            {
                ReactToClickedNode(node);
            }

            if (_selectedNodeChangedOnPush || !IsSelectingOnPush)
            {
                AfterClickSelect?.Invoke(node);
            }
        }
        catch (Exception ex)
        {
            UnhandledException?.Invoke(ex);
        }
    }

    /// <summary>
    /// Reacts to a key press that is not a bare modifier. <paramref name="key"/> is null for keys
    /// the tree does not navigate with; those still select the first node when nothing is selected.
    /// </summary>
    /// <param name="key">The navigation key pressed, or null for any other key.</param>
    /// <param name="visibleRowCount">Roughly how many rows fit in the viewport, for PageUp/PageDown.</param>
    /// <returns>Whether the key moved the selection or expansion and should be marked handled.</returns>
    public bool HandleKeyDown(TreeNavigationKey? key, int visibleRowCount)
    {
        try
        {
            if (_selectedNode == null && Nodes.Count > 0)
            {
                SetNodeSelected(Nodes[0], true);
            }

            if (_selectedNode is not { } selected || key == null)
            {
                return false;
            }

            TreeModifierKeys modifiers = _currentModifiers();
            bool shiftDown = (modifiers & TreeModifierKeys.Shift) == TreeModifierKeys.Shift;
            bool altDown = (modifiers & TreeModifierKeys.Alt) == TreeModifierKeys.Alt;
            bool controlDown = (modifiers & TreeModifierKeys.Control) == TreeModifierKeys.Control;

            switch (key.Value)
            {
                case TreeNavigationKey.Left when selected.IsExpanded && selected.ChildCount > 0:
                    selected.Collapse();
                    return true;
                case TreeNavigationKey.Left when selected.Parent != null:
                    SelectSingleNodeAndNotify(selected.Parent);
                    return true;
                case TreeNavigationKey.Right when !selected.IsExpanded:
                    selected.Expand();
                    return true;
                case TreeNavigationKey.Right when selected.FirstNode is { } firstChild:
                    SelectSingleNodeAndNotify(firstChild);
                    return true;
                case TreeNavigationKey.Up when !altDown && !controlDown:
                    if (selected.PrevVisibleNode is { } previous)
                    {
                        ReactToClickedNode(previous);
                        AfterClickSelect?.Invoke(previous);
                        return true;
                    }
                    return false;
                case TreeNavigationKey.Down when !altDown && !controlDown:
                    if (selected.NextVisibleNode is { } next)
                    {
                        ReactToClickedNode(next);
                        AfterClickSelect?.Invoke(next);
                        return true;
                    }
                    return false;
                case TreeNavigationKey.Home:
                    ApplyNavigationTarget(
                        _keyNavigationLogic.GetHomeTarget(selected, Nodes, shiftDown, out bool selectHomeRange),
                        selectHomeRange);
                    return true;
                case TreeNavigationKey.End:
                    ApplyNavigationTarget(
                        _keyNavigationLogic.GetEndTarget(selected, Nodes, shiftDown, out bool selectEndRange),
                        selectEndRange);
                    return true;
                case TreeNavigationKey.PageUp:
                    SelectSingleNodeAndNotify(_keyNavigationLogic.GetPageUpTarget(selected, visibleRowCount));
                    return true;
                case TreeNavigationKey.PageDown:
                    SelectSingleNodeAndNotify(_keyNavigationLogic.GetPageDownTarget(selected, visibleRowCount));
                    return true;
            }
        }
        catch (Exception ex)
        {
            UnhandledException?.Invoke(ex);
        }

        return false;
    }

    /// <summary>
    /// Starts a drag from <paramref name="node"/> and returns the nodes it carries. Dragging a row
    /// that is not part of the selection drags just that row, and takes the selection with it.
    /// </summary>
    public IReadOnlyList<GumTreeNode> BeginDrag(GumTreeNode node)
    {
        if (!_selectedNodes.Contains(node))
        {
            SelectSingleNode(node);
            OnAfterSelect(node);
        }

        return _selectedNodes.Count > 0
            ? new List<GumTreeNode>(_selectedNodes)
            : new List<GumTreeNode> { node };
    }

    #endregion

    #region Selection mechanics

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

    // Keyboard navigation has no other caller to raise AfterClickSelect, unlike the pointer path.
    private void SelectSingleNodeAndNotify(GumTreeNode? node)
    {
        if (node == null)
        {
            return;
        }

        SelectSingleNode(node);
        AfterClickSelect?.Invoke(node);
    }

    private void ReactToClickedNode(GumTreeNode? node)
    {
        bool shouldRaiseEvents = true;

        TreeNodeClickReaction reaction = _clickDispatchLogic.GetReaction(
            hasClickedNode: node != null,
            hasExistingSelection: _selectedNode != null,
            AlwaysHaveOneNodeSelected,
            _currentModifiers(),
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
        _ensureVisible(node);
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

    #endregion
}
