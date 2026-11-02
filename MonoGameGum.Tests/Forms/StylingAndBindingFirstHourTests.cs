using System;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Mvvm;
using Microsoft.Xna.Framework;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The first styling and binding steps of the tutorials: change a style color, restyle controls
/// that already exist, tweak one control's visual, and bind controls to a ViewModel.
/// </summary>
public class StylingAndBindingFirstHourTests : BeginnerDriverTestBase
{
    private class SettingsViewModel : ViewModel
    {
        public float Volume { get => Get<float>(); set => Set(value); }
        public string? PlayerName { get => Get<string>(); set => Set(value); }
        public bool IsMuted { get => Get<bool>(); set => Set(value); }

        [DependsOn(nameof(Volume))]
        [DependsOn(nameof(IsMuted))]
        public string Summary => IsMuted ? "muted" : $"{Volume:0}";
    }

    private readonly Color _originalPrimary = Styling.ActiveStyle.Colors.Primary;

    public override void Dispose()
    {
        Styling.ActiveStyle.Colors.Primary = _originalPrimary;
        base.Dispose();
    }

    [Fact]
    public void Background_ShouldUseNewPrimary_WhenButtonIsCreatedAfterStyleChange()
    {
        Styling.ActiveStyle.Colors.Primary = Color.DarkGreen;

        Button button = new();

        ((ButtonVisual)button.Visual).Background.Color.ShouldBe(Color.DarkGreen);
    }

    [Fact]
    public void Background_ShouldKeepOldColor_WhenStyleChangesAfterButtonWasCreated()
    {
        Button button = new();
        Color before = ((ButtonVisual)button.Visual).Background.Color;

        Styling.ActiveStyle.Colors.Primary = Color.Red;

        ((ButtonVisual)button.Visual).Background.Color.ShouldBe(before);
    }

    [Fact]
    public void Background_ShouldShowAssignedColor_WhenButtonVisualBackgroundColorIsSet()
    {
        Button button = new();

        ((ButtonVisual)button.Visual).BackgroundColor = Color.Pink;

        ((ButtonVisual)button.Visual).Background.Color.ShouldBe(Color.Pink);
    }

    [Fact]
    public void Width_ShouldBeHonored_WhenButtonVisualWidthIsAssigned()
    {
        Button button = AddToRoot(new Button { Text = "Wide" }, "Btn");

        button.Visual.Width = 300;
        Driver.Step();

        button.Visual.AbsoluteWidth.ShouldBe(300, 0.5f);
        button.ActualWidth.ShouldBe(300, 0.5f);
    }

    [Fact]
    public void Slider_Value_ShouldFollowViewModel_WhenBindingContextIsAssigned()
    {
        StackPanel panel = new();
        Slider slider = new() { Minimum = 0, Maximum = 100 };
        panel.AddChild(slider);
        slider.SetBinding(nameof(Slider.Value), nameof(SettingsViewModel.Volume));
        SettingsViewModel viewModel = new() { Volume = 30 };

        panel.BindingContext = viewModel;
        viewModel.Volume = 70;

        slider.Value.ShouldBe(70, 0.001);
    }

    [Fact]
    public void Volume_ShouldFollowSlider_WhenSliderIsDragged()
    {
        StackPanel panel = new();
        panel.AddToRoot();
        Slider slider = new() { Name = "Sl", Width = 200, Minimum = 0, Maximum = 100 };
        panel.AddChild(slider);
        slider.SetBinding(nameof(Slider.Value), nameof(SettingsViewModel.Volume));
        SettingsViewModel viewModel = new();
        panel.BindingContext = viewModel;
        Driver.Step();
        (int x, int y) = Driver.CenterOf("Sl");

        Driver.Drag("ThumbInstance", x + 400, y);

        viewModel.Volume.ShouldBe(100, 0.001f);
    }

    [Fact]
    public void IsMuted_ShouldFollowCheckBoxClick_WhenIsCheckedIsBound()
    {
        StackPanel panel = new();
        panel.AddToRoot();
        CheckBox mute = new() { Name = "Mute" };
        panel.AddChild(mute);
        mute.SetBinding(nameof(CheckBox.IsChecked), nameof(SettingsViewModel.IsMuted));
        SettingsViewModel viewModel = new();
        panel.BindingContext = viewModel;
        Driver.Step();

        Driver.Click("Mute");

        viewModel.IsMuted.ShouldBeTrue();
    }

    [Fact]
    public void PlayerName_ShouldFollowTypingAndLabelShouldFollowDependentProperty()
    {
        StackPanel panel = new();
        panel.AddToRoot();
        TextBox textBox = new() { Name = "Name", Width = 200 };
        Label summary = new();
        panel.AddChild(textBox);
        panel.AddChild(summary);
        textBox.SetBinding(nameof(TextBox.Text), nameof(SettingsViewModel.PlayerName));
        summary.SetBinding(nameof(Label.Text), nameof(SettingsViewModel.Summary));
        SettingsViewModel viewModel = new() { Volume = 20 };
        panel.BindingContext = viewModel;
        Driver.Step();
        Driver.Click("Name");

        Driver.Type("Hero");
        viewModel.IsMuted = true;

        viewModel.PlayerName.ShouldBe("Hero");
        summary.Text.ShouldBe("muted");
    }

    [Fact]
    public void ViewModelChange_ShouldNotThrow_WhenBoundScreenWasRemovedFromRoot()
    {
        StackPanel panel = new();
        panel.AddToRoot();
        Label label = new();
        panel.AddChild(label);
        label.SetBinding(nameof(Label.Text), nameof(SettingsViewModel.Summary));
        SettingsViewModel viewModel = new();
        panel.BindingContext = viewModel;
        panel.RemoveFromRoot();

        Should.NotThrow(() => viewModel.Volume = 55);
    }

    [Fact]
    public void BindingContext_ShouldNotThrow_WhenSetToNullAfterBinding()
    {
        StackPanel panel = new();
        TextBox textBox = new();
        panel.AddChild(textBox);
        textBox.SetBinding(nameof(TextBox.Text), nameof(SettingsViewModel.PlayerName));
        panel.BindingContext = new SettingsViewModel { PlayerName = "Hero" };

        Should.NotThrow(() => panel.BindingContext = null);
    }
}
