using FluentIcons.Wpf;
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
    private const double MenuIconSize = 14;

    public static Control ToMenuItem(this ContextMenuItemViewModel item)
    {
        if (item.IsSeparator)
        {
            return new Separator();
        }

        var menuItem = new MenuItem { Header = item.Text, IsEnabled = item.IsEnabled };

        if (item.Shortcut != null)
        {
            menuItem.InputGestureText = item.Shortcut;
        }

        if (item.IconKey != null)
        {
            menuItem.Icon = CreateIcon(item.IconKey);
        }

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

    /// <summary>
    /// Matches the icons the States tree itself uses for category/state rows
    /// (<c>StateTreeView.xaml</c>'s "DatabaseMultiple"/"Database" FluentIcons), so an "Add
    /// State"/"Add Category" menu item reads as the same concept as the row it will create.
    /// </summary>
    private static FluentIcon? CreateIcon(string iconKey) => iconKey switch
    {
        ContextMenuIconKeys.Category => new FluentIcon { Icon = FluentIcons.Common.Icon.DatabaseMultiple, FontSize = MenuIconSize },
        ContextMenuIconKeys.State => new FluentIcon { Icon = FluentIcons.Common.Icon.Database, FontSize = MenuIconSize },
        _ => null
    };
}
