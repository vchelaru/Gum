using System;
using System.Globalization;
using System.Windows.Data;
using DrawingColor = System.Drawing.Color;
using MediaColor = System.Windows.Media.Color;

namespace Gum.Converters;

/// <summary>
/// Converts between the headless <see cref="DrawingColor"/> used on framework-neutral ViewModels
/// (ADR-0004) and the WPF <see cref="MediaColor"/> required by WPF color-picker controls (e.g.
/// <c>ColorPicker.PortableColorPicker.SelectedColor</c>).
/// </summary>
public class DrawingColorToMediaColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DrawingColor color)
        {
            return MediaColor.FromArgb(color.A, color.R, color.G, color.B);
        }
        return MediaColor.FromArgb(0, 0, 0, 0);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MediaColor color)
        {
            return (DrawingColor?)DrawingColor.FromArgb(color.A, color.R, color.G, color.B);
        }
        return null;
    }
}
