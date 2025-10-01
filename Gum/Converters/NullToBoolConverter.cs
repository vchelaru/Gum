using System;
using System.Globalization;
using System.Windows.Data;

namespace Gum.Converters;

public class NullToBoolConverter : IValueConverter
{
    /// <summary>
    /// If true, returns true when value is null. 
    /// If false, returns true when value is not null. (default)
    /// </summary>
    public bool Invert { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNull = value is null || (value is string s && string.IsNullOrWhiteSpace(s));
        return Invert ? isNull : !isNull;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("NullToBoolConverter only supports one-way conversion.");
    }
}