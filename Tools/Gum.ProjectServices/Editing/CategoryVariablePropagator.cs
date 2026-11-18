using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// A variable or variable list that propagation added to a state.
/// </summary>
/// <param name="State">The state the variable was added to.</param>
/// <param name="Name">The variable's name.</param>
public record PropagatedVariable(StateSave State, string Name);

/// <summary>
/// Keeps a category's states consistent: a variable any state in a category sets is set in all of them, so
/// interpolating between the states always has a value to start from. The tool runs it when a categorized
/// variable is set or a state is added; <c>gumcli edit</c> does the same.
/// </summary>
public interface ICategoryVariablePropagator
{
    /// <summary>
    /// Adds <paramref name="memberName"/> to every state in <paramref name="states"/> that lacks it, with the value
    /// the default state (or the type) gives it, and marks an existing unset entry as setting its value.
    /// </summary>
    /// <returns>The variables that were added.</returns>
    IReadOnlyList<PropagatedVariable> PropagateVariablesInCategory(string memberName, ElementSave element, IList<StateSave> states);

    /// <summary>
    /// Propagates every variable and variable list that any state in the category sets to all the category's
    /// states, so a state that was just added or moved in is consistent with its siblings.
    /// </summary>
    /// <returns>The variables that were added.</returns>
    IReadOnlyList<PropagatedVariable> PropagateAllVariablesInCategory(ElementSave element, StateSaveCategory category);
}

/// <inheritdoc/>
public class CategoryVariablePropagator : ICategoryVariablePropagator
{
    /// <inheritdoc/>
    public IReadOnlyList<PropagatedVariable> PropagateAllVariablesInCategory(ElementSave element, StateSaveCategory category)
    {
        // Tools and hand-edited files leave variables set in only some states, so take the union
        // of every state's names, not just the first state's.
        HashSet<string> names = new HashSet<string>();
        foreach (StateSave state in category.States)
        {
            foreach (VariableSave variable in state.Variables)
            {
                names.Add(variable.Name);
            }
            foreach (VariableListSave variableList in state.VariableLists)
            {
                names.Add(variableList.Name);
            }
        }

        List<PropagatedVariable> added = new List<PropagatedVariable>();
        foreach (string name in names)
        {
            added.AddRange(PropagateVariablesInCategory(name, element, category.States));
        }
        return added;
    }

    /// <inheritdoc/>
    public IReadOnlyList<PropagatedVariable> PropagateVariablesInCategory(string memberName, ElementSave element, IList<StateSave> states)
    {
        List<PropagatedVariable> added = new List<PropagatedVariable>();

        var defaultState = element.GetDefaultStateOrThrow();
        var defaultVariable = defaultState.GetVariableSave(memberName);
        if (defaultVariable == null)
        {
            defaultVariable = defaultState.GetVariableRecursive(memberName);
        }
        if (defaultVariable == null)
        {
            defaultVariable = ObjectFinder.Self.GetRootVariable(memberName, element);
        }
        var defaultVariableList = defaultState.GetVariableListSave(memberName);
        if (defaultVariableList == null && defaultVariable == null)
        {
            defaultVariableList = defaultState.GetVariableListRecursive(memberName);
        }

        // If the variable is a categorized state, the default may be null. If so, the first state is the
        // default so that values are always set.
        if (defaultVariable != null && defaultVariable.Value == null)
        {
            ElementSave? variableContainer = element;

            var sourceObjectName = VariableSave.GetSourceObject(memberName);
            if (!string.IsNullOrEmpty(sourceObjectName))
            {
                var instance = variableContainer.GetInstance(sourceObjectName);
                if (instance != null)
                {
                    variableContainer = ObjectFinder.Self.GetElementSave(instance);
                }
            }

            StateSaveCategory? category = null;
            var isState = false;
            if (variableContainer != null)
            {
                isState = defaultVariable.IsState(variableContainer, out _, out category);
            }

            if (isState)
            {
                // A value is assigned below, so clone rather than modify the original.
                defaultVariable = defaultVariable.Clone();
                if (category != null)
                {
                    defaultVariable.Value = category.States.FirstOrDefault()?.Name;
                }
                // isState is only set when there is a container.
                else if (variableContainer != null)
                {
                    defaultVariable.Value = variableContainer.DefaultState?.Name;
                }
            }
        }

        var defaultValue = defaultVariable?.Value ?? defaultVariableList?.ValueAsIList;
        if (defaultValue == null)
        {
            defaultValue = defaultState.GetValueRecursive(memberName);
        }

        // A custom variable has no default value until one is typed in, but every state in the category
        // must hold an explicit value, or interpolating between its states has nothing to start from.
        if (defaultValue == null && defaultVariable != null)
        {
            defaultValue = TypeZeroValue.For(defaultVariable.Type);
        }

        foreach (var state in states)
        {
            if (defaultVariable != null)
            {
                var existingVariable = state.GetVariableSave(memberName);
                if (existingVariable == null)
                {
                    VariableSave newVariable = defaultVariable.Clone();
                    newVariable.Value = defaultValue;
                    newVariable.SetsValue = true;
                    newVariable.Name = memberName;
                    state.Variables.Add(newVariable);
                    added.Add(new PropagatedVariable(state, memberName));
                }
                else if (existingVariable.SetsValue == false)
                {
                    existingVariable.SetsValue = true;
                }
            }
            else if (defaultVariableList != null)
            {
                if (state.GetVariableListSave(memberName) == null)
                {
                    var newVariableList = defaultVariableList.Clone();
                    newVariableList.Name = memberName;
                    state.VariableLists.Add(newVariableList);
                    added.Add(new PropagatedVariable(state, memberName));
                }
            }
        }

        return added;
    }
}
