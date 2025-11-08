using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace Gum.Themes;

public class FrbTabControl
{
    public static readonly DependencyProperty PreferTabTruncationProperty =
        DependencyProperty.RegisterAttached("PreferTabTruncation", typeof(bool), typeof(FrbTabControl),
            new PropertyMetadata(default(bool)));

    public static bool GetPreferTabTruncation(TabControl obj)
    {
        return (bool)obj.GetValue(PreferTabTruncationProperty);
    }

    public static void SetPreferTabTruncation(TabControl obj, bool value)
    {
        obj.SetValue(PreferTabTruncationProperty, value);
    }

    public static readonly DependencyProperty TruncationIconProperty =
        DependencyProperty.RegisterAttached("TruncationIcon", typeof(PackIconKind?), typeof(FrbTabControl),
            new PropertyMetadata(default(PackIconKind?)));

    public static PackIconKind? GetTruncationIcon(TabItem obj)
    {
        return (PackIconKind?)obj.GetValue(TruncationIconProperty);
    }

    public static void SetTruncationIcon(TabItem obj, PackIconKind? value)
    {
        obj.SetValue(TruncationIconProperty, value);
    }

    public static readonly DependencyProperty MaxTabWidthProperty =
        DependencyProperty.RegisterAttached("MaxTabWidth", typeof(double?), typeof(FrbTabControl),
            new PropertyMetadata(default(double?)));

    public static double? GetMaxTabWidth(TabControl obj)
    {
        var valueAsObjct = obj.GetValue(MaxTabWidthProperty); 
        if(valueAsObjct is double asDouble)
        {
            return asDouble;
        }
        else
        {
            return null;
        }
    }

    public static void SetMaxTabWidth(TabControl obj, double? value)
    {
        obj.SetValue(MaxTabWidthProperty, value);
    }
}