using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Gum.Converters;

public class GridLengthToDoubleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return new GridLength(d);


        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is GridLength gridLength)
            return gridLength.Value;

        return DependencyProperty.UnsetValue;
    }
}