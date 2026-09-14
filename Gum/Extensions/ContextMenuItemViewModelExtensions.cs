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

    public static Control ToMenuItem(this ContextMenuItemViewModel item) => item.ToMenuItem(MenuIconSize);

    /// <summary>
    /// Converts <paramref name="item"/> and its children, drawing icons at <paramref name="iconSize"/>.
    /// </summary>
    public static Control ToMenuItem(this ContextMenuItemViewModel item, double iconSize)
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
            menuItem.Icon = CreateIcon(item.IconKey, iconSize);
        }

        if (item.Action != null)
        {
            menuItem.Click += (_, _) => item.Action();
        }

        foreach (var child in item.Children)
        {
            menuItem.Items.Add(child.ToMenuItem(iconSize));
        }

        return menuItem;
    }

    /// <summary>
    /// Category and State match the icons the States tree uses for its rows (<c>StateTreeView.xaml</c>'s
    /// "DatabaseMultiple"/"Database" FluentIcons), so an "Add State"/"Add Category" item reads as the
    /// concept it creates. Any other key is a tree icon file name, such as "Sprite_Instance.png" in the
    /// element tree's add menus.
    /// </summary>
    private static object? CreateIcon(string iconKey, double size) => iconKey switch
    {
        ContextMenuIconKeys.Category => new FluentIcon { Icon = FluentIcons.Common.Icon.DatabaseMultiple, FontSize = size },
        ContextMenuIconKeys.State => new FluentIcon { Icon = FluentIcons.Common.Icon.Database, FontSize = size },
        _ => Gum.Controls.TreeIconRegistry.CreateIcon(iconKey, size)
    };
}
