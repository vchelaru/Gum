using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if FRB
using FlatRedBall.Input;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Gui;
using InteractiveGue = global::Gum.Wireframe.GraphicalUiElement;
namespace FlatRedBall.Forms.Controls;
#else
using MonoGameGum.Input;
namespace MonoGameGum.Forms.Controls;
#endif

public class Menu : ItemsControl
{
    public const string MenuCategoryState = "MenuCategoryState";
    protected List<MenuItem> MenuItemsInternal = new List<MenuItem>();

    public Menu() : base()
    {
    }

    public Menu(InteractiveGue visual) : base(visual)
    {
    }

    public override void UpdateState()
    {
        var category = MenuCategoryState;
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

    protected override FrameworkElement CreateNewItemFrameworkElement(object o)
    {
        MenuItem menuItem;
        if(o is MenuItem)
        {
            menuItem = o as MenuItem;
        }
        else
        {
            // todo - eventually we want to support templating like with ListBox, but that will come later:
            menuItem = new MenuItem();
            menuItem.UpdateToObject(o);
            menuItem.BindingContext = o;
        }

        menuItem.Selected += HandleItemSelected;

        return menuItem;
    }

    private void HandleItemSelected(object? sender, EventArgs e)
    {
        var args = new SelectionChangedEventArgs();

        for (int i = 0; i < MenuItemsInternal.Count; i++)
        {
            var listBoxItem = MenuItemsInternal[i];
            if (listBoxItem != sender && listBoxItem.IsSelected)
            {
                var deselectedItem = listBoxItem.BindingContext ?? listBoxItem;
                args.RemovedItems.Add(deselectedItem);
                listBoxItem.IsSelected = false;
            }
        }

        InteractiveGue.AddNextPushAction(HandleNextPush);
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
            shouldCloseAll = menuItemPushed.Items == null || menuItemPushed.Items.Count == 0;
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
            InteractiveGue.AddNextPushAction(HandleNextPush);
        }
    }

    protected override void HandleCollectionNewItemCreated(FrameworkElement newItem, int newItemIndex)
    {
        if (newItem is MenuItem menuItem)
        {
            MenuItemsInternal.Insert(newItemIndex, menuItem);
            menuItem.Selected += HandleItemSelected;
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

    protected override void HandleCollectionItemRemoved(int inexToRemoveFrom)
    {
        MenuItemsInternal.RemoveAt(inexToRemoveFrom);
    }

    protected override void HandleCollectionReset()
    {
        MenuItemsInternal.Clear();
    }

    protected override void HandleCollectionReplace(int index)
    {
        MenuItemsInternal[index].UpdateToObject(Items[index]);
    }

}
