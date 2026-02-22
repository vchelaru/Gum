using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.PropertyGridHelpers.Converters;
using Gum.Services;
using Gum.ToolStates;
using Gum.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Gum.DataTypes
{
    public static class VariableSaveExtensionMethodsGumTool
    {
        public static bool GetIsEnumeration(this VariableSave variableSave)
        {
            if (variableSave.CustomTypeConverter is EnumConverter)
            {
                return true;
            }
            if (string.IsNullOrEmpty(variableSave.Type))
            {
                return false;
            }

            Type type = TypeManager.Self.GetTypeFromString(variableSave.Type);

            if (type == null)
            {
                return false;
            }
            else
            {
                return type.IsEnum;
            }
        }

        public static Type GetRuntimeType(this VariableSave variableSave)
        {

            string typeAsString = variableSave.Type;

            Type foundType = VariableSaveExtensionMethods.GetPrimitiveType(typeAsString);

            if (foundType != null)
            {
                return foundType;
            }

            if (variableSave.GetIsEnumeration())
            {
                if (variableSave.CustomTypeConverter is EnumConverter enumConverter)
                {
                    var values = enumConverter.GetStandardValues();

                    if (values.Count > 0)
                    {
                        return values.FirstOrDefault().GetType();
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return TypeManager.Self.GetTypeFromString(variableSave.Type);
                }
            }
            else
            {
                return typeof(object);
            }
        }

        public static bool FixEnumerationsWithReflection(this VariableSave variableSave)
        {
            if (variableSave.GetIsEnumeration() && variableSave.Value != null && variableSave.Value.GetType() == typeof(int))
            {
                Array array = Enum.GetValues(variableSave.GetRuntimeType());

                // GetValue returns the value at an index, which is bad if there are
                // gaps in the index
                // variableSave.Value = array.GetValue((int)variableSave.Value);
                for (int i = 0; i < array.Length; i++)
                {
                    if ((int)array.GetValue(i) == (int)variableSave.Value)
                    {
                        variableSave.Value = array.GetValue(i);
                        return true;
                    }
                }

                return false;
            }
            return false;
        }

        public static TypeConverter GetTypeConverter(this VariableSave variableSave, ElementSave container = null)
        {
            ElementSave categoryContainer;
            StateSaveCategory category;

            if (variableSave.CustomTypeConverter != null)
            {
                return variableSave.CustomTypeConverter;
            }
            else if (variableSave.IsFont)
            {
                return new FontTypeConverter();
            }
            else if (variableSave.Name == "Guide")
            {
                AvailableGuidesTypeConverter availableGuidesTypeConverter = new AvailableGuidesTypeConverter();
                availableGuidesTypeConverter.GumProjectSave = ObjectFinder.Self.GumProjectSave;
                availableGuidesTypeConverter.ShowNewGuide = false;
                return availableGuidesTypeConverter;
            }
            else if (variableSave.IsState(container, out categoryContainer, out category))
            {
                string categoryName = null;

                if (category != null)
                {
                    categoryName = category.Name;
                }

                AvailableStatesConverter converter = new AvailableStatesConverter(categoryName, Locator.GetRequiredService<ISelectedState>());
                converter.ElementSave = categoryContainer;
                return converter;
            }
            else if (variableSave.GetRootName() == "CurrentChainName")
            {
                var availableChainsConverter = new AvailableAnimationNamesConverter(container);
                return availableChainsConverter;
            }
            else
            {
                // We should see if it's an exposed variable, and if so, let's look to the source object's type converters
                bool foundInRoot = false;
                if (!string.IsNullOrEmpty(variableSave.SourceObject) && container != null)
                {
                    InstanceSave instance = container.GetInstance(variableSave.SourceObject);

                    if (instance != null)
                    {
                        // see if the instance has a variable
                        var foundElementSave = ObjectFinder.Self.GetRootStandardElementSave(instance);

                        if (foundElementSave != null)
                        {
                            VariableSave rootVariableSave = foundElementSave.DefaultState.GetVariableSave(variableSave.GetRootName());

                            if (rootVariableSave != null)
                            {
                                return rootVariableSave.GetTypeConverter((ElementSave)foundElementSave);
                            }
                        }
                    }
                }

            }
            Type type = variableSave.GetRuntimeType();
            return variableSave.GetTypeConverter(type);

        }

        static TypeConverter GetTypeConverter(this VariableSave variableSave, Type type)
        {
            if (type.IsEnum)
            {
                RestrictiveEnumConverter rec = new RestrictiveEnumConverter(type);

                rec.ValuesToExclude.AddRange(variableSave.ExcludedValuesForEnum);

                return rec;

                //return new EnumConverter(type);
            }
            else
            {
                return TypeDescriptor.GetConverter(type);
            }
        }
    }
}
