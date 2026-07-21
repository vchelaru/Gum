using System;

namespace Gum.Plugins;

/// <summary>
/// Headless-safe seam over a plugin's own tab (<c>PluginTab</c>) covering the members
/// <see cref="Gum.Plugins.BaseClasses.PluginBase"/> and <c>ITabManager</c>'s consumers need beyond
/// <see cref="ITabVisibility"/>'s Show/Hide. <c>PluginTab</c> itself is WPF-typed (its <c>Content</c>
/// is a <c>System.Windows.FrameworkElement</c>), which would block referencing it from
/// <c>Gum.Presentation</c> even though none of these members touch WPF - same shape as
/// <c>ITabDockingCandidate</c>/<c>ITabAutoSelectCandidate</c>/<c>ITabVisibility</c> (issue #3950).
/// </summary>
public interface IPluginTab : ITabVisibility
{
    TabLocation Location { get; set; }
    string Title { get; set; }
    bool IsVisible { get; set; }
    bool IsSelected { get; set; }
    bool CanClose { get; set; }

    event Action? TabShown;
    event Action? TabHidden;
    event Action? GotFocus;
}
