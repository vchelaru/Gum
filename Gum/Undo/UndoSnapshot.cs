using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Gum.Undo
{
    public class UndoComparison
    {
        public string? ChangedName;

        public List<StateSaveCategory> AddedCategories;
        public List<StateSaveCategory> RemovedCategories;

        public List<InstanceSave> AddedInstances;
        public List<InstanceSave> RemovedInstances;

        public List<StateSave> AddedStates;
        public List<StateSave> RemovedStates;

        public List<StateSave> ModifiedStates;

        public override string ToString()
        {
            string toReturn = "";
            const string newlinePrefix = "\n    ";

            if(ChangedName != null)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += newlinePrefix;
                toReturn += $"Change name to {ChangedName}";
            }

            if (AddedInstances?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += newlinePrefix;
                toReturn += $"Add instances: {string.Join(", ", AddedInstances.Select(item => item.Name))}";
            }
            if (RemovedInstances?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += newlinePrefix;
                toReturn += $"Remove instances: {string.Join(", ", RemovedInstances.Select(item => item.Name))}";
            }

            if(AddedCategories?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += newlinePrefix;
                toReturn += $"Add categories: {string.Join(", ", AddedCategories.Select(item => item.Name))}";
            }
            if (RemovedCategories?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += newlinePrefix;
                toReturn += $"Remove categories: {string.Join(", ", RemovedCategories.Select(item => item.Name))}";
            }

            if (AddedStates?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += newlinePrefix;
                toReturn += $"Add states: {string.Join(", ", AddedStates.Select(item => item.Name))}";
            }
            if (RemovedStates?.Count > 0)
            {
                if (!string.IsNullOrEmpty(toReturn)) toReturn += newlinePrefix;
                toReturn += $"Remove states: {string.Join(", ", RemovedStates.Select(item => item.Name))}";
            }
            foreach(var modifiedState in ModifiedStates)
            {
                if (modifiedState.Variables?.Count > 0)
                {
                    if (!string.IsNullOrEmpty(toReturn)) toReturn += newlinePrefix;
                    toReturn += $"Variables in {modifiedState.Name}: {string.Join(", ", modifiedState.Variables.Select(item => item.Name + "=" + item.Value?.ToString() ?? "<null>"))}";
                }
                if(modifiedState.VariableLists?.Count > 0)
                {
                    if (!string.IsNullOrEmpty(toReturn)) toReturn += newlinePrefix;
                    toReturn += $"Variable lists in {modifiedState.Name}: {string.Join(", ", modifiedState.VariableLists.Select(item => item.Name + "=" + item.ValueAsIList?.ToString() ?? "<null>"))}";
                }

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

        /// <summary>
        /// Creates an undo comparison object which includes information about what the undo will perform if applied.
        /// </summary>
        /// <param name="currentElement">The current state before the undo is applied. This could be a snapshot or it could be the actual element if 
        /// there is only 1 undo.</param>
        /// <param name="snapshotToApply">The snapshot to apply.</param>
        public static UndoComparison CompareAgainst(ElementSave currentElement, ElementSave snapshotToApply)
        {
            var toReturn = new UndoComparison();

            AddElementRename(currentElement, snapshotToApply, toReturn);

            AddInstanceModifiations(currentElement, snapshotToApply, toReturn);

            AddCategoryModifications(currentElement, snapshotToApply, toReturn);

            toReturn.ModifiedStates = new List<StateSave>();

            {
                var newState = snapshotToApply.DefaultState;
                var oldState = currentElement.DefaultState;
                AddVariableModifications(newState, oldState, toReturn);
            }


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

                        foreach (var oldState in currentCategory.States)
                        {
                            var newState = categoryToApply.States.FirstOrDefault(otherState => otherState.Name == oldState.Name);
                            if (newState != null)
                            {
                                AddVariableModifications(newState, oldState, toReturn);
                            }
                        }
                    }
                }
            }

            return toReturn;
        }

        private static void AddCategoryModifications(ElementSave currentElement, ElementSave snapshotToApply, UndoComparison undoComparison)
        {
            // Check for added or removed categories
            var currentCategories = currentElement.Categories;
            var categoriesToApply = snapshotToApply.Categories;

            undoComparison.AddedCategories = null;
            undoComparison.RemovedCategories = null;

            if(currentCategories != null && categoriesToApply != null)
            {
                var currentCategoryNames = currentCategories.Select(item => item.Name).ToArray();
                var toApplyCategoryNames = categoriesToApply.Select(item => item.Name).ToArray();

                undoComparison.AddedCategories = categoriesToApply.Where(item => !currentCategoryNames.Contains(item.Name)).ToList();

                undoComparison.RemovedCategories = currentCategories.Where(item => !toApplyCategoryNames.Contains(item.Name)).ToList();
            }
        }

        private static void AddElementRename(ElementSave currentElement, ElementSave snapshotToApply, UndoComparison toReturn)
        {
            if(currentElement.Name != snapshotToApply.Name)
            {
                toReturn.ChangedName = snapshotToApply.Name;
            }
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

        private static void AddVariableModifications(StateSave stateToApply, StateSave currentState, UndoComparison snapshot)
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

            var modifiedState = new StateSave();
            modifiedState.Name = stateToApply.Name ?? "<default>";
            snapshot.ModifiedStates.Add(modifiedState);

            modifiedState.Variables.AddRange(addedVariables);
            modifiedState.VariableLists.AddRange(addedVariableLists);

            // if any variables were added, then undoing goes back to the default:
            foreach (var variable in removedVariables)
            {
                var clone = variable.Clone();
                clone.Value = null;
                modifiedState.Variables.Add(clone);
            }
            foreach(var variableList in removedVariableLists)
            {
                var clone = variableList.Clone();
                clone.ValueAsIList = null;
                modifiedState.VariableLists.Add(clone);

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
                        modifiedState.Variables.Add(newVariable);
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
                        modifiedState.VariableLists.Add(newVariableList);
                    }
                }
            }
        }
    }
}
