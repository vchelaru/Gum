namespace Gum.Plugins;

/// <summary>
/// The subset of a docking-layout tab's state needed to decide whether it belongs in a given
/// docking region's view. Narrow interface so the decision logic can live in the headless assembly
/// even though the concrete tab type (<c>PluginTab</c>) is WPF-typed and stays tool-side (part of
/// #3856).
/// </summary>
public interface ITabDockingCandidate
{
    /// <summary>The docking region this tab belongs in.</summary>
    TabLocation Location { get; }

    /// <summary>Whether this tab is currently shown (not closed/hidden).</summary>
    bool IsVisible { get; }
}

/// <summary>
/// Decides whether a tab should appear in a given docking region's view. Relocated from
/// <c>MainPanelViewModel</c>'s <c>ListCollectionView.Filter</c> predicates (part of #3856) — the
/// decision itself never touched a WPF type, only the concrete <c>PluginTab</c> it operated on and
/// the WPF <c>ICollectionView</c>/<c>ListCollectionView</c> mechanism that applies it.
/// </summary>
public static class TabDockingLogic
{
    /// <summary>
    /// True if <paramref name="tab"/> belongs in <paramref name="location"/>'s view: its own
    /// <see cref="ITabDockingCandidate.Location"/> matches and it is currently visible.
    /// </summary>
    public static bool ShouldAppearInLocation<T>(T tab, TabLocation location)
        where T : ITabDockingCandidate
        => tab.Location == location && tab.IsVisible;
}
