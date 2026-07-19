using CommonFormsAndControls;
using Shouldly;
using System.Windows.Forms;
using Xunit;

namespace GumToolUnitTests.CommonFormsAndControls;

public class TreeNodeKeyNavigationLogicTests : BaseTestClass
{
    private readonly MultiSelectTreeView _treeView;
    private readonly TreeNodeKeyNavigationLogic _logic;

    public TreeNodeKeyNavigationLogicTests()
    {
        _treeView = new MultiSelectTreeView();
        _logic = new TreeNodeKeyNavigationLogic();
    }

    public override void Dispose()
    {
        base.Dispose();
        _treeView.Dispose();
    }

    // TreeNode.Expand()/PrevVisibleNode/NextVisibleNode can force the underlying native window
    // handle to be created, which requires an STA thread (see TreeViewStateServiceTests); xUnit's
    // default runner is MTA.

    [StaFact]
    public void GetHomeTarget_NoShift_ReturnsFirstRootNode()
    {
        TreeNode first = _treeView.Nodes.Add("First");
        TreeNode second = _treeView.Nodes.Add("Second");
        second.Nodes.Add("Child");

        TreeNode? target = _logic.GetHomeTarget(
            second, _treeView.Nodes, shiftDown: false, out bool selectRange);

        target.ShouldBe(first);
        selectRange.ShouldBeFalse();
    }

    [StaFact]
    public void GetHomeTarget_ShiftAndSelectedNodeIsRoot_ReturnsFirstRootNode()
    {
        TreeNode first = _treeView.Nodes.Add("First");
        TreeNode second = _treeView.Nodes.Add("Second");

        TreeNode? target = _logic.GetHomeTarget(
            second, _treeView.Nodes, shiftDown: true, out bool selectRange);

        target.ShouldBe(first);
        selectRange.ShouldBeTrue();
    }

    [StaFact]
    public void GetHomeTarget_ShiftAndSelectedNodeHasParent_ReturnsFirstSibling()
    {
        TreeNode parent = _treeView.Nodes.Add("Parent");
        TreeNode firstChild = parent.Nodes.Add("FirstChild");
        TreeNode secondChild = parent.Nodes.Add("SecondChild");

        TreeNode? target = _logic.GetHomeTarget(
            secondChild, _treeView.Nodes, shiftDown: true, out bool selectRange);

        target.ShouldBe(firstChild);
        selectRange.ShouldBeTrue();
    }

    [StaFact]
    public void GetEndTarget_NoShift_WalksDownExpandedLastChildren()
    {
        TreeNode root = _treeView.Nodes.Add("Root");
        TreeNode child = root.Nodes.Add("Child");
        TreeNode grandchild = child.Nodes.Add("Grandchild");
        root.Expand();
        child.Expand();

        TreeNode? target = _logic.GetEndTarget(
            root, _treeView.Nodes, shiftDown: false, out bool selectRange);

        target.ShouldBe(grandchild);
        selectRange.ShouldBeFalse();
    }

    [StaFact]
    public void GetEndTarget_NoShiftAndGrandchildLevelCollapsed_DoesNotDescendPastFirstUnexpandedNode()
    {
        // Matches the original behavior exactly: only descends further while the current node
        // (not its ancestor) is itself expanded, so it stops one level short of a deeper,
        // unexpanded grandchild.
        TreeNode root = _treeView.Nodes.Add("Root");
        TreeNode child = root.Nodes.Add("Child");
        child.Nodes.Add("Grandchild");

        TreeNode? target = _logic.GetEndTarget(
            root, _treeView.Nodes, shiftDown: false, out bool selectRange);

        target.ShouldBe(child);
        selectRange.ShouldBeFalse();
    }

    [StaFact]
    public void GetEndTarget_ShiftAndSelectedNodeIsRoot_ReturnsLastRootNode()
    {
        TreeNode first = _treeView.Nodes.Add("First");
        TreeNode second = _treeView.Nodes.Add("Second");

        TreeNode? target = _logic.GetEndTarget(
            first, _treeView.Nodes, shiftDown: true, out bool selectRange);

        target.ShouldBe(second);
        selectRange.ShouldBeTrue();
    }

    [StaFact]
    public void GetEndTarget_ShiftAndSelectedNodeHasParent_ReturnsLastSibling()
    {
        TreeNode parent = _treeView.Nodes.Add("Parent");
        TreeNode firstChild = parent.Nodes.Add("FirstChild");
        TreeNode secondChild = parent.Nodes.Add("SecondChild");

        TreeNode? target = _logic.GetEndTarget(
            firstChild, _treeView.Nodes, shiftDown: true, out bool selectRange);

        target.ShouldBe(secondChild);
        selectRange.ShouldBeTrue();
    }

    [StaFact]
    public void GetPageUpTarget_VisibleCountExceedsAvailableNodes_StopsAtTopmostNode()
    {
        TreeNode first = _treeView.Nodes.Add("First");
        TreeNode second = _treeView.Nodes.Add("Second");
        TreeNode third = _treeView.Nodes.Add("Third");

        TreeNode target = _logic.GetPageUpTarget(third, visibleCount: 10);

        target.ShouldBe(first);
    }

    [StaFact]
    public void GetPageUpTarget_VisibleCountLessThanAvailableNodes_StopsPartway()
    {
        TreeNode first = _treeView.Nodes.Add("First");
        TreeNode second = _treeView.Nodes.Add("Second");
        TreeNode third = _treeView.Nodes.Add("Third");

        TreeNode target = _logic.GetPageUpTarget(third, visibleCount: 1);

        target.ShouldBe(second);
    }

    [StaFact]
    public void GetPageDownTarget_VisibleCountExceedsAvailableNodes_StopsAtBottommostNode()
    {
        TreeNode first = _treeView.Nodes.Add("First");
        TreeNode second = _treeView.Nodes.Add("Second");
        TreeNode third = _treeView.Nodes.Add("Third");

        TreeNode target = _logic.GetPageDownTarget(first, visibleCount: 10);

        target.ShouldBe(third);
    }

    [StaFact]
    public void GetPageDownTarget_VisibleCountLessThanAvailableNodes_StopsPartway()
    {
        TreeNode first = _treeView.Nodes.Add("First");
        TreeNode second = _treeView.Nodes.Add("Second");
        TreeNode third = _treeView.Nodes.Add("Third");

        TreeNode target = _logic.GetPageDownTarget(first, visibleCount: 1);

        target.ShouldBe(second);
    }
}
