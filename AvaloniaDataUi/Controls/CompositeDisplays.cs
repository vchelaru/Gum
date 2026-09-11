using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using WpfDataUi;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// An angle shown in degrees: a dial to drag (Shift snaps to 15 degrees) plus a text field that
/// takes numbers and arithmetic. Writes degrees or radians per <see cref="TypeToPushToInstance"/>.
/// </summary>
public class AngleSelectorDisplay : DataUiDisplayBase
{
    private const double DialSize = 40;
    private const double NeedleLength = 22;

    private readonly AngleSelectorLogic _angleLogic;
    private readonly TextBoxDisplayLogic _textLogic;
    private readonly TextBlock _label;
    private readonly Canvas _dial;
    private readonly Line _needle;
    private readonly TextBox _textBox;
    private readonly TextBlock _hint;
    private decimal? _angle;
    private bool _isDragging;
    private bool _needsFullCommitOnRelease;

    /// <summary>Builds the displayer.</summary>
    public AngleSelectorDisplay()
    {
        _angleLogic = new AngleSelectorLogic();
        TypeToPushToInstance = AngleType.Radians;
        SnappingInterval = 1;

        _label = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0), MinWidth = 96 };

        Ellipse face = new Ellipse
        {
            Width = DialSize,
            Height = DialSize,
            Stroke = Brushes.Gray,
            Fill = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Colors.White, 0), new GradientStop(Colors.LightGray, 1) },
            },
        };
        _needle = new Line
        {
            StartPoint = new Point(DialSize / 2, DialSize / 2),
            EndPoint = new Point(DialSize / 2 + NeedleLength, DialSize / 2),
            Stroke = Brushes.Black,
            StrokeThickness = 3,
            IsHitTestVisible = false,
        };
        Ellipse center = new Ellipse { Width = 4, Height = 4, Fill = Brushes.Gray, IsHitTestVisible = false };
        Canvas.SetLeft(center, DialSize / 2 - 2);
        Canvas.SetTop(center, DialSize / 2 - 2);
        _dial = new Canvas { Width = DialSize, Height = DialSize, Margin = new Thickness(6), Background = Brushes.Transparent };
        _dial.Children.Add(face);
        _dial.Children.Add(_needle);
        _dial.Children.Add(center);
        _dial.PointerPressed += HandleDialPointerPressed;
        _dial.PointerMoved += HandleDialPointerMoved;
        _dial.PointerReleased += HandleDialPointerReleased;

        _textBox = new TextBox { MinWidth = 40, VerticalAlignment = VerticalAlignment.Center };
        // Registered before the text logic so the typed angle is parsed before the logic commits it.
        _textBox.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                ApplyTextBoxText();
            }
        }, RoutingStrategies.Tunnel);
        _textBox.LostFocus += (_, _) =>
        {
            if (_textLogic!.HasUserChangedAnything)
            {
                ApplyTextBoxText();
            }
        };
        _textLogic = AvaloniaDataUiTextBox.CreateLogic(this, _textBox);
        _hint = CreateHintTextBlock();

        StackPanel field = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        field.Children.Add(_textBox);
        field.Children.Add(new TextBlock { Text = "º", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0) });

        Grid grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
        };
        Grid.SetColumn(_dial, 1);
        Grid.SetColumn(field, 2);
        Grid.SetRow(_hint, 1);
        Grid.SetColumnSpan(_hint, 3);
        grid.Children.Add(_label);
        grid.Children.Add(_dial);
        grid.Children.Add(field);
        grid.Children.Add(_hint);
        Content = grid;

        AttachContextMenu(grid);
        AttachContextMenu(_textBox);
    }

    /// <summary>The unit written to the member; the display is always degrees.</summary>
    public AngleType TypeToPushToInstance { get; set; }

    /// <summary>The degrees a dial drag snaps to; null does not snap.</summary>
    public decimal? SnappingInterval { get; set; }

    /// <summary>The shown angle in degrees; setting it commits to the member.</summary>
    public float? Angle
    {
        get => _angle == null ? null : (float)_angle.Value;
        set
        {
            _angle = value == null ? null : (decimal)value.Value;
            ReactToAngleSet(SetPropertyCommitType.Full);
        }
    }

    /// <summary>The text field, for tests.</summary>
    internal TextBox TextBox => _textBox;

    /// <summary>The needle, for tests.</summary>
    internal Line Needle => _needle;

    /// <inheritdoc/>
    protected override void OnInstanceMemberChanged()
    {
        _textLogic.InstanceMember = InstanceMember;
        _textBox.ClearValue(TemplatedControl.BackgroundProperty);
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        SuppressSettingProperty = true;
        if (this.TryGetValueOnInstance(out object valueOnInstance) && valueOnInstance != null)
        {
            TrySetValueOnUi(valueOnInstance);
        }
        _label.Text = InstanceMember.DisplayName;
        RefreshHint(_hint);
        RefreshIsEnabled();
        _textLogic.RefreshBackgroundColor();
        SuppressSettingProperty = false;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? result)
    {
        result = _angleLogic.ToInstanceValue(_angle, TypeToPushToInstance);
        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object value)
    {
        if (value is null)
        {
            Angle = null;
            _textBox.Text = null;
            RefreshPlaceholderText();
            return ApplyValueResult.Success;
        }

        if (!_angleLogic.TryGetDisplayedDegrees(value, TypeToPushToInstance, out float? degrees))
        {
            return ApplyValueResult.NotSupported;
        }

        // Don't fight the user's drag with the value it is producing.
        if (!_isDragging)
        {
            Angle = degrees;
        }
        RefreshPlaceholderText();
        return ApplyValueResult.Success;
    }

    /// <summary>Parses the typed text into the angle and commits it, as Enter does.</summary>
    internal void ApplyTypedText() => ApplyTextBoxText();

    /// <summary>Drags the dial to a point relative to its center, as a pointer move does.</summary>
    internal void DragDialTo(double x, double y, bool isShiftDown)
    {
        if (_angleLogic.TryDragTo(x, y, isShiftDown, SnappingInterval, out decimal newAngle) && _angle != newAngle)
        {
            // Keep the decimal so the text shows 1 rather than 1.00001.
            _angle = newAngle;
            ReactToAngleSet(SetPropertyCommitType.Intermediate);
            _needsFullCommitOnRelease = true;
        }
    }

    /// <summary>Starts a dial drag, as pressing on the dial does.</summary>
    internal void BeginDialDrag()
    {
        _isDragging = true;
        _angleLogic.BeginDialDrag(_angle);
    }

    /// <summary>Ends a dial drag, committing the angle, as releasing the pointer does.</summary>
    internal void EndDialDrag()
    {
        _isDragging = false;
        if (_needsFullCommitOnRelease)
        {
            _needsFullCommitOnRelease = false;
        }
        ReactToAngleSet(SetPropertyCommitType.Full);
    }

    private void ReactToAngleSet(SetPropertyCommitType commitType)
    {
        _textBox.Text = _angle?.ToString();
        RefreshNeedle();
        RefreshPlaceholderText();

        if (TryGetValueOnUi(out object? valueOnUi) == ApplyValueResult.Success)
        {
            this.TrySetValueOnInstance(valueOnUi!, commitType);
        }
    }

    private void ApplyTextBoxText()
    {
        if (InstanceMember != null && _angleLogic.TryParseAngleText(_textBox.Text, InstanceMember.PropertyType, out float? parsed))
        {
            Angle = parsed;
        }
        // Also records the text in the text logic so Escape restores correctly.
        _textLogic.TryApplyToInstance();
    }

    private void RefreshNeedle()
    {
        double radians = (double)(_angle ?? 0) * Math.PI / 180;
        _needle.EndPoint = new Point(
            DialSize / 2 + NeedleLength * Math.Cos(radians),
            DialSize / 2 - NeedleLength * Math.Sin(radians));
    }

    private void RefreshPlaceholderText()
    {
        // An indeterminate multi-selection leaves the field blank; "<NULL>" would imply unset.
        _textBox.Watermark = InstanceMember?.IsIndeterminate != true && _angle == null ? "<NULL>" : null;
    }

    private void HandleDialPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_dial).Properties.IsLeftButtonPressed)
        {
            return;
        }

        BeginDialDrag();
        e.Pointer.Capture(_dial);
        DragToPointer(e);
        e.Handled = true;
    }

    private void HandleDialPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging && e.GetCurrentPoint(_dial).Properties.IsLeftButtonPressed)
        {
            DragToPointer(e);
        }
    }

    private void HandleDialPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        e.Pointer.Capture(null);
        EndDialDrag();
    }

    private void DragToPointer(PointerEventArgs e)
    {
        Point position = e.GetPosition(_dial);
        DragDialTo(position.X - DialSize / 2, position.Y - DialSize / 2, e.KeyModifiers.HasFlag(KeyModifiers.Shift));
    }
}

/// <summary>
/// One toggle button per option, exactly one pressed; clicking the pressed one keeps it pressed.
/// Buttons show <see cref="OptionContentFactory"/>'s content (an icon) or the option's name.
/// </summary>
public class ToggleButtonOptionDisplay : DataUiDisplayBase
{
    /// <summary>The class on the border around the buttons, for a theme to style.</summary>
    public const string OptionGroupClass = "dataUiOptionGroup";

    /// <summary>The class on each option button, for a theme to style.</summary>
    public const string OptionClass = "dataUiOption";

    private readonly List<ToggleButton> _buttons;
    private readonly Grid _grid;
    private readonly TextBlock _label;
    private readonly WrapPanel _buttonPanel;
    private readonly TextBlock _hint;
    private ToggleButtonOption[] _options;

    /// <summary>Builds a displayer with no options yet.</summary>
    public ToggleButtonOptionDisplay() : this(Array.Empty<ToggleButtonOption>())
    {
    }

    /// <summary>Builds a displayer over <paramref name="options"/>.</summary>
    public ToggleButtonOptionDisplay(ToggleButtonOption[] options)
    {
        _buttons = new List<ToggleButton>();
        _options = Array.Empty<ToggleButtonOption>();
        _label = new TextBlock { MinWidth = 100, Padding = new Thickness(4), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        _buttonPanel = new WrapPanel { Orientation = Orientation.Horizontal };
        _hint = CreateHintTextBlock();

        _grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("100,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            Margin = new Thickness(0, 4),
        };
        Border buttonGroup = new Border
        {
            Child = _buttonPanel,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        };
        buttonGroup.Classes.Add(OptionGroupClass);
        Grid.SetColumn(buttonGroup, 1);
        Grid.SetRow(_hint, 1);
        Grid.SetColumnSpan(_hint, 2);
        _grid.Children.Add(_label);
        _grid.Children.Add(buttonGroup);
        _grid.Children.Add(_hint);
        Content = _grid;

        AttachContextMenu(_grid);
        SetOptions(options);
    }

    /// <summary>Creates a button's content for an option; null or no factory shows the option's name.</summary>
    public Func<ToggleButtonOption, Control?>? OptionContentFactory { get; set; }

    /// <summary>The buttons, for tests.</summary>
    internal IReadOnlyList<ToggleButton> Buttons => _buttons;

    /// <summary>Replaces the buttons when <paramref name="options"/> differs from the current set.</summary>
    public void SetOptions(ToggleButtonOption[] options)
    {
        if (options.SequenceEqual(_options))
        {
            return;
        }

        _options = options;
        _buttonPanel.Children.Clear();
        _buttons.Clear();
        foreach (ToggleButtonOption option in options)
        {
            ToggleButton button = new ToggleButton
            {
                Tag = option,
                Content = (object?)OptionContentFactory?.Invoke(option) ?? option.Name,
                // The WPF option button: 2px padding and a 1px border around the icon, no margin.
                MinWidth = 30,
                MinHeight = 30,
                Padding = new Thickness(2),
            };
            ToolTip.SetTip(button, option.Name);
            button.Classes.Add(OptionClass);
            button.Click += HandleToggleClick;
            _buttons.Add(button);
            _buttonPanel.Children.Add(button);
        }
    }

    /// <inheritdoc/>
    protected override void OnInstanceMemberChanged()
    {
        _grid.ColumnDefinitions[0].Width = new GridLength(InstanceMember?.FirstGridLength ?? 100);
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        SuppressSettingProperty = true;
        _label.Text = DataUiText.InsertSpacesInCamelCase(InstanceMember.DisplayName);
        TrySetValueOnUi(InstanceMember.Value!);
        RefreshHint(_hint);
        RefreshButtonAppearance();
        RefreshIsEnabled();
        SuppressSettingProperty = false;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? result)
    {
        ToggleButton? pressed = _buttons.FirstOrDefault(button => button.IsChecked == true);
        result = (pressed?.Tag as ToggleButtonOption)?.Value;
        return pressed != null ? ApplyValueResult.Success : ApplyValueResult.UnknownError;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object value)
    {
        foreach (ToggleButton button in _buttons)
        {
            button.IsChecked = ((ToggleButtonOption)button.Tag!).Value.Equals(value);
        }
        return ApplyValueResult.Success;
    }

    /// <summary>Presses the button for <paramref name="option"/> and commits it, as a click does.</summary>
    internal void Press(ToggleButtonOption option)
    {
        ToggleButton button = _buttons.First(item => item.Tag == option);
        button.IsChecked = true;
        HandleToggleClick(button, new RoutedEventArgs());
    }

    private void HandleToggleClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton clicked)
        {
            return;
        }

        if (clicked.IsChecked == true)
        {
            foreach (ToggleButton other in _buttons.Where(button => button != clicked))
            {
                other.IsChecked = false;
            }
        }
        else
        {
            // Unpressing the current option is not allowed.
            clicked.IsChecked = true;
        }

        this.TrySetValueOnInstance();
        RefreshButtonAppearance();
    }

    private void RefreshButtonAppearance()
    {
        DataUiValueState state = InstanceMember?.ValueState ?? DataUiValueState.Custom;
        foreach (ToggleButton button in _buttons)
        {
            DataUiValueStateBrushes.ApplyBackground(button, state == DataUiValueState.Custom || button.IsChecked != true ? DataUiValueState.Custom : state);
        }
    }
}

/// <summary>
/// A list of strings edited as multi-line text, one entry per line, committed when focus leaves.
/// </summary>
public class StringListTextBoxDisplay : DataUiDisplayBase
{
    private readonly StringListLogic _listLogic;
    private readonly TextBlock _label;
    private readonly TextBox _textBox;
    private readonly TextBlock _hint;
    private string _textAtFocus;

    /// <summary>Builds the displayer.</summary>
    public StringListTextBoxDisplay()
    {
        _listLogic = new StringListLogic();
        _textAtFocus = string.Empty;
        _label = new TextBlock { MinWidth = 100, Padding = new Thickness(4, 4, 4, 0), TextWrapping = TextWrapping.Wrap };
        _textBox = new TextBox
        {
            MinWidth = 60,
            Height = 150,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            VerticalContentAlignment = VerticalAlignment.Top,
        };
        _textBox.GotFocus += (_, _) => _textAtFocus = _textBox.Text ?? string.Empty;
        _textBox.LostFocus += (_, _) =>
        {
            // Compared against the text at focus rather than tracked through TextChanged, so a
            // refresh's programmatic text never reads as a user edit (which would override an
            // inherited value on the next focus loss).
            if ((_textBox.Text ?? string.Empty) != _textAtFocus)
            {
                this.TrySetValueOnInstance();
            }
        };
        _hint = CreateHintTextBlock();

        StackPanel panel = new StackPanel();
        panel.Children.Add(_label);
        panel.Children.Add(_textBox);
        panel.Children.Add(_hint);
        Content = panel;

        AttachContextMenu(_textBox);
    }

    /// <summary>The text editor; the tool listens to its keys for go-to-definition.</summary>
    public TextBox EditorTextBox => _textBox;

    /// <summary>The text of the line under the caret.</summary>
    public string GetCurrentLineText() => _listLogic.GetLineAt(_textBox.Text, _textBox.CaretIndex);

    /// <inheritdoc/>
    protected override void OnInstanceMemberChanged()
    {
        _textBox.ClearValue(TemplatedControl.BackgroundProperty);
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        SuppressSettingProperty = true;
        _label.Text = InstanceMember.DisplayName;
        DataUiValueStateBrushes.ApplyBackground(_textBox, InstanceMember.ValueState);
        RefreshHint(_hint);
        TrySetValueOnUi(InstanceMember.Value!);
        RefreshIsEnabled();
        SuppressSettingProperty = false;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? result)
    {
        if (InstanceMember?.PropertyType == typeof(List<string>))
        {
            result = _listLogic.ParseLines(_textBox.Text);
            return ApplyValueResult.Success;
        }

        result = null;
        return ApplyValueResult.NotSupported;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object value)
    {
        if (value is List<string> lines)
        {
            _textBox.Text = _listLogic.JoinLines(lines);
        }
        return ApplyValueResult.Success;
    }
}

/// <summary>
/// An editable list (strings, ints, floats, or Vector2s): "+" adds an entry, double-click edits one,
/// Delete removes the selection, and Ctrl+C / Ctrl+V copy and paste entries.
/// </summary>
public class ListBoxDisplay : DataUiDisplayBase
{
    private readonly ListBoxDisplayLogic _listLogic;
    private readonly TextBlock _label;
    private readonly ListBox _listBox;
    private readonly Button _addButton;
    private readonly Grid _newEntryGrid;
    private readonly TextBox _newEntryTextBox;
    private readonly TextBlock _errorText;
    private readonly TextBlock _hint;
    private IList? _items;
    private int? _indexEditing;

    /// <summary>Builds the displayer.</summary>
    public ListBoxDisplay()
    {
        _listLogic = new ListBoxDisplayLogic();
        _label = new TextBlock { MinWidth = 100, Padding = new Thickness(4, 4, 4, 0), TextWrapping = TextWrapping.Wrap };
        _listBox = new ListBox { MinHeight = 40, MaxHeight = 200 };
        _listBox.KeyDown += HandleListBoxKeyDown;
        _listBox.DoubleTapped += (_, _) => BeginEditSelected();
        _addButton = new Button { Content = "+", MinWidth = 24, HorizontalContentAlignment = HorizontalAlignment.Center };
        _addButton.Click += (_, _) => ShowNewEntry();
        _newEntryTextBox = new TextBox();
        _newEntryTextBox.KeyDown += HandleNewEntryKeyDown;
        Button ok = new Button { Content = "OK" };
        ok.Click += (_, _) => AddOrReplace(_newEntryTextBox.Text ?? string.Empty);
        Button cancel = new Button { Content = "Cancel" };
        cancel.Click += (_, _) => CancelEntry();
        _newEntryGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), IsVisible = false };
        Grid.SetColumn(ok, 1);
        Grid.SetColumn(cancel, 2);
        _newEntryGrid.Children.Add(_newEntryTextBox);
        _newEntryGrid.Children.Add(ok);
        _newEntryGrid.Children.Add(cancel);
        _errorText = new TextBlock { Foreground = Brushes.OrangeRed, TextWrapping = TextWrapping.Wrap, IsVisible = false };
        _hint = CreateHintTextBlock();

        StackPanel panel = new StackPanel();
        panel.Children.Add(_label);
        panel.Children.Add(_listBox);
        panel.Children.Add(_addButton);
        panel.Children.Add(_newEntryGrid);
        panel.Children.Add(_errorText);
        panel.Children.Add(_hint);
        Content = panel;

        AttachContextMenu(_listBox);
    }

    /// <summary>The entries shown, for tests.</summary>
    internal IList? Items => _items;

    /// <summary>The list, for tests.</summary>
    internal ListBox ListBox => _listBox;

    /// <summary>The index an in-progress edit replaces, or null when adding.</summary>
    internal int? IndexEditing => _indexEditing;

    /// <inheritdoc/>
    protected override void OnInstanceMemberChanged()
    {
        // A re-bound control must not keep editing a position the new list may not have (#4664).
        _newEntryGrid.IsVisible = false;
        _addButton.IsVisible = true;
        _errorText.IsVisible = false;
        _indexEditing = null;
        _listBox.ClearValue(TemplatedControl.BackgroundProperty);
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        SuppressSettingProperty = true;
        _label.Text = InstanceMember.DisplayName;
        DataUiValueStateBrushes.ApplyBackground(_listBox,
            InstanceMember.IsDefault ? DataUiValueState.Default : DataUiValueState.Custom);
        RefreshHint(_hint);
        TrySetValueOnUi(InstanceMember.Value!);
        RefreshIsEnabled();
        SuppressSettingProperty = false;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? result)
    {
        if (_items != null && _listLogic.TryReadList(_items, InstanceMember?.PropertyType, out object? list))
        {
            result = list;
            return ApplyValueResult.Success;
        }

        result = null;
        return ApplyValueResult.NotSupported;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object value)
    {
        // Edit a copy so nothing reaches the member until it is committed.
        _items = _listLogic.CreateEditableCopy(value, InstanceMember?.PropertyType);
        RebindItems();
        return ApplyValueResult.Success;
    }

    /// <summary>Adds <paramref name="text"/> (or replaces the entry being edited) and commits, as OK does.</summary>
    internal void AddOrReplace(string text)
    {
        if (_items != null)
        {
            string? error = _listLogic.AddOrReplace(_items, _indexEditing, text);
            _errorText.Text = error;
            _errorText.IsVisible = error != null;
        }

        _newEntryTextBox.Text = null;
        _newEntryGrid.IsVisible = false;
        _addButton.IsVisible = true;
        this.TrySetValueOnInstance();
        if (_indexEditing != null)
        {
            _indexEditing = null;
            Refresh();
        }
        RebindItems();
    }

    /// <summary>Starts editing the selected entry, as a double-click does.</summary>
    internal void BeginEditSelected()
    {
        if (InstanceMember?.IsReadOnly == true || _listBox.SelectedIndex < 0)
        {
            return;
        }

        _indexEditing = _listBox.SelectedIndex;
        ShowNewEntry();
        _newEntryTextBox.Text = ListBoxDisplayLogic.StripAngleBrackets(_listBox.SelectedItem?.ToString());
    }

    /// <summary>Removes the selected entry and commits, as Delete does.</summary>
    internal void RemoveSelected()
    {
        int index = _listBox.SelectedIndex;
        if (_items != null && index > -1 && index < _items.Count)
        {
            _items.RemoveAt(index);
        }
        this.TrySetValueOnInstance();
        RebindItems();
    }

    private void RebindItems()
    {
        _listBox.ItemsSource = null;
        _listBox.ItemsSource = _items;
    }

    private void ShowNewEntry()
    {
        _newEntryGrid.IsVisible = true;
        _addButton.IsVisible = false;
        _newEntryTextBox.Focus();
    }

    private void CancelEntry()
    {
        _newEntryTextBox.Text = null;
        _newEntryGrid.IsVisible = false;
        _addButton.IsVisible = true;
        _indexEditing = null;
    }

    private void HandleNewEntryKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            AddOrReplace(_newEntryTextBox.Text ?? string.Empty);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            CancelEntry();
        }
    }

    private async void HandleListBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (InstanceMember?.IsReadOnly == true)
        {
            return;
        }

        bool isCtrlDown = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        global::Avalonia.Input.Platform.IClipboard? clipboard = TopLevel.GetTopLevel(this)?.Clipboard;

        if (e.Key == Key.Delete)
        {
            RemoveSelected();
        }
        else if (e.Key == Key.C && isCtrlDown && clipboard != null)
        {
            if (_listBox.SelectedItem is string selected && !string.IsNullOrEmpty(selected))
            {
                await clipboard.SetTextAsync(selected);
            }
        }
        else if (e.Key == Key.V && isCtrlDown && clipboard != null)
        {
            string? text = await global::Avalonia.Input.Platform.ClipboardExtensions.TryGetTextAsync(clipboard);
            if (!string.IsNullOrEmpty(text))
            {
                AddOrReplace(text);
            }
        }
    }
}

/// <summary>A file path field with a picker button and a button that reveals the file.</summary>
public class FileSelectionDisplay : DataUiDisplayBase
{
    private readonly TextBoxDisplayLogic _textLogic;
    private readonly FilePickingLogic _filePickingLogic;
    private readonly Grid _grid;
    private readonly TextBlock _label;
    private readonly TextBox _textBox;
    private readonly Button _revealButton;
    private readonly TextBlock _hint;

    /// <summary>Builds the displayer.</summary>
    public FileSelectionDisplay()
    {
        _filePickingLogic = new FilePickingLogic();
        _label = new TextBlock { MinWidth = 100, Padding = new Thickness(4), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        _textBox = new TextBox { MinWidth = 60, VerticalAlignment = VerticalAlignment.Center };
        _textBox.LostFocus += HandleTextBoxLostFocus;
        Button pickButton = new Button { Content = "...", MinWidth = 24, Margin = new Thickness(2, 0, 0, 0) };
        pickButton.Click += (_, _) => PickFile();
        _revealButton = new Button { Content = "↗", MinWidth = 24, Margin = new Thickness(1, 0, 0, 0) };
        ToolTip.SetTip(_revealButton, "View file in the file manager");
        _revealButton.Click += (_, _) => _filePickingLogic.ShowInExplorer(_textBox.Text ?? string.Empty);
        _hint = CreateHintTextBlock();

        _grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("100,*,Auto,Auto"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
        };
        Grid.SetColumn(_textBox, 1);
        Grid.SetColumn(pickButton, 2);
        Grid.SetColumn(_revealButton, 3);
        Grid.SetRow(_hint, 1);
        Grid.SetColumnSpan(_hint, 4);
        _grid.Children.Add(_label);
        _grid.Children.Add(_textBox);
        _grid.Children.Add(pickButton);
        _grid.Children.Add(_revealButton);
        _grid.Children.Add(_hint);
        Content = _grid;

        _textLogic = AvaloniaDataUiTextBox.CreateLogic(this, _textBox);
        AttachContextMenu(_label);
        AttachContextMenu(_textBox);
    }

    /// <summary>The picker's filter, e.g. "Bitmap Font Generator Font|*.fnt".</summary>
    public string Filter
    {
        get => _filePickingLogic.Filter;
        set => _filePickingLogic.Filter = value;
    }

    /// <summary>The path field, for tests.</summary>
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

        SuppressSettingProperty = true;
        _textLogic.RefreshDisplay(out object _);
        RefreshHint(_hint);
        _label.Text = InstanceMember.DisplayName;
        RefreshRevealButton();
        RefreshIsEnabled();
        SuppressSettingProperty = false;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object valueOnInstance)
    {
        _textBox.Text = valueOnInstance?.ToString();
        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? value) => _textLogic.TryGetValueOnUi(out value);

    /// <summary>Shows the picker and commits the chosen path, as the "..." button does.</summary>
    internal void PickFile()
    {
        string? selected = _filePickingLogic.ShowOpenDialog();
        if (selected != null)
        {
            _textBox.Text = selected;
            _textLogic.TryApplyToInstance();
            RefreshRevealButton();
        }
    }

    private void HandleTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if ((_textBox.Text ?? string.Empty) != _textLogic.TextAtStartOfEditing)
        {
            if (_textLogic.TryApplyToInstance() == ApplyValueResult.NotSupported)
            {
                IsEnabled = false;
            }
            RefreshRevealButton();
        }
    }

    private void RefreshRevealButton()
    {
        _revealButton.IsEnabled = !string.IsNullOrEmpty(InstanceMember?.Value as string);
    }
}

/// <summary>
/// An ordered list of file paths with Add (through the picker), Remove, Up, and Down. Order is
/// meaningful, e.g. localization files merge last-write-wins.
/// </summary>
public class MultiFileDisplay : DataUiDisplayBase
{
    private readonly MultiFileDisplayLogic _fileListLogic;
    private readonly FilePickingLogic _filePickingLogic;
    private readonly TextBlock _label;
    private readonly ListBox _listBox;
    private readonly Button _removeButton;
    private readonly Button _upButton;
    private readonly Button _downButton;
    private readonly TextBlock _hint;

    /// <summary>Builds the displayer.</summary>
    public MultiFileDisplay()
    {
        _fileListLogic = new MultiFileDisplayLogic();
        _filePickingLogic = new FilePickingLogic();
        _label = new TextBlock { MinWidth = 100, Padding = new Thickness(4, 4, 4, 0), TextWrapping = TextWrapping.Wrap };
        _listBox = new ListBox { MinHeight = 40, MaxHeight = 200 };
        _listBox.SelectionChanged += (_, _) => RefreshButtonVisibility();
        _listBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Delete && InstanceMember?.IsReadOnly != true)
            {
                RemoveSelected();
                e.Handled = true;
            }
        };
        Button addButton = new Button { Content = "Add..." };
        addButton.Click += (_, _) => AddFile();
        _removeButton = new Button { Content = "Remove", IsVisible = false };
        _removeButton.Click += (_, _) => RemoveSelected();
        _upButton = new Button { Content = "Up", IsVisible = false };
        _upButton.Click += (_, _) => MoveSelected(-1);
        _downButton = new Button { Content = "Down", IsVisible = false };
        _downButton.Click += (_, _) => MoveSelected(1);
        _hint = CreateHintTextBlock();

        StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, Margin = new Thickness(0, 2) };
        buttons.Children.Add(addButton);
        buttons.Children.Add(_removeButton);
        buttons.Children.Add(_upButton);
        buttons.Children.Add(_downButton);

        StackPanel panel = new StackPanel();
        panel.Children.Add(_label);
        panel.Children.Add(_listBox);
        panel.Children.Add(buttons);
        panel.Children.Add(_hint);
        Content = panel;
        AttachContextMenu(_listBox);
    }

    /// <summary>The picker's filter.</summary>
    public string Filter
    {
        get => _filePickingLogic.Filter;
        set => _filePickingLogic.Filter = value;
    }

    /// <summary>The list, for tests.</summary>
    internal ListBox ListBox => _listBox;

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        SuppressSettingProperty = true;
        _label.Text = InstanceMember.DisplayName;
        RefreshHint(_hint);
        TrySetValueOnUi(InstanceMember.Value!);
        RefreshIsEnabled();
        DataUiValueStateBrushes.ApplyBackground(_listBox,
            InstanceMember.IsDefault ? DataUiValueState.Default : DataUiValueState.Custom);
        SuppressSettingProperty = false;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object value)
    {
        _fileListLogic.SetEntries(value);
        Rebind(selectIndex: null);
        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? result)
    {
        result = _fileListLogic.GetValue();
        return ApplyValueResult.Success;
    }

    /// <summary>Adds a picked file and commits, as Add does.</summary>
    internal void AddFile()
    {
        string? selected = _filePickingLogic.ShowOpenDialog();
        if (!string.IsNullOrEmpty(selected))
        {
            _fileListLogic.Entries.Add(selected);
            Commit(selectIndex: null);
        }
    }

    /// <summary>Moves the selection by <paramref name="direction"/> and commits, keeping it selected.</summary>
    internal void MoveSelected(int direction)
    {
        if (_fileListLogic.Move(_listBox.SelectedIndex, direction, out int newIndex))
        {
            Commit(newIndex);
        }
    }

    private void RemoveSelected()
    {
        if (_fileListLogic.RemoveAt(_listBox.SelectedIndex))
        {
            Commit(selectIndex: null);
        }
    }

    private void Commit(int? selectIndex)
    {
        this.TrySetValueOnInstance();
        Rebind(selectIndex);
    }

    private void Rebind(int? selectIndex)
    {
        _listBox.ItemsSource = null;
        _listBox.ItemsSource = _fileListLogic.Entries.ToList();
        if (selectIndex is int index && index >= 0 && index < _fileListLogic.Entries.Count)
        {
            _listBox.SelectedIndex = index;
        }
        RefreshButtonVisibility();
    }

    private void RefreshButtonVisibility()
    {
        int selectedIndex = _listBox.SelectedIndex;
        _removeButton.IsVisible = _fileListLogic.IsRemoveVisible(selectedIndex);
        _upButton.IsVisible = _fileListLogic.IsMoveVisible(selectedIndex);
        _downButton.IsVisible = _fileListLogic.IsMoveVisible(selectedIndex);
    }
}

/// <summary>
/// One labeled number field per channel of a <see cref="CompositeInstanceMember"/>, each committing
/// straight to its own channel, so the control never needs the composite's type.
/// </summary>
public class InlineChannelsDisplay : DataUiDisplayBase
{
    private readonly List<(TextBox TextBox, InstanceMember Channel)> _fields;
    private readonly TextBlock _label;
    private readonly UniformGrid _fieldsPanel;
    private readonly TextBlock _hint;
    private bool _isSyncingFromInstance;

    /// <summary>Builds the displayer.</summary>
    public InlineChannelsDisplay()
    {
        _fields = new List<(TextBox, InstanceMember)>();
        _label = new TextBlock { MinWidth = 100, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0) };
        _fieldsPanel = new UniformGrid { Rows = 1 };
        _hint = CreateHintTextBlock();

        Grid grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        Grid.SetColumn(_fieldsPanel, 1);
        grid.Children.Add(_label);
        grid.Children.Add(_fieldsPanel);

        StackPanel panel = new StackPanel();
        panel.Children.Add(grid);
        panel.Children.Add(_hint);
        Content = panel;
        AttachContextMenu(grid);
    }

    /// <summary>The per-channel fields, for tests.</summary>
    internal IReadOnlyList<TextBox> FieldTextBoxes => _fields.Select(field => field.TextBox).ToList();

    /// <inheritdoc/>
    protected override void OnInstanceMemberChanged()
    {
        _fieldsPanel.Children.Clear();
        _fields.Clear();

        if (InstanceMember is not CompositeInstanceMember composite)
        {
            return;
        }

        _fieldsPanel.Columns = composite.ChannelMembers.Count;
        foreach (InstanceMember channel in composite.ChannelMembers)
        {
            TextBox textBox = new TextBox();
            textBox.LostFocus += (_, _) => Commit(channel, textBox);
            textBox.AddHandler(KeyDownEvent, (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    Commit(channel, textBox);
                    e.Handled = true;
                }
            }, RoutingStrategies.Tunnel);

            StackPanel fieldPanel = new StackPanel { Margin = new Thickness(0, 0, 4, 0) };
            fieldPanel.Children.Add(new TextBlock { Text = channel.DisplayName, FontSize = 10, Margin = new Thickness(0, 0, 0, 2) });
            fieldPanel.Children.Add(textBox);
            _fieldsPanel.Children.Add(fieldPanel);
            _fields.Add((textBox, channel));
        }
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember is not CompositeInstanceMember composite)
        {
            return;
        }

        _label.Text = InstanceMember.DisplayName;
        IsEnabled = !InstanceMember.IsReadOnly;
        RefreshHint(_hint);

        _isSyncingFromInstance = true;
        try
        {
            InlineChannelFieldState[] states = InlineChannelsDisplayLogic.BuildFieldStates(composite.ChannelMembers);
            for (int i = 0; i < _fields.Count; i++)
            {
                (TextBox textBox, InstanceMember channel) = _fields[i];
                if (forceRefreshEvenIfFocused || !textBox.IsFocused)
                {
                    textBox.Text = states[i].Text;
                }
                textBox.IsEnabled = !channel.IsReadOnly;
                DataUiValueStateBrushes.ApplyBackground(textBox,
                    states[i].IsDefault ? DataUiValueState.Default
                    : states[i].IsIndeterminate ? DataUiValueState.Indeterminate
                    : DataUiValueState.Custom);
            }
        }
        finally
        {
            _isSyncingFromInstance = false;
        }
    }

    /// <summary>Fields commit to their channels directly; there is no single composed UI value.</summary>
    public override ApplyValueResult TrySetValueOnUi(object valueOnInstance) => ApplyValueResult.NotSupported;

    /// <inheritdoc cref="TrySetValueOnUi"/>
    public override ApplyValueResult TryGetValueOnUi(out object? value)
    {
        value = null;
        return ApplyValueResult.NotSupported;
    }

    /// <summary>Commits field <paramref name="index"/>'s text to its channel, as Enter does.</summary>
    internal void CommitField(int index)
    {
        (TextBox textBox, InstanceMember channel) = _fields[index];
        Commit(channel, textBox);
    }

    private void Commit(InstanceMember channel, TextBox textBox)
    {
        if (_isSyncingFromInstance || SuppressSettingProperty)
        {
            return;
        }

        if (TextBoxDisplayLogic.TryParseNumeric(textBox.Text ?? string.Empty, channel.PropertyType, out object parsed))
        {
            channel.SetValue(parsed, SetPropertyCommitType.Full);
            channel.CallAfterSetByUi();
        }
    }
}
