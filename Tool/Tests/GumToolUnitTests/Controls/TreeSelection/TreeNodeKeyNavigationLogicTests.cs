using Gum.Controls;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Controls.TreeSelection;

public class TreeNodeKeyNavigationLogicTests : BaseTestClass
{
    private readonly GumTreeNodeCollection _rootNodes;
    private readonly TreeNodeKeyNavigationLogic _logic;

    public TreeNodeKeyNavigationLogicTests()
    {
        _rootNodes = new GumTreeNodeCollection();
        _logic = new TreeNodeKeyNavigationLogic();
    }

    private static GumTreeNode AddNode(GumTreeNodeCollection nodes, string text)
    {
        GumTreeNode node = new GumTreeNode(text);
        nodes.Add(node);
        return node;
    }

    [Fact]
    public void GetHomeTarget_NoShift_ReturnsFirstRootNode()
    {
        GumTreeNode first = AddNode(_rootNodes, "First");
        GumTreeNode second = AddNode(_rootNodes, "Second");
        AddNode(second.Nodes, "Child");

        GumTreeNode? target = _logic.GetHomeTarget(
            second, _rootNodes, shiftDown: false, out bool selectRange);

        target.ShouldBe(first);
        selectRange.ShouldBeFalse();
    }

    [Fact]
    public void GetHomeTarget_ShiftAndSelectedNodeIsRoot_ReturnsFirstRootNode()
    {
        GumTreeNode first = AddNode(_rootNodes, "First");
        GumTreeNode second = AddNode(_rootNodes, "Second");

        GumTreeNode? target = _logic.GetHomeTarget(
            second, _rootNodes, shiftDown: true, out bool selectRange);

        target.ShouldBe(first);
        selectRange.ShouldBeTrue();
    }

    [Fact]
    public void GetHomeTarget_ShiftAndSelectedNodeHasParent_ReturnsFirstSibling()
    {
        GumTreeNode parent = AddNode(_rootNodes, "Parent");
        GumTreeNode firstChild = AddNode(parent.Nodes, "FirstChild");
        GumTreeNode secondChild = AddNode(parent.Nodes, "SecondChild");

        GumTreeNode? target = _logic.GetHomeTarget(
            secondChild, _rootNodes, shiftDown: true, out bool selectRange);

        target.ShouldBe(firstChild);
        selectRange.ShouldBeTrue();
    }

    [Fact]
    public void GetEndTarget_NoShift_WalksDownExpandedLastChildren()
    {
        GumTreeNode root = AddNode(_rootNodes, "Root");
        GumTreeNode child = AddNode(root.Nodes, "Child");
        GumTreeNode grandchild = AddNode(child.Nodes, "Grandchild");
        root.IsExpanded = true;
        child.IsExpanded = true;

        GumTreeNode? target = _logic.GetEndTarget(
            root, _rootNodes, shiftDown: false, out bool selectRange);

        target.ShouldBe(grandchild);
        selectRange.ShouldBeFalse();
    }

    [Fact]
    public void GetEndTarget_NoShiftAndGrandchildLevelCollapsed_DoesNotDescendPastFirstUnexpandedNode()
    {
        // Only descends further while the current node (not its ancestor) is itself expanded, so it
        // stops one level short of a deeper, unexpanded grandchild.
        GumTreeNode root = AddNode(_rootNodes, "Root");
        GumTreeNode child = AddNode(root.Nodes, "Child");
        AddNode(child.Nodes, "Grandchild");

        GumTreeNode? target = _logic.GetEndTarget(
            root, _rootNodes, shiftDown: false, out bool selectRange);

        target.ShouldBe(child);
        selectRange.ShouldBeFalse();
    }

    [Fact]
    public void GetEndTarget_NoShiftAndFirstRootHasNoChildren_ReturnsNull()
    {
        // The tool's first root is a folder that can legitimately be empty, so the downward walk
        // has to cope with there being no last child at all.
        GumTreeNode root = AddNode(_rootNodes, "Root");

        GumTreeNode? target = _logic.GetEndTarget(
            root, _rootNodes, shiftDown: false, out bool selectRange);

        target.ShouldBeNull();
        selectRange.ShouldBeFalse();
    }

    [Fact]
    public void GetEndTarget_ShiftAndSelectedNodeIsRoot_ReturnsLastRootNode()
    {
        GumTreeNode first = AddNode(_rootNodes, "First");
        GumTreeNode second = AddNode(_rootNodes, "Second");

        GumTreeNode? target = _logic.GetEndTarget(
            first, _rootNodes, shiftDown: true, out bool selectRange);

        target.ShouldBe(second);
        selectRange.ShouldBeTrue();
    }

    [Fact]
    public void GetEndTarget_ShiftAndSelectedNodeHasParent_ReturnsLastSibling()
    {
        GumTreeNode parent = AddNode(_rootNodes, "Parent");
        GumTreeNode firstChild = AddNode(parent.Nodes, "FirstChild");
        GumTreeNode secondChild = AddNode(parent.Nodes, "SecondChild");

        GumTreeNode? target = _logic.GetEndTarget(
            firstChild, _rootNodes, shiftDown: true, out bool selectRange);

        target.ShouldBe(secondChild);
        selectRange.ShouldBeTrue();
    }

    [Fact]
    public void GetPageUpTarget_VisibleCountExceedsAvailableNodes_StopsAtTopmostNode()
    {
        GumTreeNode first = AddNode(_rootNodes, "First");
        AddNode(_rootNodes, "Second");
        GumTreeNode third = AddNode(_rootNodes, "Third");

        GumTreeNode target = _logic.GetPageUpTarget(third, visibleCount: 10);

        target.ShouldBe(first);
    }

    [Fact]
    public void GetPageUpTarget_VisibleCountLessThanAvailableNodes_StopsPartway()
    {
        AddNode(_rootNodes, "First");
        GumTreeNode second = AddNode(_rootNodes, "Second");
        GumTreeNode third = AddNode(_rootNodes, "Third");

        GumTreeNode target = _logic.GetPageUpTarget(third, visibleCount: 1);

        target.ShouldBe(second);
    }

    [Fact]
    public void GetPageDownTarget_VisibleCountExceedsAvailableNodes_StopsAtBottommostNode()
    {
        GumTreeNode first = AddNode(_rootNodes, "First");
        AddNode(_rootNodes, "Second");
        GumTreeNode third = AddNode(_rootNodes, "Third");

        GumTreeNode target = _logic.GetPageDownTarget(first, visibleCount: 10);

        target.ShouldBe(third);
    }

    [Fact]
    public void GetPageDownTarget_VisibleCountLessThanAvailableNodes_StopsPartway()
    {
        GumTreeNode first = AddNode(_rootNodes, "First");
        GumTreeNode second = AddNode(_rootNodes, "Second");
        AddNode(_rootNodes, "Third");

        GumTreeNode target = _logic.GetPageDownTarget(first, visibleCount: 1);

        target.ShouldBe(second);
    }
}
