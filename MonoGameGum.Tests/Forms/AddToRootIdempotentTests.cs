using System.Linq;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Every helper that parents a child (AddToRoot, AddChild) is idempotent when the child is already
/// under that parent, and moves the child, never duplicates it, when it is under a different parent.
/// </summary>
public class AddToRootIdempotentTests : BaseTestClass
{
    private static GraphicalUiElement Root => Gum.GumService.Default.Root;

    private static int CountIn(GraphicalUiElement parent, GraphicalUiElement child) =>
        parent.Children!.Count(c => c == child);

    [Fact]
    public void AddToRoot_AddsOnce_WhenCalledTwiceOnARuntime()
    {
        ContainerRuntime container = new();

        container.AddToRoot();
        container.AddToRoot();

        CountIn(Root, container).ShouldBe(1);
    }

    [Fact]
    public void AddToRoot_MovesControlToRoot_WhenItIsInAStackPanel()
    {
        StackPanel panel = new();
        panel.AddToRoot();
        Button button = new();
        panel.AddChild(button);

        button.AddToRoot();
        button.AddToRoot();

        CountIn(Root, button.Visual).ShouldBe(1);
        CountIn(panel.Visual, button.Visual).ShouldBe(0);
    }

    [Fact]
    public void AddToRoot_MovesRuntimeToRoot_WhenItIsInAContainer()
    {
        ContainerRuntime parent = new();
        parent.AddToRoot();
        ContainerRuntime child = new();
        parent.AddChild(child);

        child.AddToRoot();

        CountIn(Root, child).ShouldBe(1);
        CountIn(parent, child).ShouldBe(0);
    }

    [Fact]
    public void AddChild_AddsOnce_WhenCalledTwiceWithARuntime()
    {
        ContainerRuntime parent = new();
        ContainerRuntime child = new();

        parent.AddChild(child);
        parent.AddChild(child);

        CountIn(parent, child).ShouldBe(1);
    }

    [Fact]
    public void AddChild_AddsOnce_WhenCalledTwiceWithAControl()
    {
        ContainerRuntime parent = new();
        Button button = new();

        parent.AddChild(button);
        parent.AddChild(button);

        CountIn(parent, button.Visual).ShouldBe(1);
    }

    [Fact]
    public void AddChild_MovesChild_WhenItIsUnderAnotherContainer()
    {
        ContainerRuntime first = new();
        ContainerRuntime second = new();
        ContainerRuntime child = new();
        first.AddChild(child);

        second.AddChild(child);

        CountIn(first, child).ShouldBe(0);
        CountIn(second, child).ShouldBe(1);
        child.Parent.ShouldBe(second);
    }

    [Fact]
    public void FrameworkElementExtAddChild_AddsOnce_WhenCalledTwice()
    {
        ContainerRuntime parent = new();
        Button button = new();

        FrameworkElementExt.AddChild(parent, button);
        FrameworkElementExt.AddChild(parent, button);

        CountIn(parent, button.Visual).ShouldBe(1);
    }

    [Fact]
    public void WindowAddChild_AddsOnce_WhenCalledTwice()
    {
        Window window = new();
        Button button = new();
        ContainerRuntime runtime = new();

        window.AddChild(button);
        window.AddChild(button);
        window.AddChild(runtime);
        window.AddChild(runtime);

        CountIn(window.InnerPanel, button.Visual).ShouldBe(1);
        CountIn(window.InnerPanel, runtime).ShouldBe(1);
    }

    [Fact]
    public void ScrollViewerAddChild_AddsOnce_WhenCalledTwice()
    {
        ScrollViewer scrollViewer = new();
        Button button = new();
        ContainerRuntime runtime = new();

        scrollViewer.AddChild(button);
        scrollViewer.AddChild(button);
        scrollViewer.AddChild(runtime);
        scrollViewer.AddChild(runtime);

        CountIn(scrollViewer.InnerPanel, button.Visual).ShouldBe(1);
        CountIn(scrollViewer.InnerPanel, runtime).ShouldBe(1);
    }

    [Fact]
    public void FrameworkElementAddChild_AddsOnce_WhenCalledTwice()
    {
        StackPanel panel = new();
        Button button = new();
        ContainerRuntime runtime = new();

        panel.AddChild(button);
        panel.AddChild(button);
        panel.AddChild(runtime);
        panel.AddChild(runtime);

        CountIn(panel.Visual, button.Visual).ShouldBe(1);
        CountIn(panel.Visual, runtime).ShouldBe(1);
    }

    [Fact]
    public void ParentAssignment_AddsOnce_WhenAssignedTwice()
    {
        ContainerRuntime child = new();

        child.Parent = Root;
        child.Parent = Root;

        CountIn(Root, child).ShouldBe(1);
    }

    [Fact]
    public void TooltipShow_AddsOnce_WhenCalledTwice()
    {
        Tooltip tooltip = new();

        tooltip.Show();
        tooltip.Show();

        CountIn(FrameworkElement.PopupRoot, tooltip.Visual).ShouldBe(1);
    }
}
