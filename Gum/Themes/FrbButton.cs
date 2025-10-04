using System.Windows;
using System.Windows.Controls.Primitives;

namespace FlatRedBall.Glue.Themes;

internal static class FrbButton
{
    public static readonly DependencyProperty BorderCornerRadiusProperty =
        DependencyProperty.RegisterAttached("BorderCornerRadius", typeof(CornerRadius), typeof(FrbButton),
            new PropertyMetadata(default(CornerRadius)));

    public static CornerRadius GetBorderCornerRadius(ButtonBase obj)
    {
        return (CornerRadius)obj.GetValue(BorderCornerRadiusProperty);
    }

    public static void SetBorderCornerRadius(ButtonBase obj, CornerRadius value)
    {
        obj.SetValue(BorderCornerRadiusProperty, value);
    }

    public static readonly DependencyProperty IconFontSizeProperty = DependencyProperty.RegisterAttached(
        "IconFontSize", typeof(double), typeof(FrbButton), new PropertyMetadata((double)12));

    public static void SetIconFontSize(DependencyObject element, double value)
    {
        element.SetValue(IconFontSizeProperty, value);
    }

    public static double GetIconFontSize(DependencyObject element)
    {
        return (double)element.GetValue(IconFontSizeProperty);
    }

}

