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
/// state sets, but write something else, are kept so they still follow the selected state. The caller can
/// keep a dropped row whose source changed after the state was applied, as a running game re-drives it (#6861).
/// </summary>
public class SelectedStateReferenceMerger
{
    private const string ReferencesRootName = "VariableReferences";

    public StateSave Merge(StateSave defaultState, StateSave selectedState) =>
        Merge(defaultState, selectedState, null, null);

    /// <summary>
    /// Merges like <see cref="Merge(StateSave, StateSave)"/>, except that a default row the selected state
    /// overrides stays when <paramref name="keepOverriddenRow"/> returns true for its <see cref="GetRowKey"/>.
    /// The key of every overridden row, kept or not, is added to <paramref name="overriddenRowKeys"/>.
    /// </summary>
    public StateSave Merge(StateSave defaultState, StateSave selectedState, Func<string, bool>? keepOverriddenRow,
        ICollection<string>? overriddenRowKeys)
    {
        StateSave merged = defaultState.Clone();
        merged.MergeIntoThis(selectedState);

        HashSet<string> setBySelected = GetVariablesSetBy(selectedState);
        AddInnerVariablesOfExposedNames(defaultState, setBySelected);

        FilterReferenceRows(merged, (qualifiedLeft, expanded) =>
        {
            if (qualifiedLeft == null || !setBySelected.Contains(qualifiedLeft))
            {
                return true;
            }

            string key = GetRowKey(qualifiedLeft, expanded);
            overriddenRowKeys?.Add(key);
            return keepOverriddenRow != null && keepOverriddenRow(key);
        });

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
    /// Removes from <paramref name="state"/> everything that sets one of <paramref name="names"/>: its plain
    /// variables and its reference rows (a composite row such as <c>Color</c> keeps the channels that are not named).
    /// </summary>
    internal static void RemoveSettersOf(StateSave state, ICollection<string> names)
    {
        state.Variables.RemoveAll(variable => names.Contains(variable.Name));
        FilterReferenceRows(state, (qualifiedLeft, _) => qualifiedLeft == null || !names.Contains(qualifiedLeft));
    }

    /// <summary>
    /// Rewrites each reference list of <paramref name="state"/> as its channel-expanded lines, keeping the ones
    /// <paramref name="keepRow"/> accepts (given the qualified variable the line writes, or null when it writes
    /// nothing, such as a comment) and dropping lists left empty.
    /// </summary>
    private static void FilterReferenceRows(StateSave state, Func<string?, string, bool> keepRow)
    {
        for (int i = state.VariableLists.Count - 1; i >= 0; i--)
        {
            VariableListSave list = state.VariableLists[i];
            if (list is not VariableListSave<string> referenceList || list.GetRootName() != ReferencesRootName)
            {
                continue;
            }

            ElementSave? channelOwner = ElementSaveExtensions.ResolveChannelOwner(state, list.SourceObject);
            List<string> kept = new List<string>();
            foreach (string line in referenceList.Value)
            {
                foreach (string expanded in ElementSaveExtensions.ExpandCompositeReferenceLine(line, channelOwner))
                {
                    TryGetQualifiedLeft(expanded, list.SourceObject, out string? qualifiedLeft);
                    if (keepRow(qualifiedLeft, expanded))
                    {
                        kept.Add(expanded);
                    }
                }
            }

            referenceList.Value = kept;
            if (kept.Count == 0)
            {
                state.VariableLists.RemoveAt(i);
            }
        }
    }

    /// <summary>Identifies a reference row by the variable it writes and its (channel-expanded) line.</summary>
    internal static string GetRowKey(string writtenName, string line) => writtenName + "=" + line;

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

    internal static HashSet<string> GetVariablesSetBy(StateSave state)
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
