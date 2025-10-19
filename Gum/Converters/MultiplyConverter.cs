using System;
using System.Globalization;
using System.Windows.Data;

namespace Gum.Converters;

public class MultiplyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {

        if (value is double d && 
            parameter is string s && 
            double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double factor))
        {
            return d * factor;

        }
        else if(value is double)
        {
            return Math.Round((decimal)value);
        }
        else
        {
            throw new InvalidOperationException($"Could not handle parsing values {value} to {targetType} {parameter})");
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}