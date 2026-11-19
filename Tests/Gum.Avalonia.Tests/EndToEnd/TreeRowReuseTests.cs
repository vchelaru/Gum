using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.TreeView;
using Gum.DataTypes;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// A far scroll re-points the rows the tree already has at their new nodes instead of building a fresh row
/// for every visible node, which is most of what a jump to the end of a long list costs.
/// </summary>
public class TreeRowReuseTests
{
    [AvaloniaFact]
    public void ScrollingFarAcrossALongList_ReusesTheRowsItAlreadyHas()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave list = tree.Project.AddComponent("List");
        for (int i = 0; i < 200; i++)
        {
            tree.Project.AddInstance(list, "Item" + i, "Rectangle");
        }
        tree.SaveAll();
        InstanceSave first = list.Instances.First();
        InstanceSave last = list.Instances.Last();
        tree.Click(tree.NodeFor(first));
        List<TreeRowView> before = RealizedRows(tree);
        before.Count.ShouldBeGreaterThan(5);

        tree.Click(tree.NodeFor(last));

        tree.IsScrolledIntoView(tree.NodeFor(last)).ShouldBeTrue("the last row is in view");
        RealizedRows(tree).Intersect(before).ShouldNotBeEmpty("rows scrolled out of view are reused for the rows scrolled in");
    }

    private static List<TreeRowView> RealizedRows(ProjectTreeHarness tree) =>
        tree.View.Tree.GetVisualDescendants().OfType<TreeRowView>().ToList();
}
