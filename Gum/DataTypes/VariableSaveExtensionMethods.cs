using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Variables;
using Gum.Reflection;
using System.ComponentModel;
using Gum.PropertyGridHelpers.Converters;
using Gum.Managers;
using ToolsUtilities;

namespace Gum.DataTypes
{
    public static class VariableSaveExtensionMethods
    {
        public static bool GetIsEnumeration(this VariableSave variableSave)
        {
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

            Type foundType = GetPrimitiveType(typeAsString);

            if (foundType != null)
            {
                return foundType;
            }

            if (variableSave.GetIsEnumeration())
            {
                return TypeManager.Self.GetTypeFromString(variableSave.Type);
            }
            else
            {
                return typeof(object);
            }
        }

        public static Type GetPrimitiveType(string typeAsString)
        {
            Type foundType = null;
            switch (typeAsString)
            {
                case "string":
                    foundType = typeof(string);
                    break;
                case "int":
                    foundType = typeof(int);
                    break;
                case "float":
                    foundType = typeof(float);
                    break;
                case "bool":
                    foundType = typeof(bool);
                    break;
            }
            return foundType;
        }

        public static TypeConverter GetTypeConverter(this VariableSave variableSave, ElementSave container = null)
        {
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

                            if (variableSave != null)
                            {
                                return rootVariableSave.GetTypeConverter((ElementSave)null);
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

        public static void FixEnumerations(this VariableSave variableSave)
        {
            if (variableSave.GetIsEnumeration() && variableSave.Value != null && variableSave.Value.GetType() == typeof(int))
            {
                Array array = Enum.GetValues(variableSave.GetRuntimeType());

                variableSave.Value = array.GetValue((int)variableSave.Value);

            }
        }

        public static VariableSave Clone(this VariableSave whatToClone)
        {
            var toReturn = FileManager.CloneSaveObject<VariableSave>(whatToClone);

            toReturn.ExcludedValuesForEnum.AddRange(whatToClone.ExcludedValuesForEnum);
            toReturn.FixEnumerations();
            return toReturn;
        }

        public static bool GetIsFileFromRoot(this VariableSave variable, InstanceSave instance)
        {
            if (variable == null)
            {
                int m = 3;
            }

            if (string.IsNullOrEmpty(variable.SourceObject))
            {
                ElementSave root = ObjectFinder.Self.GetRootStandardElementSave(instance);

                var variableInRoot = root.DefaultState.Variables.FirstOrDefault(item => item.Name == variable.GetRootName());

                if (variableInRoot != null)
                {
                    return variableInRoot.IsFile;
                }
            }
            else
            {
                ElementSave elementForInstance = ObjectFinder.Self.GetElementSave(instance.BaseType);

                string rootName = variable.GetRootName();
                VariableSave exposedVariable = elementForInstance.DefaultState.Variables.FirstOrDefault(item => item.ExposedAsName == rootName);

                if (exposedVariable != null)
                {
                    InstanceSave subInstance = elementForInstance.Instances.FirstOrDefault(item => item.Name == exposedVariable.SourceObject);

                    if (subInstance != null)
                    {
                        return exposedVariable.GetIsFileFromRoot(subInstance);
                    }
                }
                else
                {
                    // it's not exposed, so let's just get to the root of it:

                    ElementSave root = ObjectFinder.Self.GetRootStandardElementSave(instance);

                    var variableInRoot = root.DefaultState.Variables.FirstOrDefault(item => item.Name == variable.GetRootName());

                    if (variableInRoot != null)
                    {
                        return variableInRoot.IsFile;
                    }
                }

            }
            return false;

        }
    }

    public static class VariableSaveListExtensionMethods
    {
        public static VariableSave GetVariableSave(this List<VariableSave> variables, string variableName)
        {
            foreach(var variableSave in variables)
            {
                if(variableSave.Name == variableName)
                {
                    return variableSave;
                }
            }
            return null;
        }


    }
}
