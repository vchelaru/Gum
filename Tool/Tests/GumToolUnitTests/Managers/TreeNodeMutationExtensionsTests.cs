using Gum.DataTypes;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Managers;

// Pins ElementTreeViewManager's ITreeNodeMutable-based reorder/sort/removal helpers (MoveToIndex,
// SortByName, RemoveRecursivelyIfStale), which used to operate directly on WinForms
// TreeNodeCollection. These previously had no test coverage at all - the algorithms are unchanged,
// only their indexing surface moved from TreeNodeCollection to ITreeNodeMutable, so these tests pin
// the translated behavior via real GumTreeNode trees (no ETVM construction required).
public class TreeNodeMutationExtensionsTests
{
    [Fact]
    public void MoveToIndex_NodeAlreadyAtIndex_LeavesOrderUnchanged()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable a = parent.AddChild("A");
        ITreeNodeMutable b = parent.AddChild("B");

        a.MoveToIndex(0);

        parent.Nodes[0].ShouldBeSameAs(a);
        parent.Nodes[1].ShouldBeSameAs(b);
    }

    [Fact]
    public void MoveToIndex_NodeHasNoParent_DoesNotThrow()
    {
        GumTreeNode orphan = new GumTreeNode("Orphan");

        Should.NotThrow(() => ((ITreeNodeMutable)orphan).MoveToIndex(0));
    }

    [Fact]
    public void MoveToIndex_NodeOutOfPosition_MovesToDesiredIndex()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable a = parent.AddChild("A");
        ITreeNodeMutable b = parent.AddChild("B");
        ITreeNodeMutable c = parent.AddChild("C");

        c.MoveToIndex(0);

        parent.Nodes[0].ShouldBeSameAs(c);
        parent.Nodes[1].ShouldBeSameAs(a);
        parent.Nodes[2].ShouldBeSameAs(b);
    }

    [Fact]
    public void RemoveRecursivelyIfStale_NestedFolders_RemovesDeeplyNestedStaleDescendant()
    {
        GumTreeNode root = new GumTreeNode("Root");
        GumTreeNode outerFolder = (GumTreeNode)root.AddChild("Outer");
        GumTreeNode innerFolder = (GumTreeNode)outerFolder.AddChild("Inner");
        GumTreeNode staleScreen = (GumTreeNode)innerFolder.AddChild("StaleScreen");
        staleScreen.Tag = new ScreenSave();

        ((ITreeNodeMutable)root).RemoveRecursivelyIfStale<ScreenSave>(_ => false);

        outerFolder.Nodes.Contains(innerFolder).ShouldBeTrue();
        innerFolder.Nodes.Contains(staleScreen).ShouldBeFalse();
    }

    [Fact]
    public void RemoveRecursivelyIfStale_TaggedChildFailsShouldKeep_RemovesChild()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        GumTreeNode staleScreen = (GumTreeNode)parent.AddChild("StaleScreen");
        staleScreen.Tag = new ScreenSave();

        ((ITreeNodeMutable)parent).RemoveRecursivelyIfStale<ScreenSave>(_ => false);

        parent.Nodes.Contains(staleScreen).ShouldBeFalse();
    }

    [Fact]
    public void RemoveRecursivelyIfStale_TaggedChildPassesShouldKeep_KeepsChild()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        GumTreeNode liveScreen = (GumTreeNode)parent.AddChild("LiveScreen");
        liveScreen.Tag = new ScreenSave();

        ((ITreeNodeMutable)parent).RemoveRecursivelyIfStale<ScreenSave>(_ => true);

        parent.Nodes.Contains(liveScreen).ShouldBeTrue();
    }

    [Fact]
    public void RemoveRecursivelyIfStale_UntaggedFolderChild_IsNeverRemovedItself()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        GumTreeNode folder = (GumTreeNode)parent.AddChild("Folder");
        GumTreeNode staleScreen = (GumTreeNode)folder.AddChild("StaleScreen");
        staleScreen.Tag = new ScreenSave();

        ((ITreeNodeMutable)parent).RemoveRecursivelyIfStale<ScreenSave>(_ => false);

        parent.Nodes.Contains(folder).ShouldBeTrue();
        folder.Nodes.Contains(staleScreen).ShouldBeFalse();
    }

    [Fact]
    public void SortByName_AlreadySorted_LeavesOrderUnchanged()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable alpha = parent.AddChild("Alpha");
        ITreeNodeMutable bravo = parent.AddChild("Bravo");
        ITreeNodeMutable charlie = parent.AddChild("Charlie");

        ((ITreeNodeMutable)parent).SortByName();

        parent.Nodes[0].ShouldBeSameAs(alpha);
        parent.Nodes[1].ShouldBeSameAs(bravo);
        parent.Nodes[2].ShouldBeSameAs(charlie);
    }

    [Fact]
    public void SortByName_FoldersComeBeforeFilesRegardlessOfName()
    {
        GumTreeNode componentsRoot = new GumTreeNode("Components");
        GumTreeNode subfolder = (GumTreeNode)componentsRoot.AddChild("ZFolder");
        GumTreeNode fileNode = (GumTreeNode)componentsRoot.AddChild("AFile");
        fileNode.Tag = new ComponentSave();

        ((ITreeNodeMutable)componentsRoot).SortByName();

        componentsRoot.Nodes[0].ShouldBeSameAs(subfolder);
        componentsRoot.Nodes[1].ShouldBeSameAs(fileNode);
    }

    [Fact]
    public void SortByName_LastNodeBelongsAtStart_MovesAllTheWayToFront()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable bravo = parent.AddChild("Bravo");
        ITreeNodeMutable charlie = parent.AddChild("Charlie");
        ITreeNodeMutable alpha = parent.AddChild("Alpha");

        ((ITreeNodeMutable)parent).SortByName();

        parent.Nodes[0].ShouldBeSameAs(alpha);
        parent.Nodes[1].ShouldBeSameAs(bravo);
        parent.Nodes[2].ShouldBeSameAs(charlie);
    }

    [Fact]
    public void SortByName_LastNodeBelongsInMiddle_MovesToMiddle()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable alpha = parent.AddChild("Alpha");
        ITreeNodeMutable bravo = parent.AddChild("Bravo");
        ITreeNodeMutable delta = parent.AddChild("Delta");
        ITreeNodeMutable charlie = parent.AddChild("Charlie");

        ((ITreeNodeMutable)parent).SortByName();

        parent.Nodes[0].ShouldBeSameAs(alpha);
        parent.Nodes[1].ShouldBeSameAs(bravo);
        parent.Nodes[2].ShouldBeSameAs(charlie);
        parent.Nodes[3].ShouldBeSameAs(delta);
    }

    [Fact]
    public void SortByName_Recursive_DoesNotSortChildrenOfElementNode()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        GumTreeNode componentChild = (GumTreeNode)parent.AddChild("Comp");
        componentChild.Tag = new ComponentSave();
        componentChild.AddChild("Zed");
        componentChild.AddChild("Alpha");

        ((ITreeNodeMutable)parent).SortByName(recursive: true);

        componentChild.Nodes[0].Text.ShouldBe("Zed");
        componentChild.Nodes[1].Text.ShouldBe("Alpha");
    }

    [Fact]
    public void SortByName_Recursive_SortsChildrenOfNonElementNodes()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        GumTreeNode folderChild = (GumTreeNode)parent.AddChild("Folder");
        folderChild.AddChild("Zed");
        folderChild.AddChild("Alpha");

        ((ITreeNodeMutable)parent).SortByName(recursive: true);

        folderChild.Nodes[0].Text.ShouldBe("Alpha");
        folderChild.Nodes[1].Text.ShouldBe("Zed");
    }

    [Fact]
    public void SortByName_TwoNodesOutOfOrder_Swaps()
    {
        GumTreeNode parent = new GumTreeNode("Parent");
        ITreeNodeMutable bravo = parent.AddChild("Bravo");
        ITreeNodeMutable alpha = parent.AddChild("Alpha");

        ((ITreeNodeMutable)parent).SortByName();

        parent.Nodes[0].ShouldBeSameAs(alpha);
        parent.Nodes[1].ShouldBeSameAs(bravo);
    }
}
