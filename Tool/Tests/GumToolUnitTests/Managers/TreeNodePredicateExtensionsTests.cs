using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Shouldly;
using System.Collections.Generic;
using System.Windows.Forms;
using ToolsUtilities;
using Xunit;

namespace GumToolUnitTests.Managers;

// Tests for the headless ITreeNode container/folder predicates (Gum.Presentation). The fake exercises
// the branch logic without the WinForms tree; the TreeNodeWrapper pins confirm the real live-tree
// nodes (which are always TreeNodeWrapper) still classify identically after the WinForms-unwrap stubs
// were replaced with true ITreeNode implementations.
public class TreeNodePredicateExtensionsTests
{
    private sealed class FakeTreeNode : ITreeNode
    {
        private readonly List<ITreeNode> _children = new();
        public object? Tag { get; set; }
        public string Text { get; set; } = "";
        public string FullPath { get; set; } = "";
        public ITreeNode? Parent { get; set; }
        public IEnumerable<ITreeNode> Children => _children;
        public FilePath GetFullFilePath() => new FilePath(FullPath);
        public void Expand() { }

        public FakeTreeNode AddChild(FakeTreeNode child)
        {
            child.Parent = this;
            _children.Add(child);
            return child;
        }
    }

    [Fact]
    public void Equals_DifferentUnderlyingNode_ReturnsFalse()
    {
        TreeNodeWrapper first = new TreeNodeWrapper(new TreeNode("A"));
        TreeNodeWrapper second = new TreeNodeWrapper(new TreeNode("A"));

        first.Equals(second).ShouldBeFalse();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        TreeNodeWrapper wrapper = new TreeNodeWrapper(new TreeNode("A"));

        wrapper.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_SameUnderlyingNode_ReturnsTrueAndSharesHashCode()
    {
        TreeNode node = new TreeNode("A");
        TreeNodeWrapper first = new TreeNodeWrapper(node);
        TreeNodeWrapper second = new TreeNodeWrapper(node);

        first.Equals(second).ShouldBeTrue();
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void GetAllChildrenNodesRecursively_LeafNode_ReturnsEmpty()
    {
        FakeTreeNode leaf = new FakeTreeNode { Text = "Leaf" };

        leaf.GetAllChildrenNodesRecursively().ShouldBeEmpty();
    }

    [Fact]
    public void GetAllChildrenNodesRecursively_MultiLevelTree_ReturnsDescendantsDepthFirst()
    {
        FakeTreeNode root = new FakeTreeNode { Text = "Root" };
        FakeTreeNode a = root.AddChild(new FakeTreeNode { Text = "A" });
        FakeTreeNode a1 = a.AddChild(new FakeTreeNode { Text = "A1" });
        FakeTreeNode b = root.AddChild(new FakeTreeNode { Text = "B" });

        List<ITreeNode> descendants = root.GetAllChildrenNodesRecursively();

        descendants.ShouldBe(new ITreeNode[] { a, a1, b });
    }

    [Fact]
    public void GetInstanceTreeNodeByName_MatchingInstanceNameNested_ReturnsNode()
    {
        FakeTreeNode container = new FakeTreeNode { Text = "Element" };
        FakeTreeNode parentInstance = container.AddChild(new FakeTreeNode { Tag = new InstanceSave { Name = "Parent" } });
        FakeTreeNode childInstance = parentInstance.AddChild(new FakeTreeNode { Tag = new InstanceSave { Name = "Child" } });

        container.GetInstanceTreeNodeByName("Child").ShouldBe(childInstance);
    }

    [Fact]
    public void GetInstanceTreeNodeByName_NonInstanceTagWithMatchingName_ReturnsNull()
    {
        FakeTreeNode container = new FakeTreeNode { Text = "Element" };
        container.AddChild(new FakeTreeNode { Tag = new ScreenSave { Name = "Target" } });

        container.GetInstanceTreeNodeByName("Target").ShouldBeNull();
    }

    [Fact]
    public void GetTreeNodeFor_InstanceSaveByReference_ReturnsNodeNotSameNamedSibling()
    {
        InstanceSave target = new InstanceSave { Name = "Duplicate" };
        InstanceSave sameNameOther = new InstanceSave { Name = "Duplicate" };
        FakeTreeNode container = new FakeTreeNode { Text = "Element" };
        container.AddChild(new FakeTreeNode { Tag = sameNameOther });
        FakeTreeNode targetNode = container.AddChild(new FakeTreeNode { Tag = target });

        container.GetTreeNodeFor(target).ShouldBe(targetNode);
    }

    [Fact]
    public void GetTreeNodeForTag_MatchNestedInTree_ReturnsNode()
    {
        ComponentSave tag = new ComponentSave { Name = "Comp" };
        FakeTreeNode root = new FakeTreeNode { Text = "Components" };
        FakeTreeNode folder = root.AddChild(new FakeTreeNode { Text = "Sub" });
        FakeTreeNode elementNode = folder.AddChild(new FakeTreeNode { Tag = tag });

        root.GetTreeNodeForTag(tag).ShouldBe(elementNode);
    }

    [Fact]
    public void GetTreeNodeForTag_NoMatch_ReturnsNull()
    {
        FakeTreeNode root = new FakeTreeNode { Text = "Components" };
        root.AddChild(new FakeTreeNode { Tag = new ComponentSave { Name = "Other" } });

        root.GetTreeNodeForTag(new ComponentSave { Name = "Missing" }).ShouldBeNull();
    }

    [Fact]
    public void IsPartOfStandardElementsFolderStructure_ElementUnderStandardRoot_ReturnsTrue()
    {
        FakeTreeNode standardRoot = new FakeTreeNode { Text = "Standard" };
        FakeTreeNode element = standardRoot.AddChild(new FakeTreeNode { Tag = new StandardElementSave { Name = "Sprite" } });

        element.IsPartOfStandardElementsFolderStructure().ShouldBeTrue();
    }

    [Fact]
    public void IsPartOfStandardElementsFolderStructure_ScreensRoot_ReturnsFalse()
    {
        new FakeTreeNode { Text = "Screens" }.IsPartOfStandardElementsFolderStructure().ShouldBeFalse();
    }

    [Fact]
    public void IsTopBehaviorTreeNode_BehaviorsRoot_ReturnsTrue()
    {
        new FakeTreeNode { Text = "Behaviors" }.IsTopBehaviorTreeNode().ShouldBeTrue();
    }

    [Fact]
    public void IsTopBehaviorTreeNode_ChildWithSameText_ReturnsFalse()
    {
        FakeTreeNode root = new FakeTreeNode { Text = "Behaviors" };
        FakeTreeNode child = root.AddChild(new FakeTreeNode { Text = "Behaviors" });

        child.IsTopBehaviorTreeNode().ShouldBeFalse();
    }

    [Fact]
    public void IsTopBehaviorTreeNode_TreeNodeWrapperOverBehaviorsRoot_ReturnsTrue()
    {
        ITreeNode wrapper = new TreeNodeWrapper(new TreeNode("Behaviors"));

        wrapper.IsTopBehaviorTreeNode().ShouldBeTrue();
    }

    [Fact]
    public void IsTopElementContainerTreeNode_ElementNode_ReturnsFalse()
    {
        new FakeTreeNode { Tag = new ScreenSave { Name = "X" } }.IsTopElementContainerTreeNode().ShouldBeFalse();
    }

    [Fact]
    public void IsTopElementContainerTreeNode_TaglessRoot_ReturnsTrue()
    {
        new FakeTreeNode { Text = "Screens" }.IsTopElementContainerTreeNode().ShouldBeTrue();
    }

    [Fact]
    public void IsTopStandardElementTreeNode_StandardRoot_ReturnsTrue()
    {
        new FakeTreeNode { Text = "Standard" }.IsTopStandardElementTreeNode().ShouldBeTrue();
    }

    [Fact]
    public void IsTopStandardElementTreeNode_TreeNodeWrapperOverStandardRoot_ReturnsTrue()
    {
        ITreeNode wrapper = new TreeNodeWrapper(new TreeNode("Standard"));

        wrapper.IsTopStandardElementTreeNode().ShouldBeTrue();
    }

    [Fact]
    public void IsTopStandardElementTreeNode_WrongText_ReturnsFalse()
    {
        new FakeTreeNode { Text = "Screens" }.IsTopStandardElementTreeNode().ShouldBeFalse();
    }

    [Fact]
    public void IsBehaviorTreeNode_BehaviorTag_ReturnsTrue()
    {
        new FakeTreeNode { Tag = new BehaviorSave() }.IsBehaviorTreeNode().ShouldBeTrue();
    }

    [Fact]
    public void IsComponentTreeNode_ComponentTag_ReturnsTrue()
    {
        new FakeTreeNode { Tag = new ComponentSave() }.IsComponentTreeNode().ShouldBeTrue();
    }

    [Fact]
    public void IsScreenTreeNode_ComponentTag_ReturnsFalse()
    {
        new FakeTreeNode { Tag = new ComponentSave() }.IsScreenTreeNode().ShouldBeFalse();
    }

    [Fact]
    public void IsScreenTreeNode_ScreenTag_ReturnsTrue()
    {
        new FakeTreeNode { Tag = new ScreenSave() }.IsScreenTreeNode().ShouldBeTrue();
    }

    [Fact]
    public void IsStandardElementTreeNode_StandardTag_ReturnsTrue()
    {
        new FakeTreeNode { Tag = new StandardElementSave() }.IsStandardElementTreeNode().ShouldBeTrue();
    }
}
