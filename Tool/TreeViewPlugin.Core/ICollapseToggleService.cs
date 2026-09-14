using System;
using System.Collections.Generic;
using Gum.Managers;

namespace Gum.Plugins.InternalPlugins.TreeView;

/// <summary>
/// Service that makes collapse buttons act as toggles.
/// First click captures expansion state and collapses; second click restores.
/// Any manual expand/collapse of nodes invalidates the saved state.
/// </summary>
public interface ICollapseToggleService
{
    /// <summary>
    /// Handle the Collapse All button click. Toggles between collapsing all nodes
    /// and restoring the previously saved expansion state.
    /// </summary>
    void HandleCollapseAll(IReadOnlyList<ITreeNode> roots, Action collapseAllAction);

    /// <summary>
    /// Handle the Collapse to Element Level button click. Toggles between collapsing
    /// element-level nodes and restoring the previously saved expansion state.
    /// </summary>
    void HandleCollapseToElementLevel(IReadOnlyList<ITreeNode> roots, Action collapseToElementLevelAction);

    /// <summary>
    /// Called when a node is manually expanded or collapsed by the user.
    /// Marks the saved state as dirty so the next button click will re-capture.
    /// </summary>
    void OnNodeManuallyChanged();

    /// <summary>
    /// Clears all saved state. Called when the tree is refreshed.
    /// </summary>
    void Clear();

    /// <summary>
    /// Returns the paths of all currently expanded nodes.
    /// </summary>
    List<string> SaveExpandedPaths(IReadOnlyList<ITreeNode> roots);

    /// <summary>
    /// Expands nodes matching the previously saved paths.
    /// </summary>
    void RestoreExpandedPaths(IReadOnlyList<ITreeNode> roots, List<string> paths);
}
