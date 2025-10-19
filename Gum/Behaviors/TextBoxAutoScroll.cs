using System.Windows;
using System.Windows.Controls;

namespace Gum.Behaviors;

public static class TextBoxAutoScroll
{
    public static readonly DependencyProperty AutoScrollToEndProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToEnd",
            typeof(bool),
            typeof(TextBoxAutoScroll),
            new PropertyMetadata(false, OnAutoScrollToEndChanged));

    public static void SetAutoScrollToEnd(DependencyObject d, bool value) =>
        d.SetValue(AutoScrollToEndProperty, value);

    public static bool GetAutoScrollToEnd(DependencyObject d) =>
        (bool)d.GetValue(AutoScrollToEndProperty);

    private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox tb)
        {
            if ((bool)e.NewValue)
            {
                tb.TextChanged += OnTextChanged;
            }
            else
            {
                tb.TextChanged -= OnTextChanged;
            }
        }
    }

    private static void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.CaretIndex = tb.Text.Length;
            tb.ScrollToEnd();
        }
    }
}