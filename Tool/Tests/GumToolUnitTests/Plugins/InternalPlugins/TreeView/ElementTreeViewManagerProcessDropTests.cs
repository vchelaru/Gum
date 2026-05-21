using CommonFormsAndControls;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
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
        result.Value.TreeTarget.ShouldBe(target);
        result.Value.Drop.ShouldNotBeNull();
        result.Value.Drop!.ParentElement.ShouldBe(component);
        result.Value.Drop!.ParentInstance.ShouldBeNull();
        result.Value.Drop!.Position.ShouldBeOfType<DropPosition.Append>();
    }

    [Fact]
    public void ProcessDrop_IntoElementSave_ReturnsAppendOnElement()
    {
        // Issue #2864: dropping a component into a screen tree node landed at
        // index 0 because the index path returned a tree-child-derived value
        // that did not line up with the element's Instances list. Issue #2869:
        // the typed result eliminates the int-index ambiguity entirely — the
        // consumer receives an Append on the element.
        ScreenSave screen = new ScreenSave();
        screen.Name = "TargetScreen";
        screen.Instances.Add(new InstanceSave { Name = "Existing1" });
        screen.Instances.Add(new InstanceSave { Name = "Existing2" });
        screen.Instances.Add(new InstanceSave { Name = "Existing3" });

        TreeNode target = new TreeNode { Tag = screen };

        var result = ElementTreeViewManager.ProcessDrop(target, MultiSelectTreeView.DropKind.Into);

        result.ShouldNotBeNull();
        result.Value.TreeTarget.ShouldBe(target);
        result.Value.Drop.ShouldNotBeNull();
        result.Value.Drop!.ParentElement.ShouldBe(screen);
        result.Value.Drop!.ParentInstance.ShouldBeNull();
        result.Value.Drop!.Position.ShouldBeOfType<DropPosition.Append>();
    }

    [Fact]
    public void ProcessDrop_IntoInstanceSave_ReturnsAppendWithParentInstance()
    {
        // Issue #2864 follow-up: dropping a Container onto another Container
        // (target Tag is an InstanceSave) landed the new instance in the
        // middle of MainScreen.Instances. Issue #2869: the typed Append +
        // ParentInstance carries the intent without an int-index that could
        // be reinterpreted downstream.
        ScreenSave screen = new ScreenSave();
        screen.Name = "MainScreen";
        InstanceSave leftContainer = new InstanceSave { Name = "LeftContainer", ParentContainer = screen };
        screen.Instances.Add(leftContainer);
        for (int i = 0; i < 14; i++)
        {
            screen.Instances.Add(new InstanceSave { Name = $"Other{i}", ParentContainer = screen });
        }

        TreeNode target = new TreeNode { Tag = leftContainer };
        // Simulate that LeftContainer has 4 children visible in the tree view —
        // this is what GetNodeCount(false) would have returned in the buggy path.
        target.Nodes.Add("Child1");
        target.Nodes.Add("Child2");
        target.Nodes.Add("Child3");
        target.Nodes.Add("Child4");

        var result = ElementTreeViewManager.ProcessDrop(target, MultiSelectTreeView.DropKind.Into);

        result.ShouldNotBeNull();
        result.Value.TreeTarget.ShouldBe(target);
        result.Value.Drop.ShouldNotBeNull();
        result.Value.Drop!.ParentElement.ShouldBe(screen);
        result.Value.Drop!.ParentInstance.ShouldBe(leftContainer);
        result.Value.Drop!.Position.ShouldBeOfType<DropPosition.Append>();
    }

    [Fact]
    public void ProcessDrop_BeforeInstanceSibling_ReturnsBeforeSiblingOnParent()
    {
        ScreenSave screen = new ScreenSave();
        screen.Name = "Screen";
        InstanceSave first = new InstanceSave { Name = "First", ParentContainer = screen };
        InstanceSave second = new InstanceSave { Name = "Second", ParentContainer = screen };
        screen.Instances.Add(first);
        screen.Instances.Add(second);

        TreeNode parent = new TreeNode { Tag = screen };
        TreeNode firstNode = parent.Nodes.Add("First");
        firstNode.Tag = first;
        TreeNode secondNode = parent.Nodes.Add("Second");
        secondNode.Tag = second;

        var result = ElementTreeViewManager.ProcessDrop(secondNode, MultiSelectTreeView.DropKind.Before);

        result.ShouldNotBeNull();
        result.Value.TreeTarget.ShouldBe(parent);
        result.Value.Drop.ShouldNotBeNull();
        result.Value.Drop!.ParentElement.ShouldBe(screen);
        result.Value.Drop!.ParentInstance.ShouldBeNull();
        DropPosition.BeforeSibling before = result.Value.Drop!.Position.ShouldBeOfType<DropPosition.BeforeSibling>();
        before.Sibling.ShouldBe(second);
    }

    [Fact]
    public void ProcessDrop_AfterInstanceSibling_ReturnsAfterSiblingOnParent()
    {
        ScreenSave screen = new ScreenSave();
        screen.Name = "Screen";
        InstanceSave first = new InstanceSave { Name = "First", ParentContainer = screen };
        InstanceSave second = new InstanceSave { Name = "Second", ParentContainer = screen };
        screen.Instances.Add(first);
        screen.Instances.Add(second);

        TreeNode parent = new TreeNode { Tag = screen };
        TreeNode firstNode = parent.Nodes.Add("First");
        firstNode.Tag = first;
        TreeNode secondNode = parent.Nodes.Add("Second");
        secondNode.Tag = second;

        var result = ElementTreeViewManager.ProcessDrop(firstNode, MultiSelectTreeView.DropKind.After);

        result.ShouldNotBeNull();
        result.Value.TreeTarget.ShouldBe(parent);
        result.Value.Drop.ShouldNotBeNull();
        result.Value.Drop!.ParentElement.ShouldBe(screen);
        result.Value.Drop!.ParentInstance.ShouldBeNull();
        DropPosition.AfterSibling after = result.Value.Drop!.Position.ShouldBeOfType<DropPosition.AfterSibling>();
        after.Sibling.ShouldBe(first);
    }

    [Fact]
    public void ProcessDrop_BeforeNonInstanceSibling_ReturnsNullDrop()
    {
        // Reordering element nodes (or other non-InstanceSave-tagged nodes)
        // does not feed an instances list — the downstream consumer should
        // route by tree node alone.
        TreeNode parent = new TreeNode();
        TreeNode first = parent.Nodes.Add("First");
        TreeNode second = parent.Nodes.Add("Second");

        var result = ElementTreeViewManager.ProcessDrop(second, MultiSelectTreeView.DropKind.Before);

        result.ShouldNotBeNull();
        result.Value.TreeTarget.ShouldBe(parent);
        result.Value.Drop.ShouldBeNull();
    }
}
