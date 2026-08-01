using Gum.Controls;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins.InternalPlugins.TreeView;

public class ElementTreeViewManagerProcessDropTests : BaseTestClass
{
    [Fact]
    public void ProcessDrop_NullTarget_ReturnsNull()
    {
        (GumTreeNode TreeTarget, DropTarget? Drop)? result =
            ElementTreeViewManager.ProcessDrop(null, TreeDropKind.Into);

        result.ShouldBeNull();
    }

    [Fact]
    public void ProcessDrop_NoneKind_ReturnsNull()
    {
        GumTreeNode target = new GumTreeNode();

        (GumTreeNode TreeTarget, DropTarget? Drop)? result =
            ElementTreeViewManager.ProcessDrop(target, TreeDropKind.None);

        result.ShouldBeNull();
    }

    [Fact]
    public void ProcessDrop_IntoFirstOnElementSave_AppendsSameAsInto()
    {
        // Issue #2864: the visual adornment for IntoFirst is identical to Into
        // (both draw a rectangle around the row, not a between-rows line). The
        // user cannot distinguish the two visually, so they must behave the same.
        // Insertion at index 0 of the element is still reachable via TreeDropKind.Before
        // on the first child node, which shows an unambiguous line.
        ComponentSave component = new ComponentSave();
        component.Name = "TargetComponent";
        component.Instances.Add(new InstanceSave { Name = "Existing" });

        GumTreeNode target = new GumTreeNode { Tag = component };

        (GumTreeNode TreeTarget, DropTarget? Drop)? result =
            ElementTreeViewManager.ProcessDrop(target, TreeDropKind.IntoFirst);

        result.ShouldNotBeNull();
        result.Value.TreeTarget.ShouldBeSameAs(target);
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

        GumTreeNode target = new GumTreeNode { Tag = screen };

        (GumTreeNode TreeTarget, DropTarget? Drop)? result =
            ElementTreeViewManager.ProcessDrop(target, TreeDropKind.Into);

        result.ShouldNotBeNull();
        result.Value.TreeTarget.ShouldBeSameAs(target);
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

        GumTreeNode target = new GumTreeNode { Tag = leftContainer };
        // Simulate that LeftContainer has 4 children visible in the tree view —
        // this is what the child count would have returned in the buggy path.
        target.AddChild("Child1");
        target.AddChild("Child2");
        target.AddChild("Child3");
        target.AddChild("Child4");

        (GumTreeNode TreeTarget, DropTarget? Drop)? result =
            ElementTreeViewManager.ProcessDrop(target, TreeDropKind.Into);

        result.ShouldNotBeNull();
        result.Value.TreeTarget.ShouldBeSameAs(target);
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

        GumTreeNode parent = new GumTreeNode { Tag = screen };
        GumTreeNode firstNode = new GumTreeNode("First") { Tag = first };
        parent.Nodes.Add(firstNode);
        GumTreeNode secondNode = new GumTreeNode("Second") { Tag = second };
        parent.Nodes.Add(secondNode);

        (GumTreeNode TreeTarget, DropTarget? Drop)? result =
            ElementTreeViewManager.ProcessDrop(secondNode, TreeDropKind.Before);

        result.ShouldNotBeNull();
        result.Value.TreeTarget.ShouldBeSameAs(parent);
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

        GumTreeNode parent = new GumTreeNode { Tag = screen };
        GumTreeNode firstNode = new GumTreeNode("First") { Tag = first };
        parent.Nodes.Add(firstNode);
        GumTreeNode secondNode = new GumTreeNode("Second") { Tag = second };
        parent.Nodes.Add(secondNode);

        (GumTreeNode TreeTarget, DropTarget? Drop)? result =
            ElementTreeViewManager.ProcessDrop(firstNode, TreeDropKind.After);

        result.ShouldNotBeNull();
        result.Value.TreeTarget.ShouldBeSameAs(parent);
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
        GumTreeNode parent = new GumTreeNode();
        parent.AddChild("First");
        GumTreeNode second = new GumTreeNode("Second");
        parent.Nodes.Add(second);

        (GumTreeNode TreeTarget, DropTarget? Drop)? result =
            ElementTreeViewManager.ProcessDrop(second, TreeDropKind.Before);

        result.ShouldNotBeNull();
        result.Value.TreeTarget.ShouldBeSameAs(parent);
        result.Value.Drop.ShouldBeNull();
    }
}
