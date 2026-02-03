using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfDataUi;

// obtained from:
// http://stackoverflow.com/questions/1070685/hiding-the-arrows-for-the-wpf-expander-control
public class DataGridAttachedProperties
{
    #region HideExpanderArrow AttachedProperty

    [AttachedPropertyBrowsableForType(typeof(Expander))]
    public static bool GetHideExpanderArrow(DependencyObject obj)
    {
        return (bool)obj.GetValue(HideExpanderArrowProperty);
    }

    [AttachedPropertyBrowsableForType(typeof(Expander))]
    public static void SetHideExpanderArrow(DependencyObject obj, bool value)
    {
        obj.SetValue(HideExpanderArrowProperty, value);
    }

    // Using a DependencyProperty as the backing store for HideExpanderArrow.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty HideExpanderArrowProperty =
        DependencyProperty.RegisterAttached("HideExpanderArrow", typeof(bool), 
                typeof(DataGridAttachedProperties), 
                new UIPropertyMetadata(false, OnHideExpanderArrowChanged));

    private static void OnHideExpanderArrowChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
    {
        Expander expander = (Expander)o;

        if (expander.IsLoaded)
        {
            UpdateExpanderArrow(expander, (bool)e.NewValue);
        }
        else
        {
            expander.Loaded += new RoutedEventHandler((x, y) => UpdateExpanderArrow(expander, (bool)e.NewValue));
        }
    }

    private static void UpdateExpanderArrow(Expander expander, bool hidden)
    {
        FrameworkElement? headerSite = FindVisualChildByName<FrameworkElement>(expander, "HeaderSite");

        if (headerSite != null)
        {
            headerSite.Visibility = hidden ? Visibility.Collapsed : Visibility.Visible;
        }

        T? FindVisualChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && typedChild.Name == name)
                {
                    return typedChild;
                }

                var result = FindVisualChildByName<T>(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }

    #endregion
}
