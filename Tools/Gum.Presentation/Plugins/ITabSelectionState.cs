namespace Gum.Plugins;

/// <summary>
/// Narrow, headless-safe seam over a plugin's own tab (<c>PluginTab</c>) for logic that only needs to
/// know whether the tab is currently selected. Kept separate from <see cref="ITabVisibility"/>:
/// selection and visibility are different concerns, and <see cref="ITabVisibility"/> already has a
/// consumer that has no need to care about selection.
/// </summary>
public interface ITabSelectionState
{
    bool IsSelected { get; }
}
