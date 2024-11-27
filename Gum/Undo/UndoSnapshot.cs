using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;

namespace Gum.Undo
{
    public class UndoComparison
    {
        public List<InstanceSave> AddedInstances;
        public List<InstanceSave> RemovedInstances;

        public List<StateSave> AddedStates;
        public List<StateSave> RemovedStates;

        public List<VariableSave> ModifiedVariables;
        public List<VariableListSave> ModifiedVariableLists;

        public override string ToString()
        {
            string toReturn = "";

            if (AddedInstances?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += '\n';
                toReturn += $"Added instances: {string.Join(", ", AddedInstances.Select(item => item.Name))}";
            }
            if (RemovedInstances?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += '\n';
                toReturn += $"Removed instances: {string.Join(", ", RemovedInstances.Select(item => item.Name))}";
            }
            if (AddedStates?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += '\n';
                toReturn += $"Added states: {string.Join(", ", AddedStates.Select(item => item.Name))}";
            }
            if (RemovedStates?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += '\n';
                toReturn += $"Removed states: {string.Join(", ", RemovedStates.Select(item => item.Name))}";
            }
            if (ModifiedVariables?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += '\n';
                toReturn += $"Modified variables: {string.Join(", ", ModifiedVariables.Select(item => item.Name + "=" + item.Value?.ToString() ?? "<null>"))}";
            }
            if (ModifiedVariableLists?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += '\n';
                toReturn += $"Modified variable lists: {string.Join(", ", ModifiedVariableLists.Select(item => item.Name))}";
            }
            return toReturn;
        }
    }

    public class UndoSnapshot
    {
        public ElementSave Element;
        public string CategoryName;
        public string StateName;

        public override string ToString()
        {
            // It would be nice to know what differed on this undo, but the way Gum works, it just takes a snapshot
            // of the entire element. This is lazy and performance isn't great, but it's also really easy to code against
            // and handles undos accurately. In the future we may add more info here if we want deeper diagnostics.
            var toReturn = $"{Element.Name} in {StateName ?? "<default>"}";

            if(!string.IsNullOrEmpty(CategoryName) )
            {
                toReturn += $" ({CategoryName})";
            }

            return toReturn;
        }



        public UndoComparison CompareAgainst(UndoSnapshot other)
        {
            var thisElement = Element;
            var oldElement = other.Element;
            return CompareAgainst(thisElement, oldElement);
        }

        public UndoComparison CompareAgainst(ElementSave newElement, ElementSave oldElement)
        {
            var toReturn = new UndoComparison();

            // check for instances that were added or removed
            var thisInstances = newElement.Instances;
            var otherInstances = oldElement.Instances;
            //toReturn.AddedInstances = thisInstances.Except(otherInstances).ToList();
            //toReturn.RemovedInstances = otherInstances.Except(thisInstances).ToList();

            // Check for added or removed categories
            var thisCategories = newElement.Categories;
            var otherCategories = oldElement.Categories;
            List<StateSaveCategory> addedCategories = null;

            if(otherCategories != null)
            {
                addedCategories = thisCategories?.Except(otherCategories).ToList();
            }

            List<StateSaveCategory> removedCategories = null;
            if (thisCategories != null)
            {
                removedCategories = otherCategories?.Except(thisCategories).ToList();
            }

            toReturn.AddedInstances = new List<InstanceSave>();
            toReturn.RemovedInstances = new List<InstanceSave>();
            toReturn.ModifiedVariables = new List<VariableSave>();
            toReturn.ModifiedVariableLists = new List<VariableListSave>();

            AddVariableModifications(newElement.DefaultState, oldElement.DefaultState, toReturn);

            // loop through each category, check for states that were added or removed:
            if (newElement.Categories != null)
            {
                foreach (var category in newElement.Categories)
                {
                    var matchingCategory = oldElement.Categories?.FirstOrDefault(otherCategory => otherCategory.Name == category.Name);

                    if (matchingCategory != null)
                    {
                        toReturn.AddedStates.AddRange(category.States.Except(matchingCategory.States).ToList());
                        toReturn.RemovedStates.AddRange(matchingCategory.States.Except(category.States).ToList());

                        foreach (var state in category.States)
                        {
                            var matchingState = matchingCategory.States.FirstOrDefault(otherState => otherState.Name == state.Name);
                            if (matchingState != null)
                            {
                                AddVariableModifications(state, matchingState, toReturn);
                            }
                        }
                    }
                }
            }

            return toReturn;
        }

        private void AddVariableModifications(StateSave newState, StateSave oldState, UndoComparison snapshot)
        {
            if(newState == null || oldState == null)
            {
                return;
            }
            var newVariableNameLists = newState.Variables.Select(item => item.Name).ToList();
            newVariableNameLists.AddRange(newState.VariableLists.Select(item => item.Name));
            var newVariableHash = newVariableNameLists.ToHashSet();

            var oldVariableNameList = oldState.Variables.Select(item => item.Name).ToList();
            oldVariableNameList.AddRange(oldState.VariableLists.Select(item => item.Name));
            var oldVariableHash = oldVariableNameList.ToHashSet();

            var addedVariables = newState.Variables.Where(item => oldVariableHash.Contains(item.Name) == false);
            var removedVariables = oldState.Variables.Where(item => newVariableHash.Contains(item.Name) == false);
            var addedVariableLists = newState.VariableLists.Where(item => oldVariableHash.Contains(item.Name) == false);
            var removedVariableLists = oldState.VariableLists.Where(item => newVariableHash.Contains(item.Name) == false);

            // if any variables were added, then undoing goes back to the default:
            foreach(var variable in addedVariables)
            {
                var clone = variable.Clone();
                clone.Value = null;
                snapshot.ModifiedVariables.Add(clone);
            }
            foreach(var variableList in addedVariableLists)
            {
                var clone = variableList.Clone();
                clone.ValueAsIList = null;
                snapshot.ModifiedVariableLists.Add(clone);

            }


            snapshot.ModifiedVariables.AddRange(removedVariables);
            snapshot.ModifiedVariableLists.AddRange(removedVariableLists);

            foreach (var newVariable in newState.Variables)
            {
                var matchingOldVariable = oldState.Variables.FirstOrDefault(otherVariable => otherVariable.Name == newVariable.Name);
                if (matchingOldVariable != null)
                {
                    var areEqual = (newVariable.Value == null && matchingOldVariable.Value == null) ||
                        (newVariable.Value != null && newVariable.Value.Equals(matchingOldVariable.Value));
                    if(!areEqual)
                    {
                        snapshot.ModifiedVariables.Add(matchingOldVariable);
                    }
                }
            }

            foreach (var variableList in newState.VariableLists)
            {
                var matchingVariableList = oldState.VariableLists.FirstOrDefault(otherVariableList => otherVariableList.Name == variableList.Name);
                if (matchingVariableList != null && variableList.ValueAsIList != matchingVariableList.ValueAsIList)
                {
                    var areEqual = (variableList.ValueAsIList == null && matchingVariableList.ValueAsIList == null);

                    if (!areEqual)
                    {
                        if(variableList.ValueAsIList != null && matchingVariableList.ValueAsIList != null)
                        {
                            var thisList = variableList.ValueAsIList;
                            var otherList = matchingVariableList.ValueAsIList;

                            if (thisList.Count != otherList.Count)
                            {
                                areEqual = false;
                            }
                            else
                            {
                                areEqual = true;
                                for (int i = 0; i < thisList.Count; i++)
                                {
                                    if (thisList[i].Equals(otherList[i]) == false)
                                    {
                                        areEqual = false;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            areEqual = false;
                        }
                    }
                    if(!areEqual)
                    {
                        snapshot.ModifiedVariableLists.Add(variableList);
                    }
                }
            }
        }
    }
}
