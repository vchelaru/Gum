namespace Gum.Managers;

/// <summary>
/// Narrow, WPF-free seam over <c>StateTreeViewRightClickService</c> so headless consumers (like
/// <c>StateTreeViewModel</c>) can rebuild the state tree's right-click context menu without
/// depending on the concrete, WPF-coupled service.
/// </summary>
public interface IStateTreeViewRightClickService
{
    /// <summary>
    /// Rebuilds the right-click context menu items to match the currently selected state/category.
    /// </summary>
    void PopulateContextMenu();
}
