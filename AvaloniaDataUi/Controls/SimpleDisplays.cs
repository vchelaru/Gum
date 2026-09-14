using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi.Controls;

/// <summary>A check box for a <see cref="bool"/>; its text turns green for a default value.</summary>
public class CheckBoxDisplay : DataUiDisplayBase
{
    private readonly CheckBox _checkBox;
    private readonly TextBlock _hint;

    /// <summary>Builds the displayer.</summary>
    public CheckBoxDisplay()
    {
        _checkBox = new CheckBox { Margin = new Thickness(4), VerticalAlignment = VerticalAlignment.Center };
        _checkBox.IsCheckedChanged += HandleCheckedChanged;
        _hint = CreateHintTextBlock();

        StackPanel panel = new StackPanel();
        panel.Children.Add(_checkBox);
        panel.Children.Add(_hint);
        Content = panel;
        AttachContextMenu(_checkBox);
    }

    /// <summary>The check box, for tests.</summary>
    internal CheckBox CheckBox => _checkBox;

    /// <inheritdoc/>
    protected override void OnInstanceMemberChanged()
    {
        _checkBox.ClearValue(ForegroundProperty);
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        SuppressSettingProperty = true;

        if (this.TryGetValueOnInstance(out object valueOnInstance))
        {
            bool wasSet = valueOnInstance != null && TrySetValueOnUi(valueOnInstance) == ApplyValueResult.Success;
            if (!wasSet)
            {
                _checkBox.IsChecked = false;
            }
        }
        _checkBox.Content = InstanceMember.DisplayName;
        RefreshHint(_hint);
        RefreshForeground();
        RefreshIsEnabled();

        SuppressSettingProperty = false;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object valueOnInstance)
    {
        if (valueOnInstance is bool asBool)
        {
            _checkBox.IsChecked = asBool;
            return ApplyValueResult.Success;
        }
        return ApplyValueResult.NotSupported;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? value)
    {
        value = _checkBox.IsChecked;
        return ApplyValueResult.Success;
    }

    private void HandleCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (!SuppressSettingProperty)
        {
            this.TrySetValueOnInstance();
            RefreshForeground();
        }
    }

    private void RefreshForeground()
    {
        // The grid's OverridesIsDefaultStyling is inherited, so it can only be read once the box is in the tree.
        if (DataUiValueStateBrushes.DeferUntilInTree(_checkBox, RefreshForeground))
        {
            return;
        }
        if (DataUiGrid.GetOverridesIsDefaultStyling(_checkBox))
        {
            _checkBox.ClearValue(ForegroundProperty);
            return;
        }

        switch (InstanceMember?.ValueState)
        {
            case DataUiValueState.Default:
                _checkBox.Foreground = Brushes.Green;
                break;
            case DataUiValueState.Indeterminate:
                _checkBox.Foreground = Brushes.LightGray;
                break;
            default:
                _checkBox.ClearValue(ForegroundProperty);
                break;
        }
    }
}

/// <summary>True / False / None radio buttons for a nullable <see cref="bool"/>.</summary>
public class NullableBoolDisplay : DataUiDisplayBase
{
    private readonly TextBlock _header;
    private readonly RadioButton _trueButton;
    private readonly RadioButton _falseButton;
    private readonly RadioButton _nullButton;
    private readonly TextBlock _hint;

    /// <summary>Builds the displayer.</summary>
    public NullableBoolDisplay()
    {
        string group = Guid.NewGuid().ToString("N");
        _header = new TextBlock { Margin = new Thickness(4, 4, 4, 0), FontWeight = FontWeight.Medium };
        _trueButton = new RadioButton { Content = "True", GroupName = group, Margin = new Thickness(8, 0, 0, 0) };
        _falseButton = new RadioButton { Content = "False", GroupName = group, Margin = new Thickness(8, 0, 0, 0) };
        _nullButton = new RadioButton { Content = "None", GroupName = group, Margin = new Thickness(8, 0, 0, 0) };
        foreach (RadioButton button in new[] { _trueButton, _falseButton, _nullButton })
        {
            button.IsCheckedChanged += HandleRadioChanged;
        }
        _hint = CreateHintTextBlock();

        StackPanel panel = new StackPanel();
        panel.Children.Add(_header);
        panel.Children.Add(_trueButton);
        panel.Children.Add(_falseButton);
        panel.Children.Add(_nullButton);
        panel.Children.Add(_hint);
        Content = panel;
        AttachContextMenu(panel);
    }

    /// <summary>The text of the true option.</summary>
    public string TrueText { get => _trueButton.Content?.ToString() ?? string.Empty; set => _trueButton.Content = value; }

    /// <summary>The text of the false option.</summary>
    public string FalseText { get => _falseButton.Content?.ToString() ?? string.Empty; set => _falseButton.Content = value; }

    /// <summary>The text of the no-value option.</summary>
    public string NullText { get => _nullButton.Content?.ToString() ?? string.Empty; set => _nullButton.Content = value; }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        SuppressSettingProperty = true;
        if (this.TryGetValueOnInstance(out object valueOnInstance))
        {
            TrySetValueOnUi(valueOnInstance);
        }
        RefreshHint(_hint);
        _header.Text = InstanceMember.DisplayName;
        RefreshIsEnabled();
        SuppressSettingProperty = false;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object valueOnInstance)
    {
        switch (valueOnInstance as bool?)
        {
            case true:
                _trueButton.IsChecked = true;
                break;
            case false:
                _falseButton.IsChecked = true;
                break;
            default:
                _nullButton.IsChecked = true;
                break;
        }
        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? value)
    {
        value = _trueButton.IsChecked == true ? true
            : _falseButton.IsChecked == true ? false
            : null;
        return ApplyValueResult.Success;
    }

    private void HandleRadioChanged(object? sender, RoutedEventArgs e)
    {
        if (!SuppressSettingProperty && sender is RadioButton { IsChecked: true })
        {
            this.TrySetValueOnInstance();
        }
    }
}

/// <summary>
/// A drop-down of the member's custom options or enum values (see <see cref="ComboBoxDisplayLogic"/>);
/// editable when <see cref="IsEditable"/>, committing on Enter or focus loss.
/// </summary>
public class ComboBoxDisplay : DataUiDisplayBase
{
    private readonly ComboBoxDisplayLogic _logic;
    private readonly Grid _grid;
    private readonly TextBlock _label;
    private readonly ComboBox _comboBox;
    private readonly TextBlock _hint;
    private Type? _propertyType;
    private bool _isInSelectionChanged;

    /// <summary>Builds the displayer.</summary>
    public ComboBoxDisplay()
    {
        _logic = new ComboBoxDisplayLogic();
        _label = new TextBlock { Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        _comboBox = new ComboBox { MinWidth = 60, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center };
        _comboBox.SelectionChanged += HandleSelectionChanged;
        _comboBox.LostFocus += (_, _) =>
        {
            if (IsEditable)
            {
                HandleChange();
            }
        };
        _comboBox.AddHandler(KeyDownEvent, HandlePreviewKeyDown, RoutingStrategies.Tunnel);
        _hint = CreateHintTextBlock();

        _grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("100,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
        };
        Grid.SetColumn(_comboBox, 1);
        Grid.SetRow(_hint, 1);
        Grid.SetColumnSpan(_hint, 2);
        _grid.Children.Add(_label);
        _grid.Children.Add(_comboBox);
        _grid.Children.Add(_hint);
        Content = _grid;

        AttachContextMenu(_label);
        AttachContextMenu(_comboBox);
    }

    /// <summary>Whether the user can type a value that is not in the list.</summary>
    public bool IsEditable
    {
        get => _comboBox.IsEditable;
        set => _comboBox.IsEditable = value;
    }

    /// <summary>The drop-down, for tests.</summary>
    internal ComboBox ComboBox => _comboBox;

    /// <inheritdoc/>
    protected override void OnInstanceMemberChanged()
    {
        _grid.ColumnDefinitions[0].Width = new GridLength(InstanceMember?.FirstGridLength ?? 100);
        _comboBox.ClearValue(TemplatedControl.BackgroundProperty);
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        if (this.HasEnoughInformationToWork())
        {
            _propertyType = this.GetPropertyType();
            PopulateItems();
        }

        if (this.TryGetValueOnInstance(out object valueOnInstance))
        {
            if (valueOnInstance != null)
            {
                TrySetValueOnUi(valueOnInstance);
            }
            else
            {
                SuppressSettingProperty = true;
                _comboBox.SelectedItem = _logic.GetItemToSelect(null, _propertyType);
                if (IsEditable)
                {
                    _comboBox.Text = _comboBox.SelectedItem?.ToString();
                }
                SuppressSettingProperty = false;
            }
        }

        RefreshHint(_hint);
        RefreshBackground();
        _label.Text = InstanceMember.DisplayName;
        RefreshIsEnabled();
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object valueOnInstance)
    {
        SuppressSettingProperty = true;
        object? itemToSelect = _logic.GetItemToSelect(valueOnInstance, _propertyType);
        _comboBox.SelectedItem = itemToSelect;
        if (IsEditable)
        {
            _comboBox.Text = itemToSelect?.ToString();
        }
        SuppressSettingProperty = false;

        RefreshBackground();
        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? value)
    {
        value = IsEditable ? _comboBox.Text : _comboBox.SelectedItem;
        return ApplyValueResult.Success;
    }

    private void PopulateItems()
    {
        List<object> options = _logic.GetOptions(InstanceMember, _propertyType).ToList();

        bool same = _comboBox.Items.Count == options.Count;
        for (int i = 0; same && i < options.Count; i++)
        {
            same = options[i].Equals(_comboBox.Items[i]);
        }

        if (!same)
        {
            SuppressSettingProperty = true;
            _comboBox.Items.Clear();
            foreach (object option in options)
            {
                _comboBox.Items.Add(option);
            }
            SuppressSettingProperty = false;
        }
    }

    private void HandleSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isInSelectionChanged || SuppressSettingProperty)
        {
            return;
        }

        _isInSelectionChanged = true;
        // Typed text that matches no item clears the selection; keep the typed text then.
        if (IsEditable && _comboBox.SelectedItem != null)
        {
            _comboBox.Text = _comboBox.SelectedItem.ToString();
        }

        // Typing that happens to match an item waits for Enter or focus loss.
        bool shouldApplyImmediately = !IsEditable || _comboBox.IsDropDownOpen;
        if (shouldApplyImmediately)
        {
            HandleChange();
        }
        _isInSelectionChanged = false;
    }

    private void HandlePreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && IsEditable)
        {
            HandleChange();
            e.Handled = true;
        }
        else if (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            // The combo's own undo can restore a stale value (Gum #658); the tool's undo owns Ctrl+Z.
            e.Handled = true;
        }
    }

    private void HandleChange()
    {
        this.TrySetValueOnInstance();
        RefreshBackground();
    }

    private void RefreshBackground()
    {
        // Only the default state is tinted, matching the WPF combo box.
        DataUiValueStateBrushes.ApplyBackground(_comboBox,
            InstanceMember?.IsDefault == true ? DataUiValueState.Default : DataUiValueState.Custom);
    }
}

/// <summary>A <see cref="ComboBoxDisplay"/> that is always editable.</summary>
public class EditableComboBoxDisplay : ComboBoxDisplay
{
    /// <summary>Builds the displayer.</summary>
    public EditableComboBoxDisplay()
    {
        IsEditable = true;
    }
}

/// <summary>
/// A slider plus a text field for a bounded number; the text field is the source of truth, and the
/// slider commits when the pointer is released.
/// </summary>
public class SliderDisplay : DataUiDisplayBase, ISetDefaultable
{
    private readonly TextBoxDisplayLogic _textLogic;
    private readonly SliderDisplayLogic _sliderLogic;
    private readonly Grid _grid;
    private readonly TextBlock _label;
    private readonly Slider _slider;
    private readonly TextBox _textBox;
    private readonly TextBlock _minValueText;
    private readonly TextBlock _maxValueText;
    private readonly TextBlock _hint;
    private double _minValue;
    private double _maxValue;
    private bool _isSettingSliderValueProgrammatically;

    /// <summary>Builds the displayer.</summary>
    public SliderDisplay()
    {
        _sliderLogic = new SliderDisplayLogic();
        _label = new TextBlock { MinWidth = 100, Padding = new Thickness(4), VerticalAlignment = VerticalAlignment.Top, TextWrapping = TextWrapping.Wrap };
        _slider = new Slider { MinWidth = 60, VerticalAlignment = VerticalAlignment.Center };
        _slider.PropertyChanged += HandleSliderPropertyChanged;
        _slider.AddHandler(PointerReleasedEvent, (_, _) => HandleSliderCommitted(), RoutingStrategies.Bubble, handledEventsToo: true);
        _textBox = new TextBox { Margin = new Thickness(3, 1, 1, 1), VerticalAlignment = VerticalAlignment.Center };
        _textBox.LostFocus += HandleTextBoxLostFocus;
        _minValueText = new TextBlock { FontSize = 10, IsHitTestVisible = false };
        _maxValueText = new TextBlock { FontSize = 10, IsHitTestVisible = false, HorizontalAlignment = HorizontalAlignment.Right };
        _hint = CreateHintTextBlock();

        Grid minMax = new Grid();
        minMax.Children.Add(_minValueText);
        minMax.Children.Add(_maxValueText);

        _grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("100,*,65"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
        };
        Grid.SetRowSpan(_label, 2);
        Grid.SetColumn(_slider, 1);
        Grid.SetColumn(_textBox, 2);
        Grid.SetColumn(minMax, 1);
        Grid.SetRow(minMax, 1);
        Grid.SetRow(_hint, 2);
        Grid.SetColumnSpan(_hint, 3);
        _grid.Children.Add(_label);
        _grid.Children.Add(_slider);
        _grid.Children.Add(_textBox);
        _grid.Children.Add(minMax);
        _grid.Children.Add(_hint);
        Content = _grid;

        _textLogic = AvaloniaDataUiTextBox.CreateLogic(this, _textBox);
        MaxValue = 100;
        MinValue = 0;

        AttachContextMenu(_label);
        AttachContextMenu(_slider);
        AttachContextMenu(_textBox);
    }

    /// <summary>The largest value the slider reaches (before <see cref="DisplayedValueMultiplier"/>).</summary>
    public double MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            RefreshMinAndMaxValues();
        }
    }

    /// <summary>The smallest value the slider reaches (before <see cref="DisplayedValueMultiplier"/>).</summary>
    public double MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            RefreshMinAndMaxValues();
        }
    }

    /// <inheritdoc cref="SliderDisplayLogic.DecimalPointsFromSlider"/>
    public int DecimalPointsFromSlider
    {
        get => _sliderLogic.DecimalPointsFromSlider;
        set => _sliderLogic.DecimalPointsFromSlider = value;
    }

    /// <inheritdoc cref="SliderDisplayLogic.DisplayedValueMultiplier"/>
    public double DisplayedValueMultiplier
    {
        get => _sliderLogic.DisplayedValueMultiplier;
        set
        {
            _sliderLogic.DisplayedValueMultiplier = value;
            RefreshMinAndMaxValues();
        }
    }

    /// <summary>Whether the minimum and maximum are shown under the slider.</summary>
    public bool IsShowingMinAndMax
    {
        get => _minValueText.IsVisible;
        set
        {
            _minValueText.IsVisible = value;
            _maxValueText.IsVisible = value;
        }
    }

    /// <summary>The slider, for tests.</summary>
    internal Slider Slider => _slider;

    /// <summary>The text field, for tests.</summary>
    internal TextBox TextBox => _textBox;

    /// <inheritdoc/>
    protected override void OnInstanceMemberChanged()
    {
        _textLogic.InstanceMember = InstanceMember;
        _grid.ColumnDefinitions[0].Width = new GridLength(InstanceMember?.FirstGridLength ?? 100);
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        bool isFocused = _textBox.IsFocused || _slider.IsFocused;
        if (isFocused && !forceRefreshEvenIfFocused && !InstanceMember.IsDefault)
        {
            return;
        }

        SuppressSettingProperty = true;
        _textLogic.RefreshDisplay(out object _);
        _label.Text = InstanceMember.DisplayName;
        RefreshMinMaxText();
        RefreshIsEnabled();
        SuppressSettingProperty = false;
        RefreshHint(_hint);
    }

    /// <inheritdoc/>
    public void SetToDefault()
    {
        _textLogic.HasUserChangedAnything = false;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? value)
    {
        ApplyValueResult result = _textLogic.TryGetValueOnUi(out value);
        value = _sliderLogic.ToInstanceValue(value);
        return result;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object valueOnInstance)
    {
        if (valueOnInstance == null)
        {
            return ApplyValueResult.NotSupported;
        }

        object displayed = _sliderLogic.ToDisplayedValue(valueOnInstance);
        _textBox.Text = _textLogic.ConvertNumberToString(displayed, DecimalPointsFromSlider);

        double? position = _sliderLogic.ToSliderPosition(displayed);
        if (position != null)
        {
            // The slider coerces into its range; that coerced value must not echo back to the text.
            _isSettingSliderValueProgrammatically = true;
            _slider.Value = position.Value;
            _isSettingSliderValueProgrammatically = false;
        }

        return ApplyValueResult.Success;
    }

    private void HandleSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == RangeBase.ValueProperty && !_isSettingSliderValueProgrammatically)
        {
            // Show the value while dragging; it is committed when the pointer is released.
            _textBox.Text = _sliderLogic.FormatSliderValue(_slider.Value, InstanceMember?.PropertyType);
        }
    }

    /// <summary>Commits the slider's position, as releasing the pointer does.</summary>
    internal void HandleSliderCommitted()
    {
        if (SuppressSettingProperty)
        {
            return;
        }

        _textBox.Text = _sliderLogic.FormatSliderValue(_slider.Value, InstanceMember?.PropertyType);
        _textLogic.TryApplyToInstance();
        _textLogic.RefreshBackgroundColor();
    }

    private void HandleTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        _textLogic.ClampTextBoxValuesToMinMax();
        if (_textLogic.HasUserChangedAnything)
        {
            _textLogic.TryApplyToInstance();
        }
    }

    private void RefreshMinAndMaxValues()
    {
        double multiplier = _sliderLogic.DisplayedValueMultiplier;
        _slider.Maximum = _maxValue * multiplier;
        _slider.Minimum = _minValue * multiplier;
        _textLogic.MaxValue = (decimal)(_maxValue * multiplier);
        _textLogic.MinValue = (decimal)(_minValue * multiplier);
        RefreshMinMaxText();
    }

    private void RefreshMinMaxText()
    {
        _minValueText.Text = (_minValue * _sliderLogic.DisplayedValueMultiplier).ToString();
        _maxValueText.Text = (_maxValue * _sliderLogic.DisplayedValueMultiplier).ToString();
    }
}

/// <summary>A number field between minus and plus buttons; Ctrl steps by 5.</summary>
public class PlusMinusTextBox : DataUiDisplayBase, ISetDefaultable
{
    private readonly TextBoxDisplayLogic _logic;
    private readonly Grid _grid;
    private readonly TextBlock _label;
    private readonly TextBox _textBox;
    private readonly TextBlock _hint;
    private ApplyValueResult? _lastApplyValueResult;
    private KeyModifiers _lastPressModifiers;

    /// <summary>Builds the displayer.</summary>
    public PlusMinusTextBox()
    {
        _label = new TextBlock { MinWidth = 100, Padding = new Thickness(4, 4, 4, 0), VerticalAlignment = VerticalAlignment.Center };
        _textBox = new TextBox { Width = 60, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _textBox.LostFocus += (_, _) =>
        {
            _lastApplyValueResult = _logic!.TryApplyToInstance();
            RefreshEnabledState();
        };
        Button minus = CreateStepButton("-", -1);
        Button plus = CreateStepButton("+", 1);
        _hint = CreateHintTextBlock();

        StackPanel field = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, Margin = new Thickness(3, 0) };
        field.Children.Add(minus);
        field.Children.Add(_textBox);
        field.Children.Add(plus);

        _grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("100,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
        };
        Grid.SetColumn(field, 1);
        Grid.SetRow(_hint, 1);
        Grid.SetColumnSpan(_hint, 2);
        _grid.Children.Add(_label);
        _grid.Children.Add(field);
        _grid.Children.Add(_hint);
        Content = _grid;

        _logic = AvaloniaDataUiTextBox.CreateLogic(this, _textBox);
        AttachContextMenu(_textBox);
        AttachContextMenu(_grid);
    }

    /// <summary>The text field, for tests.</summary>
    internal TextBox TextBox => _textBox;

    /// <inheritdoc/>
    protected override void OnInstanceMemberChanged()
    {
        _logic.InstanceMember = InstanceMember;
        _lastApplyValueResult = null;
        _grid.ColumnDefinitions[0].Width = new GridLength(InstanceMember?.FirstGridLength ?? 100);
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        if (_textBox.IsFocused && !forceRefreshEvenIfFocused && !InstanceMember.IsDefault)
        {
            return;
        }

        SuppressSettingProperty = true;
        _logic.RefreshDisplay(out object _);
        _label.Text = InstanceMember.DisplayName;
        RefreshHint(_hint);
        RefreshEnabledState();
        SuppressSettingProperty = false;
    }

    /// <inheritdoc/>
    public void SetToDefault()
    {
        _logic.HasUserChangedAnything = false;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object valueOnInstance)
    {
        _textBox.Text = _logic.ConvertNumberToString(valueOnInstance);
        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? value) => _logic.TryGetValueOnUi(out value);

    /// <summary>Steps the value by <paramref name="direction"/> (times 5 with Ctrl) and commits it.</summary>
    internal void Step(int direction, bool isCtrlDown)
    {
        if (TryGetValueOnUi(out object? value) != ApplyValueResult.Success)
        {
            return;
        }

        object newValue = _logic.GetValueInDirection(isCtrlDown ? direction * 5 : direction, value!);
        TrySetValueOnUi(newValue);
        _lastApplyValueResult = _logic.TryApplyToInstance();
    }

    private Button CreateStepButton(string text, int direction)
    {
        Button button = new Button
        {
            Content = text,
            Width = 22,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        button.AddHandler(PointerPressedEvent, (_, e) => _lastPressModifiers = e.KeyModifiers, RoutingStrategies.Tunnel);
        button.Click += (_, _) => Step(direction, _lastPressModifiers.HasFlag(KeyModifiers.Control));
        return button;
    }

    private void RefreshEnabledState()
    {
        IsEnabled = _lastApplyValueResult != ApplyValueResult.NotSupported && InstanceMember?.IsReadOnly != true;
    }
}
