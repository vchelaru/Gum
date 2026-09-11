using System.Collections.Generic;
using Gum.Managers;

namespace Gum.Plugins.InternalPlugins.TreeView;

/// <summary>
/// Service responsible for saving/loading tree view expansion state.
/// </summary>
public interface ITreeViewStateService
{
    /// <summary>
    /// Load tree view state from settings and apply to the tree rooted at <paramref name="roots"/>.
    /// Called after project load and tree population.
    /// </summary>
    void LoadAndApplyState(IReadOnlyList<ITreeNode> roots);

    /// <summary>
    /// Capture current tree view state and save to settings.
    /// Called on application exit.
    /// </summary>
    void CaptureAndSaveState(IReadOnlyList<ITreeNode> roots);
}
