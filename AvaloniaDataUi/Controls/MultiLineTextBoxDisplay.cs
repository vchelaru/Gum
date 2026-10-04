using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using WpfDataUi;
using WpfDataUi.Controls;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// A multi-line string editor. Enter inserts a line; edits apply on focus loss, Ctrl+Enter (Cmd+Enter
/// on macOS), or the Apply button, which shows only while edits are unapplied. Escape reverts
/// unapplied edits.
/// Subclasses change how the member's value maps to and from the text.
/// </summary>
public class MultiLineTextBoxDisplay : DataUiDisplayBase
{
    private readonly Grid _grid;
    private readonly TextBlock _label;
    private readonly TextBox _textBox;
    private readonly TextBlock _hint;
    private string _appliedText;

    /// <summary>Builds the displayer.</summary>
    public MultiLineTextBoxDisplay()
    {
        _appliedText = string.Empty;
        _label = new TextBlock
        {
            MinWidth = 100,
            Padding = new Thickness(4, 4, 4, 0),
            VerticalAlignment = VerticalAlignment.Top,
            TextWrapping = TextWrapping.Wrap,
        };
        _textBox = new TextBox
        {
            MinWidth = 60,
            Height = 65,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalContentAlignment = VerticalAlignment.Top,
        };
        _textBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
            {
                RefreshApplyButton();
            }
        };
        _textBox.LostFocus += (_, _) => Apply();
        // Tunnel so the apply gesture is handled before the text box inserts a line.
        _textBox.AddHandler(KeyDownEvent, HandleTextBoxKeyDown, RoutingStrategies.Tunnel);

        // Not focusable, so clicking it leaves the caret in the text box and Tab skips it.
        ApplyButton = new Button
        {
            Content = "Apply",
            Focusable = false,
            IsTabStop = false,
            IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
        };
        ToolTip.SetTip(ApplyButton, ApplyToolTip(PlatformKeyModifiers.Command));
        ApplyButton.Click += (_, _) => Apply();

        _hint = CreateHintTextBlock();

        // The editor takes its own full-width row under the label. The button sits on the label row,
        // so showing it neither covers the text or scroll bars nor pushes the rows below.
        _grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("100,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
        };
        Grid.SetRow(_textBox, 1);
        Grid.SetColumnSpan(_textBox, 2);
        Grid.SetColumn(ApplyButton, 1);
        Grid.SetRow(_hint, 2);
        Grid.SetColumnSpan(_hint, 2);
        _grid.Children.Add(_label);
        _grid.Children.Add(ApplyButton);
        _grid.Children.Add(_textBox);
        _grid.Children.Add(_hint);
        Content = _grid;
    }

    /// <summary>Applies unapplied edits; visible only while there are some.</summary>
    public Button ApplyButton { get; }

    /// <summary>The text editor.</summary>
    public TextBox EditorTextBox => _textBox;

    /// <summary>The text shown for <paramref name="value"/>.</summary>
    protected virtual string ConvertToText(object? value) => value as string ?? string.Empty;

    /// <summary>Whether the member's type is one this editor can write.</summary>
    protected virtual bool CanEdit(System.Type? propertyType) => propertyType == typeof(string);

    /// <summary>The value written for <paramref name="text"/>.</summary>
    protected virtual object? ConvertFromText(string text) => text;

    /// <inheritdoc/>
    protected override void OnInstanceMemberChanged()
    {
        _grid.ColumnDefinitions[0].Width = new GridLength(InstanceMember?.FirstGridLength ?? 100);
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
        TrySetValueOnUi(InstanceMember.Value);
        RefreshIsEnabled();
        SuppressSettingProperty = false;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? result)
    {
        if (CanEdit(InstanceMember?.PropertyType))
        {
            result = ConvertFromText(_textBox.Text ?? string.Empty);
            return ApplyValueResult.Success;
        }

        result = null;
        return ApplyValueResult.NotSupported;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object? value)
    {
        // Set the baseline first so the text change this raises doesn't read as an edit.
        _appliedText = ConvertToText(value);
        _textBox.Text = _appliedText;
        RefreshApplyButton();
        return ApplyValueResult.Success;
    }

    private void Apply()
    {
        // Compared against the applied text rather than tracked through TextChanged, so a
        // refresh's programmatic text never reads as a user edit (which would override an
        // inherited value).
        if ((_textBox.Text ?? string.Empty) != _appliedText)
        {
            this.TrySetValueOnInstance();
            // The commit may rewrite the text (reference expansion), which refreshes it.
            _appliedText = _textBox.Text ?? string.Empty;
        }
        RefreshApplyButton();
    }

    private void HandleTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (IsApplyGesture(e.Key, e.KeyModifiers, PlatformKeyModifiers.Command))
        {
            Apply();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && (_textBox.Text ?? string.Empty) != _appliedText)
        {
            _textBox.Text = _appliedText;
            e.Handled = true;
        }
    }

    /// <summary>Whether <paramref name="key"/> with <paramref name="modifiers"/> applies the edit: Enter with the command modifier.</summary>
    public static bool IsApplyGesture(Key key, KeyModifiers modifiers, KeyModifiers commandModifiers) =>
        key == Key.Enter && modifiers.HasFlag(commandModifiers);

    /// <summary>The Apply button's tooltip, naming the apply gesture for <paramref name="commandModifiers"/>.</summary>
    public static string ApplyToolTip(KeyModifiers commandModifiers) =>
        commandModifiers == KeyModifiers.Meta ? "Apply (⌘Enter)" : "Apply (Ctrl+Enter)";

    private void RefreshApplyButton()
    {
        ApplyButton.IsVisible = (_textBox.Text ?? string.Empty) != _appliedText;
    }
}
