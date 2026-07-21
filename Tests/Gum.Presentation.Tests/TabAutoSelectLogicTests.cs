using System.Collections.Generic;
using Gum.Plugins;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization tests for <see cref="TabAutoSelectLogic"/>, pinning the "select a newly-added
/// tab by default only if its location has no existing selection" behavior relocated from
/// <c>MainPanelViewModel.PluginTabsOnCollectionChanged</c> (#3856).
/// </summary>
public class TabAutoSelectLogicTests
{
    private class FakeTab : ITabAutoSelectCandidate
    {
        public TabLocation Location { get; init; }
        public bool IsSelected { get; set; }
    }

    [Fact]
    public void SelectNewTabsWithNoExistingSelection_SelectsNewTab_WhenLocationHasNoExistingSelection()
    {
        FakeTab existingTab = new() { Location = TabLocation.RightBottom, IsSelected = false };
        FakeTab newTab = new() { Location = TabLocation.RightBottom, IsSelected = false };
        List<FakeTab> allTabs = [existingTab, newTab];

        TabAutoSelectLogic.SelectNewTabsWithNoExistingSelection(allTabs, [newTab]);

        newTab.IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void SelectNewTabsWithNoExistingSelection_LeavesNewTabUnselected_WhenLocationAlreadyHasSelection()
    {
        FakeTab existingSelectedTab = new() { Location = TabLocation.RightBottom, IsSelected = true };
        FakeTab newTab = new() { Location = TabLocation.RightBottom, IsSelected = false };
        List<FakeTab> allTabs = [existingSelectedTab, newTab];

        TabAutoSelectLogic.SelectNewTabsWithNoExistingSelection(allTabs, [newTab]);

        newTab.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void SelectNewTabsWithNoExistingSelection_OnlySelectsFirstOfMultipleNewTabsAtSameLocation()
    {
        FakeTab firstNewTab = new() { Location = TabLocation.Left, IsSelected = false };
        FakeTab secondNewTab = new() { Location = TabLocation.Left, IsSelected = false };
        List<FakeTab> allTabs = [firstNewTab, secondNewTab];

        TabAutoSelectLogic.SelectNewTabsWithNoExistingSelection(allTabs, [firstNewTab, secondNewTab]);

        firstNewTab.IsSelected.ShouldBeTrue();
        secondNewTab.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void SelectNewTabsWithNoExistingSelection_DoesNotAffectTabsAtDifferentLocations()
    {
        FakeTab selectedElsewhere = new() { Location = TabLocation.CenterTop, IsSelected = true };
        FakeTab newTab = new() { Location = TabLocation.RightTop, IsSelected = false };
        List<FakeTab> allTabs = [selectedElsewhere, newTab];

        TabAutoSelectLogic.SelectNewTabsWithNoExistingSelection(allTabs, [newTab]);

        newTab.IsSelected.ShouldBeTrue();
        selectedElsewhere.IsSelected.ShouldBeTrue();
    }
}
