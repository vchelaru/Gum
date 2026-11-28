using Gum.Forms.Controls;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Two controls bound TwoWay to one view model property stay in step; a child's BindingContext bound to a view
/// model property follows that property; a struct member on the path displays.
/// </summary>
public class BindingGameScenarioTests : BaseTestClass
{
    public struct Position
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    private class PlayerViewModel : ViewModel
    {
        public string? Name
        {
            get => Get<string?>();
            set => Set(value);
        }

        public Position Position
        {
            get => Get<Position>();
            set => Set(value);
        }
    }

    private class SettingsViewModel : ViewModel
    {
        public double Volume
        {
            get => Get<double>();
            set => Set(value);
        }

        public PlayerViewModel? Player
        {
            get => Get<PlayerViewModel?>();
            set => Set(value);
        }
    }

    [Fact]
    public void TwoSlidersSameSource_MovingOne_MovesTheOther()
    {
        SettingsViewModel vm = new() { Volume = 10 };
        Slider first = new() { Maximum = 100, BindingContext = vm };
        Slider second = new() { Maximum = 100, BindingContext = vm };
        first.SetBinding(nameof(Slider.Value), nameof(SettingsViewModel.Volume));
        second.SetBinding(nameof(Slider.Value), nameof(SettingsViewModel.Volume));

        first.Value = 40;

        vm.Volume.ShouldBe(40);
        second.Value.ShouldBe(40);
    }

    [Fact]
    public void ChildContextBoundToProperty_PropertyReplaced_ChildFollows()
    {
        SettingsViewModel vm = new() { Player = new PlayerViewModel { Name = "a" } };
        StackPanel panel = new() { BindingContext = vm };
        StackPanel playerPanel = new();
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(SettingsViewModel.Player));
        panel.AddChild(playerPanel);
        Label label = new();
        label.SetBinding(nameof(Label.Text), nameof(PlayerViewModel.Name));
        playerPanel.AddChild(label);

        vm.Player = new PlayerViewModel { Name = "b" };

        label.Text.ShouldBe("b");
    }

    [Fact]
    public void ChildContextBoundToProperty_PropertyBecomesNull_ChildShowsNoValue()
    {
        SettingsViewModel vm = new() { Player = new PlayerViewModel { Name = "a" } };
        StackPanel panel = new() { BindingContext = vm };
        StackPanel playerPanel = new();
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(SettingsViewModel.Player));
        panel.AddChild(playerPanel);
        Label label = new();
        label.SetBinding(nameof(Label.Text), nameof(PlayerViewModel.Name));
        playerPanel.AddChild(label);

        vm.Player = null;

        label.Text.ShouldNotBe("a");
    }

    [Fact]
    public void StructMemberPath_Displays()
    {
        PlayerViewModel vm = new() { Position = new Position { X = 3 } };
        TextBox textBox = new() { BindingContext = vm };

        textBox.SetBinding(nameof(TextBox.Text), "Position.X");

        textBox.Text.ShouldBe("3");
    }
}
