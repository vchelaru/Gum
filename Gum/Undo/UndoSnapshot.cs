using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
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
                toReturn += $"Add instances: {string.Join(", ", AddedInstances.Select(item => item.Name))}";
            }
            if (RemovedInstances?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += '\n';
                toReturn += $"Remove instances: {string.Join(", ", RemovedInstances.Select(item => item.Name))}";
            }
            if (AddedStates?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += '\n';
                toReturn += $"Add states: {string.Join(", ", AddedStates.Select(item => item.Name))}";
            }
            if (RemovedStates?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += '\n';
                toReturn += $"Remove states: {string.Join(", ", RemovedStates.Select(item => item.Name))}";
            }
            if (ModifiedVariables?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += '\n';
                toReturn += $"Variables: {string.Join(", ", ModifiedVariables.Select(item => item.Name + "=" + item.Value?.ToString() ?? "<null>"))}";
            }
            if (ModifiedVariableLists?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += '\n';
                toReturn += $"Variable lists: {string.Join(", ", ModifiedVariableLists.Select(item => item.Name))}";
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



        public UndoComparison CompareAgainst(UndoSnapshot snapshotToApply)
        {
            var thisElement = Element;
            var oldElement = snapshotToApply.Element;
            return CompareAgainst(thisElement, oldElement);
        }

        public UndoComparison CompareAgainst(ElementSave currentElement, ElementSave snapshotToApply)
        {
            var toReturn = new UndoComparison();

            AddInstanceModifiations(currentElement, snapshotToApply, toReturn);

            // Check for added or removed categories
            var thisCategories = currentElement.Categories;
            var otherCategories = snapshotToApply.Categories;
            List<StateSaveCategory> addedCategories = null;

            if (otherCategories != null)
            {
                addedCategories = thisCategories?.Except(otherCategories).ToList();
            }

            List<StateSaveCategory> removedCategories = null;
            if (thisCategories != null)
            {
                removedCategories = otherCategories?.Except(thisCategories).ToList();
            }

            toReturn.ModifiedVariables = new List<VariableSave>();
            toReturn.ModifiedVariableLists = new List<VariableListSave>();

            var newState = snapshotToApply.DefaultState;
            var oldState = currentElement.DefaultState;
            AddVariableModifications(newState, oldState, toReturn);

            // loop through each category, check for states that were added or removed:
            if (currentElement.Categories != null)
            {
                toReturn.AddedStates = new List<StateSave>();
                toReturn.RemovedStates = new List<StateSave>();

                foreach (var currentCategory in currentElement.Categories)
                {
                    var categoryToApply = snapshotToApply.Categories?.FirstOrDefault(otherCategory => otherCategory.Name == currentCategory.Name);

                    if (categoryToApply != null)
                    {
                        AddStateModificationsInCategory(toReturn, currentCategory, categoryToApply);

                        foreach (var state in currentCategory.States)
                        {
                            var matchingState = categoryToApply.States.FirstOrDefault(otherState => otherState.Name == state.Name);
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

        private static void AddStateModificationsInCategory(UndoComparison toReturn, StateSaveCategory currentCategory, StateSaveCategory categoryToApply)
        {
            foreach (var state in currentCategory.States)
            {
                var matchingInToApply = categoryToApply.States.FirstOrDefault(otherState => otherState.Name == state.Name);
                if (matchingInToApply == null)
                {
                    toReturn.RemovedStates.Add(state);
                }
            }
            foreach (var state in categoryToApply.States)
            {
                var matchingInCurrent = currentCategory.States.FirstOrDefault(otherState => otherState.Name == state.Name);
                if (matchingInCurrent == null)
                {
                    toReturn.AddedStates.Add(state);
                }
            }
        }

        private static void AddInstanceModifiations(ElementSave currentElement, ElementSave snapshotToApply, UndoComparison toReturn)
        {
            // check for instances that were added or removed
            var currentInstances = currentElement.Instances;
            var instancesToApply = snapshotToApply.Instances;

            toReturn.RemovedInstances = new List<InstanceSave>();
            toReturn.AddedInstances = new List<InstanceSave>();

            if (instancesToApply != null && currentInstances != null)
            {
                var instanceNamesToApply = instancesToApply?.Select(item => item.Name).ToHashSet();
                var instanceNamesInCurrent = currentInstances?.Select(item => item.Name).ToHashSet();

                foreach (var instance in instancesToApply)
                {
                    var matchingInCurrent = instanceNamesInCurrent.Contains(instance.Name);
                    if (!matchingInCurrent)
                    {
                        toReturn.AddedInstances.Add(instance);
                    }
                }

                foreach (var instance in currentInstances)
                {
                    var matchingInToApply = instanceNamesToApply.Contains(instance.Name);
                    if (!matchingInToApply)
                    {
                        toReturn.RemovedInstances.Add(instance);
                    }
                }
            }
        }

        private void AddVariableModifications(StateSave stateToApply, StateSave currentState, UndoComparison snapshot)
        {
            if(stateToApply == null || currentState == null)
            {
                return;
            }
            var newVariableNameLists = stateToApply.Variables.Select(item => item.Name).ToList();
            newVariableNameLists.AddRange(stateToApply.VariableLists.Select(item => item.Name));
            var newVariableHash = newVariableNameLists.ToHashSet();

            var oldVariableNameList = currentState.Variables.Select(item => item.Name).ToList();
            oldVariableNameList.AddRange(currentState.VariableLists.Select(item => item.Name));
            var oldVariableHash = oldVariableNameList.ToHashSet();

            var addedVariables = stateToApply.Variables.Where(item => oldVariableHash.Contains(item.Name) == false);
            var removedVariables = currentState.Variables.Where(item => newVariableHash.Contains(item.Name) == false);
            var addedVariableLists = stateToApply.VariableLists.Where(item => oldVariableHash.Contains(item.Name) == false);
            var removedVariableLists = currentState.VariableLists.Where(item => newVariableHash.Contains(item.Name) == false);

            snapshot.ModifiedVariables.AddRange(addedVariables);
            snapshot.ModifiedVariableLists.AddRange(addedVariableLists);

            // if any variables were added, then undoing goes back to the default:
            foreach (var variable in removedVariables)
            {
                var clone = variable.Clone();
                clone.Value = null;
                snapshot.ModifiedVariables.Add(clone);
            }
            foreach(var variableList in removedVariableLists)
            {
                var clone = variableList.Clone();
                clone.ValueAsIList = null;
                snapshot.ModifiedVariableLists.Add(clone);

            }

            foreach (var newVariable in stateToApply.Variables)
            {
                var matchingOldVariable = currentState.Variables.FirstOrDefault(otherVariable => otherVariable.Name == newVariable.Name);
                if (matchingOldVariable != null)
                {
                    var areEqual = (newVariable.Value == null && matchingOldVariable.Value == null) ||
                        (newVariable.Value != null && newVariable.Value.Equals(matchingOldVariable.Value));
                    if(!areEqual)
                    {
                        snapshot.ModifiedVariables.Add(newVariable);
                    }
                }
            }

            foreach (var newVariableList in stateToApply.VariableLists)
            {
                var matchingVariableList = currentState.VariableLists.FirstOrDefault(otherVariableList => otherVariableList.Name == newVariableList.Name);
                if (matchingVariableList != null && newVariableList.ValueAsIList != matchingVariableList.ValueAsIList)
                {
                    var areEqual = (newVariableList.ValueAsIList == null && matchingVariableList.ValueAsIList == null);

                    if (!areEqual)
                    {
                        if(newVariableList.ValueAsIList != null && matchingVariableList.ValueAsIList != null)
                        {
                            var thisList = newVariableList.ValueAsIList;
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
                        snapshot.ModifiedVariableLists.Add(newVariableList);
                    }
                }
            }
        }
    }
}
