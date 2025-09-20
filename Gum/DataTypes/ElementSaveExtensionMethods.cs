using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes.Variables;
using Gum.Managers;
using ToolsUtilities;

namespace Gum.DataTypes
{
    public class VariableSaveSorter : IComparer<VariableSave>
    {
        public List<VariableSave> ListOrderToMatch
        {
            get;
            set;
        }

        public int Compare(VariableSave x, VariableSave y)
        {
            int indexOfX = IndexOfByName(ListOrderToMatch, x.Name);
            int indexOfY = IndexOfByName(ListOrderToMatch, y.Name);

            return indexOfX.CompareTo(indexOfY);
        }

        public static int IndexOfByName(List<VariableSave> list, string name)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Name == name)
                {
                    return i;
                }
            }
            return -1;

        }
    }


    public static class ElementSaveExtensionMethods
    {
        public static bool Initialize(this ElementSave elementSave, StateSave? defaultState)
        {
            bool wasModified = false;

            if (AddAndModifyVariablesAccordingToDefault(elementSave, defaultState))
            {
                wasModified = true;
            }

            foreach (StateSave state in elementSave.AllStates)
            {
                state.ParentContainer = elementSave;
                state.Initialize();

                FixStateVariableTypes(elementSave, state, ref wasModified);
            }

            foreach (InstanceSave instance in elementSave.Instances)
            {
                instance.ParentContainer = elementSave;
                instance.Initialize(elementSave, ref wasModified);
            }

            return wasModified;
        }

        private static void FixStateVariableTypes(ElementSave elementSave, StateSave state, ref bool wasModified)
        {
            foreach(var variable in state.Variables.Where(item=>item.Type == "string" && item.Name.Contains("State")))
            {
                string name = variable.Name;

                var withoutState = name.Substring(0, name.Length - "State".Length);

                if(variable.Name == "State")
                {
                    variable.Type = "State";
                    wasModified = true;
                }
                else if(elementSave.Categories.Any(item=>item.Name == withoutState))
                {
                    variable.Type = withoutState;
                    wasModified = true;
                }
            }
            // Feb 2, 2022
            // State variables no longer have "State" appended. This was inconsistent at best since different systems resulted in variables with different types.
            // Removing "State" simplifies things. We'll see if this causes problems anywhere...
            foreach(var variable in state.Variables.Where(item => item.Type?.EndsWith("State") == true))
            {
                string name = variable.Type;

                var withoutState = name.Substring(0, name.Length - "State".Length);
                if (elementSave.Categories.Any(item => item.Name == withoutState))
                {
                    variable.Type = withoutState;
                    wasModified = true;
                }
            }
        }

        private static bool AddAndModifyVariablesAccordingToDefault(ElementSave elementSave, StateSave? defaultState)
        {
            bool wasModified = false;
            // Use States and not AllStates because we want to make sure we
            // have a default state.
            if (elementSave.States.Count == 0 && defaultState != null)
            {
                StateSave stateToAdd = defaultState.Clone();
                elementSave.States.Add(stateToAdd);
                wasModified = true;
            }
            else if (elementSave.States.Count != 0 && defaultState != null)
            {
                // Replacing the default state:
                // Update March 16, 2012
                // Used to replace but realized
                // it's better to not replace but
                // instead add variables that are not
                // already there.  That way when the user
                // switches types the old information isn't
                // lost.
                //elementSave.States[0] = replacement;
                StateSave stateForNewType = defaultState.Clone();

                var elementDefaultState = elementSave.DefaultState;
                foreach (VariableSave variableSave in stateForNewType.Variables)
                {
                    VariableSave existingVariable = elementDefaultState.GetVariableSave(variableSave.Name);

                    if(existingVariable == null)
                    {
                        // todo - for now we're going to check for variables that may match if we remove their spaces.
                        // Eventually this can go away. 
                        // Added February 2, 2025. Not sure when to remove this, in a few years?
                        existingVariable = elementDefaultState.Variables.FirstOrDefault(item => item.Name.Replace(" ", "") == variableSave.Name);
                    }

                    if (existingVariable == null)
                    {
                        wasModified = true;
                        elementSave.DefaultState.Variables.Add(variableSave.Clone());
                    }
                    else
                    {

                        // All of these properties are only relevant to the
                        // editor so we don't want to mark the object as modified
                        // when these properties are set.
                        existingVariable.Category = variableSave.Category;
                        existingVariable.CustomTypeConverter = variableSave.CustomTypeConverter;
                        existingVariable.ExcludedValuesForEnum.Clear();
                        existingVariable.ExcludedValuesForEnum.AddRange(variableSave.ExcludedValuesForEnum);

                        // let's fix any values that may be incorrectly set from types
                        if(existingVariable.Type == "float" && existingVariable.Value != null && (existingVariable.Value is float) == false)
                        {
                            float asFloat = 0.0f;
                            try
                            {
                                asFloat = (float)System.Convert.ChangeType(existingVariable.Value, typeof(float));
                            }
                            catch
                            {
                                // do nothing, we'll fall back to 0
                            }

                            existingVariable.Value = asFloat;
                            wasModified = true;
                            
                        }
                    }
                }

                // We also need to add any VariableListSaves here
                foreach (VariableListSave variableList in stateForNewType.VariableLists)
                {
                    VariableListSave existingList = elementSave.DefaultState.GetVariableListSave(variableList.Name);

                    if (existingList == null)
                    {
                        wasModified = true;
                        // this type doesn't have this list yet, so let's add it
                        elementSave.DefaultState.VariableLists.Add(variableList.Clone());
                    }
                    else
                    {
                        // See the VariableSave section on why we don't set
                        // wasModified = true here
                        existingList.Category = variableList.Category;

                        // on December 16, 2024 the Polygon class was given a
                        // new "Vector2" type. Old projects do not have this type
                        // so we should check and add it if not:
                        if(existingList.Type != variableList.Type)
                        {
                            existingList.Type = variableList.Type;
                            wasModified = true;
                        }
                    }
                }

                foreach (var stateSaveCategory in elementSave.Categories)
                {
                    VariableSave foundVariable = elementSave.DefaultState.Variables.FirstOrDefault(item => item.Name == stateSaveCategory.Name + "State");

                    if (foundVariable == null)
                    {
                        elementSave.DefaultState.Variables.Add(new VariableSave()
                        {
                            Name = stateSaveCategory.Name + "State",
                            Type = "string",
                            Value = null

                        });
                    }
                }

                VariableSaveSorter vss = new VariableSaveSorter();
                vss.ListOrderToMatch = defaultState.Variables;


                elementSave.DefaultState.Variables.Sort(vss);


            }
            else
            {
                // Let's give it an empty state so that it doesn't cause runtime problems
                // Nevermind, this causes problelms in Gum, and it should be okay to pass a null state here
                //elementSave.States.Add(new StateSave());
            }

            return wasModified;
        }

        public static bool ContainsName(this List<StandardElementSave> list, string name)
        {
            foreach (StandardElementSave ses in list)
            {
                if (ses.Name == name)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsOfType(this ElementSave elementSave, string typeToCheck)
        {
            if (elementSave is ComponentSave)
            {
                return (elementSave as ComponentSave).IsOfType(typeToCheck);
            }
            else
            {
                return elementSave.Name == typeToCheck;
            }


        }


        public static StateSave GetStateSaveRecursively(this ElementSave element, string stateName)
        {
            var foundState = element.AllStates.FirstOrDefault(item => item.Name == stateName);

            if (foundState != null)
            {
                return foundState;
            }

            if (!string.IsNullOrEmpty(element.BaseType))
            {
                var baseElement = ObjectFinder.Self.GetElementSave(element.BaseType);

                return baseElement.GetStateSaveRecursively(stateName);
            }

            return null;
        }

        public static StateSaveCategory? GetStateSaveCategoryRecursively(this IStateContainer element, Func<StateSaveCategory, bool> condition) =>
            GetStateSaveCategoryRecursively(element, condition, out IStateContainer? _);
        
        public static StateSaveCategory? GetStateSaveCategoryRecursively(this IStateContainer element, string categoryName) =>
            GetStateSaveCategoryRecursively(element, categoryName, out IStateContainer? _);

        public static StateSaveCategory? GetStateSaveCategoryRecursively(this IStateContainer element, string categoryName, out IStateContainer? categoryContainer) => 
            GetStateSaveCategoryRecursively(element, item => item.Name == categoryName, out categoryContainer);
        
        /// <returns>
        /// The first category of this element that meets the given <paramref name="condition"/>.
        /// </returns>
        public static StateSaveCategory? GetStateSaveCategoryRecursively(this IStateContainer stateContainer, Func<StateSaveCategory, bool> condition, 
            out IStateContainer? foundStateContainer)
        {
            StateSaveCategory? foundCategory = stateContainer.Categories.FirstOrDefault(condition);

            if (foundCategory != null)
            {
                foundStateContainer = stateContainer;
                return foundCategory;
            }

            if (stateContainer is ElementSave elementSave && !string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElement = ObjectFinder.Self.GetElementSave(elementSave.BaseType);

                if(baseElement == null)
                {
                    foundStateContainer = null;
                    return null;
                }
                else
                {
                    return baseElement.GetStateSaveCategoryRecursively(condition, out foundStateContainer);
                }
            }

            foundStateContainer = null;
            return null;
        }        
    }
}
