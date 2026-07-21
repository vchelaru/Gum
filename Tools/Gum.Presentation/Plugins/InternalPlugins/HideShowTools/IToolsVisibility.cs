namespace Gum.Plugins.InternalPlugins.HideShowTools;

/// <summary>
/// Narrow, headless-safe seam over <c>MainPanelViewModel</c> for logic that only needs to toggle
/// tool-panel visibility. <c>MainPanelViewModel</c> itself is WPF-typed (exposes
/// <c>System.Windows.FrameworkElement</c>/<c>ICollectionView</c> members), which would block
/// referencing it from <c>Gum.Presentation</c> even though this seam's members don't touch WPF at
/// all - same shape as <c>ITabVisibility</c>.
/// </summary>
public interface IToolsVisibility
{
    bool IsToolsVisible { get; set; }
    void EnsureMinimumWidth();
}
