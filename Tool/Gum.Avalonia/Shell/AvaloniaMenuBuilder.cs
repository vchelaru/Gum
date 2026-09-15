using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaDataUi;
using Gum.Avalonia.Services;
using Gum.Menus;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Renders a <see cref="MenuModel"/> into an Avalonia <see cref="Menu"/> and keeps it in sync:
/// header, enabled, and check state follow the model's property changes, and an item's children
/// are rebuilt when its collection changes. A model's <see cref="MenuItemModel.Gesture"/> is shown
/// beside the item but not bound; the hotkey manager owns the binding.
/// </summary>
public static class AvaloniaMenuBuilder
{
    /// <summary>Builds the menu control for <paramref name="model"/>, showing gestures with the running platform's command modifier.</summary>
    public static Menu Build(MenuModel model) => Build(model, PlatformKeyModifiers.Command);

    /// <summary>Builds the menu control for <paramref name="model"/>, showing gestures with <paramref name="commandModifiers"/> as the neutral Ctrl.</summary>
    public static Menu Build(MenuModel model, KeyModifiers commandModifiers)
    {
        Menu menu = new Menu();
        Populate(menu.Items, model.TopLevelItems, commandModifiers);
        model.TopLevelItems.CollectionChanged += (_, _) => Populate(menu.Items, model.TopLevelItems, commandModifiers);
        return menu;
    }

    private static void Populate(ItemCollection target, ObservableCollection<MenuItemModel> items, KeyModifiers commandModifiers)
    {
        target.Clear();
        foreach (MenuItemModel item in items)
        {
            target.Add(item.IsSeparator ? new Separator() : Create(item, commandModifiers));
        }
    }

    private static MenuItem Create(MenuItemModel model, KeyModifiers commandModifiers)
    {
        MenuItem menuItem = new MenuItem
        {
            Header = model.Header,
            IsEnabled = model.IsEnabled,
            ToggleType = model.IsCheckable ? MenuItemToggleType.CheckBox : MenuItemToggleType.None,
            IsChecked = model.IsChecked,
            InputGesture = model.Gesture?.ToKeyGesture(commandModifiers),
        };
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
        Populate(menuItem.Items, model.Items, commandModifiers);
        model.Items.CollectionChanged += (_, _) => Populate(menuItem.Items, model.Items, commandModifiers);
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
