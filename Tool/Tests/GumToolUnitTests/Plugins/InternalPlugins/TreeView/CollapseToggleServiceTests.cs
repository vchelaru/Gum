using CommonFormsAndControls;
using Gum.Plugins.InternalPlugins.TreeView;
using Shouldly;
using System.Windows.Forms;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.TreeView;

public class CollapseToggleServiceTests : BaseTestClass
{
    private readonly CollapseToggleService _service;
    private readonly MultiSelectTreeView _treeView;

    public CollapseToggleServiceTests()
    {
        _service = new CollapseToggleService();
        _treeView = new MultiSelectTreeView();
    }

    public override void Dispose()
    {
        base.Dispose();
        _treeView?.Dispose();
    }

    private void SetupTreeWithExpandedNodes()
    {
        var root = _treeView.Nodes.Add("Components");
        var child1 = root.Nodes.Add("Button");
        var child2 = root.Nodes.Add("Label");
        child1.Nodes.Add("States");
        root.Expand();
        child1.Expand();
    }

    [Fact]
    public void Clear_ShouldDiscardSavedState()
    {
        // Arrange
        SetupTreeWithExpandedNodes();
        _service.HandleCollapseAll(_treeView, () => _treeView.CollapseAll());

        // Act
        _service.Clear();
        // Click again - should capture and collapse, not restore
        SetupTreeWithExpandedNodes();
        _service.HandleCollapseAll(_treeView, () => _treeView.CollapseAll());

        // Assert - all collapsed because it re-captured, not restored
        _treeView.Nodes[0].IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void HandleCollapseAll_ShouldCaptureAndCollapse_OnFirstClick()
    {
        // Arrange
        SetupTreeWithExpandedNodes();
        _treeView.Nodes[0].IsExpanded.ShouldBeTrue();

        // Act
        _service.HandleCollapseAll(_treeView, () => _treeView.CollapseAll());

        // Assert
        _treeView.Nodes[0].IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void HandleCollapseAll_ShouldRecapture_WhenManualChangeOccurred()
    {
        // Arrange
        SetupTreeWithExpandedNodes();
        _service.HandleCollapseAll(_treeView, () => _treeView.CollapseAll());

        // Simulate manual change
        _service.OnNodeManuallyChanged();

        // Expand some nodes manually to give it new state
        _treeView.Nodes[0].Expand();

        // Act - click again after manual change, should capture new state and collapse
        _service.HandleCollapseAll(_treeView, () => _treeView.CollapseAll());

        // Assert - collapsed again (re-captured, not restored)
        _treeView.Nodes[0].IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void HandleCollapseAll_ShouldRestore_OnSecondClickWithoutManualChange()
    {
        // Arrange
        SetupTreeWithExpandedNodes();
        var rootWasExpanded = _treeView.Nodes[0].IsExpanded;
        var buttonWasExpanded = _treeView.Nodes[0].Nodes[0].IsExpanded;
        rootWasExpanded.ShouldBeTrue();
        buttonWasExpanded.ShouldBeTrue();

        // Act - first click collapses
        _service.HandleCollapseAll(_treeView, () => _treeView.CollapseAll());
        _treeView.Nodes[0].IsExpanded.ShouldBeFalse();

        // Act - second click restores
        _service.HandleCollapseAll(_treeView, () => _treeView.CollapseAll());

        // Assert - restored to original state
        _treeView.Nodes[0].IsExpanded.ShouldBeTrue();
        _treeView.Nodes[0].Nodes[0].IsExpanded.ShouldBeTrue();
    }

    [Fact]
    public void HandleCollapseToElementLevel_ShouldRestore_OnSecondClick()
    {
        // Arrange
        SetupTreeWithExpandedNodes();
        _treeView.Nodes[0].IsExpanded.ShouldBeTrue();
        _treeView.Nodes[0].Nodes[0].IsExpanded.ShouldBeTrue();

        // Act - first click collapses element-level nodes
        _service.HandleCollapseToElementLevel(_treeView, () =>
        {
            // Simulate collapsing only element-level nodes (nodes with children)
            foreach (TreeNode child in _treeView.Nodes[0].Nodes)
            {
                child.Collapse();
            }
        });
        _treeView.Nodes[0].Nodes[0].IsExpanded.ShouldBeFalse();

        // Act - second click restores
        _service.HandleCollapseToElementLevel(_treeView, () =>
        {
            foreach (TreeNode child in _treeView.Nodes[0].Nodes)
            {
                child.Collapse();
            }
        });

        // Assert - Button node restored to expanded
        _treeView.Nodes[0].Nodes[0].IsExpanded.ShouldBeTrue();
    }

    [Fact]
    public void HandleDifferentButton_ShouldInvalidatePreviousSnapshot()
    {
        // Arrange
        SetupTreeWithExpandedNodes();

        // First click on CollapseAll
        _service.HandleCollapseAll(_treeView, () => _treeView.CollapseAll());
        _treeView.Nodes[0].IsExpanded.ShouldBeFalse();

        // Expand nodes again to have something to snapshot
        _treeView.Nodes[0].Expand();
        _treeView.Nodes[0].Nodes[0].Expand();

        // Act - click the other button (CollapseToElementLevel)
        // This should discard the CollapseAll snapshot and capture a new one
        _service.HandleCollapseToElementLevel(_treeView, () =>
        {
            foreach (TreeNode child in _treeView.Nodes[0].Nodes)
            {
                child.Collapse();
            }
        });

        // Assert - collapsed element-level nodes
        _treeView.Nodes[0].Nodes[0].IsExpanded.ShouldBeFalse();

        // Act - second click on CollapseToElementLevel should restore
        _service.HandleCollapseToElementLevel(_treeView, () =>
        {
            foreach (TreeNode child in _treeView.Nodes[0].Nodes)
            {
                child.Collapse();
            }
        });

        // Assert - restored to state before element-level collapse
        _treeView.Nodes[0].IsExpanded.ShouldBeTrue();
        _treeView.Nodes[0].Nodes[0].IsExpanded.ShouldBeTrue();
    }

    [Fact]
    public void OnNodeManuallyChanged_ShouldInvalidateSnapshot()
    {
        // Arrange
        SetupTreeWithExpandedNodes();
        _service.HandleCollapseAll(_treeView, () => _treeView.CollapseAll());

        // Act - simulate manual change
        _service.OnNodeManuallyChanged();

        // Re-expand for new state
        _treeView.Nodes[0].Expand();

        // Click again - should NOT restore (snapshot invalidated), should re-capture and collapse
        _service.HandleCollapseAll(_treeView, () => _treeView.CollapseAll());

        // Assert - collapsed because it re-captured instead of restoring
        _treeView.Nodes[0].IsExpanded.ShouldBeFalse();
    }
}
