using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Gum.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public bool Collapse { get; set; } = true;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNull = value is null || value is string s && string.IsNullOrEmpty(s);

        if (Invert)
            isNull = !isNull;

        return isNull
            ? (Collapse ? Visibility.Collapsed : Visibility.Hidden)
            : Visibility.Visible;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}