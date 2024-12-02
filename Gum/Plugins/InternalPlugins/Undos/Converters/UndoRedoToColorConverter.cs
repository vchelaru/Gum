using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Gum.Plugins.InternalPlugins.Undos.Converters;

public class UndoOrRedoToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is UndoOrRedo enumValue)
        {
            // Return Black for Undo and Gray for Redo
            return enumValue == UndoOrRedo.Undo ? Brushes.Black : Brushes.Gray;
        }
        return Brushes.Black; // Default to Black if value is not an UndoOrRedo enum
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException(); // ConvertBack not required for this scenario
    }
}