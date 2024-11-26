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

            if (AddedInstances.Count != 0)
            {
                toReturn += $"Added instances: {string.Join(", ", AddedInstances.Select(item => item.Name))}\n";
            }
            if (RemovedInstances.Count != 0)
            {
                toReturn += $"Removed instances: {string.Join(", ", RemovedInstances.Select(item => item.Name))}\n";
            }
            if (AddedStates.Count != 0)
            {
                toReturn += $"Added states: {string.Join(", ", AddedStates.Select(item => item.Name))}\n";
            }
            if (RemovedStates.Count != 0)
            {
                toReturn += $"Removed states: {string.Join(", ", RemovedStates.Select(item => item.Name))}\n";
            }
            if (ModifiedVariables.Count != 0)
            {
                toReturn += $"Modified variables: {string.Join(", ", ModifiedVariables.Select(item => item.Name))}\n";
            }
            if (ModifiedVariableLists.Count != 0)
            {
                toReturn += $"Modified variable lists: {string.Join(", ", ModifiedVariableLists.Select(item => item.Name))}\n";
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
            var toReturn = new UndoComparison();

            // check for instances that were added or removed
            var thisInstances = Element.Instances;
            var otherInstances = other.Element.Instances;
            toReturn.AddedInstances = thisInstances.Except(otherInstances).ToList();
            toReturn.RemovedInstances = otherInstances.Except(thisInstances).ToList();

            // Check for added or removed categories
            var thisCategories = Element.Categories;
            var otherCategories = other.Element.Categories;
            var addedCategories = thisCategories.Except(otherCategories).ToList();
            var removedCategories = otherCategories.Except(thisCategories).ToList();

            toReturn.AddedInstances = new List<InstanceSave>();
            toReturn.RemovedInstances = new List<InstanceSave>();
            toReturn.ModifiedVariables = new List<VariableSave>();
            toReturn.ModifiedVariableLists = new List<VariableListSave>();

            AddVariableModifications(this.Element.DefaultState, other.Element.DefaultState, toReturn);
            
            // loop through each category, check for states that were added or removed:
            foreach (var category in Element.Categories)
            {
                var matchingCategory = other.Element.Categories.FirstOrDefault(otherCategory => otherCategory.Name == category.Name);

                if (matchingCategory != null)
                {
                    toReturn.AddedStates.AddRange(category.States.Except(matchingCategory.States).ToList());
                    toReturn.RemovedStates.AddRange(matchingCategory.States.Except(category.States).ToList());

                    foreach(var state in category.States)
                    {
                        var matchingState = matchingCategory.States.FirstOrDefault(otherState => otherState.Name == state.Name);
                        if (matchingState != null)
                        {
                            AddVariableModifications(state, matchingState, toReturn);
                        }
                    }
                }
            }

            return toReturn;
        }

        private void AddVariableModifications(StateSave state1, StateSave state2, UndoComparison snapshot)
        {
            var addedVariables = state1.Variables.Except(state2.Variables).ToList();
            var removedVariables = state2.Variables.Except(state1.Variables).ToList();
            var addedVariableLists = state1.VariableLists.Except(state2.VariableLists).ToList();
            var removedVariableLists = state2.VariableLists.Except(state1.VariableLists).ToList();

            snapshot.ModifiedVariables.AddRange(addedVariables);
            snapshot.ModifiedVariableLists.AddRange(addedVariableLists);

            foreach (var removedVariable in removedVariables)
            {
                var variable = removedVariable.Clone();
                variable.Value = null;
                snapshot.ModifiedVariables.Add(removedVariable);
            }

            foreach (var removedVariableList in removedVariableLists)
            {
                var variableList = removedVariableList.Clone();
                variableList.ValueAsIList = null;
                snapshot.ModifiedVariableLists.Add(removedVariableList);
            }

            foreach (var variable in state1.Variables)
            {
                var matchingVariable = state2.Variables.FirstOrDefault(otherVariable => otherVariable.Name == variable.Name);
                if (matchingVariable != null && variable.Value != matchingVariable.Value)
                {
                    snapshot.ModifiedVariables.Add(variable);
                }
            }

            foreach (var variableList in state1.VariableLists)
            {
                var matchingVariableList = state2.VariableLists.FirstOrDefault(otherVariableList => otherVariableList.Name == variableList.Name);
                if (matchingVariableList != null && variableList.ValueAsIList != matchingVariableList.ValueAsIList)
                {
                    snapshot.ModifiedVariableLists.Add(variableList);
                }
            }
        }
    }
}
