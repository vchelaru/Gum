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
using Avalonia.Threading;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// A combo box that ignores the mouse wheel while closed, so scrolling the grid over it never cycles
/// and commits its value; the wheel still scrolls the grid. An open drop-down scrolls as usual.
/// </summary>
public class WheelIgnoringComboBox : ComboBox
{
    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ComboBox);

    /// <inheritdoc/>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (IsDropDownOpen)
        {
            base.OnPointerWheelChanged(e);
        }
    }
}

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

        if (this.TryGetValueOnInstance(out object? valueOnInstance))
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
    public override ApplyValueResult TrySetValueOnUi(object? valueOnInstance)
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
    private RadioButton? _checkedButton;

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
        if (this.TryGetValueOnInstance(out object? valueOnInstance))
        {
            TrySetValueOnUi(valueOnInstance);
        }
        RefreshHint(_hint);
        _header.Text = InstanceMember.DisplayName;
        RefreshIsEnabled();
        SuppressSettingProperty = false;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object? valueOnInstance)
    {
        RadioButton button = (valueOnInstance as bool?) switch
        {
            true => _trueButton,
            false => _falseButton,
            _ => _nullButton,
        };
        button.IsChecked = true;
        _checkedButton = button;
        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? value)
    {
        value = ValueOf(_checkedButton);
        return ApplyValueResult.Success;
    }

    private void HandleRadioChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { IsChecked: true } button)
        {
            // The group unchecks the previous option only after this handler runs, so read the
            // value from the button just checked, not from whichever button reads as checked first.
            _checkedButton = button;
            if (!SuppressSettingProperty)
            {
                this.TrySetValueOnInstance();
            }
        }
    }

    private bool? ValueOf(RadioButton? button) =>
        button == _trueButton ? true
        : button == _falseButton ? false
        : null;
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
        _comboBox = new WheelIgnoringComboBox { MinWidth = 60, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center };
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
        }

        if (this.TryGetValueOnInstance(out object? valueOnInstance))
        {
            TrySetValueOnUi(valueOnInstance);
        }
        else
        {
            PopulateItems(valueToShow: null);
        }

        RefreshHint(_hint);
        RefreshBackground();
        _label.Text = InstanceMember.DisplayName;
        RefreshIsEnabled();
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object? valueOnInstance)
    {
        object? itemToSelect = _logic.GetItemToSelect(valueOnInstance, _propertyType);
        // An editable combo shows any value as its text; a fixed one can only show an item.
        PopulateItems(IsEditable ? null : itemToSelect);
        SuppressSettingProperty = true;
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

    private void PopulateItems(object? valueToShow)
    {
        if (!this.HasEnoughInformationToWork())
        {
            return;
        }

        List<object> options = _logic.GetOptionsShowing(InstanceMember, _propertyType, valueToShow);

        bool same = _comboBox.Items.Count == options.Count;
        for (int i = 0; same && i < options.Count; i++)
        {
            same = options[i].Equals(_comboBox.Items[i]);
        }

        if (!same && _isInSelectionChanged)
        {
            // Changing the items while the combo raises SelectionChanged corrupts its selection
            // model, so rebuild once the event returns (picking past a value that was not an option).
            Dispatcher.UIThread.Post(() => Refresh());
        }
        else if (!same)
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
        else if (e.Key == Key.Z && e.KeyModifiers.HasCommand())
        {
            // The combo's own undo can restore a stale value (Gum #658); the tool's undo owns Ctrl+Z (Cmd+Z on macOS).
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
    private readonly EditTrackingTextBox _textBox;
    private readonly TextBlock _minValueText;
    private readonly TextBlock _maxValueText;
    private readonly TextBlock _hint;
    private double _minValue;
    private double _maxValue;
    private bool _isSettingSliderValueProgrammatically;
    private double? _sliderValueAtLeftPress;

    /// <summary>Builds the displayer.</summary>
    public SliderDisplay()
    {
        _sliderLogic = new SliderDisplayLogic();
        _label = new TextBlock { MinWidth = 100, Padding = new Thickness(4), VerticalAlignment = VerticalAlignment.Top, TextWrapping = TextWrapping.Wrap };
        _slider = new Slider { MinWidth = 60, VerticalAlignment = VerticalAlignment.Center };
        _slider.PropertyChanged += HandleSliderPropertyChanged;
        // Tunnel, so the value is read before the slider moves the thumb to the press.
        _slider.AddHandler(PointerPressedEvent, HandleSliderPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        _slider.AddHandler(PointerReleasedEvent, (_, e) => HandleSliderPointerReleased(e.InitialPressMouseButton), RoutingStrategies.Bubble, handledEventsToo: true);
        // The thumb ends its drag here when it loses the pointer capture, with no release.
        _slider.AddHandler(Thumb.DragCompletedEvent, (_, _) => HandleSliderPointerReleased(MouseButton.Left), RoutingStrategies.Bubble, handledEventsToo: true);
        _textBox = new EditTrackingTextBox { Margin = new Thickness(3, 1, 1, 1), VerticalAlignment = VerticalAlignment.Center };
        _textBox.EditCommitRequested += HandleEditCommitRequested;
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
        _textLogic.RefreshDisplay(out _);
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
        _textBox.AcceptText();
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? value)
    {
        ApplyValueResult result = _textLogic.TryGetValueOnUi(out value);
        value = _sliderLogic.ToInstanceValue(value);
        return result;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object? valueOnInstance)
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
            _textBox.Text = _sliderLogic.FormatSliderValue(_slider.Value, InstanceMember?.PropertyType);
            // While the pointer drags, apply each value live; releasing the pointer does the full commit.
            if (_sliderValueAtLeftPress != null)
            {
                // A drag is a user edit; the text box does not report programmatic text as one.
                _textLogic.HasUserChangedAnything = true;
                _textLogic.TryApplyToInstance(SetPropertyCommitType.Intermediate);
            }
        }
    }

    private void HandleSliderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _sliderValueAtLeftPress = e.GetCurrentPoint(_slider).Properties.IsLeftButtonPressed ? _slider.Value : null;
    }

    /// <summary>
    /// Ends a left-button interaction: commits only when it moved the slider, so a right-click or a
    /// click that leaves the thumb in place writes nothing.
    /// </summary>
    private void HandleSliderPointerReleased(MouseButton button)
    {
        if (button != MouseButton.Left || _sliderValueAtLeftPress is not double valueAtPress)
        {
            return;
        }

        _sliderValueAtLeftPress = null;
        if (_slider.Value != valueAtPress)
        {
            HandleSliderCommitted();
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

    private void HandleEditCommitRequested(object? sender, EventArgs e)
    {
        _textLogic.ClampTextBoxValuesToMinMax();
        _textLogic.TryApplyToInstance();
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
