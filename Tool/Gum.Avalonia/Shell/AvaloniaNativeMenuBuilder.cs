using System;
using System.Collections;
using System.Collections.Specialized;
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
        // Lives as long as the menu, like the model it follows.
        _ = new MenuItemsSync((IList)menu.Items, model.TopLevelItems, item => Build(item, binding));
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
        menu.Items.Add(Create(new MenuItemModel("About Gum", showAbout), binding, out _));
        return menu;
    }

    private static BuiltMenuItem Build(MenuItemModel model, GestureBinding binding)
    {
        if (model.Stepper is { } stepper)
        {
            // The menu bar cannot draw a row with buttons, so the stepper becomes plain items. Their
            // models exist only here, so there is nothing to detach.
            return new BuiltMenuItem(new object[]
            {
                Create(new MenuItemModel(stepper.IncreaseHeader, stepper.Increase), binding, out _),
                Create(new MenuItemModel(stepper.DecreaseHeader, stepper.Decrease), binding, out _),
                Create(new MenuItemModel(stepper.ResetHeader, stepper.Reset), binding, out _),
            }, Detach: null);
        }
        if (model.IsSeparator)
        {
            return new BuiltMenuItem(new object[] { new NativeMenuItemSeparator() }, Detach: null);
        }
        NativeMenuItem menuItem = Create(model, binding, out Action detach);
        return new BuiltMenuItem(new object[] { menuItem }, detach);
    }

    private static NativeMenuItem Create(MenuItemModel model, GestureBinding binding, out Action detach)
    {
        NativeMenuItem menuItem = new NativeMenuItem
        {
            Header = model.Header,
            IsEnabled = model.IsEnabled,
            ToggleType = model.IsCheckable ? MenuItemToggleType.CheckBox : MenuItemToggleType.None,
            IsChecked = model.IsChecked,
            ToolTip = model.ToolTip,
        };
        if (model.Gesture?.ToKeyGesture(binding.CommandModifiers) is { } gesture)
        {
            binding.Gestures.Add(menuItem, gesture);
            menuItem.Gesture = binding.IsBound ? gesture : null;
        }

        MenuItemsSync? children = null;
        void CreateSubmenu()
        {
            menuItem.Menu = new NativeMenu();
            children = new MenuItemsSync((IList)menuItem.Menu.Items, model.Items, item => Build(item, binding));
        }
        // A leaf gets its submenu only once it has children, so an empty arrow never shows.
        void HandleFirstChildren(object? sender, NotifyCollectionChangedEventArgs e)
        {
            model.Items.CollectionChanged -= HandleFirstChildren;
            CreateSubmenu();
        }

        if (model.Items.Count > 0)
        {
            CreateSubmenu();
        }
        else
        {
            // Deferred so a synchronous dialog opens outside the native callback and, on the real
            // menu bar, after AppKit has finished tracking the menu.
            menuItem.Click += (_, _) => binding.InvokeAfterClick(model.Invoke);
            model.Items.CollectionChanged += HandleFirstChildren;
        }

        void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e) => Apply(menuItem, model, e);
        model.PropertyChanged += HandlePropertyChanged;
        detach = () =>
        {
            model.PropertyChanged -= HandlePropertyChanged;
            model.Items.CollectionChanged -= HandleFirstChildren;
            children?.Dispose();
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
