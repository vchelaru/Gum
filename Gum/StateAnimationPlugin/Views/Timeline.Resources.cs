using StateAnimationPlugin.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace StateAnimationPlugin.Controls;

internal sealed class KeyframeTemplateSelector : DataTemplateSelector
{
    public DataTemplate? EventTemplate { get; set; }
    public DataTemplate? StateTemplate { get; set; }
    public DataTemplate? DefaultTemplate { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is AnimatedKeyframeViewModel kf)
        {
            if (!string.IsNullOrEmpty(kf.EventName)) return EventTemplate ?? DefaultTemplate!;
            if (!string.IsNullOrEmpty(kf.StateName)) return StateTemplate ?? DefaultTemplate!;
        }
        return DefaultTemplate!;
    }
}

internal sealed class TimeToXConverter : IMultiValueConverter
{
    // values: [0]=time (double), [1]=duration (double), [2]=availableWidth (double)
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (!Try.Double(values[0], out var time) ||
            !Try.Double(values[1], out var total) ||
            !Try.Double(values[2], out var width) || total <= 0.0)
            return 0.0;

        return Math.Max(0, (time / total) * Math.Max(0, width));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

// length -> Rectangle.Width
internal sealed class LengthToWidthConverter : IMultiValueConverter
{
    // values: [0]=length, [1]=duration, [2]=availableWidth
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (!Try.Double(values[0], out var len) ||
            !Try.Double(values[1], out var total) ||
            !Try.Double(values[2], out var width) || total <= 0.0)
            return 0.0;

        var w = (len / total) * Math.Max(0, width);
        return Math.Max(2, w); // tiny minimum so very short segments are visible
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

}

internal sealed class TimeToCenteredLeftConverter : IMultiValueConverter
{
    // values: [0]=time, [1]=length, [2]=trackWidth (ItemsControl.ActualWidth), [3]=itemWidth (ContentPresenter.ActualWidth)
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (!Try.Double(values[0], out var time) ||
            !Try.Double(values[1], out var length) || length <= 0 ||
            !Try.Double(values[2], out var trackWidth))
            return 0d;

        var x = (time / length) * Math.Max(0, trackWidth);

        // itemWidth may be 0 on first measure; if so, don't shift.
        if (Try.Double(values[3], out var itemWidth) && itemWidth > 0)
            x -= itemWidth / 2.0;

        return x;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

}

internal sealed class HalfToHorizontalThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            var half = d / 2.0;
            return new Thickness(-half, 0, -half, 0); // negative L/R
        }
        return new Thickness(0);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

file static class Try
{
    public static bool Double(object? value, out double result)
    {
        switch (value)
        {
            case double dd: result = dd; return true;
            case float ff: result = ff; return true;
            case int ii: result = ii; return true;
            default: return double.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }
}