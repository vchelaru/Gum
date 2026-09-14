using System.Windows.Media;

namespace WpfDataUi.Controls;

/// <summary>The WPF brushes for <see cref="DataTypes.DataUiValueState"/> field tints.</summary>
public static class DataUiBrushes
{
    public static SolidColorBrush DefaultValueBackground = new SolidColorBrush(Color.FromRgb(180, 255, 180)) { Opacity = 0.5 };
    public static SolidColorBrush IndeterminateValueBackground = new SolidColorBrush(Colors.LightGray);
    public static SolidColorBrush CustomValueBackground = Brushes.White;
}
