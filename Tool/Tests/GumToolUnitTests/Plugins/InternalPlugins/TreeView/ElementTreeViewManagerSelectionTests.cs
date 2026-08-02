using Gum.DataTypes;
using Gum.Managers;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.TreeView;

public class ElementTreeViewManagerSelectionTests : BaseTestClass
{
    [Fact]
    public void GetReselectableNodes_AllInstancesResolve_ReturnsAllInOrder()
    {
        // Issue #2954: a tree refresh (e.g. toggling HasDropshadow on a
        // multi-selection) must re-select every previously-selected instance,
        // not collapse to the first one.
        InstanceSave first = new InstanceSave { Name = "First" };
        InstanceSave second = new InstanceSave { Name = "Second" };
        ITreeNode firstNode = new GumTreeNode { Tag = first };
        ITreeNode secondNode = new GumTreeNode { Tag = second };

        Dictionary<InstanceSave, ITreeNode> lookup = new()
        {
            { first, firstNode },
            { second, secondNode },
        };

        List<ITreeNode> result = ElementTreeViewManager.GetReselectableNodes(
            new List<InstanceSave> { first, second },
            instance => lookup.TryGetValue(instance, out ITreeNode? node) ? node : null);

        result.ShouldBe(new[] { firstNode, secondNode });
    }

    [Fact]
    public void GetReselectableNodes_SomeInstancesMissing_FiltersNullsPreservingOrder()
    {
        InstanceSave first = new InstanceSave { Name = "First" };
        InstanceSave deleted = new InstanceSave { Name = "Deleted" };
        InstanceSave third = new InstanceSave { Name = "Third" };
        ITreeNode firstNode = new GumTreeNode { Tag = first };
        ITreeNode thirdNode = new GumTreeNode { Tag = third };

        Dictionary<InstanceSave, ITreeNode> lookup = new()
        {
            { first, firstNode },
            { third, thirdNode },
        };

        List<ITreeNode> result = ElementTreeViewManager.GetReselectableNodes(
            new List<InstanceSave> { first, deleted, third },
            instance => lookup.TryGetValue(instance, out ITreeNode? node) ? node : null);

        result.ShouldBe(new[] { firstNode, thirdNode });
    }

    [Fact]
    public void GetReselectableNodes_EmptyInput_ReturnsEmpty()
    {
        List<ITreeNode> result = ElementTreeViewManager.GetReselectableNodes(
            new List<InstanceSave>(),
            _ => null);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void ResolveNodeToSelect_NodeAttachedToTree_ReturnsSameNode()
    {
        GumTreeNodeCollection rootNodes = new GumTreeNodeCollection();
        GumTreeNode attached = new GumTreeNode { Tag = new InstanceSave() };
        rootNodes.Add(attached);

        GumTreeNode? result = ElementTreeViewManager.ResolveNodeToSelect(attached, rootNodes);

        result.ShouldBeSameAs(attached);
    }

    [Fact]
    public void ResolveNodeToSelect_NodeDetachedFromTree_ReturnsNull()
    {
        // Issue #4267: editing a standard's defaults (via the Standards palette) left the
        // previously-selected instance's node highlighted in the tree. The standard element's
        // node isn't attached to the tree (the palette is a separate control), and selection
        // used to bail out entirely on a detached node instead of clearing the old selection.
        GumTreeNodeCollection rootNodes = new GumTreeNodeCollection();
        GumTreeNode detached = new GumTreeNode { Tag = new StandardElementSave() };

        GumTreeNode? result = ElementTreeViewManager.ResolveNodeToSelect(detached, rootNodes);

        result.ShouldBeNull();
    }

    [Fact]
    public void ResolveNodeToSelect_NodeNestedUnderAttachedRoot_ReturnsSameNode()
    {
        GumTreeNodeCollection rootNodes = new GumTreeNodeCollection();
        GumTreeNode root = new GumTreeNode { Tag = new InstanceSave { Name = "Root" } };
        rootNodes.Add(root);
        GumTreeNode child = new GumTreeNode { Tag = new InstanceSave { Name = "Child" } };
        root.AddChild(child);

        GumTreeNode? result = ElementTreeViewManager.ResolveNodeToSelect(child, rootNodes);

        result.ShouldBeSameAs(child);
    }

    [Fact]
    public void ResolveNodeToSelect_NullNode_ReturnsNull()
    {
        GumTreeNodeCollection rootNodes = new GumTreeNodeCollection();

        GumTreeNode? result = ElementTreeViewManager.ResolveNodeToSelect(null, rootNodes);

        result.ShouldBeNull();
    }
}
