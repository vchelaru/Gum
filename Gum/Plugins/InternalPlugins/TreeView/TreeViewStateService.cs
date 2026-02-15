using CommonFormsAndControls;
using Gum.Managers;
using Gum.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Gum.Plugins.InternalPlugins.TreeView;

/// <summary>
/// Service responsible for saving/loading tree view expansion state.
/// Sits between ElementTreeViewManager and UserProjectSettingsManager.
/// </summary>
public class TreeViewStateService : ITreeViewStateService
{
    private readonly IUserProjectSettingsManager _settingsManager;
    private readonly IOutputManager _outputManager;

    public TreeViewStateService(IUserProjectSettingsManager settingsManager, IOutputManager outputManager)
    {
        _settingsManager = settingsManager;
        _outputManager = outputManager;
    }

    /// <summary>
    /// Load tree view state from settings and apply to tree.
    /// Called after project load and tree population.
    /// </summary>
    public void LoadAndApplyState(MultiSelectTreeView treeView)
    {
        if (treeView == null)
        {
            return;
        }

        try
        {
            var settings = _settingsManager.CurrentSettings;
            if (settings?.TreeViewState?.ExpandedNodes == null)
            {
                return;
            }

            ApplyExpandedNodePaths(treeView, settings.TreeViewState.ExpandedNodes);
        }
        catch (Exception ex)
        {
            _outputManager.AddError($"Error applying tree view state: {ex.Message}");
        }
    }

    /// <summary>
    /// Capture current tree view state and save to settings.
    /// Called on application exit.
    /// </summary>
    public void CaptureAndSaveState(MultiSelectTreeView treeView)
    {
        if (treeView == null)
        {
            return;
        }

        try
        {
            var settings = _settingsManager.CurrentSettings;
            if (settings == null)
            {
                return;
            }

            List<string> expandedPaths = GetExpandedNodePaths(treeView);

            if (settings.TreeViewState == null)
            {
                settings.TreeViewState = new Settings.TreeViewState();
            }

            settings.TreeViewState.ExpandedNodes = expandedPaths;
        }
        catch (Exception ex)
        {
            _outputManager.AddError($"Error capturing tree view state: {ex.Message}");
        }
    }

    /// <summary>
    /// Get list of paths for all expanded nodes in the tree.
    /// </summary>
    private List<string> GetExpandedNodePaths(MultiSelectTreeView treeView)
    {
        List<string> expandedPaths = new List<string>();

        // Walk through all root nodes
        foreach (TreeNode rootNode in treeView.Nodes)
        {
            CollectExpandedPaths(rootNode, expandedPaths);
        }

        return expandedPaths;
    }

    /// <summary>
    /// Recursively collect paths of expanded nodes.
    /// </summary>
    private void CollectExpandedPaths(TreeNode node, List<string> expandedPaths)
    {
        if (node.IsExpanded)
        {
            string path = GetNodePath(node);
            if (!string.IsNullOrEmpty(path))
            {
                expandedPaths.Add(path);
            }

            // Recursively check child nodes
            foreach (TreeNode childNode in node.Nodes)
            {
                CollectExpandedPaths(childNode, expandedPaths);
            }
        }
    }

    /// <summary>
    /// Build hierarchical path for a node (e.g., "Components/Buttons/PrimaryButton").
    /// </summary>
    private string GetNodePath(TreeNode node)
    {
        List<string> pathParts = new List<string>();

        TreeNode? currentNode = node;
        while (currentNode != null)
        {
            pathParts.Insert(0, currentNode.Text);
            currentNode = currentNode.Parent;
        }

        return string.Join("/", pathParts);
    }

    /// <summary>
    /// Apply expanded state to nodes based on saved paths.
    /// </summary>
    private void ApplyExpandedNodePaths(MultiSelectTreeView treeView, List<string> paths)
    {
        // Only expand nodes that match saved paths
        // Don't collapse anything - leave tree in its default state from RefreshUi
        foreach (string path in paths)
        {
            TreeNode? node = FindNodeByPath(treeView, path);
            if (node != null)
            {
                node.Expand();
            }
        }
    }

    /// <summary>
    /// Find a node by its hierarchical path.
    /// </summary>
    private TreeNode? FindNodeByPath(MultiSelectTreeView treeView, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        string[] pathParts = path.Split('/');
        if (pathParts.Length == 0)
        {
            return null;
        }

        // Start with root nodes
        TreeNode? currentNode = null;
        foreach (TreeNode rootNode in treeView.Nodes)
        {
            if (rootNode.Text == pathParts[0])
            {
                currentNode = rootNode;
                break;
            }
        }

        if (currentNode == null)
        {
            return null;
        }

        // Walk down the path
        for (int i = 1; i < pathParts.Length; i++)
        {
            bool found = false;
            foreach (TreeNode childNode in currentNode.Nodes)
            {
                if (childNode.Text == pathParts[i])
                {
                    currentNode = childNode;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return null;
            }
        }

        return currentNode;
    }
}
