using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Gum.Menus;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Renders a <see cref="MenuModel"/> into a <see cref="NativeMenu"/> for the macOS menu bar and
/// keeps it in sync the same way <see cref="AvaloniaMenuBuilder"/> does for the in-window menu.
/// Shortcut text is left off: the hotkey manager owns the bindings, and a native gesture would be
/// a second, real binding.
/// </summary>
public static class AvaloniaNativeMenuBuilder
{
    /// <summary>Builds the menu-bar menu for <paramref name="model"/>.</summary>
    public static NativeMenu Build(MenuModel model)
    {
        NativeMenu menu = new NativeMenu();
        Populate(menu.Items, model.TopLevelItems);
        model.TopLevelItems.CollectionChanged += (_, _) => Populate(menu.Items, model.TopLevelItems);
        return menu;
    }

    /// <summary>
    /// Builds the application menu (the one named after the app, left of File) with an About Gum
    /// item that runs <paramref name="showAbout"/>. Avalonia appends the standard Services, Hide
    /// and Quit items itself.
    /// </summary>
    public static NativeMenu BuildAppMenu(Action showAbout)
    {
        NativeMenu menu = new NativeMenu();
        menu.Items.Add(Create(new MenuItemModel("About Gum", showAbout)));
        return menu;
    }

    private static void Populate(IList<NativeMenuItemBase> target, ObservableCollection<MenuItemModel> items)
    {
        target.Clear();
        foreach (MenuItemModel item in items)
        {
            target.Add(item.IsSeparator ? new NativeMenuItemSeparator() : Create(item));
        }
    }

    private static NativeMenuItem Create(MenuItemModel model)
    {
        NativeMenuItem menuItem = new NativeMenuItem
        {
            Header = model.Header,
            IsEnabled = model.IsEnabled,
            ToggleType = model.IsCheckable ? NativeMenuItemToggleType.CheckBox : NativeMenuItemToggleType.None,
            IsChecked = model.IsChecked,
            ToolTip = model.ToolTip,
        };

        if (model.Items.Count > 0)
        {
            NativeMenu submenu = new NativeMenu();
            Populate(submenu.Items, model.Items);
            menuItem.Menu = submenu;
        }
        else
        {
            // The menu has closed by the time the click reaches managed code, but the action still
            // runs off the dispatcher so a synchronous dialog opens outside the native callback.
            menuItem.Click += (_, _) => MenuItemActions.InvokeAfterClose(model.Invoke);
        }

        model.PropertyChanged += (_, e) => Apply(menuItem, model, e);
        model.Items.CollectionChanged += (_, _) =>
        {
            menuItem.Menu ??= new NativeMenu();
            Populate(menuItem.Menu.Items, model.Items);
        };
        return menuItem;
    }

    private static void Apply(NativeMenuItem menuItem, MenuItemModel model, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MenuItemModel.Header):
                menuItem.Header = model.Header;
                break;
            case nameof(MenuItemModel.IsEnabled):
                menuItem.IsEnabled = model.IsEnabled;
                break;
            case nameof(MenuItemModel.IsChecked):
                menuItem.IsChecked = model.IsChecked;
                break;
        }
    }
}
