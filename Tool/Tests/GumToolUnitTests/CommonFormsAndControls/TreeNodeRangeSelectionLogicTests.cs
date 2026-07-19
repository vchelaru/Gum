using CommonFormsAndControls;
using Shouldly;
using System.Collections.Generic;
using System.Windows.Forms;
using Xunit;

namespace GumToolUnitTests.CommonFormsAndControls;

public class TreeNodeRangeSelectionLogicTests : BaseTestClass
{
    private readonly MultiSelectTreeView _treeView;
    private readonly TreeNodeRangeSelectionLogic _logic;

    public TreeNodeRangeSelectionLogicTests()
    {
        _treeView = new MultiSelectTreeView();
        _logic = new TreeNodeRangeSelectionLogic();
    }

    public override void Dispose()
    {
        base.Dispose();
        _treeView.Dispose();
    }

    // TreeNode.NextVisibleNode/PrevVisibleNode/Expand() can force the underlying native window
    // handle to be created, which requires an STA thread (see TreeViewStateServiceTests); xUnit's
    // default runner is MTA.

    [StaFact]
    public void SelectRange_SameParentForward_SelectsNodesAfterStartUpToAndIncludingEnd()
    {
        TreeNode first = _treeView.Nodes.Add("First");
        TreeNode second = _treeView.Nodes.Add("Second");
        TreeNode third = _treeView.Nodes.Add("Third");
        List<TreeNode> selected = new List<TreeNode>();

        _logic.SelectRange(first, third, node => selected.Add(node));

        selected.ShouldBe(new List<TreeNode> { second, third });
    }

    [StaFact]
    public void SelectRange_SameParentBackward_SelectsNodesBeforeStartDownToAndIncludingEnd()
    {
        TreeNode first = _treeView.Nodes.Add("First");
        TreeNode second = _treeView.Nodes.Add("Second");
        TreeNode third = _treeView.Nodes.Add("Third");
        List<TreeNode> selected = new List<TreeNode>();

        _logic.SelectRange(third, first, node => selected.Add(node));

        selected.ShouldBe(new List<TreeNode> { second, first });
    }

    [StaFact]
    public void SelectRange_SameNode_SelectsNothing()
    {
        TreeNode only = _treeView.Nodes.Add("Only");
        List<TreeNode> selected = new List<TreeNode>();

        _logic.SelectRange(only, only, node => selected.Add(node));

        selected.ShouldBeEmpty();
    }

    [StaFact]
    public void SelectRange_DifferentParents_WalksThroughCommonAncestorInVisibleOrder()
    {
        TreeNode rootA = _treeView.Nodes.Add("RootA");
        TreeNode childA = rootA.Nodes.Add("ChildA");
        TreeNode rootB = _treeView.Nodes.Add("RootB");
        TreeNode childB = rootB.Nodes.Add("ChildB");
        rootA.Expand();
        rootB.Expand();
        List<TreeNode> selected = new List<TreeNode>();

        _logic.SelectRange(childA, childB, node => selected.Add(node));

        selected.ShouldBe(new List<TreeNode> { rootB, childB });
    }

    [StaFact]
    public void SelectRange_EndIsAncestorOfStart_WalksBackwardThroughIntermediateNodes()
    {
        TreeNode a = _treeView.Nodes.Add("A");
        TreeNode a1 = a.Nodes.Add("A1");
        TreeNode a1a = a1.Nodes.Add("A1a");
        a.Expand();
        a1.Expand();
        List<TreeNode> selected = new List<TreeNode>();

        _logic.SelectRange(a1a, a, node => selected.Add(node));

        selected.ShouldBe(new List<TreeNode> { a1, a });
    }
}
