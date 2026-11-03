using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Avalonia.Themes;
using Gum.ColorPicking;

namespace Gum.Avalonia.Plugins.VariableGrid;

/// <summary>
/// The Variables tab's color flyout: a hue ring with the saturation/value square inside it, R/G/B
/// sliders with 0-255 fields, an old/new swatch, and a hex field with Copy and Paste. Alpha is not
/// part of the picker: it is a separate Gum variable, and a pasted #RRGGBBAA only uses its RGB.
/// </summary>
public class CompactColorPicker : UserControl
{
    private static readonly string[] ChannelNames = { "R", "G", "B" };

    // The ring is 200 wide at the default zoom; the square sits inside its inner circle with a gap.
    private const double RingSize = 200;
    private const double SquareGap = 4;

    private readonly ColorSpectrum _spectrum;
    private readonly HueRing _hueRing;
    private readonly ColorSlider[] _channelSliders;
    private readonly TextBox[] _channelTextBoxes;
    private readonly TextBox _hexTextBox;
    private readonly Button _copyButton;
    private readonly Button _pasteButton;
    private readonly TextBlock _hexMessage;
    private readonly Border _originalSwatch;
    private readonly Border _currentSwatch;
    private HsvColor _hsv;
    private Color _color;
    private bool _isSyncing;
    private bool _isSettingHexText;

    /// <summary>Builds the picker, initially black.</summary>
    public CompactColorPicker()
    {
        double squareSize = Math.Floor(HueRingGeometry.SquareSide(RingSize * (0.5 - HueRing.ThicknessFraction), SquareGap));
        _spectrum = new ColorSpectrum
        {
            Shape = ColorSpectrumShape.Box,
            Components = ColorSpectrumComponents.SaturationValue,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AvaloniaTreeIcons.FollowBaseFontSize(_spectrum, Layoutable.WidthProperty, squareSize);
        AvaloniaTreeIcons.FollowBaseFontSize(_spectrum, Layoutable.HeightProperty, squareSize);
        _spectrum.PropertyChanged += (_, e) =>
        {
            if (e.Property == ColorSpectrum.HsvColorProperty && !_isSyncing)
            {
                SetHsv(_spectrum.HsvColor, isUserEdit: true);
            }
        };

        _hueRing = new HueRing();
        _hueRing.HueEdited += (_, hue) =>
        {
            if (!_isSyncing)
            {
                SetHsv(HsvColor.FromHsv(hue, _hsv.S, _hsv.V), isUserEdit: true);
            }
        };

        Grid ring = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
        AvaloniaTreeIcons.FollowBaseFontSize(ring, Layoutable.WidthProperty, RingSize);
        AvaloniaTreeIcons.FollowBaseFontSize(ring, Layoutable.HeightProperty, RingSize);
        ring.Children.Add(_hueRing);
        ring.Children.Add(_spectrum);

        StackPanel panel = new StackPanel { Width = 200, Spacing = 2 };
        AvaloniaTreeIcons.FollowBaseFontSize(panel, Layoutable.WidthProperty, 200);
        panel.Children.Add(ring);

        _channelSliders = new ColorSlider[3];
        _channelTextBoxes = new TextBox[3];
        for (int i = 0; i < 3; i++)
        {
            panel.Children.Add(CreateChannelRow(i));
        }

        _originalSwatch = new Border { Width = 32, Height = 16, CornerRadius = new CornerRadius(2, 0, 0, 2) };
        AvaloniaTreeIcons.FollowBaseFontSize(_originalSwatch, Layoutable.WidthProperty, 32);
        AvaloniaTreeIcons.FollowBaseFontSize(_originalSwatch, Layoutable.HeightProperty, 16);
        ToolTip.SetTip(_originalSwatch, "Color when the picker opened");
        _currentSwatch = new Border { Width = 32, Height = 16, CornerRadius = new CornerRadius(0, 2, 2, 0) };
        AvaloniaTreeIcons.FollowBaseFontSize(_currentSwatch, Layoutable.WidthProperty, 32);
        AvaloniaTreeIcons.FollowBaseFontSize(_currentSwatch, Layoutable.HeightProperty, 16);
        ToolTip.SetTip(_currentSwatch, "Current color");
        StackPanel swatches = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
        swatches.Children.Add(_originalSwatch);
        swatches.Children.Add(_currentSwatch);
        panel.Children.Add(swatches);

        _hexTextBox = new TextBox
        {
            MinHeight = 22,
            Height = 22,
            Padding = new Thickness(4, 0),
            VerticalContentAlignment = VerticalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MaxLength = 12,
        };
        AvaloniaTreeIcons.FollowBaseFontSize(_hexTextBox, Layoutable.MinHeightProperty, 22);
        AvaloniaTreeIcons.FollowBaseFontSize(_hexTextBox, Layoutable.HeightProperty, 22);
        ToolTip.SetTip(_hexTextBox, "Hex color: #RGB, #RRGGBB or #RRGGBBAA. Alpha is not used.");
        _hexTextBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
            {
                HandleHexTextChanged();
            }
        };
        _hexTextBox.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CommitHexText();
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
        _hexTextBox.LostFocus += (_, _) => CommitHexText();

        _copyButton = CreateHexButton("Copy", "Copy the color as #RRGGBB");
        _copyButton.Click += async (_, _) => await CopyAsync();
        _pasteButton = CreateHexButton("Paste", "Paste a color from the clipboard");
        _pasteButton.Click += async (_, _) => await PasteAsync();

        Grid hexRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), Margin = new Thickness(0, 3, 0, 0) };
        _copyButton.Margin = new Thickness(4, 0, 0, 0);
        _pasteButton.Margin = new Thickness(4, 0, 0, 0);
        Grid.SetColumn(_copyButton, 1);
        Grid.SetColumn(_pasteButton, 2);
        hexRow.Children.Add(_hexTextBox);
        hexRow.Children.Add(_copyButton);
        hexRow.Children.Add(_pasteButton);
        panel.Children.Add(hexRow);

        // Two lines are reserved so a message appearing does not move the flyout.
        _hexMessage = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11, MinHeight = 28 };
        AvaloniaTreeIcons.FollowBaseFontSize(_hexMessage, TextBlock.FontSizeProperty, 11);
        AvaloniaTreeIcons.FollowBaseFontSize(_hexMessage, Layoutable.MinHeightProperty, 28);
        panel.Children.Add(_hexMessage);

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
    internal HueRing HueRing => _hueRing;
    internal ColorSlider[] ChannelSliders => _channelSliders;
    internal TextBox[] ChannelTextBoxes => _channelTextBoxes;
    internal TextBox HexTextBox => _hexTextBox;
    internal Button CopyButton => _copyButton;
    internal Button PasteButton => _pasteButton;
    internal TextBlock HexMessage => _hexMessage;
    internal Border OriginalSwatch => _originalSwatch;
    internal Border CurrentSwatch => _currentSwatch;

    /// <summary>What the hex message under the field is saying: nothing, a note, the orange alpha warning, or the red error.</summary>
    internal HexMessageKind HexMessageState { get; private set; }

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

    /// <summary>Puts the color as #RRGGBB on the clipboard.</summary>
    internal async Task CopyAsync()
    {
        IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
        {
            return;
        }

        string text = HexColor.Format(_color.R, _color.G, _color.B);
        try
        {
            await clipboard.SetTextAsync(text);
        }
        catch
        {
            ShowHexMessage(HexMessageKind.Error, "The clipboard is not available.");
            return;
        }
        ShowHexMessage(HexMessageKind.Note, $"Copied {text}");
    }

    /// <summary>Reads a color from the clipboard, in any form the hex field accepts.</summary>
    internal async Task PasteAsync()
    {
        IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        string? text = null;
        if (clipboard != null)
        {
            try
            {
                text = await clipboard.TryGetTextAsync();
            }
            catch
            {
                // An unreadable clipboard is the same as one with no color on it.
            }
        }
        ApplyHex(text, fromPaste: true);
    }

    private static Button CreateHexButton(string text, string toolTip)
    {
        Button button = new Button
        {
            Content = text,
            Height = 22,
            MinHeight = 22,
            Padding = new Thickness(8, 0),
            VerticalContentAlignment = VerticalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AvaloniaTreeIcons.FollowBaseFontSize(button, Layoutable.HeightProperty, 22);
        AvaloniaTreeIcons.FollowBaseFontSize(button, Layoutable.MinHeightProperty, 22);
        ToolTip.SetTip(button, toolTip);
        return button;
    }

    private void HandleHexTextChanged()
    {
        if (_isSettingHexText)
        {
            return;
        }

        // Half-typed text is not an error yet; it is judged when it is committed.
        if (_hexTextBox.IsFocused && HexColor.IsIncomplete(_hexTextBox.Text))
        {
            ShowHexMessage(HexMessageKind.None, "");
            return;
        }
        ApplyHex(_hexTextBox.Text, fromPaste: false);
    }

    /// <summary>Normalizes a valid field to #RRGGBB, as Enter does; text that is not a color reverts to the current one.</summary>
    private void CommitHexText()
    {
        HexColorParseResult result = HexColor.Parse(_hexTextBox.Text);
        if (result.IsValid)
        {
            // The alpha warning stays; it is only worth showing next to the text that caused it.
            SetHexText(HexColor.Format(result.Red, result.Green, result.Blue));
        }
        else
        {
            SetHexText(HexColor.Format(_color.R, _color.G, _color.B));
            ShowHexMessage(HexMessageKind.None, "");
        }
    }

    private void ApplyHex(string? text, bool fromPaste)
    {
        HexColorParseResult result = HexColor.Parse(text);
        if (!result.IsValid)
        {
            ShowHexMessage(HexMessageKind.Error,
                fromPaste ? "The clipboard is not a color. Use #RGB, #RRGGBB or #RRGGBBAA." : "Use #RGB, #RRGGBB or #RRGGBBAA.");
            return;
        }

        Color newColor = Color.FromRgb(result.Red, result.Green, result.Blue);
        if (newColor != _color)
        {
            SetRgb(newColor, isUserEdit: true);
        }

        string formatted = HexColor.Format(result.Red, result.Green, result.Blue);
        if (fromPaste)
        {
            SetHexText(formatted);
        }
        else
        {
            // Typed text stays as typed, alpha digits included, next to the warning about them.
            SetHexText(text!);
        }

        if (result.AlphaIgnored)
        {
            ShowHexMessage(HexMessageKind.Warning, "The alpha value is not used. Only the RGB part was applied.");
        }
        else if (fromPaste)
        {
            ShowHexMessage(HexMessageKind.Note, $"Pasted {formatted}");
        }
        else
        {
            ShowHexMessage(HexMessageKind.None, "");
        }
    }

    private void SetHexText(string text)
    {
        _isSettingHexText = true;
        _hexTextBox.Text = text;
        _isSettingHexText = false;
    }

    private void ShowHexMessage(HexMessageKind kind, string text)
    {
        HexMessageState = kind;
        _hexMessage.Text = text;

        switch (kind)
        {
            case HexMessageKind.Error:
                _hexMessage.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Error");
                _hexTextBox.WithThemeResource(BorderBrushProperty, "Frb.Brushes.Error");
                _hexTextBox.WithThemeResource(TemplatedControl.ForegroundProperty, "Frb.Brushes.Error");
                break;
            case HexMessageKind.Warning:
                _hexMessage.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Warning");
                ClearHexFieldError();
                break;
            default:
                _hexMessage.WithThemeResource(TextBlock.ForegroundProperty, "Frb.Brushes.Foreground.Muted");
                ClearHexFieldError();
                break;
        }
    }

    private void ClearHexFieldError()
    {
        _hexTextBox.ClearValue(BorderBrushProperty);
        _hexTextBox.ClearValue(TemplatedControl.ForegroundProperty);
    }

    private Grid CreateChannelRow(int channel)
    {
        TextBlock label = new TextBlock
        {
            TextAlignment = TextAlignment.Left,
            Text = ChannelNames[channel],
            VerticalAlignment = VerticalAlignment.Center,
        };
        // Wide enough for the widest letter at any zoom, so the label column never clips.
        AvaloniaTreeIcons.FollowBaseFontSize(label, Layoutable.WidthProperty, 14);

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
        AvaloniaTreeIcons.FollowBaseFontSize(slider, Layoutable.HeightProperty, 14);
        _channelSliders[channel] = slider;

        EditTrackingTextBox textBox = new EditTrackingTextBox
        {
            Width = 40,
            MinHeight = 22,
            Height = 22,
            Padding = new Thickness(4, 0),
            TextAlignment = TextAlignment.Right,
            VerticalContentAlignment = VerticalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        AvaloniaTreeIcons.FollowBaseFontSize(textBox, Layoutable.WidthProperty, 40);
        AvaloniaTreeIcons.FollowBaseFontSize(textBox, Layoutable.MinHeightProperty, 22);
        AvaloniaTreeIcons.FollowBaseFontSize(textBox, Layoutable.HeightProperty, 22);
        textBox.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CommitChannelText(channel);
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
        textBox.EditCommitRequested += (_, _) => CommitChannelText(channel);
        _channelTextBoxes[channel] = textBox;

        Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
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
        // ring do not jump while the user drags through them.
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
        _hueRing.Hue = hsv.H;
        for (int i = 0; i < 3; i++)
        {
            _channelSliders[i].Color = color;
            if (!_channelTextBoxes[i].IsFocused)
            {
                _channelTextBoxes[i].Text = GetChannel(color, i).ToString();
            }
        }
        _currentSwatch.Background = new SolidColorBrush(color);
        if (!_hexTextBox.IsFocused)
        {
            SetHexText(HexColor.Format(color.R, color.G, color.B));
        }
        // Any change clears a stale note, warning or error; the hex handlers set theirs afterwards.
        ShowHexMessage(HexMessageKind.None, "");
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

/// <summary>What the color picker's hex message is saying.</summary>
internal enum HexMessageKind
{
    /// <summary>Nothing.</summary>
    None,

    /// <summary>A plain note such as "Copied #3D9BD6".</summary>
    Note,

    /// <summary>The orange warning that typed or pasted alpha digits are not used.</summary>
    Warning,

    /// <summary>The red error for text that is not a color.</summary>
    Error,
}
