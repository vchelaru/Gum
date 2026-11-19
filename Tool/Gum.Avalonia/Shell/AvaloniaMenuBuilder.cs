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
/// follow its collection changes through <see cref="MenuItemsSync"/>. A model's
/// <see cref="MenuItemModel.Gesture"/> is shown beside the item but not bound; the hotkey manager
/// owns the binding.
/// </summary>
public static class AvaloniaMenuBuilder
{
    /// <summary>Builds the menu control for <paramref name="model"/>, showing gestures with the running platform's command modifier.</summary>
    public static Menu Build(MenuModel model) => Build(model, PlatformKeyModifiers.Command);

    /// <summary>Builds the menu control for <paramref name="model"/>, showing gestures with <paramref name="commandModifiers"/> as the neutral Ctrl.</summary>
    public static Menu Build(MenuModel model, KeyModifiers commandModifiers)
    {
        Menu menu = new Menu();
        // Lives as long as the menu, like the model it follows.
        _ = new MenuItemsSync(menu.Items, model.TopLevelItems, item => Create(item, commandModifiers));
        return menu;
    }

    private static BuiltMenuItem Create(MenuItemModel model, KeyModifiers commandModifiers)
    {
        if (model.IsSeparator)
        {
            return new BuiltMenuItem(new object[] { new Separator() }, Detach: null);
        }

        if (model.Stepper is { } stepper)
        {
            // A row with buttons, like Chrome's zoom row. It stays open so the buttons can be pressed again.
            MenuStepperRow row = new MenuStepperRow(stepper);
            return new BuiltMenuItem(new object[] { new MenuItem { Header = row, StaysOpenOnClick = true } }, row.Dispose);
        }

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

        void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e) => Apply(menuItem, model, e);
        model.PropertyChanged += HandlePropertyChanged;
        MenuItemsSync children = new MenuItemsSync(menuItem.Items, model.Items, item => Create(item, commandModifiers));
        return new BuiltMenuItem(new object[] { menuItem }, () =>
        {
            model.PropertyChanged -= HandlePropertyChanged;
            children.Dispose();
        });
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
