using Gum.Forms.Controls;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// How bindings follow an inherited BindingContext down a tree of Forms controls, and how a child's own
/// explicit context overrides it, including after the parent's context changes or the child's is cleared.
/// </summary>
public class BindingContextInheritanceTests : BaseTestClass
{
    private static (StackPanel root, TextBox leaf) CreateDeepTree(int depth)
    {
        StackPanel root = new();
        StackPanel current = root;
        for (int i = 0; i < depth; i++)
        {
            StackPanel next = new();
            current.AddChild(next);
            current = next;
        }
        TextBox leaf = new();
        current.AddChild(leaf);
        return (root, leaf);
    }

    [Fact]
    public void DeepTree_RootContextReplaced_LeafBindingFollowsNewContext()
    {
        TestViewModel first = new() { Text = "first" };
        TestViewModel second = new() { Text = "second" };
        (StackPanel root, TextBox leaf) = CreateDeepTree(depth: 6);
        leaf.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));
        root.BindingContext = first;
        leaf.Text.ShouldBe("first");

        root.BindingContext = second;
        leaf.Text.ShouldBe("second");

        first.Text = "first changed";
        leaf.Text.ShouldBe("second");

        leaf.Text = "typed";
        second.Text.ShouldBe("typed");
        first.Text.ShouldBe("first changed");
    }

    [Fact]
    public void DeepTree_BindingSetBeforeTreeIsBuilt_FollowsContextAssignedLater()
    {
        TestViewModel vm = new() { Text = "vm" };
        TextBox leaf = new();
        leaf.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));
        StackPanel middle = new();
        middle.AddChild(leaf);
        StackPanel root = new();
        root.AddChild(middle);

        root.BindingContext = vm;

        leaf.Text.ShouldBe("vm");
    }

    [Fact]
    public void ChildExplicitContext_ParentContextChanges_ChildAndItsDescendantsKeepOwn()
    {
        TestViewModel parentVm = new() { Text = "parent" };
        TestViewModel childVm = new() { Text = "child" };
        StackPanel parent = new() { BindingContext = parentVm };
        StackPanel child = new() { BindingContext = childVm };
        parent.AddChild(child);
        TextBox grandChild = new();
        child.AddChild(grandChild);
        grandChild.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        parent.BindingContext = new TestViewModel { Text = "other parent" };

        child.BindingContext.ShouldBe(childVm);
        grandChild.Text.ShouldBe("child");
    }

    [Fact]
    public void ChildExplicitContextSetBeforeAdd_KeepsOwnAfterAdd()
    {
        TestViewModel parentVm = new() { Text = "parent" };
        TestViewModel childVm = new() { Text = "child" };
        StackPanel parent = new() { BindingContext = parentVm };
        TextBox child = new() { BindingContext = childVm };
        child.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        parent.AddChild(child);

        child.Text.ShouldBe("child");
    }

    [Fact]
    public void ChildExplicitContextCleared_RevertsToParentsContext()
    {
        TestViewModel parentVm = new() { Text = "parent" };
        TestViewModel childVm = new() { Text = "child" };
        StackPanel parent = new() { BindingContext = parentVm };
        TextBox child = new() { BindingContext = childVm };
        parent.AddChild(child);
        child.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        child.BindingContext = null;

        child.BindingContext.ShouldBe(parentVm);
        child.Text.ShouldBe("parent");

        parentVm.Text = "parent changed";
        child.Text.ShouldBe("parent changed");
    }

    [Fact]
    public void ChildExplicitContextCleared_ParentContextChangedMeanwhile_UsesCurrentParentContext()
    {
        TestViewModel childVm = new() { Text = "child" };
        StackPanel parent = new() { BindingContext = new TestViewModel { Text = "old parent" } };
        TextBox child = new() { BindingContext = childVm };
        parent.AddChild(child);
        child.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));
        TestViewModel newParentVm = new() { Text = "new parent" };
        parent.BindingContext = newParentVm;

        child.BindingContext = null;

        child.Text.ShouldBe("new parent");
    }

    [Fact]
    public void InheritedContext_SiblingWithExplicitContext_DoesNotAffectOtherSibling()
    {
        TestViewModel parentVm = new() { Text = "parent" };
        StackPanel parent = new() { BindingContext = parentVm };
        TextBox explicitChild = new() { BindingContext = new TestViewModel { Text = "own" } };
        TextBox inheritingChild = new();
        parent.AddChild(explicitChild);
        parent.AddChild(inheritingChild);
        inheritingChild.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        explicitChild.BindingContext = new TestViewModel { Text = "own again" };

        inheritingChild.BindingContext.ShouldBe(parentVm);
        inheritingChild.Text.ShouldBe("parent");
    }

    [Fact]
    public void ChildRemovedFromParent_LosesInheritedContext()
    {
        TestViewModel vm = new() { Text = "parent" };
        StackPanel parent = new() { BindingContext = vm };
        TextBox child = new();
        parent.AddChild(child);
        child.BindingContext.ShouldBe(vm);

        parent.RemoveChild(child);

        child.BindingContext.ShouldBeNull();
    }
}
