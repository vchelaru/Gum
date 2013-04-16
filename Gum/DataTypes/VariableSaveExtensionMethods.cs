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

        public static TypeConverter GetTypeConverter(this VariableSave variableSave)
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
                Type type = variableSave.GetRuntimeType();
                return variableSave.GetTypeConverter(type);
            }
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
