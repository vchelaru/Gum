using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A BindingContext set on a container reaches controls added through the container's own AddChild
/// (ScrollViewer's inner panel, StackPanel, nested panels), whether the context is set before or after the add.
/// </summary>
public class BindingContextThroughContainersTests : BaseTestClass
{
    private class HudViewModel : ViewModel
    {
        public string? Title
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    [Fact]
    public void ScrollViewer_ContextBeforeAddChild_ChildBindingResolves()
    {
        ScrollViewer scrollViewer = new() { BindingContext = new HudViewModel { Title = "quests" } };
        Label label = new();
        label.SetBinding(nameof(Label.Text), nameof(HudViewModel.Title));

        scrollViewer.AddChild(label);

        label.Text.ShouldBe("quests");
    }

    [Fact]
    public void ScrollViewer_ContextAfterAddChild_ChildBindingResolves()
    {
        ScrollViewer scrollViewer = new();
        Label label = new();
        label.SetBinding(nameof(Label.Text), nameof(HudViewModel.Title));
        scrollViewer.AddChild(label);

        scrollViewer.BindingContext = new HudViewModel { Title = "quests" };

        label.Text.ShouldBe("quests");
    }

    [Fact]
    public void ScrollViewer_ContextReplaced_ChildFollows()
    {
        ScrollViewer scrollViewer = new() { BindingContext = new HudViewModel { Title = "a" } };
        Label label = new();
        label.SetBinding(nameof(Label.Text), nameof(HudViewModel.Title));
        scrollViewer.AddChild(label);

        scrollViewer.BindingContext = new HudViewModel { Title = "b" };

        label.Text.ShouldBe("b");
    }

    [Fact]
    public void NestedStackPanels_FiveDeep_LeafBindingResolvesAndFollows()
    {
        StackPanel root = new();
        StackPanel current = root;
        for (int i = 0; i < 4; i++)
        {
            StackPanel next = new();
            current.AddChild(next);
            current = next;
        }
        Label label = new();
        label.SetBinding(nameof(Label.Text), nameof(HudViewModel.Title));
        current.AddChild(label);

        HudViewModel vm = new() { Title = "deep" };
        root.BindingContext = vm;
        vm.Title = "deeper";

        label.Text.ShouldBe("deeper");
    }

    [Fact]
    public void VisualChildOfFormsPanel_InheritsContext()
    {
        StackPanel panel = new() { BindingContext = new HudViewModel { Title = "visual" } };
        TextRuntime text = new();
        text.SetBinding(nameof(TextRuntime.Text), nameof(HudViewModel.Title));

        panel.AddChild(text);

        text.Text.ShouldBe("visual");
    }

    [Fact]
    public void ChildMovedBetweenPanels_FollowsNewParentContext()
    {
        StackPanel left = new() { BindingContext = new HudViewModel { Title = "left" } };
        StackPanel right = new() { BindingContext = new HudViewModel { Title = "right" } };
        Label label = new();
        label.SetBinding(nameof(Label.Text), nameof(HudViewModel.Title));
        left.AddChild(label);

        right.AddChild(label);

        label.Text.ShouldBe("right");
    }

    [Fact]
    public void ChildWithOwnContext_ParentContextChanges_KeepsOwn()
    {
        StackPanel panel = new() { BindingContext = new HudViewModel { Title = "parent" } };
        Label label = new() { BindingContext = new HudViewModel { Title = "own" } };
        label.SetBinding(nameof(Label.Text), nameof(HudViewModel.Title));
        panel.AddChild(label);

        panel.BindingContext = new HudViewModel { Title = "parent2" };

        label.Text.ShouldBe("own");
    }
}
