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
    }


    protected override void HandleCollectionNewItemCreated(FrameworkElement newItem, int newItemIndex)
    {
        if (newItem is MenuItem menuItem)
        {
            MenuItemsInternal.Insert(newItemIndex, menuItem);
        }
    }

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
