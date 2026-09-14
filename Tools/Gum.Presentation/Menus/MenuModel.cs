using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gum.Menus;

/// <summary>
/// The tool's main menu as a tree of <see cref="MenuItemModel"/>s. Owns the rules every head and
/// every plugin relied on with the WPF menu strip: a new top-level menu goes before Help, a path
/// like "Content" > "Export" > "As Image" creates the intermediate menus, and named top-level
/// menus keep a stable grouped order (<see cref="ApplyLayout"/>).
/// </summary>
public class MenuModel
{
    // Stable per-menu layout. Items in each inner list form a group; separators are inserted
    // between groups. Items not listed fall through to a trailing group in insertion order so
    // third-party plugins remain visible.
    private static readonly Dictionary<string, string[][]> _menuLayouts = new()
    {
        ["Content"] = new[]
        {
            new[] { "Find file references..." },
            new[] { "Add Forms Components", "Import" },
            new[]
            {
                "Clear Font Cache",
                "Re-create missing font files",
                "Force re-create all font files",
                "View Font Cache",
            },
        },
    };

    /// <summary>Creates an empty menu.</summary>
    public MenuModel()
    {
        TopLevelItems = new ObservableCollection<MenuItemModel>();
    }

    /// <summary>The top-level menus, left to right.</summary>
    public ObservableCollection<MenuItemModel> TopLevelItems { get; }

    /// <summary>The top-level menu with <paramref name="header"/>, or null.</summary>
    public MenuItemModel? GetItem(string header) =>
        TopLevelItems.FirstOrDefault(item => item.Header == header);

    /// <summary>
    /// Adds an item at the given path, creating any missing top-level menu (before "Help") and
    /// intermediate submenus, then re-applies the top-level menu's layout. Returns the new item.
    /// </summary>
    public MenuItemModel AddMenuItem(IEnumerable<string> menuAndSubmenus, Action? click = null)
    {
        List<string> parts = menuAndSubmenus.ToList();
        if (parts.Count == 0)
        {
            throw new ArgumentException("A menu path needs at least one part.", nameof(menuAndSubmenus));
        }

        string topMenuName = parts[0];
        MenuItemModel? currentParent = GetItem(topMenuName);
        if (currentParent == null)
        {
            currentParent = new MenuItemModel(topMenuName);
            // Help stays last, so a new top-level menu goes just before it.
            int helpIndex = TopLevelItems.ToList().FindIndex(item => item.Header == "Help");
            TopLevelItems.Insert(helpIndex < 0 ? TopLevelItems.Count : helpIndex, currentParent);
        }

        if (parts.Count == 1)
        {
            return currentParent;
        }

        for (int i = 1; i < parts.Count - 1; i++)
        {
            MenuItemModel? submenu = currentParent.Items.FirstOrDefault(item => item.Header == parts[i]);
            if (submenu == null)
            {
                submenu = new MenuItemModel(parts[i]);
                currentParent.Items.Add(submenu);
            }
            currentParent = submenu;
        }

        MenuItemModel newItem = new MenuItemModel(parts[^1], click);
        currentParent.Items.Add(newItem);
        ApplyLayout(topMenuName);
        return newItem;
    }

    /// <summary>
    /// Re-orders the children of the given top-level menu according to the layout table and
    /// re-inserts group separators. No-op for menus without a layout entry.
    /// </summary>
    public void ApplyLayout(string topMenuName)
    {
        if (!_menuLayouts.TryGetValue(topMenuName, out string[][]? groups))
        {
            return;
        }

        MenuItemModel? parent = GetItem(topMenuName);
        if (parent == null)
        {
            return;
        }

        List<MenuItemModel> existing = parent.Items.Where(item => !item.IsSeparator).ToList();
        Dictionary<string, MenuItemModel> byHeader = new Dictionary<string, MenuItemModel>();
        foreach (MenuItemModel item in existing)
        {
            byHeader.TryAdd(item.Header, item);
        }

        List<MenuItemModel> ordered = new List<MenuItemModel>();
        HashSet<MenuItemModel> placed = new HashSet<MenuItemModel>();
        foreach (string[] group in groups)
        {
            List<MenuItemModel> groupItems = new List<MenuItemModel>();
            foreach (string header in group)
            {
                if (byHeader.TryGetValue(header, out MenuItemModel? item))
                {
                    groupItems.Add(item);
                    placed.Add(item);
                }
            }
            if (groupItems.Count == 0)
            {
                continue;
            }
            if (ordered.Count > 0)
            {
                ordered.Add(MenuItemModel.Separator());
            }
            ordered.AddRange(groupItems);
        }

        List<MenuItemModel> leftovers = existing.Where(item => !placed.Contains(item)).ToList();
        if (leftovers.Count > 0)
        {
            if (ordered.Count > 0)
            {
                ordered.Add(MenuItemModel.Separator());
            }
            ordered.AddRange(leftovers);
        }

        parent.Items.Clear();
        foreach (MenuItemModel item in ordered)
        {
            parent.Items.Add(item);
        }
    }
}
