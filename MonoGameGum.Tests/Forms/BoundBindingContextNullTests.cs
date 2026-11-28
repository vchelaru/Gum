using Gum.Forms.Controls;
using Gum.Mvvm;
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
}
