using Gum.Forms.Controls;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Two controls bound TwoWay to one view model property stay in step: moving one writes the source, which moves
/// the other.
/// </summary>
public class SharedSourceTwoWayTests : BaseTestClass
{
    private class SettingsViewModel : ViewModel
    {
        public double Volume
        {
            get => Get<double>();
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
}
