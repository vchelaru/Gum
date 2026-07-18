using Gum.ViewModels;
using System.Windows.Controls;

namespace Gum.Extensions;

/// <summary>
/// Converts framework-neutral <see cref="ContextMenuItemViewModel"/> trees (ADR-0005) into real
/// WPF <see cref="MenuItem"/>/<see cref="Separator"/> controls. Shared by every WPF view that hosts
/// a ViewModel-driven right-click menu (e.g. <c>EditingManager.RightClick.cs</c>,
/// <c>StateAnimationPlugin/Views/MainWindow.xaml.cs</c>, <c>MainPropertyGrid.xaml.cs</c>).
/// </summary>
public static class ContextMenuItemViewModelExtensions
{
    public static Control ToMenuItem(this ContextMenuItemViewModel item)
    {
        if (item.IsSeparator)
        {
            return new Separator();
        }

        var menuItem = new MenuItem { Header = item.Text };

        if (item.Action != null)
        {
            menuItem.Click += (_, _) => item.Action();
        }

        foreach (var child in item.Children)
        {
            menuItem.Items.Add(child.ToMenuItem());
        }

        return menuItem;
    }
}
