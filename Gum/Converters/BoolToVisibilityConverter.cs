using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Gum.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// If true, the boolean value is inverted before conversion.
    /// </summary>
    public bool Invert { get; set; } = false;

    /// <summary>
    /// If true, non-visible values return Collapsed; otherwise Hidden.
    /// </summary>
    public bool Collapse { get; set; } = true;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
            return DependencyProperty.UnsetValue;

        if (Invert)
            boolValue = !boolValue;

        return boolValue
            ? Visibility.Visible
            : (Collapse ? Visibility.Collapsed : Visibility.Hidden);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}