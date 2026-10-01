using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaDataUi;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Avalonia.Services;
using Gum.ViewModels;
using FluentIcons.Avalonia;
using Gum.Avalonia.Themes;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Renders framework-neutral <see cref="ContextMenuItemViewModel"/> trees as Avalonia menus. Shared
/// by every Avalonia view with a view-model-driven right-click menu; the counterpart of the WPF
/// <c>ContextMenuItemViewModelExtensions</c>. An item's shortcut becomes a <see cref="KeyGesture"/>
/// with the platform's command modifier, which Avalonia renders in the platform's spelling (⌘ on macOS).
/// </summary>
public static class AvaloniaContextMenus
{
    /// <summary>The default icon edge length in menus.</summary>
    public const double DefaultIconSize = 14;

    /// <summary>Replaces <paramref name="menu"/>'s items with <paramref name="items"/>.</summary>
    public static void Populate(ContextMenu menu, IReadOnlyList<ContextMenuItemViewModel> items, double iconSize = DefaultIconSize) =>
        Populate(menu, items, iconSize, PlatformKeyModifiers.Command);

    /// <summary>
    /// Replaces <paramref name="menu"/>'s items with <paramref name="items"/>, showing shortcuts with
    /// <paramref name="commandModifiers"/> as the neutral Ctrl.
    /// </summary>
    public static void Populate(ContextMenu menu, IReadOnlyList<ContextMenuItemViewModel> items, double iconSize, KeyModifiers commandModifiers)
    {
        menu.Items.Clear();
        foreach (ContextMenuItemViewModel item in items)
        {
            menu.Items.Add(ToMenuItem(item, iconSize, commandModifiers));
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

    /// <summary>Converts one item and its children, showing shortcuts with the running platform's command modifier.</summary>
    public static Control ToMenuItem(ContextMenuItemViewModel item, double iconSize = DefaultIconSize) =>
        ToMenuItem(item, iconSize, PlatformKeyModifiers.Command);

    /// <summary>Converts one item and its children, showing shortcuts with <paramref name="commandModifiers"/> as the neutral Ctrl.</summary>
    public static Control ToMenuItem(ContextMenuItemViewModel item, double iconSize, KeyModifiers commandModifiers)
    {
        if (item.IsSeparator)
        {
            return new Separator();
        }

        MenuItem menuItem = new MenuItem
        {
            Header = item.Text,
            IsEnabled = item.IsEnabled,
            InputGesture = item.Shortcut?.ToKeyGesture(commandModifiers),
        };
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
            menuItem.Items.Add(ToMenuItem(child, iconSize, commandModifiers));
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
}
