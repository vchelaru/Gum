using System;
using System.Globalization;
using System.Windows.Data;

namespace Gum.Converters;

public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;

        return value; // or DependencyProperty.UnsetValue if you want stricter behavior
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;

        return value;
    }
}