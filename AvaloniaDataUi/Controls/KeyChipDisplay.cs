using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// One compact line: the label, then the member's <see cref="InstanceMember.ChipText"/> as a key chip. For a long
/// list of keyboard shortcuts, where every row must stay a single short line. Shown for
/// <see cref="StandardDisplayers.KeyChip"/>.
/// </summary>
public class KeyChipDisplay : DataUiDisplayBase
{
    private readonly Grid _grid;
    private readonly TextBlock _label;
    private readonly TextBlock _chipText;
    private string? _text;

    /// <summary>Builds the displayer.</summary>
    public KeyChipDisplay()
    {
        _label = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Padding = new Thickness(4, 0) };
        _chipText = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Opacity = 0.9 };
        Border chip = new Border
        {
            Child = _chipText,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 1),
            Background = new SolidColorBrush(Color.Parse("#40808080")),
            BorderBrush = new SolidColorBrush(Color.Parse("#50808080")),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _grid = new Grid { ColumnDefinitions = new ColumnDefinitions("100,*") };
        DataUiRowLayout.ConfigureLabelColumn(_grid.ColumnDefinitions[0], 100);
        DataUiRowLayout.ConfigureValueColumn(_grid.ColumnDefinitions[1], 44);
        Grid.SetColumn(chip, 1);
        _grid.Children.Add(_label);
        _grid.Children.Add(chip);
        Content = _grid;
    }

    /// <summary>A compact row: little space above and below, so a long list stays short.</summary>
    protected override double SpaciousTopPadding => 3;

    /// <inheritdoc/>
    protected override double SpaciousBottomPadding => 3;

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
        _label.Text = InstanceMember.DisplayName;
        _chipText.Text = InstanceMember.ChipText ?? _text;
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
        return ApplyValueResult.Success;
    }
}
