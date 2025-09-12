using System.Windows;
using System.Windows.Media;

namespace Gum.Controls;

public static class TreeHelpers
{
    public static T? FindVisualAncestor<T>(DependencyObject? current)
        where T : DependencyObject
    {
        while (current != null && current is not T)
            current = VisualTreeHelper.GetParent(current);
        return current as T;
    }
}
