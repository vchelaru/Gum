using System;
using System.Globalization;
using System.Windows.Data;

namespace Gum.Themes.Converters;

public class LessThanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }

        try
        {
            double numericValue = System.Convert.ToDouble(value);
            double comparisonValue = System.Convert.ToDouble(parameter);

            return numericValue < comparisonValue;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class GreaterThanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
        {
            return false;
        }

        try
        {
            double numericValue = System.Convert.ToDouble(value);
            double comparisonValue = System.Convert.ToDouble(parameter);

            return numericValue > comparisonValue;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}