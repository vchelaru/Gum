using Gum.Managers;
using Shouldly;
using System.Linq;
using System.Windows.Forms;
using Xunit;

namespace GumToolUnitTests.Managers;

// Pins GumTreeNode's dual nature: it is a real System.Windows.Forms.TreeNode (so it drops in
// anywhere ElementTreeViewManager/MultiSelectTreeView expect a TreeNode) that also implements
// ITreeNodeMutable directly, with no per-call wrapper allocation, unlike TreeNodeWrapper.
public class GumTreeNodeTests
{
    [Fact]
    public void AddChild_String_AddsWinFormsChildAndReturnsItAsMutableNode()
    {
        GumTreeNode parent = new GumTreeNode("Parent");

        ITreeNodeMutable child = parent.AddChild("Child");

        parent.Nodes.Count.ShouldBe(1);
        parent.Nodes[0].ShouldBeSameAs(child);
        child.Text.ShouldBe("Child");
    }

    [Fact]
    public void AddChild_ExistingMutableNode_AddsUnderlyingWinFormsNode()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        GumTreeNode child = new GumTreeNode("Child");

        parent.AddChild((ITreeNodeMutable)child);

        parent.Nodes.Cast<TreeNode>().ShouldContain(child);
    }

    [Fact]
    public void InsertChild_InsertsAtGivenIndex()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        GumTreeNode first = new GumTreeNode("First");
        GumTreeNode second = new GumTreeNode("Second");
        parent.AddChild((ITreeNodeMutable)first);

        parent.InsertChild(0, second);

        parent.Nodes[0].ShouldBeSameAs(second);
        parent.Nodes[1].ShouldBeSameAs(first);
    }

    [Fact]
    public void RemoveChild_RemovesUnderlyingWinFormsNode()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        GumTreeNode child = new GumTreeNode("Child");
        parent.AddChild((ITreeNodeMutable)child);

        parent.RemoveChild(child);

        parent.Nodes.Count.ShouldBe(0);
    }

    [Fact]
    public void RemoveChildAt_RemovesNodeAtIndex()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        GumTreeNode first = new GumTreeNode("First");
        GumTreeNode second = new GumTreeNode("Second");
        parent.AddChild((ITreeNodeMutable)first);
        parent.AddChild((ITreeNodeMutable)second);

        parent.RemoveChildAt(0);

        parent.Nodes.Count.ShouldBe(1);
        parent.Nodes[0].ShouldBeSameAs(second);
    }

    [Fact]
    public void ClearChildren_RemovesAllNodes()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        parent.AddChild("First");
        parent.AddChild("Second");

        parent.ClearChildren();

        parent.Nodes.Count.ShouldBe(0);
    }

    [Fact]
    public void SetTag_UpdatesWinFormsTagAndInterfaceTag()
    {
        GumTreeNode node = new GumTreeNode("Node");
        object tag = new object();

        node.SetTag(tag);

        node.Tag.ShouldBeSameAs(tag);
        ((ITreeNode)node).Tag.ShouldBeSameAs(tag);
    }

    [Fact]
    public void ImageIndex_SetThroughInterface_ReadableFromWinFormsProperty()
    {
        GumTreeNode node = new GumTreeNode("Node");
        ITreeNodeMutable asInterface = node;

        asInterface.ImageIndex = 4;

        node.ImageIndex.ShouldBe(4);
    }

    [Fact]
    public void Parent_ChildOfGumTreeNode_ReturnsSameParentInstance_NoWrapperAllocated()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable child = parent.AddChild("Child");

        ITreeNode? childParent = ((ITreeNode)child).Parent;

        childParent.ShouldBeSameAs(parent);
    }

    [Fact]
    public void Parent_RootNode_ReturnsNull()
    {
        GumTreeNode root = new GumTreeNode("Root");

        ((ITreeNode)root).Parent.ShouldBeNull();
    }

    [Fact]
    public void Children_AllGumTreeNodeChildren_ReturnsSameInstancesUnwrapped()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable child = parent.AddChild("Child");

        ITreeNode[] children = parent.Children.ToArray();

        children.ShouldHaveSingleItem();
        children[0].ShouldBeSameAs(child);
    }

    [Fact]
    public void Children_PlainWinFormsChild_FallsBackToWrapper()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        TreeNode plainChild = new TreeNode("PlainChild");
        parent.Nodes.Add(plainChild);

        ITreeNode[] children = parent.Children.ToArray();

        children.ShouldHaveSingleItem();
        children[0].Text.ShouldBe("PlainChild");
        children[0].ShouldNotBeSameAs(plainChild);
    }
}
