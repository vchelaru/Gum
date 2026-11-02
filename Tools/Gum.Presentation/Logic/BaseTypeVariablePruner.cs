using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Services;
using GumRuntime;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Logic;

/// <summary>
/// Removes the variables a new base type cannot have, after the base type of an element or an instance changed.
/// </summary>
public interface IBaseTypeVariablePruner
{
    /// <summary>
    /// Removes from every state of <paramref name="element"/> the variables, variable lists and
    /// <c>VariableReferences</c> lines on the element itself that its new base type does not define.
    /// </summary>
    void PruneElement(ElementSave element);

    /// <summary>
    /// Removes from every state of <paramref name="container"/> the variables, variable lists and
    /// <c>VariableReferences</c> lines on <paramref name="instance"/> that the instance's new type does not define.
    /// An exposed variable goes the way an un-exposed one does: values other elements set for the exposed name are
    /// removed and recorded for undo, so the caller must hold an undo lock.
    /// </summary>
    void PruneInstance(ElementSave container, InstanceSave instance);
}

/// <inheritdoc/>
public class BaseTypeVariablePruner : IBaseTypeVariablePruner
{
    // Properties of the object itself, not of its type. The tool does not normally store them as variables, and
    // pruning one would reparent or rename the object.
    private static readonly HashSet<string> ObjectVariableNames = new HashSet<string>
    {
        "Name", "BaseType", "Locked", "IsSlot", "Parent"
    };

    private readonly IVariableDefinedByTypeLogic _variableDefinedByType;
    private readonly IRenameLogic _renameLogic;
    private readonly IInstanceOverrideRemover _instanceOverrideRemover;

    public BaseTypeVariablePruner(IVariableDefinedByTypeLogic variableDefinedByType, IRenameLogic renameLogic,
        IInstanceOverrideRemover instanceOverrideRemover)
    {
        _variableDefinedByType = variableDefinedByType;
        _renameLogic = renameLogic;
        _instanceOverrideRemover = instanceOverrideRemover;
    }

    /// <inheritdoc/>
    /// <remarks>Deliberately does not touch elements that inherit from <paramref name="element"/>: their own values
    /// stay as hidden leftovers and no check reports them (#7191). Pinned by BaseTypeChangePrunesVariablesTests.</remarks>
    public void PruneElement(ElementSave element)
    {
        foreach (StateSave state in element.AllStates)
        {
            state.Variables.RemoveAll(variable =>
                string.IsNullOrEmpty(variable.SourceObject) && IsUndefinedByType(variable, variable.Name, element));
            PruneVariableLists(state, instanceName: null, typeElement: element);
        }
    }

    /// <inheritdoc/>
    public void PruneInstance(ElementSave container, InstanceSave instance)
    {
        // A type the project does not have says nothing about what the instance can hold.
        if (ObjectFinder.Self.GetElementSave(instance.BaseType) is not { } typeElement)
        {
            return;
        }

        List<(string FullName, string ExposedName)> prunedExposures = new List<(string, string)>();
        foreach (StateSave state in container.AllStates)
        {
            foreach (VariableSave variable in state.Variables.ToList())
            {
                if (variable.SourceObject == instance.Name && IsUndefinedByType(variable, variable.GetRootName(), typeElement))
                {
                    state.Variables.Remove(variable);
                    if (!string.IsNullOrEmpty(variable.ExposedAsName)
                        && !prunedExposures.Contains((variable.Name, variable.ExposedAsName!)))
                    {
                        prunedExposures.Add((variable.Name, variable.ExposedAsName!));
                    }
                }
            }
            PruneVariableLists(state, instance.Name, typeElement);
        }

        // The same cleanup as un-exposing the variable and clearing the values others set for it.
        foreach ((string fullName, string exposedName) in prunedExposures)
        {
            VariableChangeResponse references = _renameLogic.GetChangesForRenamedVariable(container, fullName, exposedName);
            _instanceOverrideRemover.RemoveAndRecordForUndo(references.VariableChanges.Where(change => change.IsRemovableValue).ToList());
        }
    }

    private bool IsUndefinedByType(VariableSave variable, string rootName, ElementSave typeElement)
    {
        if (variable.IsCustomVariable || ObjectVariableNames.Contains(rootName) || !string.IsNullOrEmpty(variable.ShaderParameter))
        {
            return false;
        }
        return !_variableDefinedByType.GetIfVariableIsDefinedByType(rootName, typeElement);
    }

    private void PruneVariableLists(StateSave state, string? instanceName, ElementSave typeElement)
    {
        foreach (VariableListSave list in state.VariableLists.ToList())
        {
            if (list.SourceObject != instanceName)
            {
                continue;
            }

            string rootName = list.GetRootName();
            if (rootName == "VariableReferences")
            {
                if (RemoveLinesTheTypeCannotHave(list, typeElement) && list.ValueAsIList.Count == 0)
                {
                    state.VariableLists.Remove(list);
                }
            }
            else if (!ObjectVariableNames.Contains(rootName) && !_variableDefinedByType.GetIfVariableListIsDefinedByType(rootName, typeElement))
            {
                state.VariableLists.Remove(list);
            }
        }
    }

    /// <returns>Whether any line was removed.</returns>
    private bool RemoveLinesTheTypeCannotHave(VariableListSave list, ElementSave typeElement)
    {
        bool removedAny = false;
        System.Collections.IList lines = list.ValueAsIList;
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            // A comment or a line without an assignment names no variable.
            if (lines[i] is string line && !line.TrimStart().StartsWith("//") && IsUndefinedLeftSide(line, typeElement))
            {
                lines.RemoveAt(i);
                removedAny = true;
            }
        }
        return removedAny;
    }

    // A composite line ("Color = Other.FillColor") stands for one line per channel, and each channel is checked.
    private bool IsUndefinedLeftSide(string line, ElementSave typeElement)
    {
        foreach (string expanded in ElementSaveExtensions.ExpandCompositeReferenceLine(line, typeElement))
        {
            int equalsIndex = expanded.IndexOf('=');
            if (equalsIndex <= 0)
            {
                continue;
            }
            string leftSide = expanded.Substring(0, equalsIndex).Trim();
            if (!_variableDefinedByType.GetIfVariableIsDefinedByType(leftSide, typeElement))
            {
                return true;
            }
        }
        return false;
    }
}
