using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Avalonia.Plugins.VariableGrid;
using Shouldly;

namespace Gum.Avalonia.Tests;

public class CompactColorPickerTests
{
    [AvaloniaFact]
    public void SettingTheColor_SyncsEveryPart_WithoutRaisingColorChanged()
    {
        CompactColorPicker picker = new CompactColorPicker();
        int raised = 0;
        picker.ColorChanged += (_, _) => raised++;

        picker.Color = Color.FromRgb(255, 0, 0);

        picker.Spectrum.Color.ShouldBe(Color.FromRgb(255, 0, 0));
        picker.HueSlider.HsvColor.H.ShouldBe(0, tolerance: 0.5);
        picker.ChannelTextBoxes.Select(textBox => textBox.Text).ShouldBe(new[] { "255", "0", "0" });
        picker.ChannelSliders.ShouldAllBe(slider => slider.Color == Color.FromRgb(255, 0, 0));
        ((ISolidColorBrush)picker.CurrentSwatch.Background!).Color.ShouldBe(Color.FromRgb(255, 0, 0));
        raised.ShouldBe(0);
    }

    [AvaloniaFact]
    public void EditingAPart_UpdatesTheOthers_AndRaisesColorChanged()
    {
        CompactColorPicker picker = new CompactColorPicker { Color = Color.FromRgb(10, 20, 30) };
        List<Color> raised = new List<Color>();
        picker.ColorChanged += (_, color) => raised.Add(color);

        picker.ChannelTextBoxes[1].Text = "200";
        picker.CommitChannelText(1);

        picker.Color.ShouldBe(Color.FromRgb(10, 200, 30));
        picker.Spectrum.Color.ShouldBe(Color.FromRgb(10, 200, 30));
        raised.ShouldBe(new[] { Color.FromRgb(10, 200, 30) });

        picker.ChannelSliders[0].Color = Color.FromRgb(90, 200, 30);

        picker.Color.ShouldBe(Color.FromRgb(90, 200, 30));
        picker.ChannelTextBoxes[0].Text.ShouldBe("90");
        raised.Last().ShouldBe(Color.FromRgb(90, 200, 30));

        picker.Spectrum.HsvColor = HsvColor.FromHsv(picker.Spectrum.HsvColor.H, 0, 1);

        picker.Color.ShouldBe(Color.FromRgb(255, 255, 255));
        picker.ChannelTextBoxes.Select(textBox => textBox.Text).ShouldBe(new[] { "255", "255", "255" });
    }

    [AvaloniaFact]
    public void ChannelText_OutOfRangeIsClamped_AndNonsenseReverts()
    {
        CompactColorPicker picker = new CompactColorPicker { Color = Color.FromRgb(10, 20, 30) };

        picker.ChannelTextBoxes[2].Text = "999";
        picker.CommitChannelText(2);
        picker.Color.ShouldBe(Color.FromRgb(10, 20, 255));
        picker.ChannelTextBoxes[2].Text.ShouldBe("255");

        picker.ChannelTextBoxes[0].Text = "abc";
        picker.CommitChannelText(0);
        picker.Color.ShouldBe(Color.FromRgb(10, 20, 255));
        picker.ChannelTextBoxes[0].Text.ShouldBe("10");
    }

    [AvaloniaFact]
    public void TheHueBar_KeepsItsHue_WhenTheColorIsGray()
    {
        // Gray has no hue; the spectrum must not snap back to red while the user picks one.
        CompactColorPicker picker = new CompactColorPicker { Color = Color.FromRgb(128, 128, 128) };

        picker.HueSlider.HsvColor = HsvColor.FromHsv(240, 0, picker.HueSlider.HsvColor.V);

        picker.Spectrum.HsvColor.H.ShouldBe(240, tolerance: 0.5);

        // Nor when a channel edit lands on black, which has neither hue nor saturation.
        picker.Color = Color.FromRgb(0, 0, 255);
        picker.ChannelTextBoxes[2].Text = "0";
        picker.CommitChannelText(2);

        picker.Spectrum.HsvColor.H.ShouldBe(240, tolerance: 0.5);
        picker.Spectrum.HsvColor.S.ShouldBe(1, tolerance: 0.01);
    }

    [AvaloniaFact]
    public void OriginalSwatch_ShowsTheColorItWasGiven()
    {
        CompactColorPicker picker = new CompactColorPicker { Color = Color.FromRgb(1, 2, 3) };

        picker.OriginalColor = Color.FromRgb(4, 5, 6);

        ((ISolidColorBrush)picker.OriginalSwatch.Background!).Color.ShouldBe(Color.FromRgb(4, 5, 6));
    }

    [AvaloniaFact]
    public void ChannelNumbers_AreVerticallyCentered()
    {
        CompactColorPicker picker = new CompactColorPicker();

        picker.ChannelTextBoxes.ShouldAllBe(textBox => textBox.VerticalContentAlignment == VerticalAlignment.Center);
    }
}
