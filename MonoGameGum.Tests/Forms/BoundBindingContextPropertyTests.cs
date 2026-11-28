using Gum.Forms.Controls;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A panel whose BindingContext is bound to a view model property (Player) gives its children that property's
/// value: they follow a replacement.
/// </summary>
public class BoundBindingContextPropertyTests : BaseTestClass
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

    private static Label AddBoundLabel(GameViewModel vm)
    {
        StackPanel panel = new() { BindingContext = vm };
        StackPanel playerPanel = new();
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameViewModel.Player));
        panel.AddChild(playerPanel);
        Label label = new();
        label.SetBinding(nameof(Label.Text), nameof(PlayerViewModel.Name));
        playerPanel.AddChild(label);
        return label;
    }

    [Fact]
    public void PropertyReplaced_ChildFollows()
    {
        GameViewModel vm = new() { Player = new PlayerViewModel { Name = "a" } };
        Label label = AddBoundLabel(vm);

        vm.Player = new PlayerViewModel { Name = "b" };

        label.Text.ShouldBe("b");
    }
}
