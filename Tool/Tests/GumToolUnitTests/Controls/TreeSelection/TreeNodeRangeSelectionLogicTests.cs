using Gum.Controls;
using Gum.Managers;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace GumToolUnitTests.Controls.TreeSelection;

public class TreeNodeRangeSelectionLogicTests : BaseTestClass
{
    private readonly GumTreeNodeCollection _rootNodes;
    private readonly TreeNodeRangeSelectionLogic _logic;

    public TreeNodeRangeSelectionLogicTests()
    {
        _rootNodes = new GumTreeNodeCollection();
        _logic = new TreeNodeRangeSelectionLogic();
    }

    private static GumTreeNode AddNode(GumTreeNodeCollection nodes, string text)
    {
        GumTreeNode node = new GumTreeNode(text);
        nodes.Add(node);
        return node;
    }

    [Fact]
    public void SelectRange_SameParentForward_SelectsNodesAfterStartUpToAndIncludingEnd()
    {
        GumTreeNode first = AddNode(_rootNodes, "First");
        GumTreeNode second = AddNode(_rootNodes, "Second");
        GumTreeNode third = AddNode(_rootNodes, "Third");
        List<GumTreeNode> selected = new List<GumTreeNode>();

        _logic.SelectRange(first, third, node => selected.Add(node));

        selected.ShouldBe(new List<GumTreeNode> { second, third });
    }

    [Fact]
    public void SelectRange_SameParentBackward_SelectsNodesBeforeStartDownToAndIncludingEnd()
    {
        GumTreeNode first = AddNode(_rootNodes, "First");
        GumTreeNode second = AddNode(_rootNodes, "Second");
        GumTreeNode third = AddNode(_rootNodes, "Third");
        List<GumTreeNode> selected = new List<GumTreeNode>();

        _logic.SelectRange(third, first, node => selected.Add(node));

        selected.ShouldBe(new List<GumTreeNode> { second, first });
    }

    [Fact]
    public void SelectRange_SameNode_SelectsNothing()
    {
        GumTreeNode only = AddNode(_rootNodes, "Only");
        List<GumTreeNode> selected = new List<GumTreeNode>();

        _logic.SelectRange(only, only, node => selected.Add(node));

        selected.ShouldBeEmpty();
    }

    [Fact]
    public void SelectRange_DifferentParents_WalksThroughCommonAncestorInVisibleOrder()
    {
        GumTreeNode rootA = AddNode(_rootNodes, "RootA");
        GumTreeNode childA = AddNode(rootA.Nodes, "ChildA");
        GumTreeNode rootB = AddNode(_rootNodes, "RootB");
        GumTreeNode childB = AddNode(rootB.Nodes, "ChildB");
        rootA.IsExpanded = true;
        rootB.IsExpanded = true;
        List<GumTreeNode> selected = new List<GumTreeNode>();

        _logic.SelectRange(childA, childB, node => selected.Add(node));

        selected.ShouldBe(new List<GumTreeNode> { rootB, childB });
    }

    [Fact]
    public void SelectRange_EndIsAncestorOfStart_WalksBackwardThroughIntermediateNodes()
    {
        GumTreeNode a = AddNode(_rootNodes, "A");
        GumTreeNode a1 = AddNode(a.Nodes, "A1");
        GumTreeNode a1a = AddNode(a1.Nodes, "A1a");
        a.IsExpanded = true;
        a1.IsExpanded = true;
        List<GumTreeNode> selected = new List<GumTreeNode>();

        _logic.SelectRange(a1a, a, node => selected.Add(node));

        selected.ShouldBe(new List<GumTreeNode> { a1, a });
    }
}
