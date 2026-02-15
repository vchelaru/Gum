using CommonFormsAndControls;

namespace Gum.Plugins.InternalPlugins.TreeView;

/// <summary>
/// Service responsible for saving/loading tree view expansion state.
/// </summary>
public interface ITreeViewStateService
{
    /// <summary>
    /// Load tree view state from settings and apply to tree.
    /// Called after project load and tree population.
    /// </summary>
    void LoadAndApplyState(MultiSelectTreeView treeView);

    /// <summary>
    /// Capture current tree view state and save to settings.
    /// Called on application exit.
    /// </summary>
    void CaptureAndSaveState(MultiSelectTreeView treeView);
}
