using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// A label, then a read-only, muted field holding the member's value with a small chip after it
/// (<see cref="InstanceMember.ChipText"/>, such as where a detected value came from), then the hint.
/// Shown for <see cref="StandardDisplayers.ReadOnlyValueWithChip"/>.
/// </summary>
public class ReadOnlyValueWithChipDisplay : DataUiDisplayBase
{
    private readonly Grid _grid;
    private readonly TextBlock _label;
    private readonly TextBlock _value;
    private readonly TextBlock _chipText;
    private readonly Border _chip;
    private readonly Border _field;
    private readonly TextBlock _hint;
    private string? _text;

    /// <summary>Builds the displayer.</summary>
    public ReadOnlyValueWithChipDisplay()
    {
        _label = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Padding = new Thickness(4, 0) };
        _value = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Opacity = 0.7 };
        _chipText = new TextBlock { Opacity = 0.7 };
        _chipText.Bind(TextBlock.FontSizeProperty, _chipText.GetResourceObservable("Frb.FontSize.Caption"));
        _chip = new Border
        {
            Child = _chipText,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 1),
            Margin = new Thickness(6, 0, 0, 0),
            Background = new SolidColorBrush(Color.Parse("#40808080")),
            VerticalAlignment = VerticalAlignment.Center,
        };
        StackPanel content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(_value);
        content.Children.Add(_chip);
        _field = new Border
        {
            Child = content,
            MinHeight = 26,
            Padding = new Thickness(8, 4),
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#30808080")),
            Background = new SolidColorBrush(Color.Parse("#18000000")),
            VerticalAlignment = VerticalAlignment.Center,
        };
        _hint = CreateHintTextBlock();

        _grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("100,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
        };
        DataUiRowLayout.ConfigureLabelColumn(_grid.ColumnDefinitions[0], 100);
        DataUiRowLayout.ConfigureValueColumn(_grid.ColumnDefinitions[1], 44);
        Grid.SetColumn(_field, 1);
        Grid.SetRow(_hint, 1);
        Grid.SetColumnSpan(_hint, 2);
        _grid.Children.Add(_label);
        _grid.Children.Add(_field);
        _grid.Children.Add(_hint);
        Content = _grid;
    }

    /// <inheritdoc/>
    protected override void OnInstanceMemberChanged()
    {
        DataUiRowLayout.ConfigureLabelColumn(_grid.ColumnDefinitions[0], InstanceMember?.FirstGridLength ?? 100);
    }

    /// <inheritdoc/>
    public override void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        if (InstanceMember == null)
        {
            return;
        }

        _text = InstanceMember.Value?.ToString();
        _value.Text = _text;
        _label.Text = InstanceMember.DisplayName;
        _chipText.Text = InstanceMember.ChipText;
        _chip.IsVisible = !string.IsNullOrEmpty(InstanceMember.ChipText);
        RefreshHint(_hint);
    }

    /// <inheritdoc/>
    public override ApplyValueResult TryGetValueOnUi(out object? result)
    {
        result = _text;
        return ApplyValueResult.Success;
    }

    /// <inheritdoc/>
    public override ApplyValueResult TrySetValueOnUi(object? value)
    {
        _text = value?.ToString();
        _value.Text = _text;
        return ApplyValueResult.Success;
    }
}
