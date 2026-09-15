using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaDataUi;
using Gum.Avalonia.Services;
using Gum.Menus;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Renders a <see cref="MenuModel"/> into a <see cref="NativeMenu"/> for the macOS menu bar and
/// keeps it in sync the same way <see cref="AvaloniaMenuBuilder"/> does for the in-window menu.
/// A model's <see cref="MenuItemModel.Gesture"/> becomes the item's key equivalent, which is a
/// real binding: AppKit matches it before the window's keyDown, so the item fires and the hotkey
/// manager never sees the key. Both call the same action, so nothing runs twice.
/// </summary>
public static class AvaloniaNativeMenuBuilder
{
    /// <summary>Builds the menu-bar menu for <paramref name="model"/>, binding gestures with the running platform's command modifier.</summary>
    public static NativeMenu Build(MenuModel model) => Build(model, PlatformKeyModifiers.Command);

    /// <summary>Builds the menu-bar menu for <paramref name="model"/>, binding gestures with <paramref name="commandModifiers"/> as the neutral Ctrl.</summary>
    public static NativeMenu Build(MenuModel model, KeyModifiers commandModifiers)
    {
        NativeMenu menu = new NativeMenu();
        Populate(menu.Items, model.TopLevelItems, commandModifiers);
        model.TopLevelItems.CollectionChanged += (_, _) => Populate(menu.Items, model.TopLevelItems, commandModifiers);
        return menu;
    }

    private static void Populate(IList<NativeMenuItemBase> target, ObservableCollection<MenuItemModel> items, KeyModifiers commandModifiers)
    {
        target.Clear();
        foreach (MenuItemModel item in items)
        {
            target.Add(item.IsSeparator ? new NativeMenuItemSeparator() : Create(item, commandModifiers));
        }
    }

    private static NativeMenuItem Create(MenuItemModel model, KeyModifiers commandModifiers)
    {
        NativeMenuItem menuItem = new NativeMenuItem
        {
            Header = model.Header,
            IsEnabled = model.IsEnabled,
            ToggleType = model.IsCheckable ? NativeMenuItemToggleType.CheckBox : NativeMenuItemToggleType.None,
            IsChecked = model.IsChecked,
            ToolTip = model.ToolTip,
            Gesture = model.Gesture?.ToKeyGesture(commandModifiers),
        };

        if (model.Items.Count > 0)
        {
            NativeMenu submenu = new NativeMenu();
            Populate(submenu.Items, model.Items, commandModifiers);
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
            Populate(menuItem.Menu.Items, model.Items, commandModifiers);
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
