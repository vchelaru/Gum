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

    private sealed class FakeElementTreeRoots : IElementTreeRoots
    {
        public ITreeNode? Screens { get; set; }
        public ITreeNode? Components { get; set; }
        public ITreeNode? StandardElements { get; set; }
        public ITreeNode? Behaviors { get; set; }
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
    public void GetTreeNodeFor_AbsoluteDirectoryOutsideKnownCategories_ReturnsNull()
    {
        FakeTreeNode screensRoot = new FakeTreeNode { Text = "Screens" };
        FakeElementTreeRoots roots = new FakeElementTreeRoots { Screens = screensRoot };

        roots.GetTreeNodeFor("C:/Project/Other/Sub", "C:/Project/").ShouldBeNull();
    }

    [Fact]
    public void GetTreeNodeFor_AbsoluteDirectoryUnderNestedScreensSubfolder_ReturnsNestedNode()
    {
        FakeTreeNode screensRoot = new FakeTreeNode { Text = "Screens" };
        FakeTreeNode a = screensRoot.AddChild(new FakeTreeNode { Text = "A" });
        FakeTreeNode b = a.AddChild(new FakeTreeNode { Text = "B" });
        FakeElementTreeRoots roots = new FakeElementTreeRoots { Screens = screensRoot };

        roots.GetTreeNodeFor("C:/Project/Screens/A/B", "C:/Project/").ShouldBe(b);
    }

    [Fact]
    public void GetTreeNodeFor_BehaviorSave_SelectsBehaviorsRoot()
    {
        BehaviorSave tag = new BehaviorSave { Name = "Behavior" };
        FakeTreeNode behaviorsRoot = new FakeTreeNode { Text = "Behaviors" };
        FakeTreeNode behaviorNode = behaviorsRoot.AddChild(new FakeTreeNode { Tag = tag });
        FakeElementTreeRoots roots = new FakeElementTreeRoots { Behaviors = behaviorsRoot };

        roots.GetTreeNodeFor(tag).ShouldBe(behaviorNode);
    }

    [Fact]
    public void GetTreeNodeFor_BehaviorsRootNotYetCreated_ReturnsNull()
    {
        FakeElementTreeRoots roots = new FakeElementTreeRoots();

        roots.GetTreeNodeFor("C:/Project/Behaviors/A", "C:/Project/").ShouldBeNull();
    }

    [Fact]
    public void GetTreeNodeFor_ComponentSave_SelectsComponentsRoot()
    {
        ComponentSave tag = new ComponentSave { Name = "Comp" };
        FakeTreeNode componentsRoot = new FakeTreeNode { Text = "Components" };
        FakeTreeNode componentNode = componentsRoot.AddChild(new FakeTreeNode { Tag = tag });
        FakeElementTreeRoots roots = new FakeElementTreeRoots { Components = componentsRoot };

        roots.GetTreeNodeFor(tag).ShouldBe(componentNode);
    }

    [Fact]
    public void GetTreeNodeFor_ElementSaveComponentSave_SelectsComponentsRootNotScreens()
    {
        ComponentSave tag = new ComponentSave { Name = "Comp" };
        FakeTreeNode screensRoot = new FakeTreeNode { Text = "Screens" };
        FakeTreeNode componentsRoot = new FakeTreeNode { Text = "Components" };
        FakeTreeNode componentNode = componentsRoot.AddChild(new FakeTreeNode { Tag = tag });
        FakeElementTreeRoots roots = new FakeElementTreeRoots { Screens = screensRoot, Components = componentsRoot };
        ElementSave elementSave = tag;

        roots.GetTreeNodeFor(elementSave).ShouldBe(componentNode);
    }

    [Fact]
    public void GetTreeNodeFor_ElementSaveNull_ReturnsNull()
    {
        FakeElementTreeRoots roots = new FakeElementTreeRoots();

        roots.GetTreeNodeFor((ElementSave?)null).ShouldBeNull();
    }

    [Fact]
    public void GetTreeNodeFor_EmptyRelativeDirectory_ReturnsContainerItself()
    {
        FakeTreeNode container = new FakeTreeNode { Text = "Screens" };

        container.GetTreeNodeFor("").ShouldBe(container);
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
    public void GetTreeNodeFor_NoMatchingSegment_ReturnsNull()
    {
        FakeTreeNode container = new FakeTreeNode { Text = "Screens" };
        container.AddChild(new FakeTreeNode { Text = "A" });

        container.GetTreeNodeFor("missing").ShouldBeNull();
    }

    [Fact]
    public void GetTreeNodeFor_ScreenSave_SelectsScreensRoot()
    {
        ScreenSave tag = new ScreenSave { Name = "Screen" };
        FakeTreeNode screensRoot = new FakeTreeNode { Text = "Screens" };
        FakeTreeNode screenNode = screensRoot.AddChild(new FakeTreeNode { Tag = tag });
        FakeElementTreeRoots roots = new FakeElementTreeRoots { Screens = screensRoot };

        roots.GetTreeNodeFor(tag).ShouldBe(screenNode);
    }

    [Fact]
    public void GetTreeNodeFor_SegmentCaseInsensitive_ReturnsMatch()
    {
        FakeTreeNode container = new FakeTreeNode { Text = "Screens" };
        FakeTreeNode child = container.AddChild(new FakeTreeNode { Text = "SubFolder" });

        container.GetTreeNodeFor("subfolder").ShouldBe(child);
    }

    [Fact]
    public void GetTreeNodeFor_StandardElementSave_SelectsStandardElementsRoot()
    {
        StandardElementSave tag = new StandardElementSave { Name = "Sprite" };
        FakeTreeNode standardRoot = new FakeTreeNode { Text = "Standard" };
        FakeTreeNode standardNode = standardRoot.AddChild(new FakeTreeNode { Tag = tag });
        FakeElementTreeRoots roots = new FakeElementTreeRoots { StandardElements = standardRoot };

        roots.GetTreeNodeFor(tag).ShouldBe(standardNode);
    }

    [Fact]
    public void GetTreeNodeForTag_ExplicitContainer_OverridesTagTypeRootInference()
    {
        ComponentSave tag = new ComponentSave { Name = "Comp" };
        FakeTreeNode componentsRoot = new FakeTreeNode { Text = "Components" };
        FakeTreeNode explicitContainer = new FakeTreeNode { Text = "Sub" };
        FakeTreeNode explicitNode = explicitContainer.AddChild(new FakeTreeNode { Tag = tag });
        FakeElementTreeRoots roots = new FakeElementTreeRoots { Components = componentsRoot };

        roots.GetTreeNodeForTag(tag, explicitContainer).ShouldBe(explicitNode);
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
    public void GetTreeNodeForTag_NullContainerAndRootNotYetCreated_ReturnsNull()
    {
        FakeElementTreeRoots roots = new FakeElementTreeRoots();

        roots.GetTreeNodeForTag(new BehaviorSave { Name = "Behavior" }).ShouldBeNull();
    }

    [Fact]
    public void GetTreeNodeForTag_NullContainerUnrecognizedTagType_ReturnsNull()
    {
        FakeTreeNode behaviorsRoot = new FakeTreeNode { Text = "Behaviors" };
        FakeElementTreeRoots roots = new FakeElementTreeRoots { Behaviors = behaviorsRoot };

        roots.GetTreeNodeForTag(new InstanceSave { Name = "Instance" }).ShouldBeNull();
    }

    [Fact]
    public void GetTreeNodeForTag_NullContainerWithBehaviorTag_SelectsBehaviorsRoot()
    {
        BehaviorSave tag = new BehaviorSave { Name = "Behavior" };
        FakeTreeNode behaviorsRoot = new FakeTreeNode { Text = "Behaviors" };
        FakeTreeNode behaviorNode = behaviorsRoot.AddChild(new FakeTreeNode { Tag = tag });
        FakeElementTreeRoots roots = new FakeElementTreeRoots { Behaviors = behaviorsRoot };

        roots.GetTreeNodeForTag(tag).ShouldBe(behaviorNode);
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
