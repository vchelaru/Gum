using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.TreeView;

public class CollapseToggleServiceTests : BaseTestClass
{
    private readonly CollapseToggleService _service;
    private readonly List<ITreeNode> _roots;

    public CollapseToggleServiceTests()
    {
        _service = new CollapseToggleService();
        _roots = new List<ITreeNode>();
    }

    /// <summary>
    /// Adds a "Components" root containing "Button" (which itself contains "States") and "Label",
    /// with Components and Button expanded. Returns the root.
    /// </summary>
    private GumTreeNode SetupTreeWithExpandedNodes()
    {
        GumTreeNode root = new GumTreeNode("Components");
        GumTreeNode button = (GumTreeNode)root.AddChild("Button");
        root.AddChild("Label");
        button.AddChild("States");
        root.Expand();
        button.Expand();

        _roots.Add(root);
        return root;
    }

    private void CollapseAllNodes()
    {
        foreach (ITreeNode node in _roots)
        {
            CollapseNodeRecursive(node);
        }
    }

    private static void CollapseNodeRecursive(ITreeNode node)
    {
        node.Collapse();
        foreach (ITreeNode child in node.Children)
        {
            CollapseNodeRecursive(child);
        }
    }

    /// <summary>
    /// Stands in for the tree's "collapse to element level" action, which collapses the nodes below
    /// each root but leaves the roots themselves expanded.
    /// </summary>
    private static void CollapseChildrenOf(GumTreeNode node)
    {
        foreach (ITreeNode child in node.Children)
        {
            child.Collapse();
        }
    }

    [Fact]
    public void Clear_ShouldDiscardSavedState()
    {
        // Arrange
        SetupTreeWithExpandedNodes();
        _service.HandleCollapseAll(_roots, CollapseAllNodes);

        // Act
        _service.Clear();
        _roots.Clear();
        // Click again - should capture and collapse, not restore
        GumTreeNode root = SetupTreeWithExpandedNodes();
        _service.HandleCollapseAll(_roots, CollapseAllNodes);

        // Assert - all collapsed because it re-captured, not restored
        root.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void HandleCollapseAll_ShouldCaptureAndCollapse_OnFirstClick()
    {
        // Arrange
        GumTreeNode root = SetupTreeWithExpandedNodes();
        root.IsExpanded.ShouldBeTrue();

        // Act
        _service.HandleCollapseAll(_roots, CollapseAllNodes);

        // Assert
        root.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void HandleCollapseAll_ShouldRecapture_WhenManualChangeOccurred()
    {
        // Arrange
        GumTreeNode root = SetupTreeWithExpandedNodes();
        _service.HandleCollapseAll(_roots, CollapseAllNodes);

        // Simulate manual change
        _service.OnNodeManuallyChanged();

        // Expand some nodes manually to give it new state
        root.Expand();

        // Act - click again after manual change, should capture new state and collapse
        _service.HandleCollapseAll(_roots, CollapseAllNodes);

        // Assert - collapsed again (re-captured, not restored)
        root.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void HandleCollapseAll_ShouldRestore_OnSecondClickWithoutManualChange()
    {
        // Arrange
        GumTreeNode root = SetupTreeWithExpandedNodes();
        GumTreeNode button = root.Nodes[0];
        root.IsExpanded.ShouldBeTrue();
        button.IsExpanded.ShouldBeTrue();

        // Act - first click collapses
        _service.HandleCollapseAll(_roots, CollapseAllNodes);
        root.IsExpanded.ShouldBeFalse();

        // Act - second click restores
        _service.HandleCollapseAll(_roots, CollapseAllNodes);

        // Assert - restored to original state
        root.IsExpanded.ShouldBeTrue();
        button.IsExpanded.ShouldBeTrue();
    }

    [Fact]
    public void HandleCollapseToElementLevel_ShouldRestore_OnSecondClick()
    {
        // Arrange
        GumTreeNode root = SetupTreeWithExpandedNodes();
        GumTreeNode button = root.Nodes[0];
        root.IsExpanded.ShouldBeTrue();
        button.IsExpanded.ShouldBeTrue();

        // Act - first click collapses element-level nodes
        _service.HandleCollapseToElementLevel(_roots, () => CollapseChildrenOf(root));
        button.IsExpanded.ShouldBeFalse();

        // Act - second click restores
        _service.HandleCollapseToElementLevel(_roots, () => CollapseChildrenOf(root));

        // Assert - Button node restored to expanded
        button.IsExpanded.ShouldBeTrue();
    }

    [Fact]
    public void HandleDifferentButton_ShouldInvalidatePreviousSnapshot()
    {
        // Arrange
        GumTreeNode root = SetupTreeWithExpandedNodes();
        GumTreeNode button = root.Nodes[0];

        // First click on CollapseAll
        _service.HandleCollapseAll(_roots, CollapseAllNodes);
        root.IsExpanded.ShouldBeFalse();

        // Expand nodes again to have something to snapshot
        root.Expand();
        button.Expand();

        // Act - click the other button (CollapseToElementLevel)
        // This should discard the CollapseAll snapshot and capture a new one
        _service.HandleCollapseToElementLevel(_roots, () => CollapseChildrenOf(root));

        // Assert - collapsed element-level nodes
        button.IsExpanded.ShouldBeFalse();

        // Act - second click on CollapseToElementLevel should restore
        _service.HandleCollapseToElementLevel(_roots, () => CollapseChildrenOf(root));

        // Assert - restored to state before element-level collapse
        root.IsExpanded.ShouldBeTrue();
        button.IsExpanded.ShouldBeTrue();
    }

    [Fact]
    public void OnNodeManuallyChanged_ShouldInvalidateSnapshot()
    {
        // Arrange
        GumTreeNode root = SetupTreeWithExpandedNodes();
        _service.HandleCollapseAll(_roots, CollapseAllNodes);

        // Act - simulate manual change
        _service.OnNodeManuallyChanged();

        // Re-expand for new state
        root.Expand();

        // Click again - should NOT restore (snapshot invalidated), should re-capture and collapse
        _service.HandleCollapseAll(_roots, CollapseAllNodes);

        // Assert - collapsed because it re-captured instead of restoring
        root.IsExpanded.ShouldBeFalse();
    }
}
