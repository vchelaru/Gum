using System;
using System.Globalization;
using System.Windows.Data;

namespace Gum.Themes.Converters;

/// <summary>
/// Used to determine the minimum length of a "panel" (grid section) in the main display
/// based on if it currently contains any tabs items.
/// </summary>
public class MinPanelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int count)
            throw new InvalidOperationException();

        return count > 0 ? 32 : 0; // Allow 0 (collapsed) for no tab items
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ConvertBack is not supported.");
    }
}
