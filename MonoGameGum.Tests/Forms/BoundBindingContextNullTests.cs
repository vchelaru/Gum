using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Mvvm;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A panel whose BindingContext is bound to a view model property (Player) gives its children that property's
/// value: when it becomes null they show their pre-binding values, never the parent's
/// view model.
/// </summary>
public class BoundBindingContextNullTests : BaseTestClass
{
    private class PlayerViewModel : ViewModel
    {
        public string? Name
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    private class GameViewModel : ViewModel
    {
        public PlayerViewModel? Player
        {
            get => Get<PlayerViewModel?>();
            set => Set(value);
        }
    }

    private class GameWithNameViewModel : GameViewModel
    {
        public string? Name
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    private static Label AddBoundLabel(GameViewModel vm, out string? preBindingText)
    {
        StackPanel panel = new() { BindingContext = vm };
        StackPanel playerPanel = new();
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameViewModel.Player));
        panel.AddChild(playerPanel);
        Label label = new();
        preBindingText = label.Text;
        label.SetBinding(nameof(Label.Text), nameof(PlayerViewModel.Name));
        playerPanel.AddChild(label);
        return label;
    }

    [Fact]
    public void PropertyBecomesNull_ChildShowsPreBindingValue()
    {
        GameViewModel vm = new() { Player = new PlayerViewModel { Name = "a" } };
        Label label = AddBoundLabel(vm, out string? preBindingText);

        vm.Player = null;

        label.Text.ShouldBe(preBindingText);
    }

    [Fact]
    public void PropertyBecomesNull_ParentHasSameNamedProperty_ChildDoesNotShowParentValue()
    {
        GameWithNameViewModel vm = new() { Name = "parent", Player = new PlayerViewModel { Name = "a" } };
        Label label = AddBoundLabel(vm, out string? preBindingText);

        vm.Player = null;

        label.Text.ShouldBe(preBindingText);
    }

    [Fact]
    public void PropertyNullWhenBound_ChildShowsPreBindingValue()
    {
        GameWithNameViewModel vm = new() { Name = "parent", Player = null };
        Label label = AddBoundLabel(vm, out string? preBindingText);

        label.Text.ShouldBe(preBindingText);
    }

    [Fact]
    public void PropertyBecomesNullThenSet_ChildShowsNewValue()
    {
        GameWithNameViewModel vm = new() { Name = "parent", Player = new PlayerViewModel { Name = "a" } };
        Label label = AddBoundLabel(vm, out _);

        vm.Player = null;
        vm.Player = new PlayerViewModel { Name = "b" };

        label.Text.ShouldBe("b");
    }

    [Fact]
    public void BindingCleared_ChildInheritsParentContextAgain()
    {
        GameWithNameViewModel vm = new() { Name = "parent", Player = new PlayerViewModel { Name = "a" } };
        StackPanel panel = new() { BindingContext = vm };
        StackPanel playerPanel = new();
        panel.AddChild(playerPanel);
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameViewModel.Player));
        Label label = new();
        label.SetBinding(nameof(Label.Text), nameof(PlayerViewModel.Name));
        playerPanel.AddChild(label);
        vm.Player = null;

        playerPanel.ClearBinding(nameof(FrameworkElement.BindingContext));
        label.Text.ShouldBe("parent");

        panel.BindingContext = new GameWithNameViewModel { Name = "second" };
        label.Text.ShouldBe("second");
    }

    [Fact]
    public void UnboundContextSetToNull_ChildInheritsParentContext()
    {
        GameWithNameViewModel vm = new() { Name = "parent" };
        StackPanel panel = new() { BindingContext = vm };
        StackPanel playerPanel = new() { BindingContext = new PlayerViewModel { Name = "own" } };
        panel.AddChild(playerPanel);
        Label label = new();
        label.SetBinding(nameof(Label.Text), nameof(PlayerViewModel.Name));
        playerPanel.AddChild(label);

        playerPanel.BindingContext = null;

        playerPanel.BindingContext.ShouldBe(vm);
        label.Text.ShouldBe("parent");
    }

    [Fact]
    public void VisualReplacedWhileNull_ContextStaysNull()
    {
        GameViewModel vm = new() { Player = new PlayerViewModel { Name = "a" } };
        StackPanel panel = new() { BindingContext = vm };
        Label sut = new();
        panel.AddChild(sut);
        sut.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameViewModel.Player));
        vm.Player = null;
        InteractiveGue newVisual = new LabelVisual(tryCreateFormsObject: false);
        panel.Visual.Children.Add(newVisual);

        sut.Visual = newVisual;

        sut.BindingContext.ShouldBeNull();
    }

    [Fact]
    public void VisualBinding_PropertyBecomesNull_ChildShowsPreBindingValue()
    {
        GameWithNameViewModel vm = new() { Name = "parent", Player = new PlayerViewModel { Name = "a" } };
        ContainerRuntime panel = new() { BindingContext = vm };
        ContainerRuntime playerPanel = new();
        playerPanel.SetBinding(nameof(GraphicalUiElement.BindingContext), nameof(GameViewModel.Player));
        panel.Children.Add(playerPanel);
        TextRuntime text = new();
        string? preBindingText = text.Text;
        text.SetBinding(nameof(TextRuntime.Text), nameof(PlayerViewModel.Name));
        playerPanel.Children.Add(text);

        vm.Player = null;

        text.Text.ShouldBe(preBindingText);
    }
}
