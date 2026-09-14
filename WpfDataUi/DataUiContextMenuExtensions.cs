using System;
using System.Windows;
using System.Windows.Controls;
using WpfDataUi.DataTypes;

namespace WpfDataUi;

/// <summary>
/// A menu item that remembers its click handler, so a refresh can tell whether the menu already
/// matches the member's entries and skip rebuilding it.
/// </summary>
public class MenuItemExposedClick : MenuItem
{
    public IDataUi? Owner { get; set; }

    EventHandler? storedClick;
    public EventHandler? ClickHandler => storedClick;

    public void SetMakeDefaultClick(IDataUi dataUi)
    {
        Owner = dataUi;
        this.SetClick((_, _) => dataUi.MakeDefault());
    }

    public void SetClick(EventHandler clickEventHandler)
    {
        storedClick = clickEventHandler;
        base.Click += (sender, e) => clickEventHandler(sender, e);
    }
}

/// <summary>
/// Builds a WPF displayer's right-click menu from <see cref="IDataUiExtensionMethods.MakeDefault"/>
/// and the member's <see cref="InstanceMember.ContextMenuEvents"/>.
/// </summary>
public static class DataUiContextMenuExtensions
{
    public static void RefreshContextMenu(this IDataUi dataUi, ContextMenu contextMenu)
    {

        var areSame = true;

        var expectedCount = 1;
        if(dataUi.InstanceMember != null)
        {
            expectedCount += dataUi.InstanceMember.ContextMenuEvents.Count;
        }

        if(expectedCount != contextMenu.Items.Count)
        {
            areSame = false;
        }

        if(areSame && contextMenu.Items.Count > 0)
        {
            // first item is default, so compare that:
            var firstExistingItem = (MenuItemExposedClick) contextMenu.Items[0];
            if(firstExistingItem.Owner != dataUi)
            {
                areSame = false;
            }
        }

        if(areSame && contextMenu.Items.Count > 0 && dataUi.InstanceMember != null)
        {
            int index = 1;
            foreach(var kvp in dataUi.InstanceMember.ContextMenuEvents)
            {
                var item = (MenuItemExposedClick)contextMenu.Items[index];
                var isInstanceTheSame = item.Header is string asString &&
                    asString == kvp.Key &&
                    item.ClickHandler == kvp.Value &&
                    item.Tag == dataUi.InstanceMember;

                if(!isInstanceTheSame)
                {
                    areSame = false;
                }
                index++;
            }
        }

        if(!areSame)
        {
            ForceRefreshContextMenu(dataUi, contextMenu);
        }
    }

    public static void ForceRefreshContextMenu(this IDataUi dataUi, ContextMenu contextMenu)
    {
        if(contextMenu == null)
        {
            return;
        }
        contextMenu.Items.Clear();

        var shouldAddMakeDefault = dataUi.InstanceMember == null ||
            dataUi.InstanceMember.SupportsMakeDefault;

        if(shouldAddMakeDefault)
        {
            var makeDefault = new MenuItemExposedClick();
            makeDefault.Header = "Make Default";
            makeDefault.SetMakeDefaultClick(dataUi);
            contextMenu.Items.Add(makeDefault);
        }

        if (dataUi.InstanceMember != null)
        {
            foreach (var kvp in dataUi.InstanceMember.ContextMenuEvents)
            {
                AddContextMenuItem(kvp.Key, kvp.Value, contextMenu).Tag = dataUi.InstanceMember;
            }
        }
    }

    private static MenuItem AddContextMenuItem(string text, EventHandler handler, ContextMenu contextMenu)
    {

        var menuItem = new MenuItemExposedClick();
        menuItem.Header = text;
        menuItem.SetClick(handler);

        contextMenu.Items.Add(menuItem);

        return menuItem;
    }
}
