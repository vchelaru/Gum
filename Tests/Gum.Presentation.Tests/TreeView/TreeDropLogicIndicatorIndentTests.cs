using Gum.Controls;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Issue #4913: the drop indicator line's left margin should show whether a node will land as a
/// sibling of the hovered row (same indent as that row) or as its first child (one level deeper),
/// instead of always spanning the tree's full width. A drag also highlights the row that will
/// become the dropped node's new parent, so a Before/After/IntoFirst insert doesn't leave the
/// parent to be inferred from the line's position alone.
/// </summary>
public class TreeDropLogicIndicatorIndentTests
{
    [Fact]
    public void GetIndicatorIndent_BeforeOrAfter_MatchesTargetsOwnLevel()
    {
        GumTreeNode root = new GumTreeNode("Root");
        GumTreeNode child = new GumTreeNode("Child");
        root.Nodes.Add(child);
        GumTreeNode grandchild = new GumTreeNode("Grandchild");
        child.Nodes.Add(grandchild);

        TreeDropLogic.GetIndicatorIndent(grandchild, TreeDropKind.Before, 16).ShouldBe(32);
        TreeDropLogic.GetIndicatorIndent(grandchild, TreeDropKind.After, 16).ShouldBe(32);
    }

    [Fact]
    public void GetIndicatorIndent_IntoFirst_IsOneLevelDeeperThanTarget()
    {
        GumTreeNode root = new GumTreeNode("Root");
        GumTreeNode child = new GumTreeNode("Child");
        root.Nodes.Add(child);

        TreeDropLogic.GetIndicatorIndent(child, TreeDropKind.IntoFirst, 16).ShouldBe(32);
    }

    [Fact]
    public void GetIndicatorIndent_Into_MatchesTargetsOwnLevel()
    {
        GumTreeNode root = new GumTreeNode("Root");
        GumTreeNode child = new GumTreeNode("Child");
        root.Nodes.Add(child);

        TreeDropLogic.GetIndicatorIndent(child, TreeDropKind.Into, 16).ShouldBe(16);
    }

    [Fact]
    public void GetParentHighlightNode_BeforeOrAfter_IsTargetsParent()
    {
        GumTreeNode root = new GumTreeNode("Root");
        GumTreeNode child = new GumTreeNode("Child");
        root.Nodes.Add(child);

        TreeDropLogic.GetParentHighlightNode(child, TreeDropKind.Before).ShouldBeSameAs(root);
        TreeDropLogic.GetParentHighlightNode(child, TreeDropKind.After).ShouldBeSameAs(root);
    }

    [Fact]
    public void GetParentHighlightNode_BeforeOrAfter_OnARootNode_IsNull()
    {
        GumTreeNode root = new GumTreeNode("Root");

        TreeDropLogic.GetParentHighlightNode(root, TreeDropKind.Before).ShouldBeNull();
    }

    [Fact]
    public void GetParentHighlightNode_IntoFirst_IsTheTargetItself()
    {
        GumTreeNode target = new GumTreeNode("Target");

        TreeDropLogic.GetParentHighlightNode(target, TreeDropKind.IntoFirst).ShouldBeSameAs(target);
    }

    [Fact]
    public void GetParentHighlightNode_Into_IsNull()
    {
        // Into already draws its own rectangle around the target row - a second highlight on the
        // same row would be redundant.
        GumTreeNode target = new GumTreeNode("Target");

        TreeDropLogic.GetParentHighlightNode(target, TreeDropKind.Into).ShouldBeNull();
    }
}
