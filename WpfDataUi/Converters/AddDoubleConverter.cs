using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfDataUi.Converters
{
    public class AddDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // "value" is the original font size
            // "parameter" could be "2"
            if (value is double originalValue && parameter is string paramString && double.TryParse(paramString, out double add))
            {
                return originalValue + add;
            }
            return value; // Fallback if something goes wrong
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Usually you don’t need ConvertBack for FontSize
            return Binding.DoNothing;
        }
    }

}
