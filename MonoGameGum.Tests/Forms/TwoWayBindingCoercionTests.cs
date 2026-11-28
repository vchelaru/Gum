using System;
using Gum.Forms.Controls;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A two-way binding whose view-model setter changes the value it is given (rounding, clamping,
/// trimming) or raises PropertyChanged for other properties: the control shows what the view model
/// kept, and other bindings see the change.
/// </summary>
public class TwoWayBindingCoercionTests : BaseTestClass
{
    private class CoercingViewModel : ViewModel
    {
        public double Rounded { get => Get<double>(); set => Set(Math.Round(value)); }

        public double Clamped { get => Get<double>(); set => Set(Math.Min(value, 10)); }

        public string? Trimmed { get => Get<string?>(); set => Set(value?.Trim()); }

        public string? Name
        {
            get => Get<string?>();
            set
            {
                if (Set(value))
                {
                    NotifyPropertyChanged(nameof(Greeting));
                }
            }
        }

        public string Greeting => $"Hello {Name}";
    }

    [Fact]
    public void ViewModelRoundsValue_ControlShowsRoundedValue()
    {
        CoercingViewModel vm = new();
        Slider slider = new() { Minimum = 0, Maximum = 100, BindingContext = vm };
        slider.SetBinding(nameof(Slider.Value), nameof(CoercingViewModel.Rounded));

        slider.Value = 2.6;

        vm.Rounded.ShouldBe(3);
        slider.Value.ShouldBe(3);
    }

    [Fact]
    public void ViewModelTrimsText_ControlShowsTrimmedText()
    {
        CoercingViewModel vm = new();
        TextBox textBox = new() { BindingContext = vm };
        textBox.SetBinding(nameof(TextBox.Text), nameof(CoercingViewModel.Trimmed));

        textBox.Text = "  padded  ";

        vm.Trimmed.ShouldBe("padded");
        textBox.Text.ShouldBe("padded");
    }

    [Fact]
    public void ViewModelClampsToValueItAlreadyHolds_ControlShowsTheHeldValue()
    {
        CoercingViewModel vm = new() { Clamped = 10 };
        Slider slider = new() { Minimum = 0, Maximum = 100, BindingContext = vm };
        slider.SetBinding(nameof(Slider.Value), nameof(CoercingViewModel.Clamped));
        slider.Value.ShouldBe(10);

        slider.Value = 50;

        vm.Clamped.ShouldBe(10);
        slider.Value.ShouldBe(10, "because a two-way binding shows what the view model kept, as WPF re-reads the source after writing it");
    }

    [Fact]
    public void ViewModelSetterRaisesOtherProperty_OtherBindingUpdates()
    {
        CoercingViewModel vm = new() { Name = "a" };
        StackPanel panel = new() { BindingContext = vm };
        TextBox nameBox = new();
        Label greeting = new();
        panel.AddChild(nameBox);
        panel.AddChild(greeting);
        nameBox.SetBinding(nameof(TextBox.Text), nameof(CoercingViewModel.Name));
        greeting.SetBinding(nameof(Label.Text), nameof(CoercingViewModel.Greeting));

        nameBox.Text = "Bob";

        greeting.Text.ShouldBe("Hello Bob");
        nameBox.Text.ShouldBe("Bob");
    }

    [Fact]
    public void TwoControlsBoundToSameProperty_EditInOneReachesTheOther()
    {
        TestViewModel vm = new() { Text = "start" };
        StackPanel panel = new() { BindingContext = vm };
        TextBox first = new();
        TextBox second = new();
        panel.AddChild(first);
        panel.AddChild(second);
        first.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));
        second.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));

        first.Text = "edited";

        second.Text.ShouldBe("edited");
        vm.Text.ShouldBe("edited");
    }
}
