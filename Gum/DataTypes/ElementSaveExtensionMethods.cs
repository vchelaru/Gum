﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Variables;
using Gum.Managers;
using ToolsUtilities;

#if GUM
using Gum.ToolStates;
using System.Windows.Forms;
#endif
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
        public static void Initialize(this ElementSave elementSave, StateSave defaultState)
        {
            AddAndModifyVariablesAccordingToDefault(elementSave, defaultState);

            foreach (StateSave state in elementSave.AllStates)
            {
                state.ParentContainer = elementSave;
                state.Initialize();

                FixStateVariableTypes(elementSave, state);
            }

            foreach (InstanceSave instance in elementSave.Instances)
            {
                instance.ParentContainer = elementSave;
                instance.Initialize();
            }
        }

        private static void FixStateVariableTypes(ElementSave elementSave, StateSave state)
        {
            foreach(var variable in state.Variables.Where(item=>item.Type == "string" && item.Name.Contains("State")))
            {
                string name = variable.Name;

                var withoutState = name.Substring(0, name.Length - "State".Length);
                if(variable.Name == "State")
                {
                    variable.Type = "State";
                }
                else if(elementSave.Categories.Any(item=>item.Name == withoutState))
                {
                    variable.Type = withoutState;
                }
            }
        }

        private static void AddAndModifyVariablesAccordingToDefault(ElementSave elementSave, StateSave defaultState)
        {
            // Use States and not AllStates because we want to make sure we
            // have a default state.
            if (elementSave.States.Count == 0 && defaultState != null)
            {
                StateSave stateToAdd = defaultState.Clone();
                elementSave.States.Add(stateToAdd);
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

                foreach (VariableSave variableSave in stateForNewType.Variables)
                {
                    VariableSave existingVariable = elementSave.DefaultState.GetVariableSave(variableSave.Name);

                    if (existingVariable == null)
                    {
                        elementSave.DefaultState.Variables.Add(variableSave.Clone());
                    }
                    else
                    {
                        existingVariable.Category = variableSave.Category;
#if !WINDOWS_8
                        existingVariable.CustomTypeConverter = variableSave.CustomTypeConverter;
#endif
                        existingVariable.ExcludedValuesForEnum.Clear();
                        existingVariable.ExcludedValuesForEnum.AddRange(variableSave.ExcludedValuesForEnum);
                    }
                }

                // We also need to add any VariableListSaves here
                foreach (VariableListSave variableList in stateForNewType.VariableLists)
                {
                    VariableListSave existingList = elementSave.DefaultState.GetVariableListSave(variableList.Name);

                    if (existingList == null)
                    {
                        // this type doesn't have this list yet, so let's add it
                        elementSave.DefaultState.VariableLists.Add(variableList.Clone());
                    }
                    else
                    {
                        existingList.Category = variableList.Category;
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
#if GUM
,
                            CustomTypeConverter = new Gum.PropertyGridHelpers.Converters.AvailableStatesConverter(stateSaveCategory.Name)
#endif
                        });
                    }
                    else
                    {
#if GUM

                        foundVariable.CustomTypeConverter = new Gum.PropertyGridHelpers.Converters.AvailableStatesConverter(stateSaveCategory.Name);
#endif
                    }
                }

                VariableSaveSorter vss = new VariableSaveSorter();
                vss.ListOrderToMatch = defaultState.Variables;


                elementSave.DefaultState.Variables.Sort(vss);
            }
            else
            {
                // Let's give it an empty state so that it doesn't cause runtime problems
                // Never mind, this causes problems in Gum
                //elementSave.States.Add(new StateSave());
            }
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
#if GUM
        public static string GetFullPathXmlFile(this ElementSave instance)
        {
            return instance.GetFullPathXmlFile(instance.Name);
        }


        public static string GetFullPathXmlFile(this ElementSave instance, string elementSaveName)
        {
            if (string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                return null;
            }

            string directory = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);

            return directory + instance.Subfolder + "\\" + elementSaveName + "." + instance.FileExtension;
        }


        public static void ReactToChangedBaseType(this ElementSave asElementSave, InstanceSave instanceSave, string oldValue)
        {
            if (instanceSave != null)
            {
                // nothing to do here because the new type only impacts which variables are visible, and the refresh of the PropertyGrid will handle that.
            }
            else
            {
                string newValue = asElementSave.BaseType;

                if (StandardElementsManager.Self.IsDefaultType(newValue))
                {

                    StateSave defaultStateSave = StandardElementsManager.Self.GetDefaultStateFor(newValue);

                    asElementSave.Initialize(defaultStateSave);
                }
                else
                {
                    MessageBox.Show("Currently we don't support components inheriting from other components.  But I'm sure this will be added");
                    asElementSave.BaseType = oldValue.ToString();
                }
            }
            const bool fullRefresh = true;
            PropertyGridManager.Self.RefreshUI(fullRefresh);
            StateTreeViewManager.Self.RefreshUI(asElementSave);
        }
#endif

        public static VariableSave GetVariableFromThisOrBase(this ElementSave element, string variable, bool forceDefault = false)
        {
            StateSave stateToPullFrom = element.DefaultState;
#if GUM
            if (element == SelectedState.Self.SelectedElement &&
                SelectedState.Self.SelectedStateSave != null &&
                !forceDefault)
            {
                stateToPullFrom = SelectedState.Self.SelectedStateSave;
            }
#endif
            return stateToPullFrom.GetVariableRecursive(variable);
        }

        public static object GetValueFromThisOrBase(this ElementSave element, string variable, bool forceDefault = false)
        {
            StateSave stateToPullFrom = element.DefaultState;

#if GUM
            if (element == SelectedState.Self.SelectedElement &&
                SelectedState.Self.SelectedStateSave != null &&
                !forceDefault)
            {
                stateToPullFrom = SelectedState.Self.SelectedStateSave;
            }
#endif
            VariableSave variableSave = stateToPullFrom.GetVariableRecursive(variable);
            if (variableSave != null)
            {
                return variableSave.Value;
            }
            else
            {
                return null;
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

        public static StateSaveCategory GetStateSaveCategoryRecursively(this ElementSave element, string categoryName)
        {
            ElementSave throwaway;
            return GetStateSaveCategoryRecursively(element, categoryName, out throwaway);
        }

        public static StateSaveCategory GetStateSaveCategoryRecursively(this ElementSave element, string categoryName, out ElementSave categoryContainer)
        {
            var foundCategory = element.Categories.FirstOrDefault(item => item.Name == categoryName);

            if (foundCategory != null)
            {
                categoryContainer = element;
                return foundCategory;
            }

            if (!string.IsNullOrEmpty(element.BaseType))
            {
                var baseElement = ObjectFinder.Self.GetElementSave(element.BaseType);

                return baseElement.GetStateSaveCategoryRecursively(categoryName, out categoryContainer);
            }

            categoryContainer = null;
            return null;
        }

    }
}
