using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Gum.Avalonia.Plugins.TreeView;
using Gum.ViewModels;
using FluentIcons.Avalonia;
using Gum.Avalonia.Themes;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Renders framework-neutral <see cref="ContextMenuItemViewModel"/> trees as Avalonia menus. Shared
/// by every Avalonia view with a view-model-driven right-click menu; the counterpart of the WPF
/// <c>ContextMenuItemViewModelExtensions</c>.
/// </summary>
public static class AvaloniaContextMenus
{
    /// <summary>The default icon edge length in menus.</summary>
    public const double DefaultIconSize = 14;

    /// <summary>Replaces <paramref name="menu"/>'s items with <paramref name="items"/>.</summary>
    public static void Populate(ContextMenu menu, IReadOnlyList<ContextMenuItemViewModel> items, double iconSize = DefaultIconSize)
    {
        menu.Items.Clear();
        foreach (ContextMenuItemViewModel item in items)
        {
            menu.Items.Add(ToMenuItem(item, iconSize));
        }
    }

    /// <summary>
    /// A context menu that rebuilds its items from <paramref name="items"/> each time it opens (so it
    /// always shows the view model's current entries) and stays closed when there are none.
    /// </summary>
    public static ContextMenu CreateRebuildingMenu(Func<IEnumerable<ContextMenuItemViewModel>?> items, double iconSize = DefaultIconSize)
    {
        ContextMenu menu = new ContextMenu();
        menu.Opening += (_, e) =>
        {
            menu.Items.Clear();
            foreach (ContextMenuItemViewModel item in items() ?? Array.Empty<ContextMenuItemViewModel>())
            {
                menu.Items.Add(ToMenuItem(item, iconSize));
            }
            e.Cancel = menu.Items.Count == 0;
        };
        return menu;
    }

    /// <summary>Converts one item and its children.</summary>
    public static Control ToMenuItem(ContextMenuItemViewModel item, double iconSize = DefaultIconSize)
    {
        if (item.IsSeparator)
        {
            return new Separator();
        }

        MenuItem menuItem = new MenuItem { Header = item.Text, IsEnabled = item.IsEnabled };
        if (item.Shortcut != null)
        {
            menuItem.InputGesture = TryParseGesture(item.Shortcut);
        }
        if (item.IconKey != null)
        {
            menuItem.Icon = CreateIcon(item.IconKey, iconSize);
        }
        if (item.Action != null)
        {
            menuItem.Click += (_, _) => MenuItemActions.InvokeAfterClose(item.Action);
        }
        foreach (ContextMenuItemViewModel child in item.Children)
        {
            menuItem.Items.Add(ToMenuItem(child, iconSize));
        }
        return menuItem;
    }

    // The States tree's category and state glyphs are Fluent System Icons, as in the WPF head; every
    // other key is a tree icon file name.
    private static object? CreateIcon(string key, double size) => key switch
    {
        ContextMenuIconKeys.Category => GumFluentIcons.Create(FluentIcons.Common.Icon.DatabaseMultiple, size),
        ContextMenuIconKeys.State => GumFluentIcons.Create(FluentIcons.Common.Icon.Database, size),
        _ => AvaloniaTreeIcons.CreateIcon(key, size),
    };

    private static KeyGesture? TryParseGesture(string shortcut)
    {
        try
        {
            return KeyGesture.Parse(shortcut);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
