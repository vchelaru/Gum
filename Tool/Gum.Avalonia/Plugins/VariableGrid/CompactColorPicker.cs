using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Gum.Avalonia.Plugins.VariableGrid;

/// <summary>
/// The Variables tab's color flyout: a saturation/value square, a hue bar, R/G/B sliders with 0-255
/// fields, and an old/new swatch. Alpha is not shown because it is a separate Gum variable.
/// </summary>
public class CompactColorPicker : UserControl
{
    private static readonly string[] ChannelNames = { "R", "G", "B" };

    private readonly ColorSpectrum _spectrum;
    private readonly ColorSlider _hueSlider;
    private readonly ColorSlider[] _channelSliders;
    private readonly TextBox[] _channelTextBoxes;
    private readonly Border _originalSwatch;
    private readonly Border _currentSwatch;
    private HsvColor _hsv;
    private Color _color;
    private bool _isSyncing;

    /// <summary>Builds the picker, initially black.</summary>
    public CompactColorPicker()
    {
        _spectrum = new ColorSpectrum
        {
            Shape = ColorSpectrumShape.Box,
            Components = ColorSpectrumComponents.SaturationValue,
            Height = 140,
        };
        _spectrum.PropertyChanged += (_, e) =>
        {
            if (e.Property == ColorSpectrum.HsvColorProperty && !_isSyncing)
            {
                SetHsv(_spectrum.HsvColor, isUserEdit: true);
            }
        };

        _hueSlider = new ColorSlider
        {
            ColorModel = ColorModel.Hsva,
            ColorComponent = ColorComponent.Component1,
            IsPerceptive = true,
            Orientation = Orientation.Horizontal,
            Height = 14,
            Margin = new Thickness(0, 6, 0, 2),
        };
        _hueSlider.PropertyChanged += (_, e) =>
        {
            if (e.Property == ColorSlider.HsvColorProperty && !_isSyncing)
            {
                SetHsv(_hueSlider.HsvColor, isUserEdit: true);
            }
        };

        StackPanel panel = new StackPanel { Width = 200, Spacing = 2 };
        panel.Children.Add(_spectrum);
        panel.Children.Add(_hueSlider);

        _channelSliders = new ColorSlider[3];
        _channelTextBoxes = new TextBox[3];
        for (int i = 0; i < 3; i++)
        {
            panel.Children.Add(CreateChannelRow(i));
        }

        _originalSwatch = new Border { Width = 32, Height = 16, CornerRadius = new CornerRadius(2, 0, 0, 2) };
        ToolTip.SetTip(_originalSwatch, "Color when the picker opened");
        _currentSwatch = new Border { Width = 32, Height = 16, CornerRadius = new CornerRadius(0, 2, 2, 0) };
        ToolTip.SetTip(_currentSwatch, "Current color");
        StackPanel swatches = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
        swatches.Children.Add(_originalSwatch);
        swatches.Children.Add(_currentSwatch);
        panel.Children.Add(swatches);

        Content = panel;
        SetRgb(Colors.Black, isUserEdit: false);
        OriginalColor = Colors.Black;
    }

    /// <summary>Raised when the user changes the color, with the new color.</summary>
    public event EventHandler<Color>? ColorChanged;

    /// <summary>The picked color. Setting it updates the picker without raising <see cref="ColorChanged"/>.</summary>
    public Color Color
    {
        get => _color;
        set => SetRgb(value, isUserEdit: false);
    }

    /// <summary>The color shown in the "old" half of the swatch, normally the color when the flyout opened.</summary>
    public Color OriginalColor
    {
        get => ((ISolidColorBrush)_originalSwatch.Background!).Color;
        set => _originalSwatch.Background = new SolidColorBrush(value);
    }

    internal ColorSpectrum Spectrum => _spectrum;
    internal ColorSlider HueSlider => _hueSlider;
    internal ColorSlider[] ChannelSliders => _channelSliders;
    internal TextBox[] ChannelTextBoxes => _channelTextBoxes;
    internal Border OriginalSwatch => _originalSwatch;
    internal Border CurrentSwatch => _currentSwatch;

    /// <summary>Applies a channel field's text, as Enter does; out-of-range values clamp, nonsense reverts.</summary>
    internal void CommitChannelText(int channel)
    {
        if (int.TryParse(_channelTextBoxes[channel].Text, out int parsed))
        {
            byte value = (byte)Math.Clamp(parsed, 0, 255);
            Color newColor = channel switch
            {
                0 => Color.FromRgb(value, _color.G, _color.B),
                1 => Color.FromRgb(_color.R, value, _color.B),
                _ => Color.FromRgb(_color.R, _color.G, value),
            };
            if (newColor != _color)
            {
                SetRgb(newColor, isUserEdit: true);
            }
        }
        _channelTextBoxes[channel].Text = GetChannel(_color, channel).ToString();
    }

    private Grid CreateChannelRow(int channel)
    {
        TextBlock label = new TextBlock
        {
            Text = ChannelNames[channel],
            VerticalAlignment = VerticalAlignment.Center,
        };

        ColorSlider slider = new ColorSlider
        {
            ColorModel = ColorModel.Rgba,
            ColorComponent = ColorComponent.Component1 + channel,
            // Show the channel's range at the current color rather than a pure red/green/blue ramp.
            IsPerceptive = false,
            Orientation = Orientation.Horizontal,
            Height = 14,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0),
        };
        slider.ColorChanged += (_, e) =>
        {
            if (!_isSyncing)
            {
                SetRgb(e.NewColor, isUserEdit: true);
            }
        };
        _channelSliders[channel] = slider;

        TextBox textBox = new TextBox
        {
            Width = 40,
            MinHeight = 22,
            Height = 22,
            Padding = new Thickness(4, 0),
            TextAlignment = TextAlignment.Right,
            VerticalContentAlignment = VerticalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        textBox.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CommitChannelText(channel);
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
        textBox.LostFocus += (_, _) => CommitChannelText(channel);
        _channelTextBoxes[channel] = textBox;

        Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("12,*,Auto") };
        Grid.SetColumn(slider, 1);
        Grid.SetColumn(textBox, 2);
        row.Children.Add(label);
        row.Children.Add(slider);
        row.Children.Add(textBox);
        return row;
    }

    private void SetRgb(Color color, bool isUserEdit)
    {
        HsvColor hsv = color.ToHsv();
        // Gray and black carry no hue (or saturation); keep the current ones so the square and hue
        // bar do not jump while the user drags through them.
        double hue = hsv.S == 0 || hsv.V == 0 ? _hsv.H : hsv.H;
        double saturation = hsv.V == 0 ? _hsv.S : hsv.S;
        Apply(HsvColor.FromHsv(hue, saturation, hsv.V), Color.FromRgb(color.R, color.G, color.B), isUserEdit);
    }

    private void SetHsv(HsvColor hsv, bool isUserEdit)
    {
        HsvColor opaque = HsvColor.FromHsv(hsv.H, hsv.S, hsv.V);
        Apply(opaque, opaque.ToRgb(), isUserEdit);
    }

    private void Apply(HsvColor hsv, Color color, bool isUserEdit)
    {
        _hsv = hsv;
        _color = color;

        _isSyncing = true;
        _spectrum.HsvColor = hsv;
        _hueSlider.HsvColor = hsv;
        for (int i = 0; i < 3; i++)
        {
            _channelSliders[i].Color = color;
            if (!_channelTextBoxes[i].IsFocused)
            {
                _channelTextBoxes[i].Text = GetChannel(color, i).ToString();
            }
        }
        _currentSwatch.Background = new SolidColorBrush(color);
        _isSyncing = false;

        if (isUserEdit)
        {
            ColorChanged?.Invoke(this, color);
        }
    }

    private static byte GetChannel(Color color, int channel) => channel switch
    {
        0 => color.R,
        1 => color.G,
        _ => color.B,
    };
}
