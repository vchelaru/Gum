using System;
using Avalonia.Controls;

namespace AvaloniaDataUi.Controls;

/// <summary>
/// Column rules shared by the label-then-value rows. The label column is the one that gives way when
/// the panel is dragged narrow, and every other column holds a floor of its own, so a row clips at
/// its right edge instead of laying one control on top of another. A control's own <c>MinWidth</c>
/// cannot do this: the grid hands the control less than that, and it spills into its neighbor.
/// </summary>
public static class DataUiRowLayout
{
    /// <summary>The narrowest a label column gets; its text wraps, or trims, to fit.</summary>
    public const double MinLabelWidth = 32;

    // Weighted so the label takes its full width whenever the value column's floor is met.
    private const double LabelWeight = 4;

    /// <summary>
    /// Makes <paramref name="column"/> the label column: <paramref name="width"/> when there is room,
    /// shrinking toward <see cref="MinLabelWidth"/> once the value columns are down to their floors.
    /// </summary>
    public static void ConfigureLabelColumn(ColumnDefinition column, double width)
    {
        column.Width = new GridLength(LabelWeight, GridUnitType.Star);
        column.MinWidth = Math.Min(MinLabelWidth, width);
        column.MaxWidth = width;
    }

    /// <summary>Makes <paramref name="column"/> the stretching value column, never narrower than <paramref name="minWidth"/>.</summary>
    public static void ConfigureValueColumn(ColumnDefinition column, double minWidth)
    {
        column.Width = new GridLength(1, GridUnitType.Star);
        column.MinWidth = minWidth;
    }
}
