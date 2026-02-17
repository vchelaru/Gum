using CommonFormsAndControls;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Gum.Plugins.InternalPlugins.TreeView;

/// <summary>
/// Makes collapse buttons act as toggles. First click captures expansion state
/// and collapses; second click restores the saved state.
/// </summary>
public class CollapseToggleService : ICollapseToggleService
{
    private enum CollapseActionType
    {
        CollapseAll,
        CollapseToElementLevel
    }

    private List<string>? _savedExpandedPaths;
    private CollapseActionType? _lastCollapseAction;
    private bool _isDirty;
    private bool _suppressDirtyFlag;

    public void HandleCollapseAll(MultiSelectTreeView treeView, Action collapseAllAction)
    {
        HandleCollapseToggle(treeView, collapseAllAction, CollapseActionType.CollapseAll);
    }

    public void HandleCollapseToElementLevel(MultiSelectTreeView treeView, Action collapseToElementLevelAction)
    {
        HandleCollapseToggle(treeView, collapseToElementLevelAction, CollapseActionType.CollapseToElementLevel);
    }

    public void OnNodeManuallyChanged()
    {
        if (!_suppressDirtyFlag)
        {
            _isDirty = true;
        }
    }

    public void Clear()
    {
        _savedExpandedPaths = null;
        _lastCollapseAction = null;
        _isDirty = false;
    }

    private void HandleCollapseToggle(MultiSelectTreeView treeView, Action collapseAction, CollapseActionType actionType)
    {
        bool canRestore = _lastCollapseAction == actionType
            && !_isDirty
            && _savedExpandedPaths != null;

        if (canRestore)
        {
            // Restore the saved state
            _suppressDirtyFlag = true;
            try
            {
                ApplyExpandedNodePaths(treeView, _savedExpandedPaths!);
            }
            finally
            {
                _suppressDirtyFlag = false;
            }

            // After restoring, clear the snapshot so the next click will capture again
            _savedExpandedPaths = null;
            _lastCollapseAction = null;
            _isDirty = false;
        }
        else
        {
            // Capture current state, then collapse
            _savedExpandedPaths = GetExpandedNodePaths(treeView);
            _lastCollapseAction = actionType;
            _isDirty = false;

            _suppressDirtyFlag = true;
            try
            {
                collapseAction();
            }
            finally
            {
                _suppressDirtyFlag = false;
            }
        }
    }

    #region Tree Path Helpers

    private List<string> GetExpandedNodePaths(MultiSelectTreeView treeView)
    {
        var expandedPaths = new List<string>();
        foreach (TreeNode node in treeView.Nodes)
        {
            CollectExpandedPaths(node, expandedPaths);
        }
        return expandedPaths;
    }

    private void CollectExpandedPaths(TreeNode node, List<string> expandedPaths)
    {
        if (node.IsExpanded)
        {
            string path = GetNodePath(node);
            if (!string.IsNullOrEmpty(path))
            {
                expandedPaths.Add(path);
            }

            foreach (TreeNode child in node.Nodes)
            {
                CollectExpandedPaths(child, expandedPaths);
            }
        }
    }

    private string GetNodePath(TreeNode node)
    {
        var parts = new List<string>();
        TreeNode? current = node;
        while (current != null)
        {
            parts.Insert(0, current.Text);
            current = current.Parent;
        }
        return string.Join("/", parts);
    }

    private void ApplyExpandedNodePaths(MultiSelectTreeView treeView, List<string> paths)
    {
        foreach (var path in paths)
        {
            var node = FindNodeByPath(treeView, path);
            if (node != null)
            {
                node.Expand();
            }
        }
    }

    private TreeNode? FindNodeByPath(MultiSelectTreeView treeView, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var parts = path.Split('/');
        TreeNodeCollection currentNodes = treeView.Nodes;
        TreeNode? foundNode = null;

        foreach (var part in parts)
        {
            foundNode = null;
            foreach (TreeNode node in currentNodes)
            {
                if (node.Text == part)
                {
                    foundNode = node;
                    currentNodes = node.Nodes;
                    break;
                }
            }
            if (foundNode == null)
            {
                return null;
            }
        }

        return foundNode;
    }

    #endregion
}
