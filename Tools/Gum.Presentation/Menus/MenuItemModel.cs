using System;
using System.Collections.ObjectModel;
using Gum.Managers;
using Gum.Mvvm;

namespace Gum.Menus;

/// <summary>
/// One entry in the tool's main menu, described without any UI framework: a header, an enabled
/// flag, an optional check state, an optional shortcut, and children. Each head renders the
/// model with its own menu control and calls <see cref="Invoke"/> when the user picks the item.
/// </summary>
public class MenuItemModel : ViewModel
{
    /// <summary>Creates a regular item.</summary>
    public MenuItemModel(string header, Action? click = null)
    {
        Header = header;
        Click = click;
        IsEnabled = true;
        IsSeparator = false;
        IsCheckable = false;
        IsChecked = false;
        Items = new ObservableCollection<MenuItemModel>();
    }

    /// <summary>A separator line. Heads render it as a divider and never invoke it.</summary>
    public static MenuItemModel Separator() => new MenuItemModel("") { IsSeparator = true };

    /// <summary>The text shown for the item.</summary>
    public string Header { get => Get<string>(); set => Set(value); }

    /// <summary>Whether the item can be picked.</summary>
    public bool IsEnabled { get => Get<bool>(); set => Set(value); }

    /// <summary>Whether the item toggles a check mark when picked.</summary>
    public bool IsCheckable { get => Get<bool>(); set => Set(value); }

    /// <summary>The current check state, meaningful only when <see cref="IsCheckable"/>.</summary>
    public bool IsChecked { get => Get<bool>(); set => Set(value); }

    /// <summary>
    /// The shortcut shown beside the item, as the hotkey manager's binding so the two never
    /// disagree. In-window menus only display it; the macOS menu bar binds it, since AppKit
    /// matches a menu key equivalent before the window sees the key.
    /// </summary>
    public KeyCombination? Gesture { get => Get<KeyCombination?>(); set => Set(value); }

    /// <summary>Optional hover text.</summary>
    public string? ToolTip { get => Get<string?>(); set => Set(value); }

    /// <summary>True for a divider line.</summary>
    public bool IsSeparator { get => Get<bool>(); private set => Set(value); }

    /// <summary>Runs when the item is picked. Assigned by whoever adds the item.</summary>
    public Action? Click { get; set; }

    /// <summary>Child items, in display order. Empty for a leaf.</summary>
    public ObservableCollection<MenuItemModel> Items { get; }

    /// <summary>What a head calls when the user picks this item: toggles the check state, then runs <see cref="Click"/>.</summary>
    public void Invoke()
    {
        if (IsCheckable)
        {
            IsChecked = !IsChecked;
        }
        Click?.Invoke();
    }
}
