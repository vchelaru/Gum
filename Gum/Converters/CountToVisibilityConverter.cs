using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Gum.Converters;

public sealed class CountToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// If true, flips the comparison to (count <= threshold) instead of (count > threshold).
    /// </summary>
    public bool InvertThreshold { get; set; }

    /// <summary>
    /// If true, returns Hidden instead of Collapsed when not visible.
    /// </summary>
    public bool UseHidden { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int count = value is int i ? i : 0;

        int threshold = 0;
        if (parameter is string s && int.TryParse(s, NumberStyles.Integer, culture, out var parsed))
            threshold = parsed;
        else if (parameter is int ip)
            threshold = ip;

        bool isVisible = InvertThreshold ? (count <= threshold) : (count > threshold);

        if (targetType == typeof(bool) || targetType == typeof(bool?))
            return isVisible;

        return isVisible
            ? Visibility.Visible
            : (UseHidden ? Visibility.Hidden : Visibility.Collapsed);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
