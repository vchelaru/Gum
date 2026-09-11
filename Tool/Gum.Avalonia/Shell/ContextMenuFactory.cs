using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Gum.ViewModels;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Builds Avalonia menu items from the neutral <see cref="ContextMenuItemViewModel"/> (ADR-0005), for
/// every right-click menu in this head. Twin of the WPF head's
/// <c>ContextMenuItemViewModelExtensions.ToMenuItem</c>.
/// </summary>
public static class ContextMenuFactory
{
    /// <summary>A menu item (or separator) for <paramref name="item"/>, with its children.</summary>
    public static Control ToMenuItem(ContextMenuItemViewModel item)
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
        if (item.Action != null)
        {
            menuItem.Click += (_, _) => item.Action();
        }
        foreach (ContextMenuItemViewModel child in item.Children)
        {
            menuItem.Items.Add(ToMenuItem(child));
        }
        return menuItem;
    }

    /// <summary>
    /// A context menu that rebuilds its items from <paramref name="items"/> each time it opens (so it
    /// always shows the view model's current entries) and stays closed when there are none.
    /// </summary>
    public static ContextMenu CreateRebuildingMenu(Func<IEnumerable<ContextMenuItemViewModel>?> items)
    {
        ContextMenu menu = new ContextMenu();
        menu.Opening += (_, e) =>
        {
            menu.Items.Clear();
            foreach (ContextMenuItemViewModel item in items() ?? Array.Empty<ContextMenuItemViewModel>())
            {
                menu.Items.Add(ToMenuItem(item));
            }
            e.Cancel = menu.Items.Count == 0;
        };
        return menu;
    }

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
