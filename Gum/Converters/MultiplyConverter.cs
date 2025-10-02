using System;
using System.Globalization;
using System.Windows.Data;

namespace Gum.Converters;

public class MultiplyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d && parameter is string s && double.TryParse(s, out double factor))
            return d * factor;
        return Math.Round((decimal)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}