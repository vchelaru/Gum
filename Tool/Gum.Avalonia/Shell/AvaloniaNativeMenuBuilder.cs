using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Reactive;
using AvaloniaDataUi;
using Gum.Avalonia.Services;
using Gum.Menus;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Renders a <see cref="MenuModel"/> into a <see cref="NativeMenu"/> for the macOS menu bar and
/// keeps it in sync the same way <see cref="AvaloniaMenuBuilder"/> does for the in-window menu.
/// A model's <see cref="MenuItemModel.Gesture"/> becomes the item's key equivalent, which is a
/// real binding: AppKit matches it before the window's keyDown, so the item fires and the hotkey
/// manager never sees the key. Both call the same action, so nothing runs twice. The key
/// equivalent is app-wide, so it is bound only while the owning window is active; otherwise a
/// dialog's text box would lose Cmd+Z to the tool's undo.
/// </summary>
public static class AvaloniaNativeMenuBuilder
{
    /// <summary>Builds the menu-bar menu for <paramref name="model"/>, binding gestures with the running platform's command modifier.</summary>
    public static NativeMenu Build(MenuModel model) => Build(model, PlatformKeyModifiers.Command);

    /// <summary>
    /// How a leaf item's action is run after its click. Defaults to a dispatcher post; on the real
    /// macOS menu bar that still runs inside AppKit's menu tracking, so the shell supplies
    /// <c>MenuTrackingScheduler.InvokeAfterTracking</c> instead.
    /// </summary>
    public static Action<Action> DefaultInvokeAfterClick => MenuItemActions.InvokeAfterClose;

    /// <summary>
    /// Builds the menu-bar menu for <paramref name="model"/>, binding gestures with
    /// <paramref name="commandModifiers"/> as the neutral Ctrl. Gestures are bound only while
    /// <paramref name="isOwnerActive"/> is true; with no signal they are always bound. A leaf's
    /// action runs through <paramref name="invokeAfterClick"/>, or <see cref="DefaultInvokeAfterClick"/>.
    /// </summary>
    public static NativeMenu Build(MenuModel model, KeyModifiers commandModifiers, IObservable<bool>? isOwnerActive = null,
        Action<Action>? invokeAfterClick = null)
    {
        NativeMenu menu = new NativeMenu();
        GestureBinding binding = new GestureBinding(commandModifiers, isBound: isOwnerActive == null, invokeAfterClick ?? DefaultInvokeAfterClick);
        Populate(menu.Items, model.TopLevelItems, binding);
        model.TopLevelItems.CollectionChanged += (_, _) => Populate(menu.Items, model.TopLevelItems, binding);
        isOwnerActive?.Subscribe(new AnonymousObserver<bool>(isActive =>
        {
            binding.IsBound = isActive;
            ApplyGestures(menu, binding);
        }));
        return menu;
    }

    /// <summary>
    /// Builds the application menu (the one named after the app, left of File) with an About Gum
    /// item that runs <paramref name="showAbout"/> through <paramref name="invokeAfterClick"/>, or
    /// <see cref="DefaultInvokeAfterClick"/>. Avalonia appends the standard Services, Hide and
    /// Quit items itself.
    /// </summary>
    public static NativeMenu BuildAppMenu(Action showAbout, Action<Action>? invokeAfterClick = null)
    {
        NativeMenu menu = new NativeMenu();
        GestureBinding binding = new GestureBinding(KeyModifiers.None, isBound: false, invokeAfterClick ?? DefaultInvokeAfterClick);
        menu.Items.Add(Create(new MenuItemModel("About Gum", showAbout), binding));
        return menu;
    }

    private static void Populate(IList<NativeMenuItemBase> target, ObservableCollection<MenuItemModel> items, GestureBinding binding)
    {
        target.Clear();
        foreach (MenuItemModel item in items)
        {
            target.Add(item.IsSeparator ? new NativeMenuItemSeparator() : Create(item, binding));
        }
    }

    private static NativeMenuItem Create(MenuItemModel model, GestureBinding binding)
    {
        NativeMenuItem menuItem = new NativeMenuItem
        {
            Header = model.Header,
            IsEnabled = model.IsEnabled,
            ToggleType = model.IsCheckable ? NativeMenuItemToggleType.CheckBox : NativeMenuItemToggleType.None,
            IsChecked = model.IsChecked,
            ToolTip = model.ToolTip,
        };
        if (model.Gesture?.ToKeyGesture(binding.CommandModifiers) is { } gesture)
        {
            binding.Gestures.Add(menuItem, gesture);
            menuItem.Gesture = binding.IsBound ? gesture : null;
        }

        if (model.Items.Count > 0)
        {
            NativeMenu submenu = new NativeMenu();
            Populate(submenu.Items, model.Items, binding);
            menuItem.Menu = submenu;
        }
        else
        {
            // Deferred so a synchronous dialog opens outside the native callback and, on the real
            // menu bar, after AppKit has finished tracking the menu.
            menuItem.Click += (_, _) => binding.InvokeAfterClick(model.Invoke);
        }

        model.PropertyChanged += (_, e) => Apply(menuItem, model, e);
        model.Items.CollectionChanged += (_, _) =>
        {
            menuItem.Menu ??= new NativeMenu();
            Populate(menuItem.Menu.Items, model.Items, binding);
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

    private static void ApplyGestures(NativeMenu menu, GestureBinding binding)
    {
        foreach (NativeMenuItemBase item in menu.Items)
        {
            if (item is not NativeMenuItem menuItem)
            {
                continue;
            }
            if (binding.Gestures.TryGetValue(menuItem, out KeyGesture? gesture))
            {
                menuItem.Gesture = binding.IsBound ? gesture : null;
            }
            if (menuItem.Menu != null)
            {
                ApplyGestures(menuItem.Menu, binding);
            }
        }
    }

    /// <summary>
    /// Whether the menu's key equivalents are currently bound, plus each item's full gesture so it
    /// can be put back. Items are rebuilt when a model collection changes, so the table is weak.
    /// Also carries how a leaf's action is deferred, since every item is built through it.
    /// </summary>
    private sealed class GestureBinding
    {
        public KeyModifiers CommandModifiers { get; }
        public bool IsBound { get; set; }
        public ConditionalWeakTable<NativeMenuItem, KeyGesture> Gestures { get; }
        public Action<Action> InvokeAfterClick { get; }

        public GestureBinding(KeyModifiers commandModifiers, bool isBound, Action<Action> invokeAfterClick)
        {
            CommandModifiers = commandModifiers;
            IsBound = isBound;
            Gestures = new ConditionalWeakTable<NativeMenuItem, KeyGesture>();
            InvokeAfterClick = invokeAfterClick;
        }
    }
}
