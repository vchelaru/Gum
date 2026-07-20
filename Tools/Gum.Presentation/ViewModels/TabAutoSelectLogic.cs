using System.Collections.Generic;
using System.Linq;

namespace Gum.Plugins;

/// <summary>
/// The subset of a docking-layout tab's state needed to decide default selection. Narrow interface
/// so the decision logic can live in the headless assembly even though the concrete tab type
/// (<c>PluginTab</c>) is WPF-typed and stays tool-side (part of #3856).
/// </summary>
public interface ITabAutoSelectCandidate
{
    /// <summary>The docking region this tab belongs in.</summary>
    TabLocation Location { get; }

    /// <summary>Whether this tab is the currently-active one within its <see cref="Location"/>.</summary>
    bool IsSelected { get; set; }
}

/// <summary>
/// Decides which newly-added docking-layout tabs should become selected by default. Relocated from
/// <c>MainPanelViewModel.PluginTabsOnCollectionChanged</c> (part of #3856) — the decision itself
/// never touched a WPF type, only the concrete <c>PluginTab</c> it operated on.
/// </summary>
public static class TabAutoSelectLogic
{
    /// <summary>
    /// For each tab in <paramref name="newTabs"/>, selects it if no tab already in
    /// <paramref name="allTabs"/> is selected at the same <see cref="ITabAutoSelectCandidate.Location"/>.
    /// </summary>
    /// <param name="allTabs">
    /// The full current tab collection, including <paramref name="newTabs"/>. Evaluated live as each
    /// new tab is processed, so selecting one new tab can prevent a later new tab at the same
    /// location from also being auto-selected.
    /// </param>
    /// <param name="newTabs">The tabs that were just added.</param>
    public static void SelectNewTabsWithNoExistingSelection<T>(IEnumerable<T> allTabs, IEnumerable<T> newTabs)
        where T : ITabAutoSelectCandidate
    {
        foreach (T newTab in newTabs)
        {
            bool locationAlreadyHasSelection = allTabs.Any(t => t.Location == newTab.Location && t.IsSelected);
            if (!locationAlreadyHasSelection)
            {
                newTab.IsSelected = true;
            }
        }
    }
}
