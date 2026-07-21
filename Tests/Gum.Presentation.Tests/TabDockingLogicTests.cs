using Gum.Plugins;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization tests for <see cref="TabDockingLogic"/>, pinning the "a tab appears in a
/// docking region's view only when its own location matches and it is visible" behavior relocated
/// from <c>MainPanelViewModel</c>'s <c>ListCollectionView.Filter</c> predicates (#3856).
/// </summary>
public class TabDockingLogicTests
{
    private class FakeTab : ITabDockingCandidate
    {
        public TabLocation Location { get; init; }
        public bool IsVisible { get; init; }
    }

    [Fact]
    public void ShouldAppearInLocation_ReturnsFalse_WhenLocationDiffersAndTabIsVisible()
    {
        FakeTab tab = new() { Location = TabLocation.Left, IsVisible = true };

        bool result = TabDockingLogic.ShouldAppearInLocation(tab, TabLocation.RightTop);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ShouldAppearInLocation_ReturnsFalse_WhenLocationMatchesButTabIsNotVisible()
    {
        FakeTab tab = new() { Location = TabLocation.CenterTop, IsVisible = false };

        bool result = TabDockingLogic.ShouldAppearInLocation(tab, TabLocation.CenterTop);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ShouldAppearInLocation_ReturnsFalse_WhenLocationDiffersAndTabIsNotVisible()
    {
        FakeTab tab = new() { Location = TabLocation.Left, IsVisible = false };

        bool result = TabDockingLogic.ShouldAppearInLocation(tab, TabLocation.RightTop);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ShouldAppearInLocation_ReturnsTrue_WhenLocationMatchesAndTabIsVisible()
    {
        FakeTab tab = new() { Location = TabLocation.RightBottom, IsVisible = true };

        bool result = TabDockingLogic.ShouldAppearInLocation(tab, TabLocation.RightBottom);

        result.ShouldBeTrue();
    }
}
