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
                    dataUi.InstanceMember.Value = valueOnUi;
                    result = ApplyValueResult.Success;
                    dataUi.InstanceMember.CallAfterSetByUi();
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
                if (dataUi.InstanceMember.Value != valueToSet)
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
                    if(dataUi is ISetDefaultable)
                    {
                        ((ISetDefaultable)dataUi).SetToDetafult();
                    }
                };



            contextMenu.Items.Clear();

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
