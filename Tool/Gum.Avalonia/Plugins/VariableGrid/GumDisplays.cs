using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaDataUi.Controls;
using Gum.Controls.DataUi;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;
using DrawingColor = System.Drawing.Color;

namespace Gum.Avalonia.Plugins.VariableGrid;

/// <summary>
/// A toggle-button editor over one of the shared <see cref="VariableGridToggleOptions"/> sets, with
/// the tool's icons. Subclasses pick the set.
/// </summary>
public abstract class GumToggleOptionDisplay : ToggleButtonOptionDisplay
{
    /// <summary>Builds the editor over the subclass's options.</summary>
    protected GumToggleOptionDisplay()
    {
        OptionContentFactory = CreateOptionContent;
        SetOptions(GetToggleOptions());
    }

    /// <summary>The shared option sets.</summary>
    protected static VariableGridToggleOptions ToggleOptions => Locator.GetRequiredService<VariableGridToggleOptions>();

    /// <summary>Whether the options depend on the selection and are re-read on every refresh.</summary>
    protected virtual bool RefreshOptionsOnRefresh => false;

    /// <summary>The options to show.</summary>
    protected abstract ToggleButtonOption[] GetToggleOptions();

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (RefreshOptionsOnRefresh)
        {
            SetOptions(GetToggleOptions());
        }
        base.Refresh(forceRefreshEvenIfFocused);
    }

    private static Control? CreateOptionContent(ToggleButtonOption option)
    {
        if (option.GumIconName != null)
        {
            Control? icon = Locator.GetRequiredService<GumIconRegistry>().CreateIcon(option.GumIconName);
            if (icon != null)
            {
                return icon;
            }
        }

        if (option.ImagePath != null)
        {
            string path = System.IO.Path.Combine(AppContext.BaseDirectory, option.ImagePath);
            if (File.Exists(path))
            {
                return new Image { Source = new Bitmap(path), Width = 20, Height = 20 };
            }
        }

        return null;
    }
}

/// <summary>X units toggles.</summary>
public class XUnitsDisplay : GumToggleOptionDisplay { protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.XUnits; }

/// <summary>Y units toggles.</summary>
public class YUnitsDisplay : GumToggleOptionDisplay { protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.YUnits; }

/// <summary>X origin toggles.</summary>
public class XOriginDisplay : GumToggleOptionDisplay { protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.XOrigin; }

/// <summary>Y origin toggles, minus the origins the selected standard element excludes.</summary>
public class YOriginDisplay : GumToggleOptionDisplay { protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.GetYOrigins(); }

/// <summary>Width units toggles, re-read per selection since standard elements exclude some.</summary>
public class WidthUnitsDisplay : GumToggleOptionDisplay
{
    /// <inheritdoc/>
    protected override bool RefreshOptionsOnRefresh => true;

    /// <inheritdoc/>
    protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.GetWidthUnits();
}

/// <summary>Height units toggles, re-read per selection since standard elements exclude some.</summary>
public class HeightUnitsDisplay : GumToggleOptionDisplay
{
    /// <inheritdoc/>
    protected override bool RefreshOptionsOnRefresh => true;

    /// <inheritdoc/>
    protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.GetHeightUnits();
}

/// <summary>Text horizontal alignment toggles.</summary>
public class TextHorizontalAlignmentDisplay : GumToggleOptionDisplay { protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.TextHorizontalAlignment; }

/// <summary>Text vertical alignment toggles.</summary>
public class TextVerticalAlignmentDisplay : GumToggleOptionDisplay { protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.TextVerticalAlignment; }

/// <summary>Children layout toggles.</summary>
public class ChildrenLayoutDisplay : GumToggleOptionDisplay { protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.ChildrenLayout; }

/// <summary>Horizontal text overflow toggles.</summary>
public class TextOverflowHorizontalModeDisplay : GumToggleOptionDisplay { protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.TextOverflowHorizontalMode; }

/// <summary>Vertical text overflow toggles.</summary>
public class TextOverflowVerticalModeDisplay : GumToggleOptionDisplay { protected override ToggleButtonOption[] GetToggleOptions() => ToggleOptions.TextOverflowVerticalMode; }

/// <summary>
/// The color composite's editor: a swatch that opens red, green, and blue sliders (intermediate
/// writes while dragging, a full write on release), and a hex field (RRGGBB or RRGGBBAA; alpha is
/// ignored and kept).
/// </summary>
public class ColorDisplay : DataUiDisplayBase
{
    private static readonly IBrush InvalidHexBorder = Brushes.Red;

    private readonly TextBlock _label;
    private readonly Border _swatch;
    private readonly TextBox _hexTextBox;
    private readonly Slider[] _channelSliders;
    private readonly TextBlock _hint;
    private DrawingColor _current;
    private Type? _propertyType;
    private bool _isSetting;
    private bool _isSyncing;
    private bool _needsFullCommit;

    /// <summary>Builds the editor.</summary>
    public ColorDisplay()
    {
        _label = new TextBlock { MinWidth = 100, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0) };
        _swatch = new Border { Height = 18, MinWidth = 60, CornerRadius = new CornerRadius(2), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };

        StackPanel sliders = new StackPanel { Spacing = 4, Width = 220 };
        _channelSliders = new Slider[3];
        string[] names = { "R", "G", "B" };
        for (int i = 0; i < 3; i++)
        {
            Slider slider = new Slider { Minimum = 0, Maximum = 255, SmallChange = 1, LargeChange = 16 };
            int channel = i;
            slider.PropertyChanged += (_, e) =>
            {
                if (e.Property == RangeBase.ValueProperty && !_isSyncing)
                {
                    HandleSliderMoved(channel, slider.Value);
                }
            };
            slider.AddHandler(PointerReleasedEvent, (_, _) => CommitPendingFull(), RoutingStrategies.Bubble, handledEventsToo: true);
            _channelSliders[i] = slider;

            Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("16,*") };
            Grid.SetColumn(slider, 1);
            row.Children.Add(new TextBlock { Text = names[i], VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(slider);
            sliders.Children.Add(row);
        }

        Button swatchButton = new Button
        {
            Content = _swatch,
            Padding = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Flyout = new Flyout { Content = sliders },
        };

        _hexTextBox = new TextBox { Width = 76, VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(_hexTextBox, "Type or paste a hex color (RRGGBB or RRGGBBAA). Alpha is ignored.");
        _hexTextBox.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CommitHexText();
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
        _hexTextBox.LostFocus += (_, _) => CommitHexText();
        _hexTextBox.TextChanged += (_, _) => RefreshHexValidation();

        StackPanel hex = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 0, 0) };
        hex.Children.Add(new TextBlock { Text = "#", VerticalAlignment = VerticalAlignment.Center });
        hex.Children.Add(_hexTextBox);

        _hint = CreateHintTextBlock();

        Grid grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), Margin = new Thickness(0, 3) };
        Grid.SetColumn(swatchButton, 1);
        Grid.SetColumn(hex, 2);
        grid.Children.Add(_label);
        grid.Children.Add(swatchButton);
        grid.Children.Add(hex);

        StackPanel panel = new StackPanel();
        panel.Children.Add(grid);
        panel.Children.Add(_hint);
        Content = panel;
        AttachContextMenu(grid);
    }

    /// <summary>The hex field, for tests.</summary>
    internal TextBox HexTextBox => _hexTextBox;

    /// <summary>The red, green, and blue sliders, for tests.</summary>
    internal IReadOnlyList<Slider> ChannelSliders => _channelSliders;

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        // A multi-select write refreshes this control mid-write; that must not reset the UI.
        if (_isSetting || InstanceMember == null)
        {
            return;
        }

        SuppressSettingProperty = true;
        if (this.HasEnoughInformationToWork())
        {
            _propertyType = this.GetPropertyType();
        }
        if (this.TryGetValueOnInstance(out object valueOnInstance) && valueOnInstance != null)
        {
            TrySetValueOnUi(valueOnInstance);
        }
        RefreshIsEnabled();
        _label.Text = InstanceMember.DisplayName;
        RefreshHint(_hint);
        SuppressSettingProperty = false;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object valueOnInstance)
    {
        if (valueOnInstance is not DrawingColor color)
        {
            return ApplyValueResult.NotSupported;
        }

        _current = color;
        SyncUiFromCurrent(updateHex: !_hexTextBox.IsFocused);
        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? value)
    {
        value = null;
        if (!this.HasEnoughInformationToWork() || _propertyType == null)
        {
            return ApplyValueResult.NotEnoughInformation;
        }
        if (_propertyType != typeof(DrawingColor))
        {
            return ApplyValueResult.NotSupported;
        }

        value = _current;
        return ApplyValueResult.Success;
    }

    /// <summary>Sets one channel from its slider as an intermediate write, as dragging does.</summary>
    internal void HandleSliderMoved(int channel, double value)
    {
        byte channelValue = (byte)Math.Clamp((int)Math.Round(value), 0, 255);
        _current = DrawingColor.FromArgb(_current.A,
            channel == 0 ? channelValue : _current.R,
            channel == 1 ? channelValue : _current.G,
            channel == 2 ? channelValue : _current.B);
        SyncUiFromCurrent(updateHex: true, updateSliders: false);
        Commit(SetPropertyCommitType.Intermediate);
        _needsFullCommit = true;
    }

    /// <summary>Writes the pending slider change as a full commit (one undo), as releasing does.</summary>
    internal void CommitPendingFull()
    {
        if (_needsFullCommit)
        {
            _needsFullCommit = false;
            Commit(SetPropertyCommitType.Full);
        }
    }

    /// <summary>Applies the hex field, keeping alpha, as Enter does; invalid text reverts.</summary>
    internal void CommitHexText()
    {
        if (HexColorParser.TryParse(_hexTextBox.Text, out byte r, out byte g, out byte b))
        {
            DrawingColor newColor = DrawingColor.FromArgb(_current.A, r, g, b);
            if (newColor != _current)
            {
                _current = newColor;
                SyncUiFromCurrent(updateHex: false);
                Commit(SetPropertyCommitType.Full);
            }
        }

        SetHexText(HexColorParser.ToHexRgb(_current.R, _current.G, _current.B));
    }

    private void Commit(SetPropertyCommitType commitType)
    {
        if (SuppressSettingProperty)
        {
            return;
        }

        _isSetting = true;
        if (TryGetValueOnUi(out object? value) == ApplyValueResult.Success &&
            this.TrySetValueOnInstance(value!, commitType) == ApplyValueResult.NotSupported)
        {
            IsEnabled = false;
        }
        _isSetting = false;
    }

    private void SyncUiFromCurrent(bool updateHex, bool updateSliders = true)
    {
        _swatch.Background = new SolidColorBrush(Color.FromRgb(_current.R, _current.G, _current.B));
        if (updateSliders)
        {
            _isSyncing = true;
            _channelSliders[0].Value = _current.R;
            _channelSliders[1].Value = _current.G;
            _channelSliders[2].Value = _current.B;
            _isSyncing = false;
        }
        if (updateHex)
        {
            SetHexText(HexColorParser.ToHexRgb(_current.R, _current.G, _current.B));
        }
    }

    private void SetHexText(string text)
    {
        _isSyncing = true;
        _hexTextBox.Text = text;
        _isSyncing = false;
        RefreshHexValidation();
    }

    private void RefreshHexValidation()
    {
        if (_isSyncing)
        {
            return;
        }

        // Empty is neutral so clearing the field to retype is not flagged.
        bool isNeutral = string.IsNullOrWhiteSpace(_hexTextBox.Text) || HexColorParser.TryParse(_hexTextBox.Text, out _, out _, out _);
        if (isNeutral)
        {
            _hexTextBox.ClearValue(BorderBrushProperty);
        }
        else
        {
            _hexTextBox.BorderBrush = InvalidHexBorder;
        }
    }
}

/// <summary>
/// The corner-radius composite's editor: one uniform field while linked, four per-corner fields once
/// unlinked with the chain button. Dragging a field's label scrubs it (whole numbers, floor of 0).
/// </summary>
public class CornerRadiusDisplay : DataUiDisplayBase
{
    private readonly CornerRadiusDisplayLogic _logic;
    private readonly LabelDragScrubLogic _scrubLogic;
    private readonly TextBlock _label;
    private readonly TextBox _uniformTextBox;
    private readonly Grid _cornersGrid;
    private readonly TextBox[] _cornerTextBoxes;
    private readonly Button _linkButton;
    private readonly TextBlock _hint;
    private readonly Dictionary<Control, TextBox> _dragTargets;
    private CornerRadiusComposite _current;
    private bool _isLinked;
    private bool _isSyncing;
    private TextBox? _draggingTextBox;
    private Point? _dragLastPosition;
    private Point? _dragPressedPosition;

    /// <summary>Builds the editor.</summary>
    public CornerRadiusDisplay()
    {
        _logic = new CornerRadiusDisplayLogic();
        _scrubLogic = new LabelDragScrubLogic();
        _isLinked = true;
        _dragTargets = new Dictionary<Control, TextBox>();

        _label = new TextBlock { MinWidth = 100, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0), Cursor = new Cursor(StandardCursorType.SizeWestEast) };
        _uniformTextBox = CreateField();
        AddDragTarget(_label, _uniformTextBox);

        string[] cornerNames = { "TL", "TR", "BL", "BR" };
        string[] cornerTips = { "Top-left corner radius", "Top-right corner radius", "Bottom-left corner radius", "Bottom-right corner radius" };
        _cornerTextBoxes = new TextBox[4];
        _cornersGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            IsVisible = false,
            Margin = new Thickness(0, 2),
        };
        for (int i = 0; i < 4; i++)
        {
            TextBlock cornerLabel = new TextBlock
            {
                Text = cornerNames[i],
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 2, 0),
                Cursor = new Cursor(StandardCursorType.SizeWestEast),
            };
            ToolTip.SetTip(cornerLabel, cornerTips[i]);
            TextBox field = CreateField();
            field.Margin = new Thickness(0, 0, i % 2 == 0 ? 6 : 0, i < 2 ? 2 : 0);
            _cornerTextBoxes[i] = field;
            AddDragTarget(cornerLabel, field);

            Grid.SetRow(cornerLabel, i / 2);
            Grid.SetColumn(cornerLabel, (i % 2) * 2);
            Grid.SetRow(field, i / 2);
            Grid.SetColumn(field, (i % 2) * 2 + 1);
            _cornersGrid.Children.Add(cornerLabel);
            _cornersGrid.Children.Add(field);
        }

        _linkButton = new Button { Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        _linkButton.Click += (_, _) => ToggleLinked();
        _hint = CreateHintTextBlock();

        Grid grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        Grid.SetColumn(_uniformTextBox, 1);
        Grid.SetColumn(_cornersGrid, 1);
        Grid.SetColumn(_linkButton, 2);
        grid.Children.Add(_label);
        grid.Children.Add(_uniformTextBox);
        grid.Children.Add(_cornersGrid);
        grid.Children.Add(_linkButton);

        StackPanel panel = new StackPanel();
        panel.Children.Add(grid);
        panel.Children.Add(_hint);
        Content = panel;
        AttachContextMenu(grid);
        SetLinkedState(true);
    }

    /// <summary>The uniform field, for tests.</summary>
    internal TextBox UniformTextBox => _uniformTextBox;

    /// <summary>The TL, TR, BL, BR fields, for tests.</summary>
    internal IReadOnlyList<TextBox> CornerTextBoxes => _cornerTextBoxes;

    /// <summary>Whether all corners share the uniform radius.</summary>
    internal bool IsLinked => _isLinked;

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        if (this.TryGetValueOnInstance(out object valueOnInstance) && valueOnInstance != null)
        {
            TrySetValueOnUi(valueOnInstance);
        }
        _label.Text = InstanceMember.DisplayName;
        IsEnabled = !InstanceMember.IsReadOnly;
        RefreshHint(_hint);
        RefreshBackgrounds();
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object valueOnInstance)
    {
        if (valueOnInstance is not CornerRadiusComposite composite)
        {
            return ApplyValueResult.NotSupported;
        }

        _isSyncing = true;
        try
        {
            _current = composite;
            _uniformTextBox.Text = _logic.FormatFloat(composite.Uniform);
            _cornerTextBoxes[0].Text = _logic.FormatNullableFloat(composite.TopLeft);
            _cornerTextBoxes[1].Text = _logic.FormatNullableFloat(composite.TopRight);
            _cornerTextBoxes[2].Text = _logic.FormatNullableFloat(composite.BottomLeft);
            _cornerTextBoxes[3].Text = _logic.FormatNullableFloat(composite.BottomRight);
            SetLinkedState(composite.IsLinked);
        }
        finally
        {
            _isSyncing = false;
        }

        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? value)
    {
        value = _logic.Compose(_isLinked, _uniformTextBox.Text, _cornerTextBoxes[0].Text, _cornerTextBoxes[1].Text,
            _cornerTextBoxes[2].Text, _cornerTextBoxes[3].Text, _current);
        return ApplyValueResult.Success;
    }

    /// <summary>Toggles linked and commits; unlinking seeds the corners with the uniform radius.</summary>
    internal void ToggleLinked()
    {
        bool wasLinked = _isLinked;
        SetLinkedState(!wasLinked);

        if (wasLinked)
        {
            // Seeding keeps the visual radius until a corner is edited.
            foreach (TextBox corner in _cornerTextBoxes)
            {
                corner.Text = _uniformTextBox.Text;
            }
        }

        Commit(SetPropertyCommitType.Full);
    }

    /// <summary>Commits the fields, as Enter or focus loss does.</summary>
    internal void Commit(SetPropertyCommitType commitType = SetPropertyCommitType.Full)
    {
        if (_isSyncing)
        {
            return;
        }

        if (TryGetValueOnUi(out object? value) == ApplyValueResult.Success && value != null)
        {
            this.TrySetValueOnInstance(value, commitType);
        }
    }

    private TextBox CreateField()
    {
        TextBox field = new TextBox { VerticalAlignment = VerticalAlignment.Center };
        field.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Commit();
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
        field.LostFocus += (_, _) => Commit();
        return field;
    }

    private void SetLinkedState(bool isLinked)
    {
        _isLinked = isLinked;
        _uniformTextBox.IsVisible = isLinked;
        _cornersGrid.IsVisible = !isLinked;
        string iconName = isLinked ? "ChainLinked" : "ChainUnlinked";
        _linkButton.Content = Locator.GetRequiredService<GumIconRegistry>().CreateIcon(iconName, 16)
            ?? (object)(isLinked ? "Linked" : "Unlinked");
        ToolTip.SetTip(_linkButton, isLinked
            ? "Linked - all corners share Corner Radius. Click to set corners independently."
            : "Unlinked - click to use one Corner Radius for all corners again.");
    }

    private void RefreshBackgrounds()
    {
        if (InstanceMember is CompositeInstanceMember composite && composite.ChannelMembers.Count == 5)
        {
            DataUiValueStateBrushes.ApplyBackground(_uniformTextBox, composite.ChannelMembers[0].ValueState);
            for (int i = 0; i < 4; i++)
            {
                DataUiValueStateBrushes.ApplyBackground(_cornerTextBoxes[i], composite.ChannelMembers[i + 1].ValueState);
            }
        }
        else if (InstanceMember != null)
        {
            // A multi-select wrapper only has the aggregate state.
            DataUiValueStateBrushes.ApplyBackground(_uniformTextBox, InstanceMember.ValueState);
            foreach (TextBox corner in _cornerTextBoxes)
            {
                DataUiValueStateBrushes.ApplyBackground(corner, InstanceMember.ValueState);
            }
        }
    }

    private void AddDragTarget(Control label, TextBox target)
    {
        _dragTargets[label] = target;
        label.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(label).Properties.IsLeftButtonPressed)
            {
                return;
            }
            _draggingTextBox = target;
            _dragLastPosition = e.GetPosition(this);
            _dragPressedPosition = _dragLastPosition;
            _scrubLogic.Begin(_logic.ParseFloat(target.Text) ?? _current.Uniform);
            e.Pointer.Capture(label);
            e.Handled = true;
        };
        label.PointerMoved += (_, e) =>
        {
            if (_dragLastPosition == null || _draggingTextBox == null)
            {
                return;
            }
            Point position = e.GetPosition(this);
            double difference = position.X - _dragLastPosition.Value.X;
            _dragLastPosition = position;
            if (difference != 0)
            {
                ScrubDraggedField(difference);
            }
        };
        label.PointerReleased += (_, e) =>
        {
            if (_draggingTextBox != null && _dragPressedPosition != null && e.GetPosition(this).X != _dragPressedPosition.Value.X)
            {
                Commit(SetPropertyCommitType.Full);
            }
            _draggingTextBox = null;
            _dragLastPosition = null;
            _dragPressedPosition = null;
            e.Pointer.Capture(null);
        };
    }

    private void ScrubDraggedField(double difference)
    {
        // Sticks at the 0 floor while scrubbing rather than showing a negative value that only snaps
        // back once the commit round-trips through Decompose's clamp.
        double rounded = _scrubLogic.ApplyDelta(difference, changeMultiplier: 1m, rounding: 1m, min: 0m, max: null);
        _draggingTextBox!.Text = _logic.FormatFloat((float)rounded);
        Commit(SetPropertyCommitType.Intermediate);
    }
}

/// <summary>A row with a button that removes its variable from the category; the member's setter does the removal.</summary>
public class VariableRemoveButton : DataUiDisplayBase
{
    private readonly TextBlock _name;

    /// <summary>Builds the row.</summary>
    public VariableRemoveButton()
    {
        Button remove = new Button { Content = "Remove", Margin = new Thickness(4, 0, 0, 0) };
        remove.Click += (_, _) => Remove();
        _name = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };

        StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2) };
        panel.Children.Add(remove);
        panel.Children.Add(_name);
        Content = panel;
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        _name.Text = InstanceMember?.Name;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? result)
    {
        result = null;
        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object value) => ApplyValueResult.Success;

    /// <summary>Removes the variable, as clicking does.</summary>
    internal void Remove() => this.TrySetValueOnInstance();
}
