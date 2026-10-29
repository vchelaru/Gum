using Gum.Managers;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// #7005: a refresh that asked for index -1 removed the node and then threw from the insert, which
/// disabled the tree plugin. An index outside the children is ignored or clamped instead.
/// </summary>
public class TreeNodeMoveToIndexBoundsTests
{
    [Fact]
    public void MoveToIndex_NegativeIndex_LeavesTheNodeWhereItIs()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable a = parent.AddChild("A");
        ITreeNodeMutable b = parent.AddChild("B");

        Should.NotThrow(() => b.MoveToIndex(-1));

        parent.Nodes[0].ShouldBeSameAs(a);
        parent.Nodes[1].ShouldBeSameAs(b);
        ((GumTreeNode)b).Parent.ShouldBeSameAs(parent);
    }

    [Fact]
    public void MoveToIndex_IndexPastTheEnd_MovesTheNodeLast()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable a = parent.AddChild("A");
        ITreeNodeMutable b = parent.AddChild("B");
        ITreeNodeMutable c = parent.AddChild("C");

        Should.NotThrow(() => a.MoveToIndex(50));

        parent.Nodes[0].ShouldBeSameAs(b);
        parent.Nodes[1].ShouldBeSameAs(c);
        parent.Nodes[2].ShouldBeSameAs(a);
    }

    [Fact]
    public void MoveToIndex_NegativeIndexOnOnlyChild_KeepsTheChild()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable only = parent.AddChild("Only");

        Should.NotThrow(() => only.MoveToIndex(-1));

        parent.Nodes.Count.ShouldBe(1);
    }
}
