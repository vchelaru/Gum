using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WpfDataUi
{
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
                        dataUi.InstanceMember.Value = valueOnUi;
                        result = ApplyValueResult.Success;
                        dataUi.InstanceMember.CallAfterSetByUi();
                    }
                    else
                    {
                        result = ApplyValueResult.Skipped;
                    }
                }
            }

            return result;
        }

        public static ApplyValueResult TrySetValueOnInstance(this IDataUi dataUi, object valueToSet)
        {
            ApplyValueResult result;
            bool hasErrorOccurred;
            GetIfValuesCanBeSetOnInstance(dataUi, out result, out hasErrorOccurred);

            if (!hasErrorOccurred)
            {
                if (AreEqual(dataUi.InstanceMember.Value, valueToSet) == false)
                {
                    dataUi.InstanceMember.Value = valueToSet;
                    result = ApplyValueResult.Success;
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
            RoutedEventHandler makeDefaultHandler = (sender, e) =>
                {
                    dataUi.InstanceMember.IsDefault = true;
                    dataUi.Refresh();

                    // the instance member may have undone the IsDefault, so let's only do this if it's still set to default:
                    if(dataUi is ISetDefaultable && dataUi.InstanceMember.IsDefault)
                    {
                        ((ISetDefaultable)dataUi).SetToDefault();
                    }

                    dataUi.InstanceMember.CallAfterSetByUi();
                };



            contextMenu?.Items.Clear();

            AddContextMenuItem("Make Default", makeDefaultHandler, contextMenu);
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

            MenuItem menuItem = new MenuItem();
            menuItem.Header = text;
            menuItem.Click += handler;

            contextMenu.Items.Add(menuItem);

            return menuItem;
        }
    }
}
