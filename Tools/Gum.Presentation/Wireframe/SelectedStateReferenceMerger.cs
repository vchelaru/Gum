using Gum.DataTypes;
using Gum.DataTypes.Variables;
using GumRuntime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Wireframe;

/// <summary>
/// Builds the state the wireframe evaluates variable references against when a state other than the
/// default is selected (#6851). The selected state's values sit on top of the default state's, and
/// the selected state wins for every variable it sets: a default-state reference row whose left side
/// the selected state sets (by value or by its own reference row) is dropped, so it cannot re-apply
/// the default's value over the state the user clicked. Default rows that read variables the selected
/// state sets, but write something else, are kept so they still follow the selected state.
/// </summary>
public class SelectedStateReferenceMerger
{
    private const string ReferencesRootName = "VariableReferences";

    public StateSave Merge(StateSave defaultState, StateSave selectedState)
    {
        StateSave merged = defaultState.Clone();
        merged.MergeIntoThis(selectedState);

        HashSet<string> setBySelected = GetVariablesSetBy(selectedState);
        AddInnerVariablesOfExposedNames(defaultState, setBySelected);

        for (int i = merged.VariableLists.Count - 1; i >= 0; i--)
        {
            VariableListSave list = merged.VariableLists[i];
            if (list is not VariableListSave<string> referenceList || list.GetRootName() != ReferencesRootName)
            {
                continue;
            }

            ElementSave? channelOwner = ElementSaveExtensions.ResolveChannelOwner(merged, list.SourceObject);
            List<string> kept = new List<string>();
            foreach (string line in referenceList.Value)
            {
                foreach (string expanded in ElementSaveExtensions.ExpandCompositeReferenceLine(line, channelOwner))
                {
                    if (!TryGetQualifiedLeft(expanded, list.SourceObject, out string? qualifiedLeft) ||
                        !setBySelected.Contains(qualifiedLeft!))
                    {
                        kept.Add(expanded);
                    }
                }
            }

            referenceList.Value = kept;
            if (kept.Count == 0)
            {
                merged.VariableLists.RemoveAt(i);
            }
        }

        foreach (VariableListSave selectedList in selectedState.VariableLists)
        {
            if (selectedList is not VariableListSave<string> selectedReferences ||
                selectedList.GetRootName() != ReferencesRootName ||
                selectedReferences.Value.Count == 0)
            {
                continue;
            }

            VariableListSave<string>? existing = merged.VariableLists
                .OfType<VariableListSave<string>>()
                .FirstOrDefault(item => item.Name == selectedList.Name);
            if (existing == null)
            {
                merged.VariableLists.Add((VariableListSave)selectedList.Clone());
            }
            else
            {
                existing.Value.AddRange(selectedReferences.Value);
            }
        }

        return merged;
    }

    /// <summary>
    /// A state that sets an exposed name (<c>BackgroundWidth</c>) sets the inner variable it exposes
    /// (<c>Background.Width</c>), which is the name a reference row writes.
    /// </summary>
    private static void AddInnerVariablesOfExposedNames(StateSave defaultState, HashSet<string> setBySelected)
    {
        foreach (VariableSave variable in defaultState.Variables)
        {
            if (!string.IsNullOrEmpty(variable.ExposedAsName) && setBySelected.Contains(variable.ExposedAsName!))
            {
                setBySelected.Add(variable.Name);
            }
        }
    }

    private static HashSet<string> GetVariablesSetBy(StateSave state)
    {
        HashSet<string> names = new HashSet<string>();

        foreach (VariableSave variable in state.Variables)
        {
            if (variable.SetsValue)
            {
                names.Add(variable.Name);
            }
        }

        foreach (VariableListSave list in state.VariableLists)
        {
            if (list is not VariableListSave<string> referenceList || list.GetRootName() != ReferencesRootName)
            {
                continue;
            }

            ElementSave? channelOwner = ElementSaveExtensions.ResolveChannelOwner(state, list.SourceObject);
            foreach (string line in referenceList.Value)
            {
                foreach (string expanded in ElementSaveExtensions.ExpandCompositeReferenceLine(line, channelOwner))
                {
                    if (TryGetQualifiedLeft(expanded, list.SourceObject, out string? qualifiedLeft))
                    {
                        names.Add(qualifiedLeft!);
                    }
                }
            }
        }

        return names;
    }

    private static bool TryGetQualifiedLeft(string line, string? sourceObject, out string? qualifiedLeft)
    {
        qualifiedLeft = null;
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
        {
            return false;
        }

        string[] split = line.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length != 2)
        {
            return false;
        }

        string left = split[0].Trim();
        if (left.Length == 0)
        {
            return false;
        }

        qualifiedLeft = string.IsNullOrEmpty(sourceObject) ? left : $"{sourceObject}.{left}";
        return true;
    }
}
