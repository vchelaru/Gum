using System;
using System.Collections.Generic;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Reflection;

namespace ToolsUtilities
{
    public static class ReflectionManager
    {
        // Gated because this file also compiles into ToolsUtilitiesStandard.csproj (netstandard2.0),
        // which has no trim attributes.
#if NET5_0_OR_GREATER
        [RequiresUnreferencedCode(
            "Enumerates every property and field on the container's own type, any of which may be " +
            "removed under PublishTrimmed if nothing else in the app references it.")]
#endif
        public static List<T> GetMembersOfType<T>(object container)
        {
            Type typeOfT = typeof(T);
            Type containerType = container.GetType();

            List<T> toReturn = new List<T>();


            IEnumerable<PropertyInfo> properties = containerType.GetProperties();

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

            IEnumerable<FieldInfo> fields = containerType.GetFields();
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
