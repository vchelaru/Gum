using Gum.Managers;
using Gum.Services;
using System;
using System.Collections.Generic;

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
    public void LoadAndApplyState(IReadOnlyList<ITreeNode> roots)
    {
        if (roots == null)
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

            TreeNodeExpansionPaths.Apply(roots, settings.TreeViewState.ExpandedNodes);
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
    public void CaptureAndSaveState(IReadOnlyList<ITreeNode> roots)
    {
        if (roots == null)
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

            List<string> expandedPaths = TreeNodeExpansionPaths.Capture(roots);

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
}
