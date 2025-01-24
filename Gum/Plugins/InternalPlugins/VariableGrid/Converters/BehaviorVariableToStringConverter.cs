using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml.Linq;

namespace Gum.Plugins.InternalPlugins.VariableGrid.Converters;
public class BehaviorVariableToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if(value is VariableSave variableSave)
        {
            return variableSave.Name + " (" + variableSave.Type + ")";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // do nothing...
        return null;
    }
}
