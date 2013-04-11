using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ToolsUtilities
{
    public static class ReflectionManager
    {
        public static List<T> GetMembersOfType<T>(object container)
        {
            Type typeOfT = typeof(T);
            Type containerType = container.GetType();

            List<T> toReturn = new List<T>();


            PropertyInfo[] properties = containerType.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType == typeOfT)
                {
                    object objectToAdd = property.GetValue(container, null);

                    // Fields and properties may point to
                    // the same object so wee want to check for
                    // duplicates
                    if (!toReturn.Contains((T)objectToAdd))
                    {
                        toReturn.Add((T)objectToAdd);
                    }
                }

            }

            FieldInfo[] fields = containerType.GetFields();
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeOfT)
                {
                    object objectToAdd = field.GetValue(container);

                    // Fields and properties may point to
                    // the same object so wee want to check for
                    // duplicates
                    if (!toReturn.Contains((T)objectToAdd))
                    {
                        toReturn.Add((T)objectToAdd);
                    }
                }
            }

            return toReturn;
        }
    }
}
