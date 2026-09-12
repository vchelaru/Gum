using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Gum.Menus;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Renders a <see cref="MenuModel"/> into an Avalonia <see cref="Menu"/> and keeps it in sync:
/// header, enabled, and check state follow the model's property changes, and an item's children
/// are rebuilt when its collection changes.
/// </summary>
public static class AvaloniaMenuBuilder
{
    /// <summary>Builds the menu control for <paramref name="model"/>.</summary>
    public static Menu Build(MenuModel model)
    {
        Menu menu = new Menu();
        Populate(menu.Items, model.TopLevelItems);
        model.TopLevelItems.CollectionChanged += (_, _) => Populate(menu.Items, model.TopLevelItems);
        return menu;
    }

    private static void Populate(ItemCollection target, ObservableCollection<MenuItemModel> items)
    {
        target.Clear();
        foreach (MenuItemModel item in items)
        {
            target.Add(item.IsSeparator ? new Separator() : Create(item));
        }
    }

    private static MenuItem Create(MenuItemModel model)
    {
        MenuItem menuItem = new MenuItem
        {
            Header = model.Header,
            IsEnabled = model.IsEnabled,
            ToggleType = model.IsCheckable ? MenuItemToggleType.CheckBox : MenuItemToggleType.None,
            IsChecked = model.IsChecked,
        };
        if (model.InputGestureText != null)
        {
            try
            {
                menuItem.InputGesture = KeyGesture.Parse(model.InputGestureText);
            }
            catch (ArgumentException)
            {
                // Display-only text that is not a gesture; the hotkey manager owns real bindings.
            }
        }
        if (model.ToolTip != null)
        {
            ToolTip.SetTip(menuItem, model.ToolTip);
        }

        menuItem.Click += (_, e) =>
        {
            // A submenu header's click just opens it; only leaves invoke, once the menu has closed.
            if (model.Items.Count == 0)
            {
                MenuItemActions.InvokeAfterClose(model.Invoke);
                e.Handled = true;
            }
        };

        model.PropertyChanged += (_, e) => Apply(menuItem, model, e);
        Populate(menuItem.Items, model.Items);
        model.Items.CollectionChanged += (_, _) => Populate(menuItem.Items, model.Items);
        return menuItem;
    }

    private static void Apply(MenuItem menuItem, MenuItemModel model, PropertyChangedEventArgs e)
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
