using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;



#if FRB
using FlatRedBall.Input;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Gui;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
using GamepadButton = FlatRedBall.Input.Xbox360GamePad.Button;
using RepositionDirections = FlatRedBall.Math.Geometry.RepositionDirections;
namespace FlatRedBall.Forms.Controls;
#endif


#if !FRB
using Gum.Input;
using GamepadButton = Gum.Input.GamepadButton;
namespace Gum.Forms.Controls;

#endif

public class Menu : ItemsControl
{
    #region Fields/Properties

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use MenuCategoryStateName")]
    public static string MenuCategoryState => MenuCategoryStateName;
    public const string MenuCategoryStateName = "MenuCategoryState";

    protected List<MenuItem> MenuItemsInternal = new List<MenuItem>();

    ReadOnlyCollection<MenuItem>? menuItemsReadOnly;

    public ReadOnlyCollection<MenuItem> MenuItems
    {
        get
        {
            if (menuItemsReadOnly == null)
            {
                menuItemsReadOnly = new ReadOnlyCollection<MenuItem>(MenuItemsInternal);
            }
            return menuItemsReadOnly;
        }
    }

    #endregion

    public Menu() : base()
    {
    }

    public Menu(InteractiveGue visual) : base(visual)
    {
    }

    public override void UpdateState()
    {
        var category = MenuCategoryStateName;
        if (IsEnabled == false)
        {
            if (IsFocused)
            {
                Visual.SetProperty(category, "DisabledFocused");
            }
            else
            {
                Visual.SetProperty(category, "Disabled");
            }
        }
        else if (IsFocused)
        {
            Visual.SetProperty(category, "Focused");
        }
        else
        {
            Visual.SetProperty(category, "Enabled");
        }

        // The default state may update the visibility of the scroll bar. Whenever setting the state
        // we should forcefully apply the list box visibility:
        //base.UpdateVerticalScrollBarValues();
    }

    #region Item Creation

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Automatic per-item BindingContext assignment when populating from a bound " +
            "collection. Any by-name VM member resolution happens in the item template's own bindings, " +
            "which already carry this warning at the SetBinding call that created them.")]
    protected override FrameworkElement CreateNewItemFrameworkElement(object o)
    {
        MenuItem menuItem;
        if(o is MenuItem)
        {
            menuItem = (MenuItem)o;
        }
        else
        {
            // todo - eventually we want to support templating like with ListBox, but that will come later:
            menuItem = new MenuItem();
            menuItem.UpdateToObject(o);
            menuItem.BindingContext = o;
        }

        SubscribeToItem(menuItem);

        return menuItem;
    }

    // An item can be subscribed more than once (a re-added item, such as after a Visual swap, is still
    // subscribed from before), so unsubscribe first to keep a single subscription.
    private void SubscribeToItem(MenuItem menuItem)
    {
        menuItem.Selected -= HandleItemSelected;
        menuItem.Selected += HandleItemSelected;
        menuItem.HighlightGained -= HandleTopLevelHighlightGained;
        menuItem.HighlightGained += HandleTopLevelHighlightGained;
    }

    private void HandleTopLevelHighlightGained(object? sender, EventArgs e)
    {
        foreach (MenuItem item in MenuItemsInternal)
        {
            if (item != sender)
            {
                item.SetHighlightFromNavigation(false);
            }
        }
    }

    private void HandleItemSelected(object? sender, EventArgs e)
    {
        var args = new SelectionChangedEventArgs();

        for (int i = 0; i < MenuItemsInternal.Count; i++)
        {
            var menuItem = MenuItemsInternal[i];
            if (menuItem != sender && menuItem.IsSelected)
            {
                var deselectedItem = menuItem.BindingContext ?? menuItem;
                args.RemovedItems.Add(deselectedItem);
                menuItem.IsSelected = false;
            }
        }

        // A next-push action added during a push only runs on the following push (#1437), so
        // HandleNextPush cannot tell that this selection came from the push that is happening now.
        // Decide it here: a cursor selection that opens a submenu turns on hover switching (#6692).
        bool selectedByCursor = MainCursor.PrimaryPush || MainCursor.PrimaryClick;
        if (selectedByCursor && (sender as MenuItem)?.Items?.Count > 0)
        {
            foreach (var item in MenuItemsInternal)
            {
                item.SetSelectOnHighlightRecursively(true);
            }
        }

        QueueNextPushAction();
    }

    #endregion

    // Selecting items queues this action, and one queued action is enough to close the menu on the next
    // push however many times items are selected before it. Whether it is still queued is read from the
    // engine each time, so nothing here goes stale when queued actions are dropped without running.
    private Action? _queuedPushAction;

    private void QueueNextPushAction()
    {
        _queuedPushAction ??= RunQueuedPushAction;

#if !FRB
        if (InteractiveGue.IsNextPushActionQueued(_queuedPushAction))
        {
            return;
        }
#endif

        // Not the owner overload (FrameworkElement.AddNextPushAction): the visual-left-the-tree check
        // is made in RunQueuedPushAction instead.
        InteractiveGue.AddNextPushAction(_queuedPushAction);
    }

    private void RunQueuedPushAction()
    {
#if !FRB
        if (Visual.EffectiveManagers == null)
        {
            return;
        }
#endif
        HandleNextPush();
    }

    private void HandleNextPush()
    {
        var itemPushed = MainCursor.WindowPushed as GraphicalUiElement;

        var pushedOnThis = GetIfIsOnThisOrChildVisual(MainCursor);
        var pushedOnChildItem = false;
        if(!pushedOnThis)
        {
            foreach(var item in MenuItemsInternal)
            {
                if (item.IsRecursiveMenuItem(itemPushed))
                {
                    pushedOnChildItem = true;
                    break;
                }
            }
        }

        var shouldCloseAll = true;
        if (pushedOnThis)
        {
            var wasJustOpened = MenuItemsInternal.Any(item => item.timeOpened == MainCursor.LastPrimaryPushTime);
            shouldCloseAll = !wasJustOpened;
            if(wasJustOpened)
            {
                foreach (var item in MenuItemsInternal)
                {
                    item.SetSelectOnHighlightRecursively(true);
                }
            }
        }
        else if(pushedOnChildItem)
        {
            var menuItemPushed = (MainCursor.WindowPushed as InteractiveGue)?.FormsControlAsObject 
                as MenuItem;
            shouldCloseAll = menuItemPushed?.Items == null || menuItemPushed.Items.Count == 0;
        }


        if (shouldCloseAll)
        {
            // We can toggle the top menu item, but we don't want to close if it was
            // just opened

            // toggle list items recursively:
            foreach (var item in MenuItemsInternal)
            {
                item.SetSelectOnHighlightRecursively(false);
                item.IsSelected = false;
            }
        }
        else
        {
            QueueNextPushAction();
        }
    }

    #region Keyboard and Gamepad Navigation

    /// <inheritdoc/>
    /// <remarks>
    /// Gaining focus highlights the first top-level item. Losing focus (Tab away, another control
    /// focused) closes every open submenu.
    /// </remarks>
    public override bool IsFocused
    {
        get => base.IsFocused;
        set
        {
            bool wasFocused = base.IsFocused;
            base.IsFocused = value;

            if (base.IsFocused && !wasFocused)
            {
                if (GetCurrentTopLevelItem() == null)
                {
                    StepToNavigable(MenuItemsInternal, null, 1)?.SetHighlightFromNavigation(true);
                }
            }
            else if (!base.IsFocused && wasFocused)
            {
                CloseAllSubmenus();
                foreach (MenuItem item in MenuItemsInternal)
                {
                    item.SetHighlightFromNavigation(false);
                }
            }
        }
    }

    /// <inheritdoc/>
    public override void OnFocusUpdatePreview(RoutedEventArgs args)
    {
        // While a submenu is open, Escape closes one level in OnFocusUpdate. The base preview would
        // consume it first.
        if (GetDeepestOpenItem() == null)
        {
            base.OnFocusUpdatePreview(args);
        }
    }

    /// <inheritdoc/>
    public override void OnFocusUpdate()
    {
        var gamepads = FrameworkElement.GamePadsForUiControl;
        for (int i = 0; i < gamepads.Count && IsFocused; i++)
        {
            var gamepad = gamepads[i];
            RepositionDirections? direction = GetRepeatedGamepadDirection(gamepad);

            if (direction == RepositionDirections.Up && GetDeepestOpenItem() == null)
            {
                // Nothing is open, so Up has no use in the menu bar. It moves focus to the previous
                // control, the only way out of the menu for a gamepad.
                HandleTab(TabDirection.Up, this, loop: true);
                continue;
            }

            HandleNavigation(direction,
                confirm: gamepad.ButtonPushed(GamepadButton.A),
                back: gamepad.ButtonPushed(GamepadButton.B),
                home: false,
                end: false);
        }

#if !FRB
        if (IsFocused && KeyboardsForUiControl.Count > 0)
        {
            // Read every keyboard first and navigate once: ClickCombos already looks at all keyboards, so
            // navigating per keyboard would apply one Enter several times.
            RepositionDirections? direction = null;
            bool back = false;
            bool home = false;
            bool end = false;
            foreach (var keyboard in KeyboardsForUiControl)
            {
                if (keyboard.KeyTyped(Gum.Forms.Input.Keys.Down))
                {
                    direction = RepositionDirections.Down;
                }
                if (keyboard.KeyTyped(Gum.Forms.Input.Keys.Right))
                {
                    direction = RepositionDirections.Right;
                }
                if (keyboard.KeyTyped(Gum.Forms.Input.Keys.Up))
                {
                    direction = RepositionDirections.Up;
                }
                if (keyboard.KeyTyped(Gum.Forms.Input.Keys.Left))
                {
                    direction = RepositionDirections.Left;
                }
                back |= keyboard.KeyPushed(Gum.Forms.Input.Keys.Escape);
                home |= keyboard.KeyTyped(Gum.Forms.Input.Keys.Home);
                end |= keyboard.KeyTyped(Gum.Forms.Input.Keys.End);
            }

            bool confirm = false;
            foreach (var keyCombo in FrameworkElement.ClickCombos)
            {
                if (keyCombo.IsComboPushed())
                {
                    confirm = true;
                    break;
                }
            }

            HandleNavigation(direction, confirm, back, home, end);
        }

        if (IsFocused)
        {
            base.HandleKeyboardFocusUpdate();
        }
#endif

        RaiseFocusUpdate();
    }

    private void HandleNavigation(RepositionDirections? direction, bool confirm, bool back, bool home, bool end)
    {
        MenuItem? open = GetDeepestOpenItem();

        if (open == null)
        {
            MenuItem? current = GetCurrentTopLevelItem();
            if (direction == RepositionDirections.Left)
            {
                StepToNavigable(MenuItemsInternal, current, -1)?.SetHighlightFromNavigation(true);
            }
            else if (direction == RepositionDirections.Right)
            {
                StepToNavigable(MenuItemsInternal, current, 1)?.SetHighlightFromNavigation(true);
            }
            else if (current != null && (confirm || direction == RepositionDirections.Down))
            {
                if (current.Items?.Count > 0)
                {
                    OpenSubmenu(current);
                }
                else if (confirm)
                {
                    Activate(current);
                }
            }
            return;
        }

        IReadOnlyList<MenuItem> rows = open.OpenSubItems;
        MenuItem? highlighted = rows.FirstOrDefault(item => item.IsHighlighted);
        bool isTopLevelSubmenu = MenuItemsInternal.Contains(open);

        if (back)
        {
            CloseSubmenu(open);
        }
        else if (home)
        {
            StepToNavigable(rows, null, 1)?.SetHighlightFromNavigation(true);
        }
        else if (end)
        {
            StepToNavigable(rows, null, -1)?.SetHighlightFromNavigation(true);
        }
        else if (direction == RepositionDirections.Down)
        {
            StepToNavigable(rows, highlighted, 1)?.SetHighlightFromNavigation(true);
        }
        else if (direction == RepositionDirections.Up)
        {
            StepToNavigable(rows, highlighted, -1)?.SetHighlightFromNavigation(true);
        }
        else if (direction == RepositionDirections.Right)
        {
            if (highlighted?.Items?.Count > 0)
            {
                OpenSubmenu(highlighted);
            }
            else if (isTopLevelSubmenu)
            {
                MoveToNeighborTopLevelItem(open, 1);
            }
        }
        else if (direction == RepositionDirections.Left)
        {
            if (isTopLevelSubmenu)
            {
                MoveToNeighborTopLevelItem(open, -1);
            }
            else
            {
                CloseSubmenu(open);
            }
        }
        else if (confirm && highlighted != null)
        {
            if (highlighted.Items?.Count > 0)
            {
                OpenSubmenu(highlighted);
            }
            else
            {
                Activate(highlighted);
            }
        }
    }

    // The top-level item that is open, or else the highlighted one.
    private MenuItem? GetCurrentTopLevelItem() =>
        MenuItemsInternal.FirstOrDefault(item => item.IsSelected) ??
        MenuItemsInternal.FirstOrDefault(item => item.IsHighlighted);

    // The item whose submenu is the innermost one open, or null when no submenu is open.
    private MenuItem? GetDeepestOpenItem()
    {
        MenuItem? open = MenuItemsInternal.FirstOrDefault(item => item.IsSelected && item.IsPopupVisible);
        while (open != null)
        {
            MenuItem? child = open.OpenSubItems.FirstOrDefault(item => item.IsSelected && item.IsPopupVisible);
            if (child == null)
            {
                break;
            }
            open = child;
        }
        return open;
    }

    // The next enabled, visible item from current in the given direction, wrapping. With a null current,
    // the first such item (step 1) or the last (step -1).
    private static MenuItem? StepToNavigable(IReadOnlyList<MenuItem> items, MenuItem? current, int step)
    {
        int count = items.Count;
        int start = -1;
        for (int i = 0; i < count && current != null; i++)
        {
            if (items[i] == current)
            {
                start = i;
                break;
            }
        }
        if (start == -1)
        {
            start = step > 0 ? -1 : count;
        }

        for (int offset = 1; offset <= count; offset++)
        {
            int index = ((start + step * offset) % count + count) % count;
            MenuItem candidate = items[index];
            if (candidate.IsEnabled && candidate.Visual.Visible)
            {
                return candidate;
            }
        }
        return null;
    }

    private static void OpenSubmenu(MenuItem item)
    {
        item.SetHighlightFromNavigation(true);
        item.IsSelected = true;
        StepToNavigable(item.OpenSubItems, null, 1)?.SetHighlightFromNavigation(true);
    }

    // Closes the submenu of open, which stays highlighted.
    private void CloseSubmenu(MenuItem open)
    {
        open.ClearSubItemHighlightsRecursively();
        open.SetHighlightFromNavigation(true);
        open.IsSelected = false;

        if (MenuItemsInternal.Contains(open))
        {
            // The menu bar is closed, so hovering must no longer open items.
            foreach (MenuItem item in MenuItemsInternal)
            {
                item.SetSelectOnHighlightRecursively(false);
            }
        }
    }

    private void MoveToNeighborTopLevelItem(MenuItem open, int step)
    {
        MenuItem? neighbor = StepToNavigable(MenuItemsInternal, open, step);
        if (neighbor == null || neighbor == open)
        {
            return;
        }

        if (neighbor.Items?.Count > 0)
        {
            open.ClearSubItemHighlightsRecursively();
            // Selecting the neighbor deselects (and closes) the open item.
            OpenSubmenu(neighbor);
        }
        else
        {
            CloseSubmenu(open);
            neighbor.SetHighlightFromNavigation(true);
        }
    }

    // Activates a leaf, then closes the whole menu with its top-level item highlighted and focus on the Menu.
    private void Activate(MenuItem item)
    {
        MenuItem? topLevelItem = GetCurrentTopLevelItem();

        item.ActivateFromNavigation();

        CloseAllSubmenus();
        topLevelItem?.SetHighlightFromNavigation(true);
        IsFocused = true;
    }

    private void CloseAllSubmenus()
    {
        foreach (MenuItem item in MenuItemsInternal)
        {
            item.ClearSubItemHighlightsRecursively();
            item.SetSelectOnHighlightRecursively(false);
            item.IsSelected = false;
        }
    }

    #endregion

    #region Collection Changed

    /// <inheritdoc/>
    protected override bool IsAuthoredRow(FrameworkElement control) => control is MenuItem;

    /// <inheritdoc/>
    protected override void AdoptAuthoredRow(FrameworkElement row, int rowIndex)
    {
        MenuItem menuItem = (MenuItem)row;

        // The item already has its visual in the panel, so Items must not create another.
        AdoptVisualAuthoredItem(menuItem, rowIndex);

        MenuItemsInternal.Insert(rowIndex, menuItem);
        SubscribeToItem(menuItem);
    }

    protected override void HandleCollectionNewItemCreated(FrameworkElement newItem, int newItemIndex)
    {
        if (newItem is MenuItem menuItem)
        {
            MenuItemsInternal.Insert(newItemIndex, menuItem);
            SubscribeToItem(menuItem);
        }
    }

    //private void HandleMenuItemHighlightChanged(object? sender, EventArgs e)
    //{
    //    if (this.MenuItemsInternal.Any(item => item.IsPopupVisible))
    //    {
    //        var menuItem = sender as MenuItem;
    //        if (menuItem.IsHighlighted)
    //        {
    //            var parentMenuItem = menuItem.ParentMenuItem;
    //            if(parentMenuItem != null)
    //            {
    //                parentMenuItem.HideChildrenPopupsRecursively();
    //            }
    //            else
    //            {
    //                foreach(var item in this.MenuItemsInternal)
    //                {
    //                    item.HidePopupRecursively();
    //                }
    //            }

    //            menuItem.TryShowPopup();
    //        }
    //    }
    //}


    protected override void HandleCollectionItemRemoved(int inexToRemoveFrom, FrameworkElement removedItem)
    {
        // removedItem is accepted to match the base signature (issue #556 reworked ListBox to
        // remove by reference). Menu keeps its existing index-based removal unchanged.
        MenuItemsInternal.RemoveAt(inexToRemoveFrom);
    }

    protected override void HandleCollectionReset()
    {
        MenuItemsInternal.Clear();
    }

    protected override void HandleCollectionReplace(int index)
    {
        MenuItemsInternal[index].UpdateToObject(Items?[index]);
    }

    #endregion

}
