using System;
using System.Collections.Generic;
using Gum.Managers;

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

    public void HandleCollapseAll(IReadOnlyList<ITreeNode> roots, Action collapseAllAction)
    {
        HandleCollapseToggle(roots, collapseAllAction, CollapseActionType.CollapseAll);
    }

    public void HandleCollapseToElementLevel(IReadOnlyList<ITreeNode> roots, Action collapseToElementLevelAction)
    {
        HandleCollapseToggle(roots, collapseToElementLevelAction, CollapseActionType.CollapseToElementLevel);
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

    public List<string> SaveExpandedPaths(IReadOnlyList<ITreeNode> roots) =>
        TreeNodeExpansionPaths.Capture(roots);

    public void RestoreExpandedPaths(IReadOnlyList<ITreeNode> roots, List<string> paths) =>
        TreeNodeExpansionPaths.Apply(roots, paths);

    private void HandleCollapseToggle(IReadOnlyList<ITreeNode> roots, Action collapseAction, CollapseActionType actionType)
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
                TreeNodeExpansionPaths.Apply(roots, _savedExpandedPaths!);
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
            _savedExpandedPaths = TreeNodeExpansionPaths.Capture(roots);
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
}
