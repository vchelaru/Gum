using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Shouldly;
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
        public object? Tag { get; set; }
        public string Text { get; set; } = "";
        public string FullPath { get; set; } = "";
        public ITreeNode? Parent { get; set; }
        public FilePath GetFullFilePath() => new FilePath(FullPath);
        public void Expand() { }

        public FakeTreeNode AddChild(FakeTreeNode child)
        {
            child.Parent = this;
            return child;
        }
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
