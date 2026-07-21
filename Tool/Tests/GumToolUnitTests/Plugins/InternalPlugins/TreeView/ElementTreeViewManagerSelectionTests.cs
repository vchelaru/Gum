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
}
