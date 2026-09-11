using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// A labeled text field for any type the text logic can parse, with an optional "Is Null" check
/// box for nullable types and click-and-drag over the label to scrub numbers. The drag uses pointer
/// capture and relative movement; the cursor is never moved.
/// </summary>
public class TextBoxDisplay : DataUiDisplayBase, ISetDefaultable
{
    private readonly TextBoxDisplayLogic _logic;
    private readonly LabelDragScrubLogic _scrubLogic;
    private readonly Grid _grid;
    private readonly TextBlock _label;
    private readonly TextBox _textBox;
    private readonly CheckBox _nullableCheckBox;
    private readonly TextBlock _hint;
    private ApplyValueResult? _lastApplyValueResult;
    private bool _isInSet;
    private bool _isMultiline;
    private Point? _dragLastPosition;
    private Point? _dragPressedPosition;

    /// <summary>Builds the displayer.</summary>
    public TextBoxDisplay()
    {
        _label = new TextBlock
        {
            MinWidth = 100,
            Padding = new Thickness(4, 4, 4, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
        };
        _label.PointerPressed += HandleLabelPointerPressed;
        _label.PointerMoved += HandleLabelPointerMoved;
        _label.PointerReleased += HandleLabelPointerReleased;

        _textBox = new TextBox { MinWidth = 60, VerticalAlignment = VerticalAlignment.Center };
        _textBox.GotFocus += (_, _) => RefreshPlaceholderText();
        _textBox.LostFocus += HandleTextBoxLostFocus;

        _nullableCheckBox = new CheckBox
        {
            Content = "Is Null",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0),
            IsVisible = false,
        };
        _nullableCheckBox.IsCheckedChanged += HandleNullableCheckBoxChanged;

        _hint = CreateHintTextBlock();

        _grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("100,*,Auto"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
        };
        Grid.SetColumn(_textBox, 1);
        Grid.SetColumn(_nullableCheckBox, 2);
        Grid.SetRow(_hint, 2);
        Grid.SetColumnSpan(_hint, 3);
        _grid.Children.Add(_label);
        _grid.Children.Add(_textBox);
        _grid.Children.Add(_nullableCheckBox);
        _grid.Children.Add(_hint);
        Content = _grid;

        _logic = AvaloniaDataUiTextBox.CreateLogic(this, _textBox);
        _scrubLogic = new LabelDragScrubLogic();
        LabelDragValueRounding = 1;
        LabelDragChangeMultiplier = 1;
        EnableLabelDragValueChange = true;

        AttachContextMenu(_grid);
        AttachContextMenu(_textBox);
    }

    /// <summary>The resolution a label drag snaps to; null does not snap.</summary>
    public decimal? LabelDragValueRounding { get; set; }

    /// <summary>How far one unit of pointer movement changes the value.</summary>
    public decimal LabelDragChangeMultiplier { get; set; }

    /// <summary>Whether dragging over the label scrubs a numeric value.</summary>
    public bool EnableLabelDragValueChange { get; set; }

    /// <summary>Optional inclusive lower bound, applied on every write.</summary>
    public double? MinValue
    {
        get => _logic.MinValue.HasValue ? (double)_logic.MinValue.Value : null;
        set => _logic.MinValue = value.HasValue ? (decimal)value.Value : null;
    }

    /// <summary>Optional inclusive upper bound, applied on every write.</summary>
    public double? MaxValue
    {
        get => _logic.MaxValue.HasValue ? (double)_logic.MaxValue.Value : null;
        set => _logic.MaxValue = value.HasValue ? (decimal)value.Value : null;
    }

    /// <summary>The text of the "Is Null" check box shown for nullable types.</summary>
    public string NullCheckboxText
    {
        get => _nullableCheckBox.Content?.ToString() ?? string.Empty;
        set => _nullableCheckBox.Content = value;
    }

    /// <summary>Puts the text field on its own row under the label.</summary>
    public bool IsAboveBelowLayout
    {
        get => Grid.GetRow(_textBox) == 1;
        set
        {
            Grid.SetRow(_textBox, value ? 1 : 0);
            Grid.SetColumn(_textBox, value ? 0 : 1);
            Grid.SetColumnSpan(_textBox, value ? 3 : 1);
        }
    }

    /// <summary>The text field, for tests and subclasses.</summary>
    protected internal TextBox TextBox => _textBox;

    /// <inheritdoc/>
    protected override void OnInstanceMemberChanged()
    {
        _logic.InstanceMember = InstanceMember;
        _lastApplyValueResult = null;
        _grid.ColumnDefinitions[0].Width = new GridLength(InstanceMember?.FirstGridLength ?? 100);
        if (!_isMultiline)
        {
            ResetToSingleLine();
        }
        _textBox.ClearValue(TemplatedControl.BackgroundProperty);
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        // Don't replace the text under the caret while the user is editing, unless the value is default.
        bool canRefresh = !_textBox.IsFocused || forceRefreshEvenIfFocused || InstanceMember.IsDefault;
        canRefresh = canRefresh && !_isInSet;

        if (!canRefresh)
        {
            return;
        }

        SuppressSettingProperty = true;

        _logic.RefreshDisplay(out object valueOnInstance);

        _label.Text = InstanceMember.DisplayName;
        RefreshHint(_hint);
        RefreshIsEnabled(valueOnInstance, forceNullableEnable: false);

        SuppressSettingProperty = false;

        _label.Cursor = _logic.IsNumeric ? new Cursor(StandardCursorType.SizeWestEast) : null;
        _nullableCheckBox.IsVisible = IsDisplayedTypeNullable();
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object valueOnInstance)
    {
        if (!_logic.IsInApplicationToInstance)
        {
            _nullableCheckBox.IsChecked = valueOnInstance == null;
        }
        _textBox.Text = _logic.ConvertNumberToString(valueOnInstance!);

        RefreshPlaceholderText();
        _nullableCheckBox.IsVisible = IsDisplayedTypeNullable();

        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? value)
    {
        if (_nullableCheckBox.IsVisible && _nullableCheckBox.IsChecked == true)
        {
            value = null;
            return ApplyValueResult.Success;
        }

        return _logic.TryGetValueOnUi(out value);
    }

    /// <inheritdoc/>
    public void SetToDefault()
    {
        // So focus loss doesn't write the old text back.
        _logic.HasUserChangedAnything = false;
        _logic.TextAtStartOfEditing = _textBox.Text ?? string.Empty;
    }

    /// <summary>Makes the field a tall, wrapping, multi-line editor where Enter inserts a line.</summary>
    public void MakeMultiline()
    {
        _isMultiline = true;
        _label.VerticalAlignment = VerticalAlignment.Top;
        _textBox.TextWrapping = global::Avalonia.Media.TextWrapping.Wrap;
        _textBox.AcceptsReturn = true;
        _textBox.VerticalAlignment = VerticalAlignment.Top;
        _textBox.VerticalContentAlignment = VerticalAlignment.Top;
        _textBox.Height = 65;
        _logic.HandlesEnter = false;
    }

    private void ResetToSingleLine()
    {
        _label.VerticalAlignment = VerticalAlignment.Center;
        _textBox.TextWrapping = global::Avalonia.Media.TextWrapping.NoWrap;
        _textBox.AcceptsReturn = false;
        _textBox.VerticalAlignment = VerticalAlignment.Center;
        _textBox.VerticalContentAlignment = VerticalAlignment.Center;
        _textBox.Height = double.NaN;
        _logic.HandlesEnter = true;
    }

    private bool IsDisplayedTypeNullable()
    {
        Type? type = InstanceMember?.PropertyType;
        return type != null && Nullable.GetUnderlyingType(type) != null;
    }

    private void RefreshPlaceholderText()
    {
        if (_textBox.IsFocused || InstanceMember?.IsIndeterminate == true)
        {
            // An indeterminate multi-selection shows a blank field; "<NULL>" would imply unset.
            _textBox.Watermark = null;
            return;
        }

        TryGetValueOnUi(out object? value);
        _textBox.Watermark = value == null ? "<NULL>" : null;
    }

    private void RefreshIsEnabled(object? valueOnInstance, bool forceNullableEnable)
    {
        if (_lastApplyValueResult == ApplyValueResult.NotSupported || InstanceMember?.IsReadOnly == true)
        {
            IsEnabled = false;
            return;
        }

        // A null nullable value keeps the field disabled until "Is Null" is cleared.
        _textBox.IsEnabled = !IsDisplayedTypeNullable() || forceNullableEnable || valueOnInstance != null;
        IsEnabled = true;
    }

    private void HandleTextBoxLostFocus(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        RefreshPlaceholderText();

        if ((_textBox.Text ?? string.Empty) != _logic.TextAtStartOfEditing)
        {
            // Clamp on tab-away as Enter does.
            _logic.ClampTextBoxValuesToMinMax();
            _lastApplyValueResult = _logic.TryApplyToInstance();
        }

        TryGetValueOnUi(out object? valueOnInstance);
        RefreshIsEnabled(valueOnInstance, forceNullableEnable: false);
    }

    private void HandleNullableCheckBoxChanged(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (SuppressSettingProperty || InstanceMember == null)
        {
            return;
        }

        if (_nullableCheckBox.IsChecked == false)
        {
            Type propertyType = this.GetPropertyType();
            Type? underlyingType = Nullable.GetUnderlyingType(propertyType);
            if (underlyingType != null)
            {
                TrySetValueOnUi(Activator.CreateInstance(underlyingType)!);
            }
        }

        _isInSet = true;
        _lastApplyValueResult = _logic.TryApplyToInstance();
        TryGetValueOnUi(out object? newValue);
        RefreshIsEnabled(newValue, forceNullableEnable: _nullableCheckBox.IsChecked == false);
        _isInSet = false;
    }

    #region Label scrubbing

    private void HandleLabelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_logic.IsNumeric || !EnableLabelDragValueChange || !e.GetCurrentPoint(_label).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (TryGetValueOnUi(out object? value) != ApplyValueResult.Success)
        {
            return;
        }

        _scrubLogic.Begin(value, _logic.InstancePropertyType);
        _dragLastPosition = e.GetPosition(this);
        _dragPressedPosition = _dragLastPosition;
        e.Pointer.Capture(_label);
        e.Handled = true;
    }

    private void HandleLabelPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragLastPosition == null)
        {
            return;
        }

        Point position = e.GetPosition(this);
        double difference = position.X - _dragLastPosition.Value.X;
        _dragLastPosition = position;

        if (difference == 0)
        {
            return;
        }

        ApplyScrub(difference);
    }

    /// <summary>Applies one horizontal scrub step of <paramref name="difference"/> units as an intermediate edit.</summary>
    internal void ApplyScrub(double difference)
    {
        double rounded = _scrubLogic.ApplyDelta(difference, LabelDragChangeMultiplier,
            LabelDragValueRounding, _logic.MinValue, _logic.MaxValue);

        if (TryGetValueOnUi(out _) == ApplyValueResult.Success)
        {
            TrySetValueOnUi(rounded);
            // A scrub is a user edit; the text box does not report programmatic text as one.
            _logic.HasUserChangedAnything = true;
            _lastApplyValueResult = _logic.TryApplyToInstance(SetPropertyCommitType.Intermediate);
        }
    }

    private void HandleLabelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragPressedPosition == null)
        {
            return;
        }

        bool moved = e.GetPosition(this).X != _dragPressedPosition.Value.X;
        _dragLastPosition = null;
        _dragPressedPosition = null;
        e.Pointer.Capture(null);

        if (moved)
        {
            _lastApplyValueResult = _logic.TryApplyToInstance(SetPropertyCommitType.Full);
        }
    }

    /// <summary>Starts a scrub at the field's current value, for tests.</summary>
    internal void BeginScrub()
    {
        TryGetValueOnUi(out object? value);
        _scrubLogic.Begin(value, _logic.InstancePropertyType);
    }

    /// <summary>Commits a scrub as a full edit, for tests.</summary>
    internal void EndScrub()
    {
        _lastApplyValueResult = _logic.TryApplyToInstance(SetPropertyCommitType.Full);
    }

    #endregion
}

/// <summary>A <see cref="TextBoxDisplay"/> that is always multi-line.</summary>
public class MultiLineTextBoxDisplay : TextBoxDisplay
{
    /// <summary>Builds the displayer.</summary>
    public MultiLineTextBoxDisplay()
    {
        MakeMultiline();
    }
}
