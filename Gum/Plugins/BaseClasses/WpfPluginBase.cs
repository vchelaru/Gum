using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using Gum.Gui.Windows;
using Gum.Managers;
using MenuItem = System.Windows.Controls.MenuItem;

namespace Gum.Plugins.BaseClasses;

/// <summary>
/// <see cref="PluginBase"/> plus the members that are genuinely WPF-coupled: menu items
/// (<c>System.Windows.Controls.MenuItem</c>) and the delete-confirmation dialog events
/// (<c>Gum.Gui.Windows.DeleteOptionsWindow</c> is a real WPF <see cref="System.Windows.Window"/>).
/// Plugins that don't touch menus or the delete dialog can inherit <see cref="PluginBase"/>
/// directly and stay WPF-free.
/// </summary>
public abstract class WpfPluginBase : PluginBase
{
    private MenuStripManager _menuStripManager;

    [Import] public MenuStripManager MenuStripManager { get => _menuStripManager; set => _menuStripManager = value; }

    public event Action<DeleteOptionsWindow, Array>? DeleteOptionsWindowShow;
    public event Action<DeleteOptionsWindow, Array>? DeleteConfirmed;

    #region Menu Items

    /// <summary>
    /// Adds a menu item using the path specified by the menuAndSubmenus.
    /// </summary>
    /// <param name="menuAndSubmenus">The menu path. The first item may specify an existing menu to add to.
    /// For example, to add a Properties item to the existing Edit item, the following
    /// parameter could be used:
    /// new List<string> { "Edit", "Properties" }
    /// </param>
    /// <returns>The newly-created menu item.</returns>
    public MenuItem AddMenuItem(IEnumerable<string> menuAndSubmenus) =>
        _menuStripManager.AddMenuItem(menuAndSubmenus);

    public MenuItem AddMenuItem(params string[] menuAndSubmenus)
    {
        return AddMenuItem((IEnumerable<string>)menuAndSubmenus);
    }

    MenuItem GetItem(string name) => _menuStripManager.GetItem(name);

    public MenuItem GetChildMenuItem(string parentText, string childText)
    {
        MenuItem parentItem = GetItem(parentText);
        if (parentItem != null)
        {
            MenuItem childMenuItem = parentItem.Items
                .OfType<MenuItem>()
                .FirstOrDefault(item => item.Header as string == childText);

            return childMenuItem;
        }

        return null;
    }


    protected MenuItem AddMenuItemTo(string whatToAdd, RoutedEventHandler eventHandler, string container, int? preferredIndex = null)
    {
        var menuItem = new MenuItem { Header = whatToAdd };
        if (eventHandler != null)
            menuItem.Click += eventHandler;

        MenuItem itemToAddTo = GetItem(container);

#if DEBUG
        if (itemToAddTo == null)
        {
            throw new InvalidOperationException(
                $"Could not find menu item '{container}'. Make sure the menu is populated before calling AddMenuItemTo.");
        }
#endif

        if (preferredIndex == -1)
        {
            itemToAddTo.Items.Add(menuItem);
        }
        else
        {
            int indexToInsertAt = itemToAddTo.Items.Count;
            if(preferredIndex != null)
            {
                indexToInsertAt = System.Math.Min(preferredIndex.Value, itemToAddTo.Items.Count);
            }

            itemToAddTo.Items.Insert(indexToInsertAt, menuItem);
        }

        _menuStripManager.ApplyLayout(container);

        return menuItem;
    }

    #endregion

    #region Event calling

    public void CallDeleteOptionsWindowShow(DeleteOptionsWindow optionsWindow, Array objectsToDelete) =>
        DeleteOptionsWindowShow?.Invoke(optionsWindow, objectsToDelete);

    public void CallDeleteConfirmed(DeleteOptionsWindow optionsWindow, Array deletedObjects) =>
        DeleteConfirmed?.Invoke(optionsWindow, deletedObjects);

    #endregion
}
