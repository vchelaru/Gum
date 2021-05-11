using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Gum.DataTypes
{
    public class CustomPropertySave
    {
        public string Name;

        [XmlElement("ValueAsString", typeof(string))]
        [XmlElement("ValueAsFloat", typeof(float))]
        [XmlElement("ValueAsInt", typeof(int))]
        [XmlElement("ValueAsBool", typeof(bool))]
        [XmlElement("ValueAsLong", typeof(long))]
        [XmlElement("ValueAsDouble", typeof(double))]
        [XmlElement("ValueAsObject", typeof(object))]
        // Can't do this because it's an abstract class
        //[XmlElement("ValueAsEnum", typeof(Enum))]
        public object Value;
    }

    public static class CustomPropertySaveListExtensions
    {
        public static bool ContainsValue(this List<CustomPropertySave> propertySaveList, string nameToSearchFor)
        {
            foreach (CustomPropertySave propertySave in propertySaveList)
            {
                if (propertySave.Name == nameToSearchFor)
                {
                    return true;
                }
            }
            return false;
        }

        public static object GetValue(this List<CustomPropertySave> propertySaveList, string nameToSearchFor)
        {
            foreach (CustomPropertySave propertySave in propertySaveList)
            {
                if (propertySave.Name == nameToSearchFor)
                {
                    return propertySave.Value;
                }
            }
            return null;
        }

        public static T GetValue<T>(this List<CustomPropertySave> propertySaveList, string nameToSearchFor)
        {
            foreach (CustomPropertySave propertySave in propertySaveList)
            {
                if (propertySave.Name == nameToSearchFor)
                {
                    return (T)propertySave.Value;
                }
            }
            return default(T);
        }

        public static void Remove(this List<CustomPropertySave> propertySaveList, string nameToRemove)
        {
            for (int i = 0; i < propertySaveList.Count; i++)
            {
                if (propertySaveList[i].Name == nameToRemove)
                {
                    propertySaveList.RemoveAt(i);
                    break;
                }
            }
        }

        public static void SetValue(this List<CustomPropertySave> propertySaveList, string nameToSearchFor, object value)
        {
            bool isDefault = IsValueDefault(value);

            if (isDefault)
            {
                propertySaveList.Remove(nameToSearchFor);
            }
            else
            {
                foreach (CustomPropertySave propertySave in propertySaveList)
                {
                    if (propertySave.Name == nameToSearchFor)
                    {
                        propertySave.Value = value;
                        return;
                    }
                }


                // If we got here then that means there isn't already something in place for this
                CustomPropertySave newPropertySave = new CustomPropertySave();
                newPropertySave.Name = nameToSearchFor;
                newPropertySave.Value = value;

                propertySaveList.Add(newPropertySave);
            }
        }

        static bool IsValueDefault(object value)
        {
            if (value is string)
            {
                return string.IsNullOrEmpty((string)value);
            }
            if (value is float)
            {
                return ((float)value) == 0;
            }
            if (value is int)
            {
                return ((int)value) == 0;
            }
            if (value is double)
            {
                return ((double)value) == 0;
            }
            if (value is long)
            {
                return ((long)value) == 0;
            }
            if (value is bool)
            {
                return ((bool)value) == false;
            }

            return false;


        }

    }
}
