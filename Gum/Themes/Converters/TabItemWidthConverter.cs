using System;
using System.Globalization;
using System.Windows.Data;

namespace Gum.Themes.Converters;

public class TabItemWidthMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is double totalWidth && values[1] is int tabCount and > 0)
        {
            // Calculate max width for each tab
            double maxTabWidth = totalWidth / tabCount;

            // Optional: Ensure that each tab has a minimum width, for better usability
            return maxTabWidth > 32 ? maxTabWidth : 32;
        }

        return 32; // Default minimum width
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}