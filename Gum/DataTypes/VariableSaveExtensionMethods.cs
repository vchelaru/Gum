using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes.Variables;
using System.ComponentModel;

using Gum.Managers;
using ToolsUtilities;

namespace Gum.DataTypes
{
    public static class VariableSaveExtensionMethods
    {

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
                case "int?":
                    foundType = typeof(int?);
                    break;
                case "float":
                    foundType = typeof(float);
                    break;
                case "float?":
                    foundType = typeof(float?);
                    break;
                case "decimal":
                    foundType = typeof(decimal);
                    break;
                case "decimal?":
                    foundType = typeof(decimal?);
                    break;
                case "bool":
                    foundType = typeof(bool);
                    break;
                case "bool?":
                    foundType = typeof(bool?);
                    break;
            }
            return foundType;
        }

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
                        // this happens in a background so let's create a copy to prevent thread access bugs:
                        //var subVariable = element.DefaultState.Variables.FirstOrDefault(item => item.ExposedAsName == variableSave.GetRootName());
                        var rootName = variableSave.GetRootName();
                        // why do we ToArray it? That's slow
                        //var subVariable = element.DefaultState.Variables.ToArray().FirstOrDefault(item => item.ExposedAsName == rootName);
                        VariableSave subVariable = null;
                        var variables = element.DefaultState.Variables;
                        for (int i = 0; i < variables.Count; i++)
                        {
                            var variableAtI = variables[i];
                            if(variableAtI.ExposedAsName == rootName)
                            {
                                subVariable = variableAtI;
                                break;
                            }
                        }

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
                                
                                category = element.GetStateSaveCategoryRecursively(categoryName, out IStateContainer foundCategoryContainer);
                                categoryContainer = foundCategoryContainer as ElementSave;
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

                    category = container.GetStateSaveCategoryRecursively(categoryName, out IStateContainer foundCategoryContainer);
                    categoryContainer = foundCategoryContainer as ElementSave;

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
                case "int?":
                case "float":
                case "float?":
                case "bool":
                case "bool?":
                case "decimal":
                case "double":
                case "double?":

                    return false;
                    //break;
            }

            return true;
        }

        public static void ConvertEnumerationValuesToInts(this VariableSave variableSave)
        {
            if(variableSave.Value != null)
            {
                switch (variableSave.Type)
                {
                    case "DimensionUnitType":
                    case "Gum.DataTypes.DimensionUnitType":
                    case "VerticalAlignment":
                    case "RenderingLibrary.Graphics.VerticalAlignment":
                    case "HorizontalAlignment":
                    case "RenderingLibrary.Graphics.HorizontalAlignment":
                    case "PositionUnitType":
                    case "Gum.Managers.PositionUnitType":
                    case "GeneralUnitType":
                    case "Gum.Converters.GeneralUnitType":
                    case "Gum.RenderingLibrary.Blend":
                    case "Blend":
                    case "Gum.Managers.TextureAddress":
                    case "TextureAddress":
                    case "Gum.Managers.ChildrenLayout":
                    case "ChildrenLayout":
                    case "TextOverflowHorizontalMode":
                    case "TextOverflowVerticalMode":
                    case "RenderingLibrary.Graphics.TextOverflowHorizontalMode":
                    case "RenderingLibrary.Graphics.TextOverflowVerticalMode":

                        variableSave.Value = (int)variableSave.Value;
                        break;
                }
            }
        }

        public static Func<VariableSave, bool> CustomFixEnumerations;

        /// <summary>
        /// Converts integer values to their corresponding enumeration values. This should be called
        /// after variable saves are loaded from XML.
        /// </summary>
        /// <param name="variableSave">The VariableSave to fix.</param>
        /// <returns>Whether any changes were made.</returns>
        public static bool FixEnumerations(this VariableSave variableSave)
        {
            bool toReturn = false;
            if(CustomFixEnumerations != null)
            {
                toReturn = CustomFixEnumerations(variableSave);
            }

            if(!toReturn && variableSave.Value != null)
            {
                int valueAsInt = 0;
                var isInt = false;
                if(variableSave.Value is int asInt)
                {
                    isInt = true;
                    valueAsInt = asInt;
                }
                else if(variableSave.Value is long asLong)
                {
                    isInt = true;
                    valueAsInt = (int)asLong;
                }

                if(isInt)
                {
                    // The code above ultimately relies on Gum.Reflection.TypeManager which isn't
                    // available here. variableSave types are not qualified, so this won't work
                    //var type = typeof(VariableSaveExtensionMethods).Assembly.GetType(variableSave.Type);

                    //if(type != null)
                    //{
                    //    Array array = Enum.GetValues(type);
                    //    for (int i = 0; i < array.Length; i++)
                    //    {
                    //        if ((int)array.GetValue(i) == valueAsInt)
                    //        {
                    //            variableSave.Value = array.GetValue(i);
                    //            return true;
                    //        }
                    //    }
                    //}



                    switch (variableSave.Type)
                    {
                        case "DimensionUnitType":
                        case "Gum.DataTypes.DimensionUnitType":
                            variableSave.Value = (Gum.DataTypes.DimensionUnitType)valueAsInt;

                            toReturn = true;
                            break;
                        case "VerticalAlignment":
                        case "RenderingLibrary.Graphics.VerticalAlignment":

                            variableSave.Value = (global::RenderingLibrary.Graphics.VerticalAlignment)valueAsInt;
                            toReturn = true;
                            break;
                        case "HorizontalAlignment":
                        case "RenderingLibrary.Graphics.HorizontalAlignment":
                            variableSave.Value = (global::RenderingLibrary.Graphics.HorizontalAlignment)valueAsInt;
                            toReturn = true;
                            break;
                        case "PositionUnitType":
                        case "Gum.Managers.PositionUnitType":
                            variableSave.Value = (Gum.Managers.PositionUnitType)valueAsInt;
                            toReturn = true;
                            break;
                        case "GeneralUnitType":
                        case "Gum.Converters.GeneralUnitType":
                            variableSave.Value = (Gum.Converters.GeneralUnitType)valueAsInt;
                            toReturn = true;
                            break;

                        case "Gum.RenderingLibrary.Blend":
                        case "Blend":
                            variableSave.Value = (Gum.RenderingLibrary.Blend)valueAsInt;
                            toReturn = true;
                            break;

                        case "Gum.Managers.TextureAddress":
                        case "TextureAddress":
                    
                            variableSave.Value = (TextureAddress)valueAsInt;
                            toReturn = true;
                            break;
                        case "Gum.Managers.ChildrenLayout":         
                        case "ChildrenLayout":
                            variableSave.Value = (ChildrenLayout)valueAsInt;
                            toReturn = true;
                            break;
                        case "GradientType":
                        case "RenderingLibrary.Graphics.GradientType":
                            variableSave.Value = (global::RenderingLibrary.Graphics.GradientType)valueAsInt;
                            toReturn = true;
                            break;

                        case "RenderingLibrary.Graphics.TextOverflowHorizontalMode":
                        case "TextOverflowHorizontalMode":
                            variableSave.Value = (global::RenderingLibrary.Graphics.TextOverflowHorizontalMode)valueAsInt;
                            toReturn = true;
                            break;

                        case "RenderingLibrary.Graphics.TextOverflowVerticalMode":
                        case "TextOverflowVerticalMode":
                            variableSave.Value = (global::RenderingLibrary.Graphics.TextOverflowVerticalMode)valueAsInt;
                            toReturn = true;
                            break;

                        default:
                            toReturn = false;
                            break;
                    }
                }
            
            }

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
