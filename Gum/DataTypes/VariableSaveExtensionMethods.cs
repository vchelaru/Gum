using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Variables;
using System.ComponentModel;

using Gum.Managers;
using ToolsUtilities;

#if GUM
using Gum.Reflection;
using Gum.PropertyGridHelpers.Converters;
#endif

namespace Gum.DataTypes
{
    public static class VariableSaveExtensionMethods
    {
#if GUM
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
#endif

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

#if GUM
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
            else if(variableSave.IsState(container, out categoryContainer, out category ))
            {
                string categoryName = null;

                if(category != null)
                {
                    categoryName = category.Name;
                }

                AvailableStatesConverter converter = new AvailableStatesConverter(categoryName);
                converter.ElementSave = categoryContainer;
                return converter;
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
#endif





        public static bool IsState(this VariableSave variableSave, ElementSave container)
        {
            ElementSave throwaway1;
            StateSaveCategory throwaway2;
            return variableSave.IsState(container, out throwaway1, out throwaway2);
        }

        public static bool IsState(this VariableSave variableSave, ElementSave container, out ElementSave categoryContainer, out StateSaveCategory category, bool recursive = true)
        {
            if(container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }
            category = null;
            categoryContainer = null;

            var variableName = variableSave.GetRootName();

            // This is called a lot so let's try to make it faster:
            bool endsWithState = variableName.Length >= 5 &&
                variableName[variableName.Length - 1] == 'e' &&
                variableName[variableName.Length - 2] == 't' &&
                variableName[variableName.Length - 3] == 'a' &&
                variableName[variableName.Length - 4] == 't' &&
                variableName[variableName.Length - 5] == 'S';

            ///////////////Early Out
            if (endsWithState == false && string.IsNullOrEmpty(variableSave.SourceObject))
            {
                return false;
            }
            /////////////End early out

            // what about uncategorized

            string categoryName = null;

            if (endsWithState)
            {
                categoryName = variableName.Substring(0, variableName.Length - "State".Length);
            }

            if (string.IsNullOrEmpty(variableSave.SourceObject) == false)
            {
                var instanceSave = container.GetInstance(variableSave.SourceObject);

                if (instanceSave != null)
                {
                    var element = ObjectFinder.Self.GetElementSave(instanceSave.BaseType);

                    if (element != null)
                    {
                        var defaultState = element.DefaultState;
                        if(defaultState == null)
                        {
                            throw new NullReferenceException(
                                $"Could not find a default state for {element} - this happens if the element wasn't initialized, or if its file was not loaded properly.");
                        }
                        // let's try going recursively:
                        var subVariable = element.DefaultState.Variables.FirstOrDefault(item => item.ExposedAsName == variableSave.GetRootName());

                        if (subVariable != null && recursive)
                        {
                            return subVariable.IsState(element, out categoryContainer, out category);
                        }
                        else
                        {
                            if (variableName == "State")
                            {
                                categoryContainer = element;
                                category = null;
                                return true;
                            }
                            else
                            {

                                category = element.GetStateSaveCategoryRecursively(categoryName, out categoryContainer);
                                return category != null;
                            }
                        }
                    }
                }
            }
            else
            {
                if (variableName == "State")
                {
                    return true;
                }
                else
                {

                    category = container.GetStateSaveCategoryRecursively(categoryName, out categoryContainer);

                    return category != null;
                }
            }

            return false;
        }



        public static bool IsEnumeration(this VariableSave variableSave)
        {
            string type = variableSave.Type;

            switch (type)
            {
                case "string":
                case "int":
                case "float":
                case "bool":
                case "decimal":
                case "double":

                    return false;
                    //break;
            }

            return true;
        }

        /// <summary>
        /// Converts integer values to their corresponding enumeration values. This should be called
        /// after variable saves are loaded from XML.
        /// </summary>
        /// <param name="variableSave">The VariableSave to fix.</param>
        /// <returns>Whether any changes were made.</returns>
        public static bool FixEnumerations(this VariableSave variableSave)
        {

#if GUM
            if (variableSave.GetIsEnumeration() && variableSave.Value != null && variableSave.Value.GetType() == typeof(int))
            {
                Array array = Enum.GetValues(variableSave.GetRuntimeType());

                // GetValue returns the value at an index, which is bad if there are
                // gaps in the index
                // variableSave.Value = array.GetValue((int)variableSave.Value);
                for(int i = 0; i < array.Length; i++)
                {
                    if((int)array.GetValue(i) == (int)variableSave.Value)
                    {
                        variableSave.Value = array.GetValue(i);
                        return true;
                    }
                }

                return false;
            }
            return false;
#else
            if(variableSave.Value != null && variableSave.Value is int)
            {
                switch (variableSave.Type)
                {
                    case "DimensionUnitType":
                    case "Gum.DataTypes.DimensionUnitType":
                        variableSave.Value = (Gum.DataTypes.DimensionUnitType)variableSave.Value;
                        
                        return true;
                    case "VerticalAlignment":
                    case "RenderingLibrary.Graphics.VerticalAlignment":

                        variableSave.Value = (global::RenderingLibrary.Graphics.VerticalAlignment)variableSave.Value;
                        return true;
                    case "HorizontalAlignment":
                    case "RenderingLibrary.Graphics.HorizontalAlignment":
                        variableSave.Value = (global::RenderingLibrary.Graphics.HorizontalAlignment)variableSave.Value;
                        return true;
                    case "PositionUnitType":
                    case "Gum.Managers.PositionUnitType":
                        variableSave.Value = (Gum.Managers.PositionUnitType)variableSave.Value;
                        return true;
                    case "GeneralUnitType":
                    case "Gum.Converters.GeneralUnitType":
                        variableSave.Value = (Gum.Converters.GeneralUnitType)variableSave.Value;
                        return true;
                    case "Gum.RenderingLibrary.Blend":
                    case "Blend":
                        variableSave.Value = (Gum.RenderingLibrary.Blend)variableSave.Value;
                        return true;
                    case "Gum.Managers.TextureAddress":
                    case "TextureAddress":
                    
                        variableSave.Value = (TextureAddress)variableSave.Value;
                        return true;
                    case "Gum.Managers.ChildrenLayout":         
                    case "ChildrenLayout":
                        variableSave.Value = (ChildrenLayout)variableSave.Value;
                        return true;
                    default:
                        return false;
                }
            
            }

            return false;
#endif

        }

        public static VariableSave Clone(this VariableSave whatToClone)
        {
            var toReturn = FileManager.CloneSaveObject<VariableSave>(whatToClone);

            toReturn.ExcludedValuesForEnum.AddRange(whatToClone.ExcludedValuesForEnum);
#if GUM
            toReturn.FixEnumerations();
#endif
            return toReturn;
        }

        public static bool GetIsFileFromRoot(this VariableSave variable, ElementSave element)
        {
            var variableInRoot = element.DefaultState.Variables.FirstOrDefault(item => item.Name == variable.GetRootName());

            if (variableInRoot != null)
            {
                return variableInRoot.IsFile;
            }            
            else
            {
                // unknown so assume no
                return false;
            }
        }

        public static bool GetIsFileFromRoot(this VariableSave variable, InstanceSave instance)
        {
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
