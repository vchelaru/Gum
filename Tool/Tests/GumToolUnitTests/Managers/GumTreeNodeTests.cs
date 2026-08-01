using Gum.Managers;
using Shouldly;
using System;
using System.Linq;
using Xunit;

namespace GumToolUnitTests.Managers;

// Pins GumTreeNode as the element tree's node model: it implements ITreeNodeMutable directly, so
// Parent/Children hand back the very same instances rather than allocating an adapter per access,
// and it owns the structure/navigation surface (parent tracking, RemoveSelf, visible-node walking)
// that the WinForms TreeNode base class used to supply.
public class GumTreeNodeTests
{
    [Fact]
    public void AddChild_ExistingMutableNode_AddsNode()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        GumTreeNode child = new GumTreeNode("Child");

        parent.AddChild((ITreeNodeMutable)child);

        parent.Nodes.ShouldContain(child);
    }

    [Fact]
    public void AddChild_String_AddsChildAndReturnsItAsMutableNode()
    {
        GumTreeNode parent = new GumTreeNode("Parent");

        ITreeNodeMutable child = parent.AddChild("Child");

        parent.Nodes.Count.ShouldBe(1);
        parent.Nodes[0].ShouldBeSameAs(child);
        child.Text.ShouldBe("Child");
    }

    [Fact]
    public void ChildCount_ReturnsNumberOfDirectChildren()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        parent.AddChild("First");
        parent.AddChild("Second");

        ((ITreeNodeMutable)parent).ChildCount.ShouldBe(2);
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
    public void ClearChildren_RemovesAllNodes()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        parent.AddChild("First");
        parent.AddChild("Second");

        parent.ClearChildren();

        parent.Nodes.Count.ShouldBe(0);
    }

    [Fact]
    public void GetChildAt_ReturnsChildAtIndex()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable first = parent.AddChild("First");
        ITreeNodeMutable second = parent.AddChild("Second");

        ((ITreeNodeMutable)parent).GetChildAt(0).ShouldBeSameAs(first);
        ((ITreeNodeMutable)parent).GetChildAt(1).ShouldBeSameAs(second);
    }

    [Fact]
    public void ImageIndex_SetThroughInterface_ReadableFromNodeProperty()
    {
        GumTreeNode node = new GumTreeNode("Node");
        ITreeNodeMutable asInterface = node;

        asInterface.ImageIndex = 4;

        node.ImageIndex.ShouldBe(4);
    }

    [Fact]
    public void IndexOfChild_ChildNotPresent_ReturnsNegativeOne()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        GumTreeNode notAChild = new GumTreeNode("NotAChild");

        ((ITreeNodeMutable)parent).IndexOfChild(notAChild).ShouldBe(-1);
    }

    [Fact]
    public void IndexOfChild_ReturnsPositionOfChild()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        parent.AddChild("First");
        ITreeNodeMutable second = parent.AddChild("Second");

        ((ITreeNodeMutable)parent).IndexOfChild(second).ShouldBe(1);
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
    public void NextVisibleNode_CollapsedNodeWithChildren_ReturnsNextSibling()
    {
        GumTreeNode root = new GumTreeNode("Root");
        GumTreeNode collapsed = new GumTreeNode("Collapsed");
        GumTreeNode hiddenChild = new GumTreeNode("HiddenChild");
        GumTreeNode sibling = new GumTreeNode("Sibling");
        root.Nodes.Add(collapsed);
        collapsed.Nodes.Add(hiddenChild);
        root.Nodes.Add(sibling);
        collapsed.IsExpanded = false;

        collapsed.NextVisibleNode.ShouldBeSameAs(sibling);
    }

    [Fact]
    public void NextVisibleNode_ExpandedNodeWithChildren_ReturnsFirstChild()
    {
        GumTreeNode root = new GumTreeNode("Root");
        GumTreeNode expanded = new GumTreeNode("Expanded");
        GumTreeNode firstChild = new GumTreeNode("FirstChild");
        GumTreeNode sibling = new GumTreeNode("Sibling");
        root.Nodes.Add(expanded);
        expanded.Nodes.Add(firstChild);
        root.Nodes.Add(sibling);
        expanded.IsExpanded = true;

        expanded.NextVisibleNode.ShouldBeSameAs(firstChild);
    }

    [Fact]
    public void NextVisibleNode_LastChildOfSubtree_ReturnsParentsNextSibling()
    {
        GumTreeNode root = new GumTreeNode("Root");
        GumTreeNode branch = new GumTreeNode("Branch");
        GumTreeNode lastChild = new GumTreeNode("LastChild");
        GumTreeNode uncle = new GumTreeNode("Uncle");
        root.Nodes.Add(branch);
        branch.Nodes.Add(lastChild);
        root.Nodes.Add(uncle);
        branch.IsExpanded = true;

        lastChild.NextVisibleNode.ShouldBeSameAs(uncle);
    }

    [Fact]
    public void Nodes_InsertNodeAlreadyInSameCollection_Throws()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        GumTreeNode child = new GumTreeNode("Child");
        parent.Nodes.Add(child);

        Should.Throw<ArgumentException>(() => parent.Nodes.Insert(0, child));
    }

    [Fact]
    public void Parent_AfterAddingToDifferentParent_ReturnsNewParent()
    {
        GumTreeNode originalParent = new GumTreeNode("OriginalParent");
        GumTreeNode newParent = new GumTreeNode("NewParent");
        GumTreeNode child = new GumTreeNode("Child");
        originalParent.Nodes.Add(child);

        newParent.Nodes.Add(child);

        child.Parent.ShouldBeSameAs(newParent);
        originalParent.Nodes.ShouldBeEmpty();
    }

    [Fact]
    public void Parent_AfterRemoveChild_ReturnsNull()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        GumTreeNode child = new GumTreeNode("Child");
        parent.AddChild((ITreeNodeMutable)child);

        parent.RemoveChild(child);

        child.Parent.ShouldBeNull();
    }

    [Fact]
    public void Parent_AsMutableInterface_ChildOfGumTreeNode_ReturnsSameParentInstance()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable child = parent.AddChild("Child");

        child.Parent.ShouldBeSameAs(parent);
    }

    [Fact]
    public void Parent_AsMutableInterface_RootNode_ReturnsNull()
    {
        GumTreeNode root = new GumTreeNode("Root");

        ((ITreeNodeMutable)root).Parent.ShouldBeNull();
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
    public void PrevVisibleNode_PreviousSiblingCollapsed_ReturnsSiblingNotItsChild()
    {
        GumTreeNode root = new GumTreeNode("Root");
        GumTreeNode collapsed = new GumTreeNode("Collapsed");
        GumTreeNode hiddenChild = new GumTreeNode("HiddenChild");
        GumTreeNode target = new GumTreeNode("Target");
        root.Nodes.Add(collapsed);
        collapsed.Nodes.Add(hiddenChild);
        root.Nodes.Add(target);
        collapsed.IsExpanded = false;

        target.PrevVisibleNode.ShouldBeSameAs(collapsed);
    }

    [Fact]
    public void PrevVisibleNode_PreviousSiblingExpanded_ReturnsDeepestVisibleDescendant()
    {
        GumTreeNode root = new GumTreeNode("Root");
        GumTreeNode expanded = new GumTreeNode("Expanded");
        GumTreeNode child = new GumTreeNode("Child");
        GumTreeNode grandChild = new GumTreeNode("GrandChild");
        GumTreeNode target = new GumTreeNode("Target");
        root.Nodes.Add(expanded);
        expanded.Nodes.Add(child);
        child.Nodes.Add(grandChild);
        root.Nodes.Add(target);
        expanded.IsExpanded = true;
        child.IsExpanded = true;

        target.PrevVisibleNode.ShouldBeSameAs(grandChild);
    }

    [Fact]
    public void RemoveChild_RemovesNode()
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
    public void RemoveSelf_NodeWithNoParent_DoesNotThrow()
    {
        GumTreeNode node = new GumTreeNode("Node");

        Should.NotThrow(() => ((ITreeNodeMutable)node).RemoveSelf());
    }

    [Fact]
    public void RemoveSelf_RemovesNodeFromParent()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable child = parent.AddChild("Child");

        child.RemoveSelf();

        parent.Nodes.Count.ShouldBe(0);
    }

    [Fact]
    public void RemoveSelf_RootCollectionMember_RemovesFromRootCollection()
    {
        GumTreeNodeCollection roots = new GumTreeNodeCollection();
        GumTreeNode root = new GumTreeNode("Root");
        roots.Add(root);

        root.RemoveSelf();

        roots.ShouldBeEmpty();
        root.Parent.ShouldBeNull();
    }

    [Fact]
    public void SetTag_UpdatesTagAndInterfaceTag()
    {
        GumTreeNode node = new GumTreeNode("Node");
        object tag = new object();

        node.SetTag(tag);

        node.Tag.ShouldBeSameAs(tag);
        ((ITreeNode)node).Tag.ShouldBeSameAs(tag);
    }
}
