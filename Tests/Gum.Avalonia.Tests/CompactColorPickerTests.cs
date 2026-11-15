using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Headless;
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
        picker.HueRing.Hue.ShouldBe(0, tolerance: 0.5);
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
    public void TheHueRing_KeepsItsHue_WhenTheColorIsGray()
    {
        // Gray has no hue; the spectrum must not snap back to red while the user picks one.
        CompactColorPicker picker = new CompactColorPicker { Color = Color.FromRgb(128, 128, 128) };

        picker.HueRing.SetHueByUser(240);

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

    [AvaloniaTheory]
    [InlineData("#F80", 255, 136, 0)]
    [InlineData("ff8800", 255, 136, 0)]
    [InlineData("#3D9BD6", 61, 155, 214)]
    public void TypingAValidHex_SetsTheColor_WithNoMessage(string text, int r, int g, int b)
    {
        CompactColorPicker picker = new CompactColorPicker();
        List<Color> raised = new List<Color>();
        picker.ColorChanged += (_, color) => raised.Add(color);

        picker.HexTextBox.Text = text;

        picker.Color.ShouldBe(Color.FromRgb((byte)r, (byte)g, (byte)b));
        raised.ShouldBe(new[] { Color.FromRgb((byte)r, (byte)g, (byte)b) });
        picker.HexMessageState.ShouldBe(HexMessageKind.None);
        picker.ChannelTextBoxes.Select(textBox => textBox.Text).ShouldBe(new[] { r.ToString(), g.ToString(), b.ToString() });
    }

    [AvaloniaFact]
    public void TypingAnInvalidHex_ShowsTheErrorState_AndLeavesTheColor()
    {
        CompactColorPicker picker = new CompactColorPicker { Color = Color.FromRgb(10, 20, 30) };
        Window window = new Window { Content = picker };
        window.Show();
        int raised = 0;
        picker.ColorChanged += (_, _) => raised++;

        picker.HexTextBox.Text = "#12G4";

        picker.HexMessageState.ShouldBe(HexMessageKind.Error);
        picker.HexMessage.Text.ShouldNotBeNullOrEmpty();
        Color errorColor = ThemeColor(picker, "Frb.Brushes.Error");
        ((ISolidColorBrush)picker.HexMessage.Foreground!).Color.ShouldBe(errorColor);
        ((ISolidColorBrush)picker.HexTextBox.BorderBrush!).Color.ShouldBe(errorColor);
        picker.Color.ShouldBe(Color.FromRgb(10, 20, 30));
        raised.ShouldBe(0);
        window.Close();
    }

    [AvaloniaFact]
    public void TypingRrggbbAa_UsesRgb_ShowsTheWarningNotTheError_AndKeepsAlphaOutOfTheColor()
    {
        CompactColorPicker picker = new CompactColorPicker();
        Window window = new Window { Content = picker };
        window.Show();

        picker.HexTextBox.Text = "#AABBCCDD";

        picker.Color.ShouldBe(Color.FromRgb(0xAA, 0xBB, 0xCC));
        picker.Color.A.ShouldBe((byte)255);
        picker.HexMessageState.ShouldBe(HexMessageKind.Warning);
        picker.HexMessage.Text.ShouldContain("alpha");
        Color warning = ((ISolidColorBrush)picker.HexMessage.Foreground!).Color;
        warning.ShouldNotBe(ThemeColor(picker, "Frb.Brushes.Error"));
        (warning.R > warning.G && warning.G > warning.B).ShouldBeTrue($"the warning should be orange, was {warning}");
        ((ISolidColorBrush?)picker.HexTextBox.BorderBrush)?.Color.ShouldNotBe(ThemeColor(picker, "Frb.Brushes.Error"));
        window.Close();
    }

    [AvaloniaFact]
    public void TheWarning_IsClearedByTheNextEdit()
    {
        CompactColorPicker picker = new CompactColorPicker();
        picker.HexTextBox.Text = "#AABBCCDD";

        picker.ChannelTextBoxes[0].Text = "1";
        picker.CommitChannelText(0);

        picker.HexMessageState.ShouldBe(HexMessageKind.None);
        picker.HexTextBox.Text.ShouldBe("#01BBCC");
    }

    [AvaloniaFact]
    public void TheHexField_FollowsOtherEdits()
    {
        CompactColorPicker picker = new CompactColorPicker();

        picker.ChannelSliders[2].Color = Color.FromRgb(0, 0, 255);

        picker.HexTextBox.Text.ShouldBe("#0000FF");
    }

    [AvaloniaFact]
    public async Task Paste_AppliesAClipboardColor_IncludingTheAlphaWarning_AndRejectsNonsense()
    {
        CompactColorPicker picker = new CompactColorPicker();
        Window window = new Window { Content = picker };
        window.Show();
        IClipboard clipboard = TopLevel.GetTopLevel(picker)!.Clipboard!;

        await clipboard.SetTextAsync("#FF880080");
        await picker.PasteAsync();
        picker.Color.ShouldBe(Color.FromRgb(255, 136, 0));
        picker.HexMessageState.ShouldBe(HexMessageKind.Warning);
        picker.HexTextBox.Text.ShouldBe("#FF8800");

        await clipboard.SetTextAsync("#0F0");
        await picker.PasteAsync();
        picker.Color.ShouldBe(Color.FromRgb(0, 255, 0));
        picker.HexMessageState.ShouldBe(HexMessageKind.Note);

        await clipboard.SetTextAsync("nonsense");
        await picker.PasteAsync();
        picker.Color.ShouldBe(Color.FromRgb(0, 255, 0));
        picker.HexMessageState.ShouldBe(HexMessageKind.Error);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Copy_PutsRrggbbWithNoAlphaOnTheClipboard()
    {
        CompactColorPicker picker = new CompactColorPicker { Color = Color.FromRgb(61, 155, 214) };
        Window window = new Window { Content = picker };
        window.Show();

        await picker.CopyAsync();

        string? copied = await TopLevel.GetTopLevel(picker)!.Clipboard!.TryGetTextAsync();
        copied.ShouldBe("#3D9BD6");
        picker.HexMessage.Text.ShouldBe("Copied #3D9BD6");
        window.Close();
    }

    [AvaloniaFact]
    public void TheHueRing_ArrowKeysNudgeTheHue_ThroughTheSeam()
    {
        CompactColorPicker picker = new CompactColorPicker { Color = Color.FromRgb(255, 0, 0) };
        Window window = new Window { Content = picker };
        window.Show();
        List<Color> raised = new List<Color>();
        picker.ColorChanged += (_, color) => raised.Add(color);
        picker.HueRing.Focus();

        window.KeyPress(Key.Left, RawInputModifiers.None, PhysicalKey.ArrowLeft, null);

        picker.HueRing.Hue.ShouldBe(359, tolerance: 0.001);
        picker.Spectrum.HsvColor.H.ShouldBe(359, tolerance: 0.001);
        raised.Count.ShouldBe(1);

        window.KeyPress(Key.Right, RawInputModifiers.Shift, PhysicalKey.ArrowRight, null);

        picker.HueRing.Hue.ShouldBe(9, tolerance: 0.001);
        window.Close();
    }

    [AvaloniaFact]
    public void DraggingTheHueRing_FromPurpleToOrange_PassesThroughRed_WithoutJumping()
    {
        CompactColorPicker picker = new CompactColorPicker { Color = Color.FromRgb(163, 71, 217) };
        Window window = new Window { Content = picker, Width = 300, Height = 500 };
        window.Show();
        double previousHue = picker.HueRing.Hue;
        bool passedRed = false;

        for (double angle = 278; angle <= 390; angle += 2)
        {
            Point point = picker.HueRing.TranslatePoint(picker.HueRing.ThumbCenterFor(angle), window)!.Value;
            if (angle == 278)
            {
                window.MouseDown(point, MouseButton.Left);
            }
            else
            {
                window.MouseMove(point, RawInputModifiers.LeftMouseButton);
            }
            double step = Math.Abs(picker.HueRing.Hue - previousHue);
            (step < 5 || step > 355).ShouldBeTrue($"hue jumped from {previousHue} to {picker.HueRing.Hue}");
            passedRed |= picker.HueRing.Hue >= 355 || picker.HueRing.Hue <= 5;
            previousHue = picker.HueRing.Hue;
        }
        window.MouseUp(picker.HueRing.TranslatePoint(picker.HueRing.ThumbCenterFor(30), window)!.Value, MouseButton.Left);

        passedRed.ShouldBeTrue();
        picker.HueRing.Hue.ShouldBe(30, tolerance: 2);
        picker.Spectrum.HsvColor.H.ShouldBe(30, tolerance: 2);
        window.Close();
    }

    [AvaloniaFact]
    public void PressingInsideTheRingsHole_DoesNotMoveTheHue()
    {
        CompactColorPicker picker = new CompactColorPicker { Color = Color.FromRgb(255, 0, 0) };
        Window window = new Window { Content = picker, Width = 300, Height = 500 };
        window.Show();
        Point corner = picker.HueRing.TranslatePoint(new Point(2, 2), window)!.Value;

        window.MouseDown(corner, MouseButton.Left);
        window.MouseUp(corner, MouseButton.Left);

        picker.HueRing.Hue.ShouldBe(0, tolerance: 0.001);
        window.Close();
    }

    private static Color ThemeColor(Control control, string key)
    {
        control.TryFindResource(key, control.ActualThemeVariant, out object? brush).ShouldBeTrue($"{key} should resolve in the theme");
        return ((ISolidColorBrush)brush!).Color;
    }
}
