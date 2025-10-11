using System;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Controls;
using WpfDataUi.DataTypes;

namespace WpfDataUi;

// This class lets us inspect the Click event for equality so we only have to replace items if they really do differ.
// By doing this, Refresh calls can be much faster:
public class MenuItemExposedClick : MenuItem
{
    public IDataUi Owner { get; set; }

    RoutedEventHandler storedClick;
    public RoutedEventHandler ClickHandler => storedClick;

    public void SetMakeDefaultClick(IDataUi dataUi)
    {
        Owner = dataUi;
        this.SetClick(MakeDefault);
    }

    private void MakeDefault(object sender, RoutedEventArgs e)
    {
        Owner.InstanceMember.IsDefault = true;
        Owner.Refresh();

        // the instance member may have undone the IsDefault, so let's only do this if it's still set to default:
        if (Owner is ISetDefaultable && Owner.InstanceMember.IsDefault)
        {
            ((ISetDefaultable)Owner).SetToDefault();
        }

        Owner.InstanceMember.CallAfterSetByUi();
    }

    public void SetClick(RoutedEventHandler clickEventHandler)
    {
        storedClick = clickEventHandler;
        base.Click += clickEventHandler;
    }

    //public bool IsEquivalentTo(MenuItemExposedClick other)
    //{
    //    return storedClick == other.storedClick && 
    //        Header is string thisHeaderString && 
    //        other.Header is string otherHeaderString && 
    //        thisHeaderString == otherHeaderString &&
    //        Owner == other.Owner
    //        ;
    //}
}

public static class IDataUiExtensionMethods
{
    public static bool HasEnoughInformationToWork(this IDataUi dataUi)
    {
        return dataUi.InstanceMember.IsDefined;
    }

    public static bool TryGetValueOnInstance(this IDataUi dataUi, out object value)
    {
        //////////////////Early Out/////////////////////////////////
        if (dataUi.HasEnoughInformationToWork() == false || dataUi.InstanceMember.IsWriteOnly)
        {
            value = null;
            return false;
        }
        ////////////////End Early Out///////////////////////////////

        value = dataUi.InstanceMember.Value;

        return true;

    }

    public static ApplyValueResult TrySetValueOnInstance(this IDataUi dataUi)
    {
        ApplyValueResult result;
        bool hasErrorOccurred;
        GetIfValuesCanBeSetOnInstance(dataUi, out result, out hasErrorOccurred);

        if (!hasErrorOccurred)
        {

            object valueOnUi;

            result = dataUi.TryGetValueOnUi(out valueOnUi);

            if (result == ApplyValueResult.Success)
            {
                // Why not protect against spammed same-value assignments?
                if(dataUi.InstanceMember.Value != valueOnUi)
                {
                    //dataUi.InstanceMember.Value = valueOnUi;
                    result = dataUi.InstanceMember.SetValue(valueOnUi, SetPropertyCommitType.Full);
                    if(result == ApplyValueResult.Success)
                    {
                        dataUi.InstanceMember.CallAfterSetByUi();
                    }
                }
                else
                {
                    result = ApplyValueResult.Skipped;
                }
            }
        }

        return result;
    }

    public static ApplyValueResult TrySetValueOnInstance(this IDataUi dataUi, object valueToSet, SetPropertyCommitType commitType = SetPropertyCommitType.Full)
    {
        ApplyValueResult result;
        bool hasErrorOccurred;
        GetIfValuesCanBeSetOnInstance(dataUi, out result, out hasErrorOccurred);

        if (!hasErrorOccurred)
        {
            if (AreEqual(dataUi.InstanceMember.Value, valueToSet) == false || commitType == SetPropertyCommitType.Full)
            {
                //dataUi.InstanceMember.Value = valueToSet;
                result = dataUi.InstanceMember.SetValue(valueToSet, commitType);
                dataUi.InstanceMember.CallAfterSetByUi();
            }
            else
            {
                result = ApplyValueResult.Skipped;
            }

        }

        return result;
    }

    static bool AreEqual(object object1, object object2)
    {
        if(object1 is float && object2 is float)
        {
            return (float)object1 == (float)object2;
        }
        else if (object1 is double && object2 is double)
        {
            return (double)object1 == (double)object2;
        }
        else if (object1 is decimal && object2 is decimal)
        {
            return (decimal)object1 == (decimal)object2;
        }

        else if (object1 is int && object2 is int)
        {
            return (int)object1 == (int)object2;
        }
        else if (object1 is long && object2 is long)
        {
            return (long)object1 == (long)object2;
        }
        else if (object1 is short && object2 is short)
        {
            return (short)object1 == (short)object2;
        }

        else if (object1 is bool && object2 is bool)
        {
            return (bool)object1 == (bool)object2;
        }
        else if (object1 is char && object2 is char)
        {
            return (char)object1 == (char)object2;
        }
        else if (object1 is string && object2 is string)
        {
            return (string)object1 == (string)object2;
        }
        else
        {
            return object1 == object2;
        }

    }




    private static void GetIfValuesCanBeSetOnInstance(IDataUi dataUi, out ApplyValueResult result, out bool hasErrorOccurred)
    {
        result = ApplyValueResult.UnknownError;
        hasErrorOccurred = false;

        if (dataUi.HasEnoughInformationToWork() == false)
        {
            result = ApplyValueResult.NotEnoughInformation;
            hasErrorOccurred = true;
        }
        if (dataUi.InstanceMember.IsReadOnly)
        {
            result = ApplyValueResult.NotSupported;
            hasErrorOccurred = true;
        }
        if (dataUi.SuppressSettingProperty)
        {
            result = ApplyValueResult.NotEnabled;
            hasErrorOccurred = true;
        }
    }

    public static Type GetPropertyType(this IDataUi dataUi)
    {

        return dataUi.InstanceMember.PropertyType;
    }

    public static Type GetPropertyType(string propertyName, Type instanceType)
    {
        Type type;

        type = null;
        var fieldInfo = instanceType.GetField(propertyName);

        if (fieldInfo != null)
        {
            type = fieldInfo.FieldType;
        }

        // if we haven't found it yet
        if (type == null)
        {
            var propertyInfo = instanceType.GetProperty(propertyName);

            if (propertyInfo != null)
            {
                type = propertyInfo.PropertyType;
            }
        }
        return type;
    }

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

        if(shouldAddMakeDefault  && contextMenu != null)
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

    private static MenuItem AddContextMenuItem(string text, RoutedEventHandler handler, ContextMenu contextMenu)
    {

        var menuItem = new MenuItemExposedClick();
        menuItem.Header = text;
        menuItem.SetClick(handler);

        contextMenu.Items.Add(menuItem);

        return menuItem;
    }
}
