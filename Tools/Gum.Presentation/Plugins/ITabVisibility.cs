namespace Gum.Plugins;

/// <summary>
/// Narrow, headless-safe seam over a plugin's own tab (<c>PluginTab</c>) for logic that only needs
/// to show/hide it. <c>PluginTab</c> itself is WPF-typed (its <c>Content</c> is a
/// <c>System.Windows.FrameworkElement</c>), which would block referencing it from
/// <c>Gum.Presentation</c> even though this seam's members don't touch WPF at all - same shape as
/// <c>ITabDockingCandidate</c>/<c>ITabAutoSelectCandidate</c>.
/// </summary>
public interface ITabVisibility
{
    void Show();
    void Hide();
}
