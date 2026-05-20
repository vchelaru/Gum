using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.Managers;
using Shouldly;
using System.Windows.Forms;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.TreeView;

public class ElementTreeViewManagerProcessDropTests : BaseTestClass
{
    [Fact]
    public void ProcessDrop_NullTarget_ReturnsNull()
    {
        var result = ElementTreeViewManager.ProcessDrop(null, MultiSelectTreeView.DropKind.Into);

        result.ShouldBeNull();
    }

    [Fact]
    public void ProcessDrop_NoneKind_ReturnsNull()
    {
        TreeNode target = new TreeNode();

        var result = ElementTreeViewManager.ProcessDrop(target, MultiSelectTreeView.DropKind.None);

        result.ShouldBeNull();
    }

    [Fact]
    public void ProcessDrop_IntoFirstOnElementSave_AppendsSameAsInto()
    {
        // Issue #2864: the visual adornment for IntoFirst is identical to Into
        // (both draw a rectangle around the row, not a between-rows line). The
        // user cannot distinguish the two visually, so they must behave the same.
        // Insertion at index 0 of the element is still reachable via DropKind.Before
        // on the first child node, which shows an unambiguous line.
        ComponentSave component = new ComponentSave();
        component.Name = "TargetComponent";
        component.Instances.Add(new InstanceSave { Name = "Existing" });

        TreeNode target = new TreeNode { Tag = component };

        var result = ElementTreeViewManager.ProcessDrop(target, MultiSelectTreeView.DropKind.IntoFirst);

        result.ShouldNotBeNull();
        result.Value.target.ShouldBe(target);
        result.Value.index.ShouldBe(component.Instances.Count);
    }

    [Fact]
    public void ProcessDrop_IntoElementSave_ReturnsInstancesCount_SoCallerAppends()
    {
        // Issue #2864: dropping a component into a screen tree node landed at
        // index 0 because the index path returned a tree-child-derived value
        // that did not line up with the element's Instances list. The correct
        // index for an "into-this-element" drop is Instances.Count — i.e.
        // append to the end so the new visual renders on top, not behind.
        ScreenSave screen = new ScreenSave();
        screen.Name = "TargetScreen";
        screen.Instances.Add(new InstanceSave { Name = "Existing1" });
        screen.Instances.Add(new InstanceSave { Name = "Existing2" });
        screen.Instances.Add(new InstanceSave { Name = "Existing3" });

        TreeNode target = new TreeNode { Tag = screen };

        var result = ElementTreeViewManager.ProcessDrop(target, MultiSelectTreeView.DropKind.Into);

        result.ShouldNotBeNull();
        result.Value.target.ShouldBe(target);
        result.Value.index.ShouldBe(screen.Instances.Count);
    }

    [Fact]
    public void ProcessDrop_BeforeSibling_ReturnsSiblingIndexOnParent()
    {
        TreeNode parent = new TreeNode();
        TreeNode first = parent.Nodes.Add("First");
        TreeNode second = parent.Nodes.Add("Second");

        var result = ElementTreeViewManager.ProcessDrop(second, MultiSelectTreeView.DropKind.Before);

        result.ShouldNotBeNull();
        result.Value.target.ShouldBe(parent);
        result.Value.index.ShouldBe(1);
    }

    [Fact]
    public void ProcessDrop_AfterSibling_ReturnsSiblingIndexPlusOneOnParent()
    {
        TreeNode parent = new TreeNode();
        TreeNode first = parent.Nodes.Add("First");
        TreeNode second = parent.Nodes.Add("Second");

        var result = ElementTreeViewManager.ProcessDrop(first, MultiSelectTreeView.DropKind.After);

        result.ShouldNotBeNull();
        result.Value.target.ShouldBe(parent);
        result.Value.index.ShouldBe(1);
    }
}
