using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Gum.Converters;

/// <summary>
/// Converts a <see cref="System.Drawing.Color"/> (Gum's neutral color type, ADR-0004) into a WPF
/// <see cref="Brush"/> for XAML binding.
/// </summary>
public class ColorToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not System.Drawing.Color color)
            return DependencyProperty.UnsetValue;

        return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
